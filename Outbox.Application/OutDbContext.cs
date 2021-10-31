using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;

namespace CAP.Application
{
    public class OutboxDbContext : DbContext
    {
        public const string ConnectionString = "Server=(local);Database=outbox-db;Integrated Security=true";

        public DbSet<BusinessRecord> BusinessRecords { get; set; }


        public OutboxDbContext()
        {
        }

        public OutboxDbContext(DbContextOptions<OutboxDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        }
    }

    public class BusinessRecord
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
