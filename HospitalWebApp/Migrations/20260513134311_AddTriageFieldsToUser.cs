using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTriageFieldsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserType",
                table: "AspNetUsers",
                newName: "IDNumber");

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BloodGroup",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Height",
                table: "AspNetUsers",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Weight",
                table: "AspNetUsers",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BloodGroup",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "IDNumber",
                table: "AspNetUsers",
                newName: "UserType");
        }
    }
}
