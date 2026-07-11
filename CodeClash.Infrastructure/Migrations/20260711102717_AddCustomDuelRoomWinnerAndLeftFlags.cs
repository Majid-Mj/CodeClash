using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeClash.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomDuelRoomWinnerAndLeftFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasFriendLeft",
                table: "CustomDuelRooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasHostLeft",
                table: "CustomDuelRooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "WinnerId",
                table: "CustomDuelRooms",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomDuelRooms_WinnerId",
                table: "CustomDuelRooms",
                column: "WinnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomDuelRooms_Users_WinnerId",
                table: "CustomDuelRooms",
                column: "WinnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomDuelRooms_Users_WinnerId",
                table: "CustomDuelRooms");

            migrationBuilder.DropIndex(
                name: "IX_CustomDuelRooms_WinnerId",
                table: "CustomDuelRooms");

            migrationBuilder.DropColumn(
                name: "HasFriendLeft",
                table: "CustomDuelRooms");

            migrationBuilder.DropColumn(
                name: "HasHostLeft",
                table: "CustomDuelRooms");

            migrationBuilder.DropColumn(
                name: "WinnerId",
                table: "CustomDuelRooms");
        }
    }
}
