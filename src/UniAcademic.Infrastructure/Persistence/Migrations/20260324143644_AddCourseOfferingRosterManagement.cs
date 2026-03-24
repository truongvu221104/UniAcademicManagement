using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniAcademic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseOfferingRosterManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRosterFinalized",
                table: "CourseOfferings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RosterFinalizedAtUtc",
                table: "CourseOfferings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CourseOfferingRosterSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinalizedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinalizedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseOfferingRosterSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseOfferingRosterSnapshots_CourseOfferings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "CourseOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseOfferingRosterItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RosterSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StudentFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StudentClassName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CourseOfferingCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CourseCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SemesterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseOfferingRosterItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseOfferingRosterItems_CourseOfferingRosterSnapshots_RosterSnapshotId",
                        column: x => x.RosterSnapshotId,
                        principalTable: "CourseOfferingRosterSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingRosterItems_EnrollmentId",
                table: "CourseOfferingRosterItems",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingRosterItems_RosterSnapshotId",
                table: "CourseOfferingRosterItems",
                column: "RosterSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingRosterItems_StudentProfileId",
                table: "CourseOfferingRosterItems",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingRosterSnapshots_CourseOfferingId",
                table: "CourseOfferingRosterSnapshots",
                column: "CourseOfferingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseOfferingRosterItems");

            migrationBuilder.DropTable(
                name: "CourseOfferingRosterSnapshots");

            migrationBuilder.DropColumn(
                name: "IsRosterFinalized",
                table: "CourseOfferings");

            migrationBuilder.DropColumn(
                name: "RosterFinalizedAtUtc",
                table: "CourseOfferings");
        }
    }
}
