using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentsDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class initial_new_domain_model : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payments");

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

            migrationBuilder.CreateTable(
                name: "Merchants",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_merchants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    contact_information_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                    table.ForeignKey(
                        name: "fk_customers_contact_detail_value_object_contact_information_id",
                        column: x => x.contact_information_id,
                        principalTable: "contact_detail_value_object",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubMerchants",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    merchant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<Guid>(type: "uuid", maxLength: 15, nullable: false),
                    trading_name = table.Column<string>(type: "character varying(19)", maxLength: 19, nullable: false),
                    AddressCity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AddressRegionCode = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    AddressCountryCode = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    merchant_category_code = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sub_merchants", x => x.id);
                    table.UniqueConstraint("ak_sub_merchants_code", x => x.code);
                    table.ForeignKey(
                        name: "fk_sub_merchants_merchants_merchant_id",
                        column: x => x.merchant_id,
                        principalSchema: "payments",
                        principalTable: "Merchants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankCards",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", maxLength: 50, nullable: false),
                    card_number = table.Column<string>(type: "text", nullable: false),
                    cvc = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    expiry_day = table.Column<int>(type: "integer", nullable: false),
                    expiry_month = table.Column<int>(type: "integer", nullable: false),
                    expiry_year = table.Column<int>(type: "integer", nullable: false),
                    card_status = table.Column<int>(type: "integer", nullable: false),
                    name_on_card = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    card_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_cards", x => x.id);
                    table.CheckConstraint("CK_BankCards_ExpiryMonth", "\"expiry_month\" BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "fk_bank_cards_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "payments",
                        principalTable: "Customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sub_merchant_code = table.Column<Guid>(type: "uuid", maxLength: 15, nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    idempotency_key = table.Column<Guid>(type: "uuid", fixedLength: true, maxLength: 36, nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", maxLength: 50, nullable: false),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    retrieval_reference_number = table.Column<string>(type: "character(12)", fixedLength: true, maxLength: 12, nullable: false),
                    request_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    acme_envelope_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    enqueued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acme_response_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    recon_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    auth_code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    auth_amount = table.Column<long>(type: "bigint", nullable: true),
                    refunded_amount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.CheckConstraint("CK_Payments_Amount_Positive", "\"amount\" > 0");
                    table.CheckConstraint("CK_Payments_Refunded_Range", "\"refunded_amount\" >= 0");
                    table.ForeignKey(
                        name: "fk_payments_sub_merchants_sub_merchant_code",
                        column: x => x.sub_merchant_code,
                        principalSchema: "payments",
                        principalTable: "SubMerchants",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAdjustments",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_adjustments", x => x.id);
                    table.CheckConstraint("CK_PaymentAdjustments_Amount_Positive", "\"amount\" > 0");
                    table.ForeignKey(
                        name: "fk_payment_adjustments_payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "payments",
                        principalTable: "Payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankCards_Customer_Active",
                schema: "payments",
                table: "BankCards",
                columns: new[] { "customer_id", "card_status" });

            migrationBuilder.CreateIndex(
                name: "UX_BankCards_CardToken",
                schema: "payments",
                table: "BankCards",
                column: "card_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_contact_information_id",
                schema: "payments",
                table: "Customers",
                column: "contact_information_id");

            migrationBuilder.CreateIndex(
                name: "UX_PaymentAdjustments_Payment_Event",
                schema: "payments",
                table: "PaymentAdjustments",
                columns: new[] { "payment_id", "event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Card_Status",
                schema: "payments",
                table: "Payments",
                columns: new[] { "card_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Customer_CreatedAt",
                schema: "payments",
                table: "Payments",
                columns: new[] { "customer_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_sub_merchant_code",
                schema: "payments",
                table: "Payments",
                column: "sub_merchant_code");

            migrationBuilder.CreateIndex(
                name: "UX_Payments_IdempotencyKey",
                schema: "payments",
                table: "Payments",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Payments_RetrievalReferenceNumber",
                schema: "payments",
                table: "Payments",
                column: "retrieval_reference_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Payments_TransactionId",
                schema: "payments",
                table: "Payments",
                column: "transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sub_merchants_merchant_id",
                schema: "payments",
                table: "SubMerchants",
                column: "merchant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankCards",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "PaymentAdjustments",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "contact_detail_value_object");

            migrationBuilder.DropTable(
                name: "SubMerchants",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "Merchants",
                schema: "payments");
        }
    }
}
