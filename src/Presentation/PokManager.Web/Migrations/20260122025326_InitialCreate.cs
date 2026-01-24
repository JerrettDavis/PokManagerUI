using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokManager.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InstanceSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InstanceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Health = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Uptime = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ServerMap = table.Column<string>(type: "TEXT", nullable: true),
                    MaxPlayers = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentPlayers = table.Column<int>(type: "INTEGER", nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPvE = table.Column<bool>(type: "INTEGER", nullable: false),
                    CpuUsagePercent = table.Column<double>(type: "REAL", nullable: false),
                    MemoryUsageMB = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstanceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InstanceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    InstanceSnapshotId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogEntries_InstanceSnapshots_InstanceSnapshotId",
                        column: x => x.InstanceSnapshotId,
                        principalTable: "InstanceSnapshots",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlayerSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InstanceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PlayerName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SteamId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false),
                    InstanceSnapshotId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerSessions_InstanceSnapshots_InstanceSnapshotId",
                        column: x => x.InstanceSnapshotId,
                        principalTable: "InstanceSnapshots",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TelemetrySnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InstanceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CpuUsagePercent = table.Column<double>(type: "REAL", nullable: false),
                    MemoryUsageMB = table.Column<long>(type: "INTEGER", nullable: false),
                    NetworkInKBps = table.Column<long>(type: "INTEGER", nullable: false),
                    NetworkOutKBps = table.Column<long>(type: "INTEGER", nullable: false),
                    Fps = table.Column<int>(type: "INTEGER", nullable: false),
                    TickRate = table.Column<int>(type: "INTEGER", nullable: false),
                    GameStatsJson = table.Column<string>(type: "TEXT", nullable: true),
                    InstanceSnapshotId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetrySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelemetrySnapshots_InstanceSnapshots_InstanceSnapshotId",
                        column: x => x.InstanceSnapshotId,
                        principalTable: "InstanceSnapshots",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InstanceSnapshots_InstanceId_Timestamp",
                table: "InstanceSnapshots",
                columns: new[] { "InstanceId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_InstanceId_Timestamp",
                table: "LogEntries",
                columns: new[] { "InstanceId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_InstanceSnapshotId",
                table: "LogEntries",
                column: "InstanceSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSessions_InstanceId_SteamId_JoinedAt",
                table: "PlayerSessions",
                columns: new[] { "InstanceId", "SteamId", "JoinedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSessions_InstanceSnapshotId",
                table: "PlayerSessions",
                column: "InstanceSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetrySnapshots_InstanceId_Timestamp",
                table: "TelemetrySnapshots",
                columns: new[] { "InstanceId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetrySnapshots_InstanceSnapshotId",
                table: "TelemetrySnapshots",
                column: "InstanceSnapshotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogEntries");

            migrationBuilder.DropTable(
                name: "PlayerSessions");

            migrationBuilder.DropTable(
                name: "TelemetrySnapshots");

            migrationBuilder.DropTable(
                name: "InstanceSnapshots");
        }
    }
}
