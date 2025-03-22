using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Vacation_Manager.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1de8bf05-ef14-48cc-86fa-23574e8cac02");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2d86ac89-3379-4457-81c8-ce7ef8837f57");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "4ff24582-e0ff-4b50-883e-e7a104b98ff8");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ae134a1d-51c4-402d-bb5c-271ba55d0c45");

            migrationBuilder.AddColumn<string>(
                name: "AttachmentFilePath",
                table: "VacationRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "3682947e-8faa-43ee-a471-0221991d5e25", null, "CEO", "CEO" },
                    { "59ec85a0-29c6-424a-a66c-932dc8b3d505", null, "Developer", "DEVELOPER" },
                    { "ac752969-22f5-438c-92d5-67fa02687b78", null, "Team Lead", "TEAM LEAD" },
                    { "c9668154-c9a9-4936-8074-4f45592ea0a6", null, "Unassigned", "UNASSIGNED" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3682947e-8faa-43ee-a471-0221991d5e25");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "59ec85a0-29c6-424a-a66c-932dc8b3d505");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ac752969-22f5-438c-92d5-67fa02687b78");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "c9668154-c9a9-4936-8074-4f45592ea0a6");

            migrationBuilder.DropColumn(
                name: "AttachmentFilePath",
                table: "VacationRequests");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "1de8bf05-ef14-48cc-86fa-23574e8cac02", null, "CEO", "CEO" },
                    { "2d86ac89-3379-4457-81c8-ce7ef8837f57", null, "Unassigned", "UNASSIGNED" },
                    { "4ff24582-e0ff-4b50-883e-e7a104b98ff8", null, "Developer", "DEVELOPER" },
                    { "ae134a1d-51c4-402d-bb5c-271ba55d0c45", null, "Team Lead", "TEAM LEAD" }
                });
        }
    }
}
