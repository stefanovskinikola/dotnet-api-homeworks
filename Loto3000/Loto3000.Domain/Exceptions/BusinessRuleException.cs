namespace Loto3000.Domain.Exceptions;

/// <summary>A safe, client-actionable business violation translated to an HTTP 400 response.</summary>
public sealed class BusinessRuleException : Exception
{
    /// <summary>Creates a violation with a message safe to show to the caller.</summary>
    /// <param name="message">A non-sensitive explanation of the violated rule.</param>
    public BusinessRuleException(string message) : base(message)
    {
    }

    /// <summary>Wraps a technical cause without exposing it in the client-facing message.</summary>
    /// <param name="message">A non-sensitive explanation of the violated rule.</param>
    /// <param name="innerException">The underlying cause retained for server-side diagnostics.</param>
    public BusinessRuleException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
