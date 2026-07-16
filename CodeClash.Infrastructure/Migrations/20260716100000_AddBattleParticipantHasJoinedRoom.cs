using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using CodeClash.Infrastructure.Persistence;

#nullable disable

namespace CodeClash.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260716100000_AddBattleParticipantHasJoinedRoom")]
    /// <inheritdoc />
    public partial class AddBattleParticipantHasJoinedRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasJoinedRoom",
                table: "BattleParticipants",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasJoinedRoom",
                table: "BattleParticipants");
        }
    }
}
