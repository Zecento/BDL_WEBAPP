using Microsoft.EntityFrameworkCore;

namespace BDL_WEBAPP.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
        
    }
    
    // These are the tables that will be created in the database
    public DbSet<User> Users => Set<User>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
}