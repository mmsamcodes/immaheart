using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAndVideoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppointmentId",
                table: "BillingInvoices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceId",
                table: "Appointments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PaymentConfirmed",
                table: "Appointments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Appointments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "Appointments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VideoJoinUrl",
                table: "Appointments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoSessionId",
                table: "Appointments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VirtualConfirmedByDoctor",
                table: "Appointments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_AppointmentId",
                table: "BillingInvoices",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_InvoiceId",
                table: "Appointments",
                column: "InvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_BillingInvoices_InvoiceId",
                table: "Appointments",
                column: "InvoiceId",
                principalTable: "BillingInvoices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingInvoices_Appointments_AppointmentId",
                table: "BillingInvoices",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_BillingInvoices_InvoiceId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_BillingInvoices_Appointments_AppointmentId",
                table: "BillingInvoices");

            migrationBuilder.DropIndex(
                name: "IX_BillingInvoices_AppointmentId",
                table: "BillingInvoices");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_InvoiceId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PaymentConfirmed",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "VideoJoinUrl",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "VideoSessionId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "VirtualConfirmedByDoctor",
                table: "Appointments");
        }
    }
}
