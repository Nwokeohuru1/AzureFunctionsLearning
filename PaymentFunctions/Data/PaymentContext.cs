using Microsoft.EntityFrameworkCore;
using PaymentFunctions.Models;

namespace PaymentFunctions.Data
{
    public class PaymentContext : DbContext
    {
        public PaymentContext(DbContextOptions<PaymentContext> options) : base(options) { }
        public DbSet<Transfer> Transfers => Set<Transfer>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Transfer>()
                .Property(x => x.Amount)
                .HasPrecision(18, 6);
        }
    }
}
