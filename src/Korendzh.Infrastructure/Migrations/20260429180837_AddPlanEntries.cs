using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Korendzh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PlannedHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanEntries_WorkDate",
                table: "PlanEntries",
                column: "WorkDate");

            migrationBuilder.CreateIndex(
                name: "IX_PlanEntries_WorkerId_WorkDate",
                table: "PlanEntries",
                columns: new[] { "WorkerId", "WorkDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanEntries");
        }
    }
}
