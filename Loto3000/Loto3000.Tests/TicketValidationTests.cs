using FluentAssertions;
using Loto3000.DataAccess.Interfaces;
using Loto3000.Domain.Models;
using Loto3000.Domain.Enums;
using Loto3000.Domain.Exceptions;
using Loto3000.Services.DTOs.Tickets;
using Loto3000.Services.Implementations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Loto3000.Tests;

/// <summary>Exercises number validation, ownership, stale-session rejection and ticket transaction cleanup.</summary>
public sealed class TicketValidationTests
{
    /// <summary>Tickets with too few selections are rejected.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public Task RejectsFewerThanSevenNumbers() => RejectAsync([1, 2, 3, 4, 5, 6], "*exactly 7*");

    /// <summary>Tickets with too many selections are rejected.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public Task RejectsMoreThanSevenNumbers() => RejectAsync([1, 2, 3, 4, 5, 6, 7, 8], "*exactly 7*");

    /// <summary>Values outside the inclusive pool are rejected.</summary>
    /// <param name="invalidNumber">A value below one or above 37.</param>
    /// <returns>The asynchronous assertion task.</returns>
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(38)]
    [InlineData(100)]
    public Task RejectsOutOfRangeNumbers(int invalidNumber) => RejectAsync([1, 2, 3, 4, 5, 6, invalidNumber], "*between 1 and 37*");

    /// <summary>Seven entries must also be seven distinct values.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public Task RejectsDuplicateNumbers() => RejectAsync([1, 2, 3, 4, 5, 6, 6], "*unique*");

    /// <summary>A missing selection is rejected before a transaction begins.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task RejectsNullNumbers()
    {
        var fixture = new Fixture();
        var act = () => fixture.Service.CreateTicketAsync(42, new CreateTicketDto { Numbers = null! });
        await act.Should().ThrowAsync<BusinessRuleException>();
        fixture.Unit.Verify(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Both pool boundaries are accepted and input mutations cannot change the persisted ticket.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task AcceptsSevenUniqueNumbersAndCopiesThemInAscendingOrder()
    {
        var fixture = new Fixture();
        List<int> numbers = [37, 1, 12, 4, 9, 22, 6];
        var response = await fixture.Service.CreateTicketAsync(42, new CreateTicketDto
        {
            UserId = 42, Username = "PLAYER", SessionId = 10, Numbers = numbers
        });
        numbers.Clear();

        response.Id.Should().Be(101);
        response.UserId.Should().Be(42);
        response.SessionId.Should().Be(10);
        response.Numbers.Should().Equal(1, 4, 6, 9, 12, 22, 37);
        response.Message.Should().Contain("Winners Board");
        response.MatchedCount.Should().BeNull();
        fixture.SavedTicket!.Numbers.Should().Equal(response.Numbers);
        fixture.Unit.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        fixture.Unit.Verify(unit => unit.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        fixture.Unit.Verify(unit => unit.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Request-body identity fields cannot impersonate another owner.</summary>
    /// <param name="userId">The asserted owner identifier.</param>
    /// <param name="username">The asserted login name.</param>
    /// <returns>The asynchronous assertion task.</returns>
    [Theory]
    [InlineData(99, "player")]
    [InlineData(42, "someone_else")]
    public async Task RejectsIdentitySpoofing(int userId, string username)
    {
        var fixture = new Fixture();
        var act = () => fixture.Service.CreateTicketAsync(42, new CreateTicketDto
        {
            UserId = userId, Username = username, Numbers = [1, 2, 3, 4, 5, 6, 7]
        });
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*your own account*");
        fixture.Unit.Verify(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>No ticket is persisted if the active session is missing or already closed.</summary>
    /// <param name="completed">Whether to return a completed session rather than null.</param>
    /// <returns>The asynchronous assertion task.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectsMissingOrCompletedSession(bool completed)
    {
        var fixture = new Fixture();
        fixture.Session.Status = SessionStatus.Completed;
        fixture.Sessions.Setup(repository => repository.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(completed ? fixture.Session : null);
        var act = () => fixture.Service.CreateTicketAsync(42, ValidRequest());
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*no active lottery session*");
        fixture.Tickets.Verify(repository => repository.AddAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Unit.Verify(unit => unit.RollbackAsync(CancellationToken.None), Times.Once);
    }

    /// <summary>Stale client session identifiers trigger rollback instead of entering a new session.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task RejectsStaleSessionAndRollsBack()
    {
        var fixture = new Fixture();
        var act = () => fixture.Service.CreateTicketAsync(42, ValidRequest() with { SessionId = 9 });
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*session has changed*");
        fixture.Unit.Verify(unit => unit.RollbackAsync(CancellationToken.None), Times.Once);
        fixture.Unit.Verify(unit => unit.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>A valid-looking subject cannot submit a ticket for a deleted account.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task RejectsAnAccountThatNoLongerExists()
    {
        var fixture = new Fixture();
        fixture.Users.Setup(repository => repository.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var act = () => fixture.Service.CreateTicketAsync(42, ValidRequest());
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*no longer exists*");
        fixture.Unit.Verify(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>A failed save is rolled back and never committed.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task RollsBackWhenPersistenceFails()
    {
        var fixture = new Fixture();
        fixture.Unit.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Storage unavailable"));
        var act = () => fixture.Service.CreateTicketAsync(42, ValidRequest());
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Storage unavailable");
        fixture.Unit.Verify(unit => unit.RollbackAsync(CancellationToken.None), Times.Once);
        fixture.Unit.Verify(unit => unit.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>History queries use the authenticated identifier.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task ReturnsOnlyTheAuthenticatedUsersTicketHistory()
    {
        var fixture = new Fixture();
        fixture.Tickets.Setup(repository => repository.GetUserTicketsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Ticket { Id = 101, UserId = 42, SessionId = 10, Session = fixture.Session, Numbers = [1, 2, 3, 4, 5, 6, 7] }]);
        var response = await fixture.Service.GetUserTicketsAsync(42);
        response.Should().ContainSingle().Which.UserId.Should().Be(42);
        fixture.Tickets.Verify(repository => repository.GetUserTicketsAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CreateTicketDto ValidRequest() => new() { Numbers = [1, 2, 3, 4, 5, 6, 7] };

    private static async Task RejectAsync(List<int> numbers, string message)
    {
        var fixture = new Fixture();
        var act = () => fixture.Service.CreateTicketAsync(42, new CreateTicketDto { Numbers = numbers });
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage(message);
        fixture.Tickets.Verify(repository => repository.AddAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Unit.Verify(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class Fixture
    {
        public Mock<ITicketRepository> Tickets { get; } = new(MockBehavior.Strict);
        public Mock<ISessionRepository> Sessions { get; } = new(MockBehavior.Strict);
        public Mock<IUserRepository> Users { get; } = new(MockBehavior.Strict);
        public Mock<IUnitOfWork> Unit { get; } = new(MockBehavior.Strict);
        public LotterySession Session { get; } = new() { Id = 10, SessionNumber = 1, Status = SessionStatus.Active, StartTime = DateTimeOffset.UtcNow.AddMinutes(-1) };
        public Ticket? SavedTicket { get; private set; }
        public TicketService Service { get; }

        public Fixture()
        {
            Users.Setup(repository => repository.GetByIdAsync(42, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { Id = 42, Username = "player", Role = UserRole.Player });
            Sessions.Setup(repository => repository.GetActiveSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Session);
            Tickets.Setup(repository => repository.AddAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
                .Callback<Ticket, CancellationToken>((ticket, _) => { ticket.Id = 101; SavedTicket = ticket; }).Returns(Task.CompletedTask);
            Unit.Setup(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            Unit.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            Unit.Setup(unit => unit.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            Unit.Setup(unit => unit.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            Service = new TicketService(Tickets.Object, Sessions.Object, Users.Object, Unit.Object, NullLogger<TicketService>.Instance);
        }
    }
}
