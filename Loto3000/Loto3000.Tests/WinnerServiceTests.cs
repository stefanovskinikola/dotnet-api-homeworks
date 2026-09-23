using FluentAssertions;
using Loto3000.DataAccess.Interfaces;
using Loto3000.Domain.Models;
using Loto3000.Domain.Enums;
using Loto3000.Services.Implementations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Loto3000.Tests;

/// <summary>Verifies empty and populated public-board projection behavior.</summary>
public sealed class WinnerServiceTests
{
    /// <summary>No invented results appear before a prize-bearing ticket exists.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task ReturnsAnEmptyBoardBeforeAnyoneWins()
    {
        var repository = new Mock<IWinnerRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetAllWinnersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Winner>());
        var service = new WinnerService(repository.Object, NullLogger<WinnerService>.Instance);
        (await service.GetWinnersBoardAsync()).Should().BeEmpty();
    }

    /// <summary>Winner responses contain the required public columns and preserve repository order.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task OrdersByDrawDateDescendingAndMapsOnlyPublicWinnerDetails()
    {
        var now = DateTimeOffset.UtcNow;
        var older = CreateWinner(1, 1, now.AddDays(-1));
        var newer = CreateWinner(2, 2, now);
        var tied = CreateWinner(3, 2, now);
        var repository = new Mock<IWinnerRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetAllWinnersAsync(It.IsAny<CancellationToken>())).ReturnsAsync([older, newer, tied]);
        var service = new WinnerService(repository.Object, NullLogger<WinnerService>.Instance);

        var result = await service.GetWinnersBoardAsync();
        result.Select(winner => winner.TicketId).Should().Equal(3, 2, 1);
        result[0].WinnerFullName.Should().Be("Alice Player");
        result[0].WinningNumbers.Should().Equal(1, 2, 3);
        result[0].PrizeName.Should().Be("$50 Gift Card");
        result[0].SessionNumber.Should().Be(2);
        result[0].DrawDate.Should().Be(now);
        tied.MatchedNumbers.Clear();
        result[0].WinningNumbers.Should().HaveCount(3);
    }

    private static Winner CreateWinner(int id, int sessionNumber, DateTimeOffset drawnAt) => new()
    {
        Id = id, TicketId = id, UserId = 42,
        User = new User { FirstName = "Alice", LastName = "Player" },
        Draw = new Draw { DrawnAt = drawnAt, Session = new LotterySession { SessionNumber = sessionNumber } },
        MatchedNumbers = [1, 2, 3], MatchedCount = 3, Prize = PrizeType.FiftyDollarGiftCard, WonAt = drawnAt
    };
}
