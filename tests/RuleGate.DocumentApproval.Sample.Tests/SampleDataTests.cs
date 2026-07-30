using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RuleGate.DocumentApproval.Api.Data;

namespace RuleGate.DocumentApproval.Sample.Tests;

public sealed class SampleDataTests
{
    [Fact]
    public async Task Initialize_adds_schedule_table_without_replacing_existing_sample_data()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await CreateLegacySchemaAsync(connection);
        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var database = new SampleDbContext(options);

        await SampleData.InitializeAsync(database);

        Assert.Equal(2, await database.OrganizationSchedules.CountAsync());
        Assert.True(
            await database.Documents.AnyAsync(
                document => document.Title == "Existing document"));
        Assert.True(
            await database.Documents.AnyAsync(
                document => document.Title == "Confidential legal opinion"));
    }

    private static async Task CreateLegacySchemaAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE "UserProfiles" (
                "Username" TEXT NOT NULL CONSTRAINT "PK_UserProfiles" PRIMARY KEY,
                "DisplayName" TEXT NOT NULL,
                "OrganizationId" TEXT NOT NULL,
                "Clearance" TEXT NOT NULL
            );

            CREATE TABLE "Documents" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Documents" PRIMARY KEY AUTOINCREMENT,
                "Title" TEXT NOT NULL,
                "OwnerUsername" TEXT NOT NULL,
                "OrganizationId" TEXT NOT NULL,
                "Classification" TEXT NOT NULL,
                "Status" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL
            );

            INSERT INTO "UserProfiles"
                ("Username", "DisplayName", "OrganizationId", "Clearance")
            VALUES
                ('existing-user', 'Existing User', 'records', 'internal');

            INSERT INTO "Documents"
                ("Title", "OwnerUsername", "OrganizationId", "Classification", "Status", "UpdatedAt")
            VALUES
                ('Existing document', 'existing-user', 'records', 'internal', 'draft', '2026-07-30 00:00:00+00:00');
            """;
        await command.ExecuteNonQueryAsync();
    }
}
