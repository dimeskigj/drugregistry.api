using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrugRegistry.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDrugEanData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DrugEanData",
                columns: table => new
                {
                    EanCode = table.Column<string>(type: "text", nullable: false),
                    DecisionNumber = table.Column<string>(type: "text", nullable: true),
                    LatinName = table.Column<string>(type: "text", nullable: true),
                    GenericName = table.Column<string>(type: "text", nullable: true),
                    PharmaceuticalForm = table.Column<string>(type: "text", nullable: true),
                    Strength = table.Column<string>(type: "text", nullable: true),
                    Packaging = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugEanData", x => x.EanCode);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrugEanData");
        }
    }
}
