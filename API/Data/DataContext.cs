using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions options) : base(options) //Potrebno dodati - lahko z show fixes
        {
        }

        public DbSet<AppUser> Users {get; set;}

    }
}