using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniAcademic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGradeResultFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GradeResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingRosterSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RosterItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeightedFinalScore = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PassingScore = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsPassed = table.Column<bool>(type: "bit", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CalculatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeResults_CourseOfferingRosterItems_RosterItemId",
                        column: x => x.RosterItemId,
                        principalTable: "CourseOfferingRosterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradeResults_CourseOfferingRosterSnapshots_CourseOfferingRosterSnapshotId",
                        column: x => x.CourseOfferingRosterSnapshotId,
                        principalTable: "CourseOfferingRosterSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradeResults_CourseOfferings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "CourseOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GradeResults_CourseOfferingId",
                table: "GradeResults",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeResults_CourseOfferingId_RosterItemId",
                table: "GradeResults",
                columns: new[] { "CourseOfferingId", "RosterItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeResults_CourseOfferingRosterSnapshotId",
                table: "GradeResults",
                column: "CourseOfferingRosterSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeResults_RosterItemId",
                table: "GradeResults",
                column: "RosterItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GradeResults");
        }
    }
}
