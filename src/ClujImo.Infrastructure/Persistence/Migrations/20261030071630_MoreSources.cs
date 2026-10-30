using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClujImo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoreSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GeoPrecision",
                table: "Permits",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Permits",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Permits",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "IngestionRuns",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ConstructionSites",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Building = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Neighborhood = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConstructionSites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CouncilDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Topic = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouncilDecisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetroStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Indicator = table.Column<int>(type: "INTEGER", nullable: false),
                    Siruta = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Locality = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetroStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Neighborhoods",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CenterLatitude = table.Column<double>(type: "REAL", nullable: false),
                    CenterLongitude = table.Column<double>(type: "REAL", nullable: false),
                    RingsJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Neighborhoods", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "OsmAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StreetKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    HouseNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OsmAddresses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedResources",
                columns: table => new
                {
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Rows = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedResources", x => x.Url);
                });

            migrationBuilder.CreateTable(
                name: "PublicContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AuthorityCui = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Authority = table.Column<string>(type: "TEXT", nullable: false),
                    ContractNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AwardNoticeNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Lot = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    ContractDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ContractType = table.Column<string>(type: "TEXT", nullable: true),
                    Procedure = table.Column<string>(type: "TEXT", nullable: true),
                    CpvCode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    CpvName = table.Column<string>(type: "TEXT", nullable: true),
                    ValueRon = table.Column<double>(type: "REAL", nullable: true),
                    Winner = table.Column<string>(type: "TEXT", nullable: true),
                    WinnerCui = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicContracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Beneficiary = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Amount = table.Column<double>(type: "REAL", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    RowKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsWorks = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicPayments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UrbanPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Stage = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Initiator = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    SourceUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    StreetKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    StreetDisplay = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    HouseNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Longitude = table.Column<double>(type: "REAL", nullable: true),
                    GeoPrecision = table.Column<int>(type: "INTEGER", nullable: false),
                    Neighborhood = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrbanPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Permits_Latitude_Longitude",
                table: "Permits",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestionRuns_Source_Id",
                table: "IngestionRuns",
                columns: new[] { "Source", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CouncilDecisions_Topic",
                table: "CouncilDecisions",
                column: "Topic");

            migrationBuilder.CreateIndex(
                name: "IX_CouncilDecisions_Year_Number",
                table: "CouncilDecisions",
                columns: new[] { "Year", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetroStats_Indicator_Siruta_Year",
                table: "MetroStats",
                columns: new[] { "Indicator", "Siruta", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OsmAddresses_StreetKey",
                table: "OsmAddresses",
                column: "StreetKey");

            migrationBuilder.CreateIndex(
                name: "IX_PublicContracts_ContractDate",
                table: "PublicContracts",
                column: "ContractDate");

            migrationBuilder.CreateIndex(
                name: "IX_PublicContracts_ContractNumber_AwardNoticeNumber_Lot",
                table: "PublicContracts",
                columns: new[] { "ContractNumber", "AwardNoticeNumber", "Lot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicPayments_Date",
                table: "PublicPayments",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_PublicPayments_RowKey",
                table: "PublicPayments",
                column: "RowKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UrbanPlans_Date",
                table: "UrbanPlans",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_UrbanPlans_Stage_Reference",
                table: "UrbanPlans",
                columns: new[] { "Stage", "Reference" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConstructionSites");

            migrationBuilder.DropTable(
                name: "CouncilDecisions");

            migrationBuilder.DropTable(
                name: "MetroStats");

            migrationBuilder.DropTable(
                name: "Neighborhoods");

            migrationBuilder.DropTable(
                name: "OsmAddresses");

            migrationBuilder.DropTable(
                name: "ProcessedResources");

            migrationBuilder.DropTable(
                name: "PublicContracts");

            migrationBuilder.DropTable(
                name: "PublicPayments");

            migrationBuilder.DropTable(
                name: "UrbanPlans");

            migrationBuilder.DropIndex(
                name: "IX_Permits_Latitude_Longitude",
                table: "Permits");

            migrationBuilder.DropIndex(
                name: "IX_IngestionRuns_Source_Id",
                table: "IngestionRuns");

            migrationBuilder.DropColumn(
                name: "GeoPrecision",
                table: "Permits");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Permits");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Permits");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "IngestionRuns");
        }
    }
}
