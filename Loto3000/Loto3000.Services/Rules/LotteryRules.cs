using System.Security.Cryptography;
using Loto3000.Domain.Models;
using Loto3000.Domain.Enums;
using Loto3000.Domain.Exceptions;

namespace Loto3000.Services.Rules;

/// <summary>Pure validation, cryptographic sampling and set-based prize rules.</summary>
public static class LotteryRules
{
    /// <summary>The required number of distinct selections on a ticket.</summary>
    public const int TicketNumberCount = 7;
    /// <summary>The number of distinct values drawn per session.</summary>
    public const int DrawNumberCount = 8;
    /// <summary>The inclusive upper bound of the pool, whose lower bound is one.</summary>
    public const int MaximumNumber = 37;

    /// <summary>Rejects missing, wrong-count, repeated or out-of-range selections.</summary>
    /// <param name="numbers">The submitted ticket numbers.</param>
    /// <exception cref="BusinessRuleException">The selection is not seven distinct integers in [1,37].</exception>
    public static void ValidateTicketNumbers(IReadOnlyCollection<int>? numbers)
    {
        if (numbers is null || numbers.Count != TicketNumberCount)
        {
            throw new BusinessRuleException("Choose exactly 7 numbers.");
        }

        if (numbers.Any(number => number is < 1 or > MaximumNumber))
        {
            throw new BusinessRuleException("Every number must be between 1 and 37.");
        }

        if (numbers.Distinct().Count() != TicketNumberCount)
        {
            throw new BusinessRuleException("All 7 numbers must be unique.");
        }
    }

    /// <summary>Samples eight distinct pool values without replacement using a cryptographic RNG.</summary>
    /// <returns>Eight uniformly sampled distinct numbers, sorted for display.</returns>
    public static List<int> GenerateDrawNumbers()
    {
        var pool = Enumerable.Range(1, MaximumNumber).ToArray();
        for (var index = 0; index < DrawNumberCount; index++)
        {
            // Partial Fisher-Yates selects from the remaining suffix. GetInt32 uses unbiased
            // cryptographic randomness with an exclusive upper bound; swaps prevent duplicates.
            var randomIndex = RandomNumberGenerator.GetInt32(index, pool.Length);
            (pool[index], pool[randomIndex]) = (pool[randomIndex], pool[index]);
        }

        return pool.Take(DrawNumberCount).Order().ToList();
    }

    /// <summary>Maps the ticket/draw intersection cardinality to the exact prize tier.</summary>
    /// <param name="matchedCount">A match count from zero through seven.</param>
    /// <returns>The qualifying prize, or None below three matches.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The count is outside [0,7].</exception>
    public static PrizeType GetPrize(int matchedCount) => matchedCount switch
    {
        0 or 1 or 2 => PrizeType.None,
        3 => PrizeType.FiftyDollarGiftCard,
        4 => PrizeType.HundredDollarGiftCard,
        5 => PrizeType.TV,
        6 => PrizeType.Vacation,
        7 => PrizeType.Car,
        _ => throw new ArgumentOutOfRangeException(nameof(matchedCount))
    };

    /// <summary>Gets the user-facing label for a supported prize.</summary>
    /// <param name="prize">The calculated prize tier.</param>
    /// <returns>The display label used consistently by API responses and the SPA.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The prize enum value is undefined.</exception>
    public static string GetPrizeName(PrizeType prize) => prize switch
    {
        PrizeType.None => "No prize",
        PrizeType.FiftyDollarGiftCard => "$50 Gift Card",
        PrizeType.HundredDollarGiftCard => "$100 Gift Card",
        PrizeType.TV => "TV",
        PrizeType.Vacation => "Vacation",
        PrizeType.Car => "Car (Jackpot)",
        _ => throw new ArgumentOutOfRangeException(nameof(prize))
    };

    /// <summary>Evaluates a valid ticket against a server-generated draw without persisting anything.</summary>
    /// <param name="ticket">A seven-number ticket with its owner loaded.</param>
    /// <param name="draw">The valid eight-number draw generated for that ticket's session.</param>
    /// <returns>A staged winner for three or more matches, otherwise null.</returns>
    /// <exception cref="BusinessRuleException">Persisted ticket numbers violate lottery rules.</exception>
    public static Winner? EvaluateTicket(Ticket ticket, Draw draw)
    {
        ValidateTicketNumbers(ticket.Numbers);
        // |ticket ∩ draw| counts distinct common values, not positions or a bonus number.
        // All eight drawn values participate; a seven-number ticket can match at most seven.
        var matchedNumbers = ticket.Numbers.Intersect(draw.DrawnNumbers).Order().ToList();
        var prize = GetPrize(matchedNumbers.Count);
        return prize == PrizeType.None ? null : new Winner
        {
            DrawId = draw.Id,
            Draw = draw,
            TicketId = ticket.Id,
            Ticket = ticket,
            UserId = ticket.UserId,
            User = ticket.User,
            MatchedNumbers = matchedNumbers,
            MatchedCount = matchedNumbers.Count,
            Prize = prize,
            WonAt = draw.DrawnAt
        };
    }
}
