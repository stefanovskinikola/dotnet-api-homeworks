using Loto3000.Domain.Models;

namespace Loto3000.DataAccess.Interfaces;

/// <summary>Persists users without exposing database APIs to authentication services.</summary>
public interface IUserRepository
{
    /// <summary>Reads a user by normalized username without tracking.</summary>
    /// <param name="username">The normalized login name.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The user, or null if absent.</returns>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    /// <summary>Reads a user by identifier without tracking.</summary>
    /// <param name="id">The authenticated user's identifier.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The user, or null if absent.</returns>
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Stages a new account; unique constraints are checked when saved.</summary>
    /// <param name="user">The account with a password hash, never plaintext.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task completing when the account is tracked.</returns>
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    /// <summary>Checks whether either normalized username or email is already in use.</summary>
    /// <param name="username">The normalized login name.</param>
    /// <param name="email">The normalized email address.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>True when either value conflicts with an existing account.</returns>
    Task<bool> ExistsAsync(string username, string email, CancellationToken cancellationToken = default);
}
