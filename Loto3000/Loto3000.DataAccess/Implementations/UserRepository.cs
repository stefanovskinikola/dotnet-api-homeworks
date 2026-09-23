using Loto3000.DataAccess.Interfaces;
using Loto3000.DataAccess.Data;
using Loto3000.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Loto3000.DataAccess.Implementations;

internal sealed class UserRepository(Loto3000DbContext context) : IUserRepository
{
    /// <inheritdoc />
    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        context.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Username == username, cancellationToken);

    /// <inheritdoc />
    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await context.Users.AddAsync(user, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string username, string email, CancellationToken cancellationToken = default) =>
        context.Users.AnyAsync(user => user.Username == username || user.Email == email, cancellationToken);
}
