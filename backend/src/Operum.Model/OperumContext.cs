using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Operum.Model.Models;

namespace Operum.Model
{
    public class OperumContext(DbContextOptions<OperumContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
    }
}
