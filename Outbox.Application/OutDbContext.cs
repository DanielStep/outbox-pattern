using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;

namespace CAP.Application
{
    public class OutboxDbContext : DbContext
    {
        public const string ConnectionString = "Server=(local);Database=outbox-db;Integrated Security=true";

        public DbSet<BusinessRecord> BusinessRecords { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }


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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("OutboxMessages");
                entity.HasKey(e => e.Id).IsClustered(false);

                entity.Property(e => e.Id).UseIdentityColumn();
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.ChannelType).IsRequired();
                entity.Property(e => e.ChannelName).IsRequired();
                entity.Property(e => e.MessageId).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasConversion<string>();
                entity.Property(e => e.Payload).IsRequired();
                entity.Property(e => e.OccurredOn).IsRequired();
            });
        }
    }

    public class BusinessRecord
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class OutboxMessage
    {
        public long Id { get; set; }
        public Guid MessageId { get; set; }
        public string Name { get; set; }
        public ChannelType ChannelType { get; set; }
        public string ChannelName { get; set; }
        public string Payload { get; set; }
        public OutboxMessageStatus Status { get; set; }
        public DateTime OccurredOn { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public enum ChannelType
    {
        Queue = 1,
        Topic = 2
    }

    public enum OutboxMessageStatus
    {
        Pending = 1,
        Dispatched = 2,
        Processing = 3
    }
}
