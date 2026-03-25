using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniAcademic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebEndUserAcademicUserMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LecturerProfileId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StudentProfileId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_LecturerProfileId",
                table: "Users",
                column: "LecturerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_StudentProfileId",
                table: "Users",
                column: "StudentProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_LecturerProfiles_LecturerProfileId",
                table: "Users",
                column: "LecturerProfileId",
                principalTable: "LecturerProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_StudentProfiles_StudentProfileId",
                table: "Users",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_LecturerProfiles_LecturerProfileId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_StudentProfiles_StudentProfileId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_LecturerProfileId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_StudentProfileId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LecturerProfileId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StudentProfileId",
                table: "Users");
        }
    }
}
