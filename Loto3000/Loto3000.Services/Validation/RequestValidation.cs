using System.ComponentModel.DataAnnotations;
using System.Text;
using Loto3000.Domain.Exceptions;

namespace Loto3000.Services.Validation;

/// <summary>Enforces request contracts even when a service is invoked outside an HTTP controller.</summary>
internal static class RequestValidation
{
    /// <summary>Translates data-annotation failures to client-actionable business errors.</summary>
    /// <param name="request">The non-null DTO to validate.</param>
    /// <exception cref="BusinessRuleException">One or more annotations fail.</exception>
    public static void Validate(object request)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true))
        {
            throw new BusinessRuleException(string.Join(" ", results.Select(result => result.ErrorMessage)));
        }
    }

    /// <summary>Rejects weak passwords and prevents BCrypt's silent truncation beyond 72 UTF-8 bytes.</summary>
    /// <param name="password">The plaintext secret being registered or bootstrapped.</param>
    /// <exception cref="BusinessRuleException">The password violates length or complexity rules.</exception>
    public static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8 || Encoding.UTF8.GetByteCount(password) > 72
            || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit)
            || !password.Any(character => !char.IsLetterOrDigit(character) && !char.IsWhiteSpace(character)))
        {
            throw new BusinessRuleException("Passwords must be at least 8 characters, at most 72 UTF-8 bytes, and include uppercase, lowercase, a digit, and a symbol.");
        }
    }
}
