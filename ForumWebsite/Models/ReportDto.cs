namespace ForumWebsite.Models
{
    public class ReportDto
    {
        public int Id { get; set; }
        public ReportTargetType TargetType { get; set; }
        public ReportStatus Status { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public object? Reporter { get; set; }
        public object? Target { get; set; }
    }
}
