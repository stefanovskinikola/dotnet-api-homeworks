using Loto3000.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loto3000.DataAccess.Configurations;

internal sealed class SessionConfiguration : IEntityTypeConfiguration<LotterySession>
{
    public void Configure(EntityTypeBuilder<LotterySession> builder)
    {
        builder.ToTable("LotterySessions", table =>
        {
            table.HasCheckConstraint("CK_LotterySessions_Number", "[SessionNumber] > 0");
            table.HasCheckConstraint("CK_LotterySessions_Lifecycle", "([Status] = 0 AND [EndTime] IS NULL) OR ([Status] = 1 AND [EndTime] IS NOT NULL AND [EndTime] >= [StartTime])");
        });
        builder.HasKey(session => session.Id);
        builder.HasIndex(session => session.SessionNumber).IsUnique();
        // Only Active rows participate, so completed history is unlimited but active rows are unique.
        builder.HasIndex(session => session.Status).IsUnique().HasFilter("[Status] = 0");
        builder.HasMany(session => session.Tickets).WithOne(ticket => ticket.Session)
            .HasForeignKey(ticket => ticket.SessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(session => session.Draw).WithOne(draw => draw.Session)
            .HasForeignKey<Draw>(draw => draw.SessionId).OnDelete(DeleteBehavior.Restrict);
    }
}
