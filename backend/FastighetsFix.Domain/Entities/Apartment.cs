namespace FastighetsFix.Domain.Entities;

public class Apartment
{
    public int Id { get; set; }

    public string ApartmentNumber { get; set; } = string.Empty;

    public int PropertyId { get; set; }

    public Property Property { get; set; } = null!;
    
    
    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; }
        = new List<MaintenanceRequest>();
}