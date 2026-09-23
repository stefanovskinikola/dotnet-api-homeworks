using Loto3000.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loto3000.DataAccess.Configurations;

internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets", table => table.HasCheckConstraint("CK_Tickets_NumbersJson", "ISJSON([Numbers]) = 1"));
        builder.HasKey(ticket => ticket.Id);
        NumberListMapping.Configure(builder.Property(ticket => ticket.Numbers));
        builder.HasOne(ticket => ticket.User).WithMany().HasForeignKey(ticket => ticket.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(ticket => new { ticket.SessionId, ticket.SubmittedAt });
        builder.HasIndex(ticket => new { ticket.UserId, ticket.SubmittedAt });
    }
}
