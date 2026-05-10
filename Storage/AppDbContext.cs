using MessengerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MessengerApi.Storage;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<DeliveryRecord> DeliveryRecords => Set<DeliveryRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite PK for ConversationMember
        modelBuilder.Entity<ConversationMember>()
            .HasKey(cm => new { cm.ConversationId, cm.UserId });

        modelBuilder.Entity<ConversationMember>()
            .HasOne(cm => cm.Conversation)
            .WithMany(c => c.Members)
            .HasForeignKey(cm => cm.ConversationId);

        modelBuilder.Entity<ConversationMember>()
            .HasOne(cm => cm.User)
            .WithMany(u => u.ConversationMemberships)
            .HasForeignKey(cm => cm.UserId);

        // Message → Conversation
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId);

        // Message → Sender (User)
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // DeliveryRecord → Message
        modelBuilder.Entity<DeliveryRecord>()
            .HasOne(dr => dr.Message)
            .WithMany(m => m.DeliveryRecords)
            .HasForeignKey(dr => dr.MessageId);

        // DeliveryRecord → Recipient (User)
        modelBuilder.Entity<DeliveryRecord>()
            .HasOne(dr => dr.Recipient)
            .WithMany(u => u.DeliveryRecords)
            .HasForeignKey(dr => dr.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Enum → string storage for readability
        modelBuilder.Entity<Conversation>()
            .Property(c => c.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Message>()
            .Property(m => m.Status)
            .HasConversion<string>();

        modelBuilder.Entity<DeliveryRecord>()
            .Property(dr => dr.Status)
            .HasConversion<string>();
    }
}