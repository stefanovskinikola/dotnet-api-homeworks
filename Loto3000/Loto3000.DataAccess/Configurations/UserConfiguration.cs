using Loto3000.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loto3000.DataAccess.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", table => table.HasCheckConstraint("CK_Users_Role", "[Role] IN (0, 1)"));
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Username).HasMaxLength(32).UseCollation("Latin1_General_100_CI_AS").IsRequired();
        builder.Property(user => user.FirstName).HasMaxLength(80).IsRequired();
        builder.Property(user => user.LastName).HasMaxLength(80).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(254).UseCollation("Latin1_General_100_CI_AS").IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(100).IsRequired();
        builder.HasIndex(user => user.Username).IsUnique();
        builder.HasIndex(user => user.Email).IsUnique();
    }
}
