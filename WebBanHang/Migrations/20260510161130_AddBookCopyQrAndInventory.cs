using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHang.Migrations
{
    /// <inheritdoc />
    public partial class AddBookCopyQrAndInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookCopyId",
                table: "Borrows",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LibraryMemberQrImageRelativePath",
                table: "AspNetUsers",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LibraryMemberQrToken",
                table: "AspNetUsers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookCopies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CopyCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    QrPayload = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    QrImageRelativePath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ShelfLocation = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastInventoryVerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookCopies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookCopies_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookInventorySessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookInventorySessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookInventorySessions_AspNetUsers_StartedByUserId",
                        column: x => x.StartedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BookInventoryScans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    BookCopyId = table.Column<int>(type: "int", nullable: false),
                    ScannedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ObservedShelfLocation = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookInventoryScans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookInventoryScans_BookCopies_BookCopyId",
                        column: x => x.BookCopyId,
                        principalTable: "BookCopies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BookInventoryScans_BookInventorySessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "BookInventorySessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Borrows_BookCopyId",
                table: "Borrows",
                column: "BookCopyId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LibraryMemberQrToken",
                table: "AspNetUsers",
                column: "LibraryMemberQrToken",
                unique: true,
                filter: "[LibraryMemberQrToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BookCopies_CopyCode",
                table: "BookCopies",
                column: "CopyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookCopies_ProductId",
                table: "BookCopies",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BookInventoryScans_BookCopyId",
                table: "BookInventoryScans",
                column: "BookCopyId");

            migrationBuilder.CreateIndex(
                name: "IX_BookInventoryScans_SessionId_BookCopyId",
                table: "BookInventoryScans",
                columns: new[] { "SessionId", "BookCopyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookInventorySessions_StartedByUserId",
                table: "BookInventorySessions",
                column: "StartedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Borrows_BookCopies_BookCopyId",
                table: "Borrows",
                column: "BookCopyId",
                principalTable: "BookCopies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Borrows_BookCopies_BookCopyId",
                table: "Borrows");

            migrationBuilder.DropTable(
                name: "BookInventoryScans");

            migrationBuilder.DropTable(
                name: "BookCopies");

            migrationBuilder.DropTable(
                name: "BookInventorySessions");

            migrationBuilder.DropIndex(
                name: "IX_Borrows_BookCopyId",
                table: "Borrows");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_LibraryMemberQrToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BookCopyId",
                table: "Borrows");

            migrationBuilder.DropColumn(
                name: "LibraryMemberQrImageRelativePath",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LibraryMemberQrToken",
                table: "AspNetUsers");
        }
    }
}
