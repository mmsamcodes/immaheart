using System;
using HospitalWebApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalWebApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261122150000_AddOnlinePharmacy")]
public partial class AddOnlinePharmacy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "RequiresPrescription",
            table: "Drugs",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "PharmacyOrders",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                OrderNumber = table.Column<string>(type: "TEXT", nullable: false),
                PatientId = table.Column<string>(type: "TEXT", nullable: false),
                PlacedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", nullable: false),
                PatientNote = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                PrescriptionDocument = table.Column<byte[]>(type: "BLOB", nullable: true),
                PrescriptionContentType = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PharmacyOrders", x => x.Id);
                table.ForeignKey(
                    name: "FK_PharmacyOrders_AspNetUsers_PatientId",
                    column: x => x.PatientId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PharmacyOrderItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                PharmacyOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                DrugId = table.Column<int>(type: "INTEGER", nullable: false),
                DrugName = table.Column<string>(type: "TEXT", nullable: false),
                Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                RequiresPrescription = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PharmacyOrderItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_PharmacyOrderItems_PharmacyOrders_PharmacyOrderId",
                    column: x => x.PharmacyOrderId,
                    principalTable: "PharmacyOrders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PharmacyOrderItems_PharmacyOrderId",
            table: "PharmacyOrderItems",
            column: "PharmacyOrderId");
        migrationBuilder.CreateIndex(
            name: "IX_PharmacyOrders_OrderNumber",
            table: "PharmacyOrders",
            column: "OrderNumber",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_PharmacyOrders_PatientId",
            table: "PharmacyOrders",
            column: "PatientId");
        migrationBuilder.CreateIndex(
            name: "IX_PharmacyOrders_Status",
            table: "PharmacyOrders",
            column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PharmacyOrderItems");
        migrationBuilder.DropTable(name: "PharmacyOrders");
        migrationBuilder.DropColumn(name: "RequiresPrescription", table: "Drugs");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        new SnapshotAccessor().Apply(modelBuilder);
    }

    private sealed class SnapshotAccessor : ApplicationDbContextModelSnapshot
    {
        public void Apply(ModelBuilder modelBuilder) => BuildModel(modelBuilder);
    }
}
