using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicalModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Author",
                table: "BlogPosts",
                newName: "Category");

            migrationBuilder.AddColumn<string>(
                name: "AuthorId",
                table: "BlogPosts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateUpdated",
                table: "BlogPosts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "BlogPosts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Admissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<string>(type: "TEXT", nullable: false),
                    AdmittedById = table.Column<string>(type: "TEXT", nullable: false),
                    Ward = table.Column<string>(type: "TEXT", nullable: false),
                    Bed = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    AdmissionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DischargeDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Admissions_AspNetUsers_AdmittedById",
                        column: x => x.AdmittedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Admissions_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillingInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvoiceNumber = table.Column<string>(type: "TEXT", nullable: false),
                    PatientId = table.Column<string>(type: "TEXT", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingInvoices_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<string>(type: "TEXT", nullable: false),
                    DoctorId = table.Column<string>(type: "TEXT", nullable: false),
                    TestName = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    Cost = table.Column<decimal>(type: "TEXT", nullable: false),
                    OrderedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResultSummary = table.Column<string>(type: "TEXT", nullable: false),
                    ResultFileName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabOrders_AspNetUsers_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LabOrders_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillingItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingItems_BillingInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "BillingInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthRecords_PatientId",
                table: "HealthRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationMessages_ReceiverId",
                table: "ConsultationMessages",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationMessages_SenderId",
                table: "ConsultationMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_AuthorId",
                table: "BlogPosts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_AdmittedById",
                table: "Admissions",
                column: "AdmittedById");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_PatientId",
                table: "Admissions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_PatientId",
                table: "BillingInvoices",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingItems_InvoiceId",
                table: "BillingItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_DoctorId",
                table: "LabOrders",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_PatientId",
                table: "LabOrders",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_BlogPosts_AspNetUsers_AuthorId",
                table: "BlogPosts",
                column: "AuthorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationMessages_AspNetUsers_ReceiverId",
                table: "ConsultationMessages",
                column: "ReceiverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationMessages_AspNetUsers_SenderId",
                table: "ConsultationMessages",
                column: "SenderId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HealthRecords_AspNetUsers_PatientId",
                table: "HealthRecords",
                column: "PatientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlogPosts_AspNetUsers_AuthorId",
                table: "BlogPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationMessages_AspNetUsers_ReceiverId",
                table: "ConsultationMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationMessages_AspNetUsers_SenderId",
                table: "ConsultationMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_HealthRecords_AspNetUsers_PatientId",
                table: "HealthRecords");

            migrationBuilder.DropTable(
                name: "Admissions");

            migrationBuilder.DropTable(
                name: "BillingItems");

            migrationBuilder.DropTable(
                name: "LabOrders");

            migrationBuilder.DropTable(
                name: "BillingInvoices");

            migrationBuilder.DropIndex(
                name: "IX_HealthRecords_PatientId",
                table: "HealthRecords");

            migrationBuilder.DropIndex(
                name: "IX_ConsultationMessages_ReceiverId",
                table: "ConsultationMessages");

            migrationBuilder.DropIndex(
                name: "IX_ConsultationMessages_SenderId",
                table: "ConsultationMessages");

            migrationBuilder.DropIndex(
                name: "IX_BlogPosts_AuthorId",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "DateUpdated",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "BlogPosts");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "BlogPosts",
                newName: "Author");
        }
    }
}
