using FastighetsFix.Domain.Enums;

namespace FastighetsFix.Domain.Entities;

public class StatusHistory
{
    public int Id { get; set; }

    public int RequestId { get; set; }

    public MaintenanceRequest Request { get; set; } = null!;

    public string ChangedById { get; set; } = string.Empty;

    public RequestStatus FromStatus { get; set; }

    public RequestStatus ToStatus { get; set; }

    public DateTimeOffset ChangedAt { get; set; }
        = DateTimeOffset.UtcNow;
}