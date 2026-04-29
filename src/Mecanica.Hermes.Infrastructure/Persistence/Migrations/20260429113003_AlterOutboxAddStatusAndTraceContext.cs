using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mecanica.Hermes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlterOutboxAddStatusAndTraceContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_OccurredOn_Pending",
                table: "OutboxMessages");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TraceContext",
                table: "OutboxMessages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_OccurredOn_Pending",
                table: "OutboxMessages",
                column: "OccurredOn",
                filter: "\"Status\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_OccurredOn_Pending",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "TraceContext",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_OccurredOn_Pending",
                table: "OutboxMessages",
                column: "OccurredOn",
                filter: "\"ProcessedOn\" IS NULL");
        }
    }
}
