using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loto3000.DataAccess.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LotterySessions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SessionNumber = table.Column<int>(type: "int", nullable: false),
                StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                EndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                Status = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LotterySessions", x => x.Id);
                table.CheckConstraint("CK_LotterySessions_Lifecycle", "([Status] = 0 AND [EndTime] IS NULL) OR ([Status] = 1 AND [EndTime] IS NOT NULL AND [EndTime] >= [StartTime])");
                table.CheckConstraint("CK_LotterySessions_Number", "[SessionNumber] > 0");
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Username = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, collation: "Latin1_General_100_CI_AS"),
                FirstName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false, collation: "Latin1_General_100_CI_AS"),
                Role = table.Column<int>(type: "int", nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
                table.CheckConstraint("CK_Users_Role", "[Role] IN (0, 1)");
            });

        migrationBuilder.CreateTable(
            name: "Draws",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SessionId = table.Column<int>(type: "int", nullable: false),
                DrawnNumbers = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                DrawnAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                InitiatedByAdminId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Draws", x => x.Id);
                table.CheckConstraint("CK_Draws_NumbersJson", "ISJSON([DrawnNumbers]) = 1");
                table.ForeignKey(
                    name: "FK_Draws_LotterySessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "LotterySessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Draws_Users_InitiatedByAdminId",
                    column: x => x.InitiatedByAdminId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Tickets",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<int>(type: "int", nullable: false),
                SessionId = table.Column<int>(type: "int", nullable: false),
                Numbers = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tickets", x => x.Id);
                table.CheckConstraint("CK_Tickets_NumbersJson", "ISJSON([Numbers]) = 1");
                table.ForeignKey(
                    name: "FK_Tickets_LotterySessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "LotterySessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Tickets_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Winners",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                DrawId = table.Column<int>(type: "int", nullable: false),
                TicketId = table.Column<int>(type: "int", nullable: false),
                UserId = table.Column<int>(type: "int", nullable: false),
                MatchedNumbers = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                MatchedCount = table.Column<int>(type: "int", nullable: false),
                Prize = table.Column<int>(type: "int", nullable: false),
                WonAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Winners", x => x.Id);
                table.CheckConstraint("CK_Winners_NumbersJson", "ISJSON([MatchedNumbers]) = 1");
                table.CheckConstraint("CK_Winners_Prize", "[MatchedCount] BETWEEN 3 AND 7 AND [Prize] = [MatchedCount]");
                table.ForeignKey(
                    name: "FK_Winners_Draws_DrawId",
                    column: x => x.DrawId,
                    principalTable: "Draws",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Winners_Tickets_TicketId",
                    column: x => x.TicketId,
                    principalTable: "Tickets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Winners_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Draws_DrawnAt",
            table: "Draws",
            column: "DrawnAt");

        migrationBuilder.CreateIndex(
            name: "IX_Draws_InitiatedByAdminId",
            table: "Draws",
            column: "InitiatedByAdminId");

        migrationBuilder.CreateIndex(
            name: "IX_Draws_SessionId",
            table: "Draws",
            column: "SessionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_LotterySessions_SessionNumber",
            table: "LotterySessions",
            column: "SessionNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_LotterySessions_Status",
            table: "LotterySessions",
            column: "Status",
            unique: true,
            filter: "[Status] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_Tickets_SessionId_SubmittedAt",
            table: "Tickets",
            columns: new[] { "SessionId", "SubmittedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Tickets_UserId_SubmittedAt",
            table: "Tickets",
            columns: new[] { "UserId", "SubmittedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_Username",
            table: "Users",
            column: "Username",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Winners_DrawId_TicketId",
            table: "Winners",
            columns: new[] { "DrawId", "TicketId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Winners_TicketId",
            table: "Winners",
            column: "TicketId");

        migrationBuilder.CreateIndex(
            name: "IX_Winners_UserId",
            table: "Winners",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Winners_WonAt",
            table: "Winners",
            column: "WonAt",
            descending: new bool[0]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Winners");

        migrationBuilder.DropTable(
            name: "Draws");

        migrationBuilder.DropTable(
            name: "Tickets");

        migrationBuilder.DropTable(
            name: "LotterySessions");

        migrationBuilder.DropTable(
            name: "Users");
    }
}
