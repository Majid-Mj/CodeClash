using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeClash.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBattleIdToTournamentMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BattleId",
                table: "TournamentMatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_BattleId",
                table: "TournamentMatches",
                column: "BattleId");

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentMatches_Battles_BattleId",
                table: "TournamentMatches",
                column: "BattleId",
                principalTable: "Battles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TournamentMatches_Battles_BattleId",
                table: "TournamentMatches");

            migrationBuilder.DropIndex(
                name: "IX_TournamentMatches_BattleId",
                table: "TournamentMatches");

            migrationBuilder.DropColumn(
                name: "BattleId",
                table: "TournamentMatches");
        }
    }
}
