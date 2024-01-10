public class DailyMenuModel
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string Description { get; set; }
    public double PriceForUPT { get; set; }
    public double PriceOutsidersUPT { get; set; }
    public byte[]? Picture { get; set; }

    public int Portions { get; set; }

}