using FastighetsFix.Domain.Enums;

namespace FastighetsFix.Domain.Entities;

public class MaintenanceRequest
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int ApartmentId { get; set; }

    public Apartment Apartment { get; set; } = null!;

    public string CreatedById { get; set; } = string.Empty;

    public string? AssignedTechnicianId { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.New;

    public RequestPriority Priority { get; set; } = RequestPriority.Normal;

    public string? Resolution { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? ClosedAt { get; set; }
    public ICollection<Comment> Comments { get; set; }
        = new List<Comment>();

    public ICollection<StatusHistory> StatusHistoryEntries { get; set; }
        = new List<StatusHistory>();
}