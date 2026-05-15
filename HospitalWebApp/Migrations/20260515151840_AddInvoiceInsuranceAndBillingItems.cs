using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceInsuranceAndBillingItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppointmentId",
                table: "HealthRecords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DoctorId",
                table: "HealthRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceCoveragePercent",
                table: "BillingInvoices",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceNumber",
                table: "BillingInvoices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InsuranceScheme",
                table: "BillingInvoices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PaymentConfirmed",
                table: "BillingInvoices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "BillingInvoices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DischargeDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<string>(type: "TEXT", nullable: false),
                    UploadedById = table.Column<string>(type: "TEXT", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DischargeDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DischargeDocuments_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DischargeDocuments_AspNetUsers_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Drugs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", nullable: false),
                    StockQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drugs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QueueEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<string>(type: "TEXT", nullable: false),
                    AssignedById = table.Column<string>(type: "TEXT", nullable: false),
                    QueueType = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    AssignedDoctorId = table.Column<string>(type: "TEXT", nullable: true),
                    Room = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueueEntries_AspNetUsers_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QueueEntries_AspNetUsers_AssignedDoctorId",
                        column: x => x.AssignedDoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QueueEntries_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCatalogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCatalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TriageEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<string>(type: "TEXT", nullable: false),
                    PatientName = table.Column<string>(type: "TEXT", nullable: false),
                    PatientEmail = table.Column<string>(type: "TEXT", nullable: false),
                    PatientPhone = table.Column<string>(type: "TEXT", nullable: false),
                    PatientIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    NurseId = table.Column<string>(type: "TEXT", nullable: false),
                    TriageDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BloodPressure = table.Column<string>(type: "TEXT", nullable: false),
                    TemperatureC = table.Column<double>(type: "REAL", nullable: false),
                    HeartRate = table.Column<int>(type: "INTEGER", nullable: false),
                    RespiratoryRate = table.Column<int>(type: "INTEGER", nullable: false),
                    OxygenSaturation = table.Column<int>(type: "INTEGER", nullable: false),
                    WeightKg = table.Column<double>(type: "REAL", nullable: false),
                    HeightCm = table.Column<double>(type: "REAL", nullable: false),
                    Symptoms = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    PrimaryConcern = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    NextStep = table.Column<string>(type: "TEXT", nullable: false),
                    AssignedDoctorId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TriageEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TriageEntries_AspNetUsers_AssignedDoctorId",
                        column: x => x.AssignedDoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TriageEntries_AspNetUsers_NurseId",
                        column: x => x.NurseId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TriageEntries_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarePlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<string>(type: "TEXT", nullable: false),
                    TriageEntryId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedById = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Instructions = table.Column<string>(type: "TEXT", nullable: false),
                    Goals = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ReviewDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarePlans_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CarePlans_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CarePlans_TriageEntries_TriageEntryId",
                        column: x => x.TriageEntryId,
                        principalTable: "TriageEntries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthRecords_AppointmentId",
                table: "HealthRecords",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthRecords_DoctorId",
                table: "HealthRecords",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_CarePlans_CreatedById",
                table: "CarePlans",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CarePlans_PatientId",
                table: "CarePlans",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_CarePlans_TriageEntryId",
                table: "CarePlans",
                column: "TriageEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_DischargeDocuments_PatientId",
                table: "DischargeDocuments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_DischargeDocuments_UploadedById",
                table: "DischargeDocuments",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_AssignedById",
                table: "QueueEntries",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_AssignedDoctorId",
                table: "QueueEntries",
                column: "AssignedDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_PatientId",
                table: "QueueEntries",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_TriageEntries_AssignedDoctorId",
                table: "TriageEntries",
                column: "AssignedDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_TriageEntries_NurseId",
                table: "TriageEntries",
                column: "NurseId");

            migrationBuilder.CreateIndex(
                name: "IX_TriageEntries_PatientId",
                table: "TriageEntries",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_HealthRecords_Appointments_AppointmentId",
                table: "HealthRecords",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HealthRecords_AspNetUsers_DoctorId",
                table: "HealthRecords",
                column: "DoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HealthRecords_Appointments_AppointmentId",
                table: "HealthRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_HealthRecords_AspNetUsers_DoctorId",
                table: "HealthRecords");

            migrationBuilder.DropTable(
                name: "CarePlans");

            migrationBuilder.DropTable(
                name: "DischargeDocuments");

            migrationBuilder.DropTable(
                name: "Drugs");

            migrationBuilder.DropTable(
                name: "QueueEntries");

            migrationBuilder.DropTable(
                name: "ServiceCatalogs");

            migrationBuilder.DropTable(
                name: "TriageEntries");

            migrationBuilder.DropIndex(
                name: "IX_HealthRecords_AppointmentId",
                table: "HealthRecords");

            migrationBuilder.DropIndex(
                name: "IX_HealthRecords_DoctorId",
                table: "HealthRecords");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "HealthRecords");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "HealthRecords");

            migrationBuilder.DropColumn(
                name: "InsuranceCoveragePercent",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "InsuranceNumber",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "InsuranceScheme",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "PaymentConfirmed",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "BillingInvoices");
        }
    }
}
