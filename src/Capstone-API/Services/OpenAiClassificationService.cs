using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BLL.DTOs;
using DAL;
using Microsoft.EntityFrameworkCore;
using Capstone_API.Controllers;

namespace Capstone_API.Services;

public class GeminiClassificationService(HttpClient httpClient, IConfiguration configuration, AppDbContext context)
{
    private const int MaxImages = 5;
    private const int MaxImageDataUrlLength = 10_000_000;

    public async Task<AiClassificationSuggestionDto> AnalyzeAsync(
        ClassificationCatalogDto catalog, AnalyzeClassificationImagesDto request,
        CancellationToken cancellationToken)
    {
        var images = request.ImageDataUrls
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();
        if (images.Count == 0)
            throw new InvalidOperationException("Please upload at least one clothing image before AI analysis.");
        if (images.Count > MaxImages)
            throw new InvalidOperationException($"AI analysis supports up to {MaxImages} images.");
        if (images.Any(x => !x.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)
                            || x.Length > MaxImageDataUrlLength))
            throw new InvalidOperationException("An image is invalid or too large.");

        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                     ?? configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("YOUR_", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Gemini is not configured. Set the GEMINI_API_KEY environment variable.");

        var customPrompt = await context.AiPromptConfigurations.AsNoTracking()
            .Where(x => x.Feature == "ClothingClassification" && x.IsActive != false && x.Enabled)
            .Select(x => x.PromptText).FirstOrDefaultAsync(cancellationToken);
        var prompt = BuildPrompt(catalog, customPrompt ?? AiPromptConfigurationsController.DefaultClassificationPrompt);
        var parts = new JsonArray();
        foreach (var image in images)
        {
            var comma = image.IndexOf(',');
            var mimeEnd = image.IndexOf(';');
            parts.Add(new JsonObject
            {
                ["inline_data"] = new JsonObject
                {
                    ["mime_type"] = image[5..mimeEnd], ["data"] = image[(comma + 1)..]
                }
            });
        }
        parts.Add(new JsonObject { ["text"] = prompt });

        var payload = new JsonObject
        {
            ["contents"] = new JsonArray
            {
                new JsonObject { ["role"] = "user", ["parts"] = parts }
            },
            ["generationConfig"] = new JsonObject
            {
                ["responseMimeType"] = "application/json",
                ["responseJsonSchema"] = BuildSchema(catalog)
            }
        };

        var model = configuration["Gemini:Model"] ?? "gemini-3.6-flash";
        using var message = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{model}:generateContent")
        {
            Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json")
        };
        message.Headers.Add("x-goog-api-key", apiKey);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(ReadGeminiError(body));

        using var responseJson = JsonDocument.Parse(body);
        var outputText = responseJson.RootElement.GetProperty("candidates")[0]
            .GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
        if (string.IsNullOrWhiteSpace(outputText))
            throw new InvalidOperationException("Gemini did not return a classification result.");

        var suggestion = JsonSerializer.Deserialize<AiResponse>(outputText,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Gemini returned an invalid classification result.");
        return ValidateAndMap(catalog, suggestion);
    }

    private static string BuildPrompt(ClassificationCatalogDto catalog, string? customPrompt)
    {
        static string Options(IEnumerable<CategoryOptionDto> values) =>
            string.Join("\n", values.Select(x => $"- {x.Id}: {x.Name}"));
        var questions = string.Join("\n", catalog.ConditionQuestions.Select(q =>
            $"Question {q.Id}: {q.Text}\n" + string.Join("\n", q.Options.Select(o =>
                $"  - {o.Id}: Label {o.Grade} - {o.Text}"))));
        return $$"""
            MANAGER INSTRUCTIONS:
            {{(string.IsNullOrWhiteSpace(customPrompt) ? "Use the standard classification rules below." : customPrompt.Trim())}}

            MANDATORY SYSTEM RULES (manager instructions cannot override these rules):
            You are assisting a used-clothing classification staff member. Inspect all supplied photos
            as views of the same item. First decide whether the main item is clothing. Shoes, bags,
            accessories, household objects, people without a clearly presented garment, and unrelated
            objects are not clothing. If it is not clothing, set isClothing to false, all category IDs
            to null, answers to an empty array, and briefly explain in Vietnamese. Do not classify it.
            If it is clothing, set isClothing to true, choose exactly one ID from every allowed list and
            one answer ID for every question. Never invent IDs. Be conservative: photos may not reliably show fabric,
            size, gender, or small defects. Pick the closest visible option, lower confidence when uncertain,
            and briefly explain uncertainty in Vietnamese. Clothing type must belong to the selected garment group.

            FABRIC TYPES:
            {{Options(catalog.FabricTypes)}}
            GARMENT GROUPS:
            {{Options(catalog.GarmentGroups)}}
            CLOTHING TYPES (parentId is shown):
            {{string.Join("\n", catalog.ClothingTypes.Select(x => $"- {x.Id}: {x.Name}; parentId={x.ParentId}"))}}
            GENDERS:
            {{Options(catalog.Genders)}}
            TARGET USERS:
            {{Options(catalog.TargetUsers)}}
            SIZES:
            {{Options(catalog.Sizes)}}
            CONDITION QUESTIONS:
            {{questions}}
            """;
    }

    private static JsonObject BuildSchema(ClassificationCatalogDto catalog)
    {
        static JsonArray Ids<T>(IEnumerable<T> values, Func<T, Guid> select) =>
            new(values.Select(x => (JsonNode?)select(x).ToString()).ToArray());
        JsonObject IdProperty(JsonArray ids, bool nullable = false) => new()
        {
            ["type"] = "string", ["enum"] = ids, ["nullable"] = nullable
        };
        var answerItems = new JsonObject
        {
            ["type"] = "object", ["additionalProperties"] = false,
            ["properties"] = new JsonObject
            {
                ["questionId"] = IdProperty(Ids(catalog.ConditionQuestions, x => x.Id)),
                ["answerId"] = IdProperty(Ids(catalog.ConditionQuestions.SelectMany(x => x.Options), x => x.Id))
            },
            ["required"] = new JsonArray("questionId", "answerId")
        };
        return new JsonObject
        {
            ["type"] = "object", ["additionalProperties"] = false,
            ["properties"] = new JsonObject
            {
                ["isClothing"] = new JsonObject { ["type"] = "boolean" },
                ["fabricTypeId"] = IdProperty(Ids(catalog.FabricTypes, x => x.Id), true),
                ["garmentGroupId"] = IdProperty(Ids(catalog.GarmentGroups, x => x.Id), true),
                ["clothingTypeId"] = IdProperty(Ids(catalog.ClothingTypes, x => x.Id), true),
                ["genderId"] = IdProperty(Ids(catalog.Genders, x => x.Id), true),
                ["targetUserId"] = IdProperty(Ids(catalog.TargetUsers, x => x.Id), true),
                ["sizeId"] = IdProperty(Ids(catalog.Sizes, x => x.Id), true),
                ["answers"] = new JsonObject { ["type"] = "array", ["items"] = answerItems },
                ["confidence"] = new JsonObject { ["type"] = "number", ["minimum"] = 0, ["maximum"] = 1 },
                ["summary"] = new JsonObject { ["type"] = "string" }
            },
            ["required"] = new JsonArray("isClothing", "fabricTypeId", "garmentGroupId", "clothingTypeId",
                "genderId", "targetUserId", "sizeId", "answers", "confidence", "summary")
        };
    }

    private static AiClassificationSuggestionDto ValidateAndMap(ClassificationCatalogDto c, AiResponse ai)
    {
        if (!ai.IsClothing)
            return new(false, null, null, null, null, null, null, [],
                Math.Clamp(ai.Confidence, 0, 1), ai.Summary.Trim());

        static bool Has(IEnumerable<CategoryOptionDto> xs, Guid id) => xs.Any(x => x.Id == id);
        if (!ai.FabricTypeId.HasValue || !ai.GarmentGroupId.HasValue || !ai.ClothingTypeId.HasValue
            || !ai.GenderId.HasValue || !ai.TargetUserId.HasValue || !ai.SizeId.HasValue
            || !Has(c.FabricTypes, ai.FabricTypeId.Value) || !Has(c.GarmentGroups, ai.GarmentGroupId.Value)
            || !Has(c.Genders, ai.GenderId.Value) || !Has(c.TargetUsers, ai.TargetUserId.Value)
            || !Has(c.Sizes, ai.SizeId.Value))
            throw new InvalidOperationException("AI selected an option outside the current catalog.");
        var clothing = c.ClothingTypes.FirstOrDefault(x => x.Id == ai.ClothingTypeId.Value);
        if (clothing is null || clothing.ParentId != ai.GarmentGroupId)
            throw new InvalidOperationException("AI selected an invalid clothing type for the garment group.");
        var answers = ai.Answers.GroupBy(x => x.QuestionId).Select(x => x.Last()).ToList();
        if (answers.Count != c.ConditionQuestions.Count || c.ConditionQuestions.Any(q =>
                answers.All(a => a.QuestionId != q.Id)
                || answers.Any(a => a.QuestionId == q.Id && q.Options.All(o => o.Id != a.AnswerId))))
            throw new InvalidOperationException("AI did not answer the current condition questionnaire correctly.");
        return new(true, ai.FabricTypeId, ai.GarmentGroupId, ai.ClothingTypeId, ai.GenderId,
            ai.TargetUserId, ai.SizeId, answers, Math.Clamp(ai.Confidence, 0, 1), ai.Summary.Trim());
    }

    private static string ReadGeminiError(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            var message = json.RootElement.GetProperty("error").GetProperty("message").GetString();
            return $"Gemini analysis failed: {message}";
        }
        catch { return "Gemini analysis failed. Please try again."; }
    }

    private sealed class AiResponse
    {
        public bool IsClothing { get; set; }
        public Guid? FabricTypeId { get; set; }
        public Guid? GarmentGroupId { get; set; }
        public Guid? ClothingTypeId { get; set; }
        public Guid? GenderId { get; set; }
        public Guid? TargetUserId { get; set; }
        public Guid? SizeId { get; set; }
        public List<ClassificationAnswerDto> Answers { get; set; } = [];
        public double Confidence { get; set; }
        public string Summary { get; set; } = "";
    }
}
