using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalErpWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PharmacyOrders_Status",
                table: "PharmacyOrders");

            migrationBuilder.AddColumn<int>(
                name: "PatientEncounterId",
                table: "TriageEntries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatientEncounterId",
                table: "QueueEntries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BillingInvoiceId",
                table: "PharmacyOrders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatientEncounterId",
                table: "PharmacyOrders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrescriberId",
                table: "PharmacyOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrescriptionText",
                table: "PharmacyOrders",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatientEncounterId",
                table: "LabOrders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LabOrderId",
                table: "BillingItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatientServiceId",
                table: "BillingItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatientEncounterId",
                table: "BillingInvoices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillingPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "TEXT", nullable: false),
                    Method = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    GatewayReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RequestKey = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReceivedById = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingPayments_AspNetUsers_ReceivedById",
                        column: x => x.ReceivedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BillingPayments_BillingInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "BillingInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CareNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<string>(type: "TEXT", nullable: false),
                    AddedById = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<string>(type: "TEXT", nullable: false),
                    IsUrgent = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareNotes_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CareNotes_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientEncounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VisitNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PatientId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CurrentDepartment = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssignedClinicianId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientEncounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientEncounters_AspNetUsers_AssignedClinicianId",
                        column: x => x.AssignedClinicianId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PatientEncounters_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientEncounterEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientEncounterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Department = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    PatientMessage = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChangedById = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientEncounterEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientEncounterEvents_AspNetUsers_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PatientEncounterEvents_PatientEncounters_PatientEncounterId",
                        column: x => x.PatientEncounterId,
                        principalTable: "PatientEncounters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<string>(type: "TEXT", nullable: false),
                    ServiceCatalogId = table.Column<int>(type: "INTEGER", nullable: false),
                    PatientEncounterId = table.Column<int>(type: "INTEGER", nullable: true),
                    AssignedById = table.Column<string>(type: "TEXT", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UnitPriceAtOrder = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientServices_AspNetUsers_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientServices_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientServices_PatientEncounters_PatientEncounterId",
                        column: x => x.PatientEncounterId,
                        principalTable: "PatientEncounters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PatientServices_ServiceCatalogs_ServiceCatalogId",
                        column: x => x.ServiceCatalogId,
                        principalTable: "ServiceCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TriageEntries_PatientEncounterId",
                table: "TriageEntries",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_PatientEncounterId",
                table: "QueueEntries",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyOrders_BillingInvoiceId",
                table: "PharmacyOrders",
                column: "BillingInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyOrders_PatientEncounterId",
                table: "PharmacyOrders",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyOrders_PrescriberId",
                table: "PharmacyOrders",
                column: "PrescriberId");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_PatientEncounterId",
                table: "LabOrders",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingItems_LabOrderId",
                table: "BillingItems",
                column: "LabOrderId",
                unique: true,
                filter: "\"LabOrderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BillingItems_PatientServiceId",
                table: "BillingItems",
                column: "PatientServiceId",
                unique: true,
                filter: "\"PatientServiceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_PatientEncounterId",
                table: "BillingInvoices",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_GatewayReference",
                table: "BillingPayments",
                column: "GatewayReference",
                unique: true,
                filter: "\"GatewayReference\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_InvoiceId",
                table: "BillingPayments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_ReceiptNumber",
                table: "BillingPayments",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_ReceivedById",
                table: "BillingPayments",
                column: "ReceivedById");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_RequestKey",
                table: "BillingPayments",
                column: "RequestKey",
                unique: true,
                filter: "\"RequestKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CareNotes_AddedById",
                table: "CareNotes",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_CareNotes_PatientId",
                table: "CareNotes",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientEncounterEvents_ChangedById",
                table: "PatientEncounterEvents",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_PatientEncounterEvents_PatientEncounterId",
                table: "PatientEncounterEvents",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientEncounters_AssignedClinicianId",
                table: "PatientEncounters",
                column: "AssignedClinicianId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientEncounters_PatientId",
                table: "PatientEncounters",
                column: "PatientId",
                unique: true,
                filter: "\"ClosedAt\" IS NULL AND \"Status\" <> 'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_PatientEncounters_VisitNumber",
                table: "PatientEncounters",
                column: "VisitNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientServices_AssignedById",
                table: "PatientServices",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_PatientServices_PatientEncounterId",
                table: "PatientServices",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientServices_PatientId",
                table: "PatientServices",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientServices_ServiceCatalogId",
                table: "PatientServices",
                column: "ServiceCatalogId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingInvoices_PatientEncounters_PatientEncounterId",
                table: "BillingInvoices",
                column: "PatientEncounterId",
                principalTable: "PatientEncounters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingItems_LabOrders_LabOrderId",
                table: "BillingItems",
                column: "LabOrderId",
                principalTable: "LabOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingItems_PatientServices_PatientServiceId",
                table: "BillingItems",
                column: "PatientServiceId",
                principalTable: "PatientServices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LabOrders_PatientEncounters_PatientEncounterId",
                table: "LabOrders",
                column: "PatientEncounterId",
                principalTable: "PatientEncounters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacyOrders_AspNetUsers_PrescriberId",
                table: "PharmacyOrders",
                column: "PrescriberId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacyOrders_BillingInvoices_BillingInvoiceId",
                table: "PharmacyOrders",
                column: "BillingInvoiceId",
                principalTable: "BillingInvoices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacyOrders_PatientEncounters_PatientEncounterId",
                table: "PharmacyOrders",
                column: "PatientEncounterId",
                principalTable: "PatientEncounters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QueueEntries_PatientEncounters_PatientEncounterId",
                table: "QueueEntries",
                column: "PatientEncounterId",
                principalTable: "PatientEncounters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TriageEntries_PatientEncounters_PatientEncounterId",
                table: "TriageEntries",
                column: "PatientEncounterId",
                principalTable: "PatientEncounters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingInvoices_PatientEncounters_PatientEncounterId",
                table: "BillingInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_BillingItems_LabOrders_LabOrderId",
                table: "BillingItems");

            migrationBuilder.DropForeignKey(
                name: "FK_BillingItems_PatientServices_PatientServiceId",
                table: "BillingItems");

            migrationBuilder.DropForeignKey(
                name: "FK_LabOrders_PatientEncounters_PatientEncounterId",
                table: "LabOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PharmacyOrders_AspNetUsers_PrescriberId",
                table: "PharmacyOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PharmacyOrders_BillingInvoices_BillingInvoiceId",
                table: "PharmacyOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PharmacyOrders_PatientEncounters_PatientEncounterId",
                table: "PharmacyOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_PatientEncounters_PatientEncounterId",
                table: "QueueEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_TriageEntries_PatientEncounters_PatientEncounterId",
                table: "TriageEntries");

            migrationBuilder.DropTable(
                name: "BillingPayments");

            migrationBuilder.DropTable(
                name: "CareNotes");

            migrationBuilder.DropTable(
                name: "PatientEncounterEvents");

            migrationBuilder.DropTable(
                name: "PatientServices");

            migrationBuilder.DropTable(
                name: "PatientEncounters");

            migrationBuilder.DropIndex(
                name: "IX_TriageEntries_PatientEncounterId",
                table: "TriageEntries");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_PatientEncounterId",
                table: "QueueEntries");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyOrders_BillingInvoiceId",
                table: "PharmacyOrders");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyOrders_PatientEncounterId",
                table: "PharmacyOrders");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyOrders_PrescriberId",
                table: "PharmacyOrders");

            migrationBuilder.DropIndex(
                name: "IX_LabOrders_PatientEncounterId",
                table: "LabOrders");

            migrationBuilder.DropIndex(
                name: "IX_BillingItems_LabOrderId",
                table: "BillingItems");

            migrationBuilder.DropIndex(
                name: "IX_BillingItems_PatientServiceId",
                table: "BillingItems");

            migrationBuilder.DropIndex(
                name: "IX_BillingInvoices_PatientEncounterId",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "PatientEncounterId",
                table: "TriageEntries");

            migrationBuilder.DropColumn(
                name: "PatientEncounterId",
                table: "QueueEntries");

            migrationBuilder.DropColumn(
                name: "BillingInvoiceId",
                table: "PharmacyOrders");

            migrationBuilder.DropColumn(
                name: "PatientEncounterId",
                table: "PharmacyOrders");

            migrationBuilder.DropColumn(
                name: "PrescriberId",
                table: "PharmacyOrders");

            migrationBuilder.DropColumn(
                name: "PrescriptionText",
                table: "PharmacyOrders");

            migrationBuilder.DropColumn(
                name: "PatientEncounterId",
                table: "LabOrders");

            migrationBuilder.DropColumn(
                name: "LabOrderId",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "PatientServiceId",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "PatientEncounterId",
                table: "BillingInvoices");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyOrders_Status",
                table: "PharmacyOrders",
                column: "Status");
        }
    }
}
