using DAL.Models.Commons;

namespace DAL.Models;

public class AiPromptConfiguration : BaseEntity
{
    public string Feature { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PromptText { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}
