using System.Reflection.Metadata;

public class DailyMenuModel
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal PriceForUPT { get; set; }
    public decimal PriceOutsidersUPT { get; set; }
    public byte[]? Picture { get; set; }

}