namespace Auction.DAL.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<Lot> Lots { get; set; } = new List<Lot>();
}
