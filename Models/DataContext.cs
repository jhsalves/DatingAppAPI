using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Models
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options): base(options){}

        public DbSet<Value> Values { get; set; }
        
    }
}