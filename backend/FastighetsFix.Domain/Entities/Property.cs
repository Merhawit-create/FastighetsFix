namespace FastighetsFix.Domain.Entities;

public class Property
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public ICollection<Apartment> Apartments { get; set; }
        = new List<Apartment>();
}