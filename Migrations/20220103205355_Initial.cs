using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RevitToIfcScheduler.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IfcSettingsSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IfcSettingsSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HubId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cron = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeZoneId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IfcSettingsName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerializedFolderUrns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerializedFileUrns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFileCount = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HashedSessionKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AutodeskId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfilePicture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerializedPermissions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EncryptedToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EncryptedRefresh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenExpiration = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConversionJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HubId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FolderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IfcSettingsSetName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileUrn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DerivativeUrn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ForgeUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JobFinished = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCompositeDesign = table.Column<bool>(type: "bit", nullable: false),
                    InputStorageLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputStorageLocation = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversionJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversionJobs_Schedules_JobScheduleId",
                        column: x => x.JobScheduleId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversionJobs_JobScheduleId",
                table: "ConversionJobs",
                column: "JobScheduleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "ConversionJobs");

            migrationBuilder.DropTable(
                name: "IfcSettingsSets");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Schedules");
        }
    }
}
