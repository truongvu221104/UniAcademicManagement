using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniAcademic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseOfferingRosterItemReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_CourseOfferingRosterItems_Enrollments_EnrollmentId",
                table: "CourseOfferingRosterItems",
                column: "EnrollmentId",
                principalTable: "Enrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseOfferingRosterItems_StudentProfiles_StudentProfileId",
                table: "CourseOfferingRosterItems",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseOfferingRosterItems_Enrollments_EnrollmentId",
                table: "CourseOfferingRosterItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseOfferingRosterItems_StudentProfiles_StudentProfileId",
                table: "CourseOfferingRosterItems");
        }
    }
}
