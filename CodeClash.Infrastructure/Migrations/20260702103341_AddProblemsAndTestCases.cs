using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeClash.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProblemsAndTestCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Problems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Difficulty = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StatementMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConstraintsJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    AllowedLanguagesJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    TimeLimitMs = table.Column<int>(type: "int", nullable: false, defaultValue: 2000),
                    MemoryLimitMb = table.Column<int>(type: "int", nullable: false, defaultValue: 256),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Problems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProblemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Input = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedOutput = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestCases_Problems_ProblemId",
                        column: x => x.ProblemId,
                        principalTable: "Problems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Problems_Category",
                table: "Problems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_Difficulty",
                table: "Problems",
                column: "Difficulty");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_IsActive",
                table: "Problems",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_IsActive_Difficulty",
                table: "Problems",
                columns: new[] { "IsActive", "Difficulty" });

            migrationBuilder.CreateIndex(
                name: "IX_Problems_Slug",
                table: "Problems",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Problems_Title",
                table: "Problems",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestCases_IsHidden",
                table: "TestCases",
                column: "IsHidden");

            migrationBuilder.CreateIndex(
                name: "IX_TestCases_ProblemId",
                table: "TestCases",
                column: "ProblemId");

            migrationBuilder.CreateIndex(
                name: "IX_TestCases_ProblemId_Order",
                table: "TestCases",
                columns: new[] { "ProblemId", "OrderIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestCases");

            migrationBuilder.DropTable(
                name: "Problems");
        }
    }
}
