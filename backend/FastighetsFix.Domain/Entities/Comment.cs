namespace FastighetsFix.Domain.Entities;

public class Comment
{
    public int Id { get; set; }

    public int RequestId { get; set; }

    public MaintenanceRequest Request { get; set; } = null!;

    public string AuthorId { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;
}