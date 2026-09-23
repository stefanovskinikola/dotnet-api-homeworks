using Loto3000.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loto3000.DataAccess.Configurations;

internal sealed class WinnerConfiguration : IEntityTypeConfiguration<Winner>
{
    public void Configure(EntityTypeBuilder<Winner> builder)
    {
        builder.ToTable("Winners", table =>
        {
            table.HasCheckConstraint("CK_Winners_NumbersJson", "ISJSON([MatchedNumbers]) = 1");
            table.HasCheckConstraint("CK_Winners_Prize", "[MatchedCount] BETWEEN 3 AND 7 AND [Prize] = [MatchedCount]");
        });
        builder.HasKey(winner => winner.Id);
        NumberListMapping.Configure(builder.Property(winner => winner.MatchedNumbers));
        builder.HasOne(winner => winner.Draw).WithMany().HasForeignKey(winner => winner.DrawId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(winner => winner.Ticket).WithMany().HasForeignKey(winner => winner.TicketId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(winner => winner.User).WithMany().HasForeignKey(winner => winner.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(winner => new { winner.DrawId, winner.TicketId }).IsUnique();
        builder.HasIndex(winner => winner.WonAt).IsDescending();
    }
}
