using Microsoft.EntityFrameworkCore;
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
    }

    public class BusinessRecord
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
