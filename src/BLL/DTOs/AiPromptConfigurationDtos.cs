namespace BLL.DTOs;

public record AiPromptConfigurationDto(Guid? Id, string Feature, string Name, string PromptText,
    bool Enabled, bool IsUsingDefault, DateTime? UpdatedAt);
public record SaveAiPromptConfigurationDto(string Name, string PromptText, bool Enabled);
