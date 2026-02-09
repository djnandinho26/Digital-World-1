using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_World.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "acct",
                columns: table => new
                {
                    accountId = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecondPassword = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    uniId = table.Column<uint>(type: "int unsigned", nullable: true),
                    char1 = table.Column<int>(type: "int", nullable: true),
                    char2 = table.Column<int>(type: "int", nullable: true),
                    char3 = table.Column<int>(type: "int", nullable: true),
                    char4 = table.Column<int>(type: "int", nullable: true),
                    lastChar = table.Column<int>(type: "int", nullable: true),
                    premium = table.Column<int>(type: "int", nullable: false),
                    cash = table.Column<long>(type: "bigint", nullable: false),
                    silk = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acct", x => x.accountId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "servers",
                columns: table => new
                {
                    serverId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    port = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servers", x => x.serverId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "chars",
                columns: table => new
                {
                    characterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    accountId = table.Column<uint>(type: "int unsigned", nullable: false),
                    charName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    model = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    level = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    partner = table.Column<int>(type: "int", nullable: true),
                    map = table.Column<uint>(type: "int unsigned", nullable: false),
                    x = table.Column<int>(type: "int", nullable: false),
                    y = table.Column<int>(type: "int", nullable: false),
                    hp = table.Column<int>(type: "int", nullable: false),
                    ds = table.Column<int>(type: "int", nullable: false),
                    money = table.Column<long>(type: "bigint", nullable: false),
                    inventory = table.Column<byte[]>(type: "blob", nullable: true),
                    warehouse = table.Column<byte[]>(type: "blob", nullable: true),
                    archive = table.Column<byte[]>(type: "blob", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chars", x => x.characterId);
                    table.ForeignKey(
                        name: "FK_chars_acct_accountId",
                        column: x => x.accountId,
                        principalTable: "acct",
                        principalColumn: "accountId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "digimon",
                columns: table => new
                {
                    digimonId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    characterId = table.Column<int>(type: "int", nullable: true),
                    digiName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digiModel = table.Column<uint>(type: "int unsigned", nullable: false),
                    level = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    size = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    hp = table.Column<int>(type: "int", nullable: false),
                    ds = table.Column<int>(type: "int", nullable: false),
                    exp = table.Column<long>(type: "bigint", nullable: false),
                    digiSlot = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    evolutions = table.Column<byte[]>(type: "blob", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digimon", x => x.digimonId);
                    table.ForeignKey(
                        name: "FK_digimon_chars_characterId",
                        column: x => x.characterId,
                        principalTable: "chars",
                        principalColumn: "characterId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_acct_username",
                table: "acct",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chars_accountId",
                table: "chars",
                column: "accountId");

            migrationBuilder.CreateIndex(
                name: "IX_chars_charName",
                table: "chars",
                column: "charName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_digimon_characterId",
                table: "digimon",
                column: "characterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "digimon");

            migrationBuilder.DropTable(
                name: "servers");

            migrationBuilder.DropTable(
                name: "chars");

            migrationBuilder.DropTable(
                name: "acct");
        }
    }
}
