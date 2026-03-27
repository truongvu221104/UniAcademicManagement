using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniAcademic.Infrastructure.Persistence.Migrations
{
    public partial class AddCourseChatSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SenderDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SenderRole = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MessageText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseChatMessages_CourseOfferings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "CourseOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseChatMessages_Users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseChatMessages_CourseOfferingId",
                table: "CourseChatMessages",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseChatMessages_CourseOfferingId_CreatedAtUtc",
                table: "CourseChatMessages",
                columns: new[] { "CourseOfferingId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseChatMessages_SenderUserId",
                table: "CourseChatMessages",
                column: "SenderUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseChatMessages");
        }
    }
}
