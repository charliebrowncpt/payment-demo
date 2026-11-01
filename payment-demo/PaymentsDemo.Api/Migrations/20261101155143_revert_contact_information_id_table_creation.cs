using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentsDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class revert_contact_information_id_table_creation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customers_contact_detail_value_object_contact_information_id",
                schema: "payments",
                table: "Customers");

            migrationBuilder.DropTable(
                name: "contact_detail_value_object");

            migrationBuilder.DropIndex(
                name: "ix_customers_contact_information_id",
                schema: "payments",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "contact_information_id",
                schema: "payments",
                table: "Customers");

            migrationBuilder.AddColumn<string>(
                name: "email_address",
                schema: "payments",
                table: "Customers",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "phone_number",
                schema: "payments",
                table: "Customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email_address",
                schema: "payments",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "phone_number",
                schema: "payments",
                table: "Customers");

            migrationBuilder.AddColumn<Guid>(
                name: "contact_information_id",
                schema: "payments",
                table: "Customers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "contact_detail_value_object",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_address = table.Column<string>(type: "text", nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contact_detail_value_object", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customers_contact_information_id",
                schema: "payments",
                table: "Customers",
                column: "contact_information_id");

            migrationBuilder.AddForeignKey(
                name: "fk_customers_contact_detail_value_object_contact_information_id",
                schema: "payments",
                table: "Customers",
                column: "contact_information_id",
                principalTable: "contact_detail_value_object",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
