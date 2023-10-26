using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<LoginModel> LoginUser { get; set; }
    public DbSet<DailyMenuModel> DailyMenu { get; set; }
}