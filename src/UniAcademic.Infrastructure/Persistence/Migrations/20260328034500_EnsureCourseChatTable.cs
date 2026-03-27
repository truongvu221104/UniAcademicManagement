using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UniAcademic.Infrastructure.Persistence;

#nullable disable

namespace UniAcademic.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260328034500_EnsureCourseChatTable")]
    public partial class EnsureCourseChatTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[CourseChatMessages]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[CourseChatMessages]
                    (
                        [Id] uniqueidentifier NOT NULL,
                        [CourseOfferingId] uniqueidentifier NOT NULL,
                        [SenderUserId] uniqueidentifier NOT NULL,
                        [SenderUsername] nvarchar(100) NOT NULL,
                        [SenderDisplayName] nvarchar(200) NOT NULL,
                        [SenderRole] nvarchar(30) NOT NULL,
                        [MessageText] nvarchar(2000) NOT NULL,
                        [RowVersion] rowversion NOT NULL,
                        [CreatedAtUtc] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(100) NULL,
                        [ModifiedAtUtc] datetime2 NULL,
                        [ModifiedBy] nvarchar(100) NULL,
                        CONSTRAINT [PK_CourseChatMessages] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_CourseChatMessages_CourseOfferings_CourseOfferingId] FOREIGN KEY ([CourseOfferingId]) REFERENCES [dbo].[CourseOfferings]([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_CourseChatMessages_Users_SenderUserId] FOREIGN KEY ([SenderUserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION
                    );
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CourseChatMessages_CourseOfferingId' AND object_id = OBJECT_ID(N'[dbo].[CourseChatMessages]'))
                BEGIN
                    CREATE INDEX [IX_CourseChatMessages_CourseOfferingId]
                        ON [dbo].[CourseChatMessages]([CourseOfferingId]);
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CourseChatMessages_CourseOfferingId_CreatedAtUtc' AND object_id = OBJECT_ID(N'[dbo].[CourseChatMessages]'))
                BEGIN
                    CREATE INDEX [IX_CourseChatMessages_CourseOfferingId_CreatedAtUtc]
                        ON [dbo].[CourseChatMessages]([CourseOfferingId], [CreatedAtUtc]);
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CourseChatMessages_SenderUserId' AND object_id = OBJECT_ID(N'[dbo].[CourseChatMessages]'))
                BEGIN
                    CREATE INDEX [IX_CourseChatMessages_SenderUserId]
                        ON [dbo].[CourseChatMessages]([SenderUserId]);
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[CourseChatMessages]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [dbo].[CourseChatMessages];
                END;
                """);
        }
    }
}
