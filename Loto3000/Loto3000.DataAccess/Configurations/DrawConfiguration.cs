using Loto3000.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loto3000.DataAccess.Configurations;

internal sealed class DrawConfiguration : IEntityTypeConfiguration<Draw>
{
    public void Configure(EntityTypeBuilder<Draw> builder)
    {
        builder.ToTable("Draws", table => table.HasCheckConstraint("CK_Draws_NumbersJson", "ISJSON([DrawnNumbers]) = 1"));
        builder.HasKey(draw => draw.Id);
        NumberListMapping.Configure(builder.Property(draw => draw.DrawnNumbers));
        builder.HasOne<User>().WithMany().HasForeignKey(draw => draw.InitiatedByAdminId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(draw => draw.DrawnAt);
    }
}
