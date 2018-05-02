using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RenewalWebsite.Models;

namespace RenewalWebsite.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public static int MessageMaxLength;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
            builder.Entity<InvoiceHistory>().Property(propertyExpression: p => p.ExchangeRate).HasColumnType("decimal(18,3)");
        }

        public DbSet<Donation> Donations { get; set; }
        public DbSet<EventLog> EventLog { get; set; }
        public DbSet<UnsubscribeUsers> UnsubscribeUsers { get; set; }
        public DbSet<InvoiceHistory> InvoiceHistory { get; set; }
        public DbSet<Country> Country { get; set; }
    }
}
