using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeploymentCommandTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeploymentId",
                table: "EdgeCommands",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeploymentKind",
                table: "EdgeCommands",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EdgeCommands_DeploymentId",
                table: "EdgeCommands",
                column: "DeploymentId",
                unique: true,
                filter: "\"DeploymentId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EdgeCommands_DeploymentId",
                table: "EdgeCommands");

            migrationBuilder.DropColumn(
                name: "DeploymentId",
                table: "EdgeCommands");

            migrationBuilder.DropColumn(
                name: "DeploymentKind",
                table: "EdgeCommands");
        }
    }
}
