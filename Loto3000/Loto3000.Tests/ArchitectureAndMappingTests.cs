using FluentAssertions;
using System.Xml.Linq;
using Loto3000.DataAccess.Data;
using Loto3000.Domain.Models;
using Loto3000.Domain.Enums;
using Loto3000.Services.DTOs.Auth;
using Loto3000.Services.DTOs.Tickets;
using Loto3000.Services.DTOs.Draws;
using Loto3000.Services.DTOs.Winners;
using Loto3000.Services.DTOs.Sessions;
using Loto3000.Services.Implementations;
using Loto3000.Services.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Loto3000.Tests;

/// <summary>Enforces layer boundaries, EF mapping invariants and detached API projections.</summary>
public sealed class ArchitectureAndMappingTests
{
    /// <summary>Business constructors accept only persistence interfaces, logging and the approved JWT settings.</summary>
    /// <param name="serviceType">The concrete service being inspected.</param>
    [Theory]
    [InlineData(typeof(AuthService))]
    [InlineData(typeof(TicketService))]
    [InlineData(typeof(DrawService))]
    [InlineData(typeof(WinnerService))]
    public void BusinessServicesInjectOnlyApprovedAbstractions(Type serviceType)
    {
        var dependencies = serviceType.GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType);
        dependencies.Should().OnlyContain(type =>
            (type.IsInterface && type.Namespace == "Loto3000.DataAccess.Interfaces")
            || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ILogger<>))
            || (serviceType == typeof(AuthService) && type == typeof(JwtOptions)));
    }

    /// <summary>JSON conversions preserve numbers and snapshot comparers detect in-place edits.</summary>
    /// <param name="entityType">The mapped entity type.</param>
    /// <param name="propertyName">Its list-valued property.</param>
    [Theory]
    [InlineData(typeof(Ticket), nameof(Ticket.Numbers))]
    [InlineData(typeof(Draw), nameof(Draw.DrawnNumbers))]
    [InlineData(typeof(Winner), nameof(Winner.MatchedNumbers))]
    public void NumberListsRoundTripAndDetectInPlaceChanges(Type entityType, string propertyName)
    {
        using var context = CreateContext();
        var property = context.Model.FindEntityType(entityType)!.FindProperty(propertyName)!;
        var converter = property.GetValueConverter()!;
        var comparer = property.GetValueComparer()!;
        List<int> original = [1, 4, 8, 11, 20, 30, 37];
        var json = converter.ConvertToProvider(original);
        var restored = (List<int>)converter.ConvertFromProvider(json)!;
        restored.Should().Equal(original);
        var snapshot = (List<int>)comparer.Snapshot(original)!;
        snapshot.Should().NotBeSameAs(original);
        comparer.Equals(original, snapshot).Should().BeTrue();
        original[0] = 2;
        comparer.Equals(original, snapshot).Should().BeFalse();
    }

    /// <summary>SQL model metadata enforces active-session uniqueness, one draw and non-cascading history.</summary>
    [Fact]
    public void SchemaAllowsOnlyOneActiveSessionAndOneDrawPerSession()
    {
        using var context = CreateContext();
        var session = context.Model.FindEntityType(typeof(LotterySession))!;
        var activeIndex = session.GetIndexes().Single(index => index.Properties.Count == 1 && index.Properties[0].Name == nameof(LotterySession.Status));
        activeIndex.IsUnique.Should().BeTrue();
        activeIndex.GetFilter().Should().Be("[Status] = 0");
        var draw = context.Model.FindEntityType(typeof(Draw))!;
        draw.GetForeignKeys().Single(key => key.PrincipalEntityType.ClrType == typeof(LotterySession)).IsUnique.Should().BeTrue();
        context.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys())
            .Should().OnlyContain(key => key.DeleteBehavior == DeleteBehavior.Restrict);
    }

    /// <summary>The domain assembly references only platform assemblies.</summary>
    [Fact]
    public void DomainHasNoThirdPartyAssemblyDependencies()
    {
        typeof(User).Assembly.GetReferencedAssemblies().Should()
            .OnlyContain(assembly => assembly.Name != null && assembly.Name.StartsWith("System", StringComparison.Ordinal));
    }

    /// <summary>Each project retains exactly its specified direct dependencies and target framework.</summary>
    /// <param name="project">The project short name.</param>
    /// <param name="references">Comma-separated permitted project short names.</param>
    /// <param name="packages">Comma-separated permitted package IDs.</param>
    [Theory]
    [InlineData("Domain", "", "")]
    [InlineData("DataAccess", "Domain", "Microsoft.EntityFrameworkCore.SqlServer,Microsoft.EntityFrameworkCore.Tools")]
    [InlineData("Services", "Domain,DataAccess", "System.IdentityModel.Tokens.Jwt,Microsoft.IdentityModel.Tokens,BCrypt.Net-Next")]
    [InlineData("Web", "Services,DataAccess,Domain", "Microsoft.AspNetCore.Authentication.JwtBearer,Microsoft.EntityFrameworkCore.Design,Serilog.AspNetCore,Serilog.Sinks.File,Swashbuckle.AspNetCore")]
    [InlineData("Tests", "Services,Domain", "Microsoft.NET.Test.Sdk,xunit,xunit.runner.visualstudio,Moq,FluentAssertions")]
    public void ProjectReferencesAndPackagesStayInTheirDesignatedLayers(string project, string references, string packages)
    {
        var root = FindSolutionRoot();
        var document = XDocument.Load(Path.Combine(root, $"Loto3000.{project}", $"Loto3000.{project}.csproj"));
        document.Descendants("TargetFramework").Single().Value.Should().Be("net10.0");
        var projectReferences = document.Descendants("ProjectReference")
            .Select(element => Path.GetFileNameWithoutExtension(((string)element.Attribute("Include")!).Replace('\\', '/'))[9..]);
        projectReferences.Should().BeEquivalentTo(references.Split(',', StringSplitOptions.RemoveEmptyEntries));
        var packageReferences = document.Descendants("PackageReference").Select(element => (string)element.Attribute("Include")!);
        packageReferences.Should().OnlyHaveUniqueItems().And.BeEquivalentTo(packages.Split(',', StringSplitOptions.RemoveEmptyEntries));
        File.ReadLines(Path.Combine(root, "Loto3000.sln")).Count(line => line.Contains(".csproj", StringComparison.Ordinal))
            .Should().Be(5);
    }

    /// <summary>DTO types live in feature folders inside the same Services assembly.</summary>
    /// <param name="dtoType">A representative request, response or configuration type.</param>
    /// <param name="feature">The required feature namespace suffix.</param>
    [Theory]
    [InlineData(typeof(RegisterDto), "Auth")]
    [InlineData(typeof(LoginDto), "Auth")]
    [InlineData(typeof(AuthResponseDto), "Auth")]
    [InlineData(typeof(UserDto), "Auth")]
    [InlineData(typeof(JwtOptions), "Auth")]
    [InlineData(typeof(CreateTicketDto), "Tickets")]
    [InlineData(typeof(TicketResponseDto), "Tickets")]
    [InlineData(typeof(DrawResultDto), "Draws")]
    [InlineData(typeof(InitiateDrawDto), "Draws")]
    [InlineData(typeof(WinnerBoardDto), "Winners")]
    [InlineData(typeof(SessionDto), "Sessions")]
    public void DtosRemainLogicalFeatureFoldersInServices(Type dtoType, string feature)
    {
        ReferenceEquals(dtoType.Assembly, typeof(AuthService).Assembly).Should().BeTrue();
        dtoType.Namespace.Should().Be($"Loto3000.Services.DTOs.{feature}");
    }

    /// <summary>Ticket projections compute all-eight-number intersections and copy mutable lists.</summary>
    [Fact]
    public void TicketMapperReturnsDetachedNumbersAndExactPrize()
    {
        var session = new LotterySession { Id = 1, SessionNumber = 9, Status = SessionStatus.Completed };
        var draw = new Draw { SessionId = 1, Session = session, DrawnNumbers = [1, 2, 3, 4, 5, 6, 7, 8] };
        session.Draw = draw;
        var ticket = new Ticket { Session = session, SessionId = 1, Numbers = [2, 3, 4, 5, 6, 7, 8] };
        var response = ticket.ToDto();
        ticket.Numbers.Clear();
        draw.DrawnNumbers.Clear();
        response.Numbers.Should().HaveCount(7);
        response.DrawnNumbers.Should().HaveCount(8);
        response.MatchedCount.Should().Be(7);
        response.PrizeName.Should().Be("Car (Jackpot)");
    }

    /// <summary>Winner projections retain public fields but do not share mutable model lists.</summary>
    [Fact]
    public void WinnerMapperCopiesMatchesAndExcludesPrivateAccountData()
    {
        var session = new LotterySession { SessionNumber = 12 };
        var draw = new Draw { Session = session, DrawnAt = DateTimeOffset.UtcNow };
        var winner = new Winner
        {
            TicketId = 7, Draw = draw, User = new User { FirstName = "Test", LastName = "Player", Email = "private@example.com", PasswordHash = "private" },
            MatchedNumbers = [1, 2, 3], MatchedCount = 3, Prize = PrizeType.FiftyDollarGiftCard
        };
        var response = winner.ToDto();
        winner.MatchedNumbers.Clear();
        response.WinningNumbers.Should().Equal(1, 2, 3);
        response.WinnerFullName.Should().Be("Test Player");
        response.SessionNumber.Should().Be(12);
        response.DrawDate.Should().Be(draw.DrawnAt);
        typeof(WinnerBoardDto).GetProperties().Should().NotContain(property => property.Name == "Email" || property.Name == "PasswordHash");
    }

    private static string FindSolutionRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Loto3000.sln"))) return directory.FullName;
        }
        throw new InvalidOperationException("Run architecture tests from the solution checkout so project boundaries can be verified.");
    }

    private static Loto3000DbContext CreateContext() => new(new DbContextOptionsBuilder<Loto3000DbContext>()
        .UseSqlServer("Server=unused;Database=ModelValidationOnly;Trusted_Connection=True;TrustServerCertificate=True;").Options);
}
