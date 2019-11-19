using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BattleSimulator.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attacks",
                columns: table => new
                {
                    AttackId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttackIndex = table.Column<int>(nullable: false),
                    AttackChance = table.Column<int>(nullable: false),
                    AttackRoll = table.Column<int>(nullable: false),
                    AttackSuccessful = table.Column<bool>(nullable: false),
                    AttackDamage = table.Column<double>(nullable: false),
                    UnitsAttacking = table.Column<int>(nullable: false),
                    UnitsTargeted = table.Column<int>(nullable: false),
                    UnitsDestroyed = table.Column<int>(nullable: false),
                    BattleTime = table.Column<TimeSpan>(nullable: false),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    BattleId = table.Column<int>(nullable: false),
                    AttackerId = table.Column<int>(nullable: true),
                    TargetId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attacks", x => x.AttackId);
                });

            migrationBuilder.CreateTable(
                name: "Battles",
                columns: table => new
                {
                    BattleId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Started = table.Column<bool>(nullable: false),
                    Completed = table.Column<bool>(nullable: false),
                    Reset = table.Column<bool>(nullable: false),
                    Time = table.Column<TimeSpan>(nullable: false),
                    ArmyCount = table.Column<int>(nullable: false),
                    AttackCount = table.Column<int>(nullable: false),
                    SimulationId = table.Column<string>(nullable: true),
                    Rolls = table.Column<int>(nullable: false),
                    VictorId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Battles", x => x.BattleId);
                });

            migrationBuilder.CreateTable(
                name: "Armies",
                columns: table => new
                {
                    ArmyId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: false),
                    Units = table.Column<int>(nullable: false),
                    UnitsAlive = table.Column<int>(nullable: false),
                    UnitsLost = table.Column<int>(nullable: false),
                    Strategy = table.Column<int>(nullable: false),
                    DamageDone = table.Column<double>(nullable: false),
                    DamageTaken = table.Column<double>(nullable: false),
                    NextAttackTime = table.Column<TimeSpan>(nullable: false),
                    BattleId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Armies", x => x.ArmyId);
                    table.ForeignKey(
                        name: "FK_Armies_Battles_BattleId",
                        column: x => x.BattleId,
                        principalTable: "Battles",
                        principalColumn: "BattleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    SettingsId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitHealth = table.Column<double>(nullable: false),
                    UnitAttackDamage = table.Column<double>(nullable: false),
                    UnitAttackChance = table.Column<double>(nullable: false),
                    UnitAttackTime = table.Column<TimeSpan>(nullable: false),
                    BattleId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.SettingsId);
                    table.ForeignKey(
                        name: "FK_Settings_Battles_BattleId",
                        column: x => x.BattleId,
                        principalTable: "Battles",
                        principalColumn: "BattleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Armies_BattleId",
                table: "Armies",
                column: "BattleId");

            migrationBuilder.CreateIndex(
                name: "IX_Attacks_AttackerId",
                table: "Attacks",
                column: "AttackerId");

            migrationBuilder.CreateIndex(
                name: "IX_Attacks_BattleId",
                table: "Attacks",
                column: "BattleId");

            migrationBuilder.CreateIndex(
                name: "IX_Attacks_TargetId",
                table: "Attacks",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_VictorId",
                table: "Battles",
                column: "VictorId");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_BattleId",
                table: "Settings",
                column: "BattleId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Attacks_Battles_BattleId",
                table: "Attacks",
                column: "BattleId",
                principalTable: "Battles",
                principalColumn: "BattleId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Attacks_Armies_AttackerId",
                table: "Attacks",
                column: "AttackerId",
                principalTable: "Armies",
                principalColumn: "ArmyId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attacks_Armies_TargetId",
                table: "Attacks",
                column: "TargetId",
                principalTable: "Armies",
                principalColumn: "ArmyId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Battles_Armies_VictorId",
                table: "Battles",
                column: "VictorId",
                principalTable: "Armies",
                principalColumn: "ArmyId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Armies_Battles_BattleId",
                table: "Armies");

            migrationBuilder.DropTable(
                name: "Attacks");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Battles");

            migrationBuilder.DropTable(
                name: "Armies");
        }
    }
}
