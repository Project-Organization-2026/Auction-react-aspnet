using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auction.DAL.Migrations;

public partial class AddUniqueMainLotImageIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH ranked_main_images AS (
                SELECT "Id",
                       ROW_NUMBER() OVER (PARTITION BY "LotId" ORDER BY "Id") AS row_number
                FROM "LotImages"
                WHERE "IsMain" = TRUE
            )
            UPDATE "LotImages"
            SET "IsMain" = FALSE
            WHERE "Id" IN (
                SELECT "Id"
                FROM ranked_main_images
                WHERE row_number > 1
            );
            """);

        migrationBuilder.CreateIndex(
            name: "IX_LotImages_LotId_IsMain",
            table: "LotImages",
            column: "LotId",
            unique: true,
            filter: "\"IsMain\" = TRUE");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_LotImages_LotId_IsMain",
            table: "LotImages");
    }
}
