using Auction.DAL.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auction.DAL.Migrations;

[DbContext(typeof(AuctionDbContext))]
[Migration("20260930120000_PrepareBlockchainIntegration")]
public partial class PrepareBlockchainIntegration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AmountWei",
            table: "Bids",
            type: "character varying(78)",
            maxLength: 78,
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "BlockNumber",
            table: "Bids",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TransactionHash",
            table: "Bids",
            type: "character varying(66)",
            maxLength: 66,
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "ChainId",
            table: "Lots",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ContractAddress",
            table: "Lots",
            type: "character varying(42)",
            maxLength: 42,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CreationTransactionHash",
            table: "Lots",
            type: "character varying(66)",
            maxLength: 66,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OnChainAuctionId",
            table: "Lots",
            type: "character varying(78)",
            maxLength: 78,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "SettlementMode",
            table: "Lots",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "SettlementTransactionHash",
            table: "Lots",
            type: "character varying(66)",
            maxLength: 66,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "WalletAddress",
            table: "Users",
            type: "character varying(42)",
            maxLength: 42,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "WalletVerifiedAt",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Bids_TransactionHash",
            table: "Bids",
            column: "TransactionHash",
            unique: true,
            filter: "\"TransactionHash\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Lots_ContractAddress_OnChainAuctionId",
            table: "Lots",
            columns: new[] { "ContractAddress", "OnChainAuctionId" },
            unique: true,
            filter: "\"ContractAddress\" IS NOT NULL AND \"OnChainAuctionId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Users_WalletAddress",
            table: "Users",
            column: "WalletAddress",
            unique: true,
            filter: "\"WalletAddress\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Bids_TransactionHash",
            table: "Bids");

        migrationBuilder.DropIndex(
            name: "IX_Lots_ContractAddress_OnChainAuctionId",
            table: "Lots");

        migrationBuilder.DropIndex(
            name: "IX_Users_WalletAddress",
            table: "Users");

        migrationBuilder.DropColumn(name: "AmountWei", table: "Bids");
        migrationBuilder.DropColumn(name: "BlockNumber", table: "Bids");
        migrationBuilder.DropColumn(name: "TransactionHash", table: "Bids");
        migrationBuilder.DropColumn(name: "ChainId", table: "Lots");
        migrationBuilder.DropColumn(name: "ContractAddress", table: "Lots");
        migrationBuilder.DropColumn(name: "CreationTransactionHash", table: "Lots");
        migrationBuilder.DropColumn(name: "OnChainAuctionId", table: "Lots");
        migrationBuilder.DropColumn(name: "SettlementMode", table: "Lots");
        migrationBuilder.DropColumn(name: "SettlementTransactionHash", table: "Lots");
        migrationBuilder.DropColumn(name: "WalletAddress", table: "Users");
        migrationBuilder.DropColumn(name: "WalletVerifiedAt", table: "Users");
    }
}
