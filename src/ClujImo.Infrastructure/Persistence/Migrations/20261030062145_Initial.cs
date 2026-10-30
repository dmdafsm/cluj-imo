using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClujImo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IngestionRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Succeeded = table.Column<bool>(type: "INTEGER", nullable: false),
                    RowsRead = table.Column<int>(type: "INTEGER", nullable: false),
                    RowsInserted = table.Column<int>(type: "INTEGER", nullable: false),
                    RowsUpdated = table.Column<int>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Number = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IssuedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    RequestedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    RequestNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    RawAddress = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    ValidityMonths = table.Column<int>(type: "INTEGER", nullable: true),
                    ReferencedPermitNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    ReferencedPermitDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    StreetKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    StreetDisplay = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    HouseNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    IsZone = table.Column<bool>(type: "INTEGER", nullable: false),
                    Neighborhood = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    IsNewBuild = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsContinuation = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAmendment = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludesDemolition = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsUrbanPlan = table.Column<bool>(type: "INTEGER", nullable: false),
                    HeightRegime = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    LevelsAboveGround = table.Column<int>(type: "INTEGER", nullable: true),
                    Units = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Streets",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    FormerName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    FiscalZone = table.Column<string>(type: "TEXT", maxLength: 4, nullable: true),
                    Area = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Longitude = table.Column<double>(type: "REAL", nullable: true),
                    Neighborhood = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    GeocodeStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    GeocodedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Streets", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Permits_Category",
                table: "Permits",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Permits_IssuedOn",
                table: "Permits",
                column: "IssuedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Permits_Kind_Year_Number",
                table: "Permits",
                columns: new[] { "Kind", "Year", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permits_Neighborhood",
                table: "Permits",
                column: "Neighborhood");

            migrationBuilder.CreateIndex(
                name: "IX_Permits_StreetKey",
                table: "Permits",
                column: "StreetKey");

            migrationBuilder.CreateIndex(
                name: "IX_Streets_GeocodeStatus",
                table: "Streets",
                column: "GeocodeStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngestionRuns");

            migrationBuilder.DropTable(
                name: "Permits");

            migrationBuilder.DropTable(
                name: "Streets");
        }
    }
}
