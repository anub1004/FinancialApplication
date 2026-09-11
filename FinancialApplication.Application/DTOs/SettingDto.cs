namespace FinancialApplication.Application.DTOs
{
    public class SettingDto
{
    public string? Email { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "INR";
    public string DefaultFy { get; set; } = "2025-26";
    public string PreferredRegime { get; set; } = "New";
    public string AvatarColor { get; set; } = "#6366f1";
}
}
