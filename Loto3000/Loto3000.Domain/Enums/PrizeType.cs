namespace Loto3000.Domain.Enums;

/// <summary>Prize tiers whose stored values equal the qualifying match counts.</summary>
public enum PrizeType
{
    /// <summary>No prize for zero, one or two matches.</summary>
    None = 0,
    /// <summary>A $50 gift card for three matches.</summary>
    FiftyDollarGiftCard = 3,
    /// <summary>A $100 gift card for four matches.</summary>
    HundredDollarGiftCard = 4,
    /// <summary>A television for five matches.</summary>
    TV = 5,
    /// <summary>A vacation for six matches.</summary>
    Vacation = 6,
    /// <summary>The car jackpot for seven matches.</summary>
    Car = 7
}
