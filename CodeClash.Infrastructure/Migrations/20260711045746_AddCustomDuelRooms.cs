using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeClash.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomDuelRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BattleRecords");

            migrationBuilder.CreateTable(
                name: "CustomDuelRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HostUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FriendUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsHostReady = table.Column<bool>(type: "bit", nullable: false),
                    IsFriendReady = table.Column<bool>(type: "bit", nullable: false),
                    SelectedProblemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomDuelRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomDuelRooms_Problems_SelectedProblemId",
                        column: x => x.SelectedProblemId,
                        principalTable: "Problems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomDuelRooms_Users_FriendUserId",
                        column: x => x.FriendUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomDuelRooms_Users_HostUserId",
                        column: x => x.HostUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomDuelRooms_FriendUserId",
                table: "CustomDuelRooms",
                column: "FriendUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomDuelRooms_HostUserId",
                table: "CustomDuelRooms",
                column: "HostUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomDuelRooms_SelectedProblemId",
                table: "CustomDuelRooms",
                column: "SelectedProblemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomDuelRooms");

            migrationBuilder.CreateTable(
                name: "BattleRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EloChange = table.Column<int>(type: "int", nullable: false),
                    IsWin = table.Column<bool>(type: "bit", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OpponentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProblemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BattleRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BattleRecords_UserId",
                table: "BattleRecords",
                column: "UserId");
        }
    }
}
