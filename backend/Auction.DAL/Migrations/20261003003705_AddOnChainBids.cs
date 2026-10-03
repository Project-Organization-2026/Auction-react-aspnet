using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auction.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddOnChainBids : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bids_Users_UserId",
                table: "Bids");

            migrationBuilder.AddColumn<string>(
                name: "WalletAddress",
                table: "Users",
                type: "character varying(42)",
                maxLength: 42,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractAddress",
                table: "Lots",
                type: "character varying(42)",
                maxLength: 42,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentPriceEth",
                table: "Lots",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Bids",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountEth",
                table: "Bids",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "Bids",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TxHash",
                table: "Bids",
                type: "character varying(66)",
                maxLength: 66,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WalletAddress",
                table: "Bids",
                type: "character varying(42)",
                maxLength: 42,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bids_TxHash",
                table: "Bids",
                column: "TxHash",
                unique: true,
                filter: "\"TxHash\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Bids_Users_UserId",
                table: "Bids",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bids_Users_UserId",
                table: "Bids");

            migrationBuilder.DropIndex(
                name: "IX_Bids_TxHash",
                table: "Bids");

            migrationBuilder.DropColumn(
                name: "WalletAddress",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ContractAddress",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "CurrentPriceEth",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "AmountEth",
                table: "Bids");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Bids");

            migrationBuilder.DropColumn(
                name: "TxHash",
                table: "Bids");

            migrationBuilder.DropColumn(
                name: "WalletAddress",
                table: "Bids");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Bids",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bids_Users_UserId",
                table: "Bids",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
