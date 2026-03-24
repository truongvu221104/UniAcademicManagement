using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniAcademic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGradesManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GradeCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingRosterSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeCategories_CourseOfferingRosterSnapshots_CourseOfferingRosterSnapshotId",
                        column: x => x.CourseOfferingRosterSnapshotId,
                        principalTable: "CourseOfferingRosterSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradeCategories_CourseOfferings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "CourseOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RosterItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeEntries_CourseOfferingRosterItems_RosterItemId",
                        column: x => x.RosterItemId,
                        principalTable: "CourseOfferingRosterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradeEntries_GradeCategories_GradeCategoryId",
                        column: x => x.GradeCategoryId,
                        principalTable: "GradeCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GradeCategories_CourseOfferingId",
                table: "GradeCategories",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeCategories_CourseOfferingId_Name",
                table: "GradeCategories",
                columns: new[] { "CourseOfferingId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeCategories_CourseOfferingRosterSnapshotId",
                table: "GradeCategories",
                column: "CourseOfferingRosterSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeEntries_GradeCategoryId_RosterItemId",
                table: "GradeEntries",
                columns: new[] { "GradeCategoryId", "RosterItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeEntries_RosterItemId",
                table: "GradeEntries",
                column: "RosterItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GradeEntries");

            migrationBuilder.DropTable(
                name: "GradeCategories");
        }
    }
}
