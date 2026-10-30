using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StaySmart.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingHistory : Migration
    {
        /// <inheritdoc />

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingHistories",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    BookingId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    OldStatus = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: true),

                    NewStatus = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false),

                    ChangedBy = table.Column<string>(
                        type: "nvarchar(256)",
                        maxLength: 256,
                        nullable: false),

                    ChangedAtUtc = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_BookingHistories",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_BookingHistories_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingHistories_BookingId",
                table: "BookingHistories",
                column: "BookingId");
        }

      
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingHistories");
        }
    }
}
