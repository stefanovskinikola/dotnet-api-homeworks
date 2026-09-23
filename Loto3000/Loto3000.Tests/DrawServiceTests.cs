using FluentAssertions;
using Loto3000.DataAccess.Interfaces;
using Loto3000.Domain.Models;
using Loto3000.Domain.Enums;
using Loto3000.Domain.Exceptions;
using Loto3000.Services.Implementations;
using Loto3000.Services.Rules;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Loto3000.Tests;

/// <summary>Exercises real crypto draws, exact prizes, atomic rollover and failure cleanup.</summary>
public sealed class DrawServiceTests
{
    /// <summary>Repeated real draws always contain eight distinct sorted pool values.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task GeneratesExactlyEightUniqueNumbersInRange()
    {
        for (var iteration = 0; iteration < 50; iteration++)
        {
            var fixture = new Fixture();
            var result = await fixture.Service.InitiateDrawAsync(1, 11);
            result.DrawnNumbers.Should().HaveCount(8).And.OnlyHaveUniqueItems().And.OnlyContain(number => number >= 1 && number <= 37);
            result.DrawnNumbers.Should().BeInAscendingOrder();
        }
    }

    /// <summary>Every possible match count produces the specified prize and intersection.</summary>
    /// <param name="matches">The constructed intersection cardinality.</param>
    /// <param name="expectedPrize">The required prize enum.</param>
    /// <param name="expectedName">The required display label.</param>
    [Theory]
    [InlineData(0, PrizeType.None, "No prize")]
    [InlineData(1, PrizeType.None, "No prize")]
    [InlineData(2, PrizeType.None, "No prize")]
    [InlineData(3, PrizeType.FiftyDollarGiftCard, "$50 Gift Card")]
    [InlineData(4, PrizeType.HundredDollarGiftCard, "$100 Gift Card")]
    [InlineData(5, PrizeType.TV, "TV")]
    [InlineData(6, PrizeType.Vacation, "Vacation")]
    [InlineData(7, PrizeType.Car, "Car (Jackpot)")]
    public void AllocatesTheCorrectPrizeAndMatchedNumbers(int matches, PrizeType expectedPrize, string expectedName)
    {
        var fixture = new Fixture();
        var draw = new Draw { Id = 501, SessionId = 11, Session = fixture.Session, DrawnNumbers = [1, 2, 3, 4, 5, 6, 7, 8], DrawnAt = DateTimeOffset.UtcNow };
        var ticket = fixture.CreateTicket(101, Enumerable.Range(1, matches).Concat(Enumerable.Range(9, 7 - matches)).ToList());
        var winner = LotteryRules.EvaluateTicket(ticket, draw);

        LotteryRules.GetPrize(matches).Should().Be(expectedPrize);
        LotteryRules.GetPrizeName(expectedPrize).Should().Be(expectedName);
        if (expectedPrize == PrizeType.None)
        {
            winner.Should().BeNull();
            return;
        }

        winner.Should().NotBeNull();
        winner!.Prize.Should().Be(expectedPrize);
        winner.MatchedCount.Should().Be(matches);
        winner.MatchedNumbers.Should().Equal(Enumerable.Range(1, matches));
        winner.DrawId.Should().Be(501);
        winner.TicketId.Should().Be(101);
        winner.UserId.Should().Be(42);
        winner.WonAt.Should().Be(draw.DrawnAt);
    }

