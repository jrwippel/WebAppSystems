using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class AlterTableClients : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Adress",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "CellPhone",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "ComlPhone",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "Genre",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "MaritalStatus",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "TypePerson",
                table: "Client");

            migrationBuilder.RenameColumn(
                name: "Observation",
                table: "Client",
                newName: "Telephone");

            migrationBuilder.RenameColumn(
                name: "NumberDoc",
                table: "Client",
                newName: "Document");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Telephone",
                table: "Client",
                newName: "Observation");

            migrationBuilder.RenameColumn(
                name: "Document",
                table: "Client",
                newName: "NumberDoc");

            migrationBuilder.AddColumn<string>(
                name: "Adress",
                table: "Client",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "Client",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CellPhone",
                table: "Client",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ComlPhone",
                table: "Client",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Genre",
                table: "Client",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaritalStatus",
                table: "Client",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TypePerson",
                table: "Client",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