    /// <summary>Draws, winners and session writes occur in the required transaction order.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task PersistsDrawAndAllWinnersAndTransitionsSessionsAtomically()
    {
        var fixture = new Fixture();
        var submitted = fixture.CreateCoveringTickets();
        fixture.Tickets.Setup(repository => repository.GetTicketsBySessionIdAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(submitted);

        var result = await fixture.Service.InitiateDrawAsync(1, 11);

        fixture.SavedDraw.Should().NotBeNull();
        fixture.SavedDraw!.InitiatedByAdminId.Should().Be(1);
        fixture.SavedDraw.SessionId.Should().Be(11);
        fixture.Session.Status.Should().Be(SessionStatus.Completed);
        fixture.Session.EndTime.Should().Be(result.DrawnAt);
        fixture.Session.Draw.Should().BeSameAs(fixture.SavedDraw);
        fixture.NextSession.Should().NotBeNull();
        fixture.NextSession!.Status.Should().Be(SessionStatus.Active);
        fixture.NextSession.SessionNumber.Should().Be(5);
        fixture.NextSession.EndTime.Should().BeNull();
        fixture.NextSession.StartTime.Should().Be(result.DrawnAt);
        result.NextSession.Id.Should().Be(12);
        result.TicketCount.Should().Be(submitted.Count);

        var expected = submitted.Select(ticket => new
        {
            TicketId = ticket.Id,
            Matches = ticket.Numbers.Intersect(result.DrawnNumbers).Order().ToArray()
        }).Where(ticket => ticket.Matches.Length >= 3).ToArray();
        fixture.SavedWinners.Should().NotBeEmpty().And.HaveCount(expected.Length);
        fixture.SavedWinners.Select(winner => new { winner.TicketId, Matches = winner.MatchedNumbers.ToArray() })
            .Should().BeEquivalentTo(expected);
        foreach (var winner in fixture.SavedWinners)
        {
            winner.Draw.Should().BeSameAs(fixture.SavedDraw);
            winner.Prize.Should().Be(LotteryRules.GetPrize(winner.MatchedCount));
            winner.MatchedCount.Should().Be(winner.MatchedNumbers.Count);
            winner.WonAt.Should().Be(result.DrawnAt);
        }

        result.Winners.Should().HaveCount(expected.Length);
        result.Winners.Select(winner => winner.MatchedCount).Should().BeInDescendingOrder();
        result.Winners.Should().OnlyContain(winner => winner.WinnerFullName == "Sample Player" && winner.SessionNumber == 4);
        fixture.Events.Should().Equal("begin", "draw", "winners", "complete", "save", "next", "save", "commit");
        fixture.Unit.Verify(unit => unit.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Empty sessions still roll forward without generating fictitious winners.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task AllowsAnEmptySessionToCompleteWithoutInventingWinners()
    {
        var fixture = new Fixture();
        var result = await fixture.Service.InitiateDrawAsync(1, 11);
        result.TicketCount.Should().Be(0);
        result.Winners.Should().BeEmpty();
        fixture.SavedWinners.Should().BeEmpty();
        fixture.NextSession!.SessionNumber.Should().Be(5);
        fixture.Unit.Verify(unit => unit.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Service-level role checks reject players even without controller authorization.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task RejectsPlayersBeforeStartingATransaction()
    {
        var fixture = new Fixture();
        fixture.Admin.Role = UserRole.Player;
        var act = () => fixture.Service.InitiateDrawAsync(1, 11);
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*administrator*");
        fixture.Unit.Verify(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>A missing administrator account cannot execute a draw.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task RejectsAnUnknownAdministrator()
    {
        var fixture = new Fixture();
        fixture.Users.Setup(repository => repository.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var act = () => fixture.Service.InitiateDrawAsync(1, 11);
        await act.Should().ThrowAsync<BusinessRuleException>();
        fixture.Unit.Verify(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Missing and already-completed sessions cannot be drawn.</summary>
    /// <param name="completed">Whether to return a completed session rather than null.</param>
    /// <returns>The asynchronous assertion task.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectsAMissingOrCompletedSession(bool completed)
    {
        var fixture = new Fixture();
        fixture.Session.Status = SessionStatus.Completed;
        fixture.Sessions.Setup(repository => repository.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(completed ? fixture.Session : null);
        var act = () => fixture.Service.InitiateDrawAsync(1, 11);
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*no active lottery session*");
        fixture.Draws.Verify(repository => repository.AddAsync(It.IsAny<Draw>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Unit.Verify(unit => unit.RollbackAsync(CancellationToken.None), Times.Once);
    }

    /// <summary>A stale request never draws the newly active successor session.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task RejectsAStaleOrRepeatedDrawRequest()
    {
        var fixture = new Fixture();
        var act = () => fixture.Service.InitiateDrawAsync(1, 10);
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*already been drawn or has changed*");
        fixture.Draws.Verify(repository => repository.AddAsync(It.IsAny<Draw>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Unit.Verify(unit => unit.RollbackAsync(CancellationToken.None), Times.Once);
    }

    /// <summary>Failure of either flush rolls back the entire draw without committing.</summary>
    /// <param name="failingSave">The first or second save to fail.</param>
    /// <returns>The asynchronous assertion task.</returns>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task RollsBackIfEitherSaveFails(int failingSave)
    {
        var fixture = new Fixture();
        var sequence = fixture.Unit.SetupSequence(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()));
        if (failingSave == 2)
        {
            sequence.ReturnsAsync(1);
        }
        sequence.ThrowsAsync(new InvalidOperationException("Storage unavailable"));
        var act = () => fixture.Service.InitiateDrawAsync(1, 11);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Storage unavailable");
        fixture.Unit.Verify(unit => unit.RollbackAsync(CancellationToken.None), Times.Once);
        fixture.Unit.Verify(unit => unit.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Cleanup errors do not obscure the original persistence failure.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task PreservesTheOriginalFailureIfRollbackAlsoFails()
    {
        var fixture = new Fixture();
        fixture.Unit.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Original failure"));
        fixture.Unit.Setup(unit => unit.RollbackAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Rollback failure"));
        var act = () => fixture.Service.InitiateDrawAsync(1, 11);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Original failure");
    }

    /// <summary>Cancellation cannot suppress the best-effort rollback attempt.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task UsesANonCanceledTokenToRollBackACanceledDraw()
    {
        var fixture = new Fixture();
        var cancellationToken = new CancellationToken(canceled: true);
        fixture.Unit.Setup(unit => unit.BeginTransactionAsync(cancellationToken)).ThrowsAsync(new OperationCanceledException(cancellationToken));
        var act = () => fixture.Service.InitiateDrawAsync(1, 11, cancellationToken);
        await act.Should().ThrowAsync<OperationCanceledException>();
        fixture.Unit.Verify(unit => unit.RollbackAsync(CancellationToken.None), Times.Once);
    }

    /// <summary>Session responses include the current public state and repository ticket count.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task ReturnsCurrentSessionStatistics()
    {
        var fixture = new Fixture();
        fixture.Tickets.Setup(repository => repository.CountBySessionIdAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(23);
        var result = await fixture.Service.GetCurrentSessionAsync();
        result.Id.Should().Be(11);
        result.Status.Should().Be("Active");
        result.TicketCount.Should().Be(23);
    }

    /// <summary>Overflow in the session number cannot commit a completed session without a successor.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task RollsBackWhenSessionNumberWouldOverflow()
    {
        var fixture = new Fixture();
        fixture.Session.SessionNumber = int.MaxValue;
        var act = () => fixture.Service.InitiateDrawAsync(1, 11);
        await act.Should().ThrowAsync<OverflowException>();
        fixture.Unit.Verify(unit => unit.RollbackAsync(CancellationToken.None), Times.Once);
        fixture.Unit.Verify(unit => unit.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        fixture.Sessions.Verify(repository => repository.AddAsync(It.IsAny<LotterySession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class Fixture
    {
        public Mock<ISessionRepository> Sessions { get; } = new(MockBehavior.Strict);
        public Mock<ITicketRepository> Tickets { get; } = new(MockBehavior.Strict);
        public Mock<IDrawRepository> Draws { get; } = new(MockBehavior.Strict);
        public Mock<IWinnerRepository> Winners { get; } = new(MockBehavior.Strict);
        public Mock<IUserRepository> Users { get; } = new(MockBehavior.Strict);
        public Mock<IUnitOfWork> Unit { get; } = new(MockBehavior.Strict);
        public User Admin { get; } = new() { Id = 1, Role = UserRole.Admin };
        public LotterySession Session { get; } = new() { Id = 11, SessionNumber = 4, Status = SessionStatus.Active, StartTime = DateTimeOffset.UtcNow.AddHours(-1) };
        public Draw? SavedDraw { get; private set; }
        public LotterySession? NextSession { get; private set; }
        public IReadOnlyCollection<Winner> SavedWinners { get; private set; } = [];
        public List<string> Events { get; } = [];
        public DrawService Service { get; }

        public Fixture()
        {
            Users.Setup(repository => repository.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Admin);
            Sessions.Setup(repository => repository.GetActiveSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Session);
            Tickets.Setup(repository => repository.GetTicketsBySessionIdAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Ticket>());
            Draws.Setup(repository => repository.AddAsync(It.IsAny<Draw>(), It.IsAny<CancellationToken>()))
                .Callback<Draw, CancellationToken>((draw, _) => { draw.Id = 501; SavedDraw = draw; Events.Add("draw"); }).Returns(Task.CompletedTask);
            Winners.Setup(repository => repository.AddWinnersAsync(It.IsAny<IReadOnlyCollection<Winner>>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyCollection<Winner>, CancellationToken>((winners, _) => { SavedWinners = winners; Events.Add("winners"); }).Returns(Task.CompletedTask);
            Sessions.Setup(repository => repository.UpdateAsync(It.IsAny<LotterySession>(), It.IsAny<CancellationToken>()))
                .Callback(() => Events.Add("complete")).Returns(Task.CompletedTask);
            Sessions.Setup(repository => repository.AddAsync(It.IsAny<LotterySession>(), It.IsAny<CancellationToken>()))
                .Callback<LotterySession, CancellationToken>((session, _) => { session.Id = 12; NextSession = session; Events.Add("next"); }).Returns(Task.CompletedTask);
            Unit.Setup(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>())).Callback(() => Events.Add("begin")).Returns(Task.CompletedTask);
            Unit.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).Callback(() => Events.Add("save")).ReturnsAsync(1);
            Unit.Setup(unit => unit.CommitAsync(It.IsAny<CancellationToken>())).Callback(() => Events.Add("commit")).Returns(Task.CompletedTask);
            Unit.Setup(unit => unit.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            Service = new DrawService(Sessions.Object, Tickets.Object, Draws.Object, Winners.Object, Users.Object, Unit.Object, NullLogger<DrawService>.Instance);
        }

        public Ticket CreateTicket(int id, List<int> numbers) => new()
        {
            Id = id, UserId = 42, SessionId = 11, Session = Session, Numbers = numbers,
            User = new User { Id = 42, FirstName = "Sample", LastName = "Player" }
        };

        public IReadOnlyList<Ticket> CreateCoveringTickets()
        {
            // Cover every triple so the real random draw always produces winners without replacing the CSPRNG.
            var tickets = new List<Ticket>();
            for (var first = 1; first <= 35; first++)
            for (var second = first + 1; second <= 36; second++)
            for (var third = second + 1; third <= 37; third++)
            {
                int[] triple = [first, second, third];
                var numbers = triple.Concat(Enumerable.Range(1, 37).Except(triple).Take(4)).Order().ToList();
                tickets.Add(CreateTicket(tickets.Count + 1, numbers));
            }
            return tickets;
        }
    }
}
