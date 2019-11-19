using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BattleSimulator.Models
{
    public enum AttackStrategy
    {
        Random,
        Weakest,
        Strongest
    }
    public class Battle
    {
        public int BattleId { get; set; }
        public bool Started { get; set; }
        public bool Completed { get; set; }
        public bool Reset { get; set; }
        public TimeSpan Time { get; set; }
        public int ArmyCount { get; set; }
        public int AttackCount { get; set; }
        [JsonIgnore]
        public string SimulationId { get; set; } // Hangfire job Id.
        [JsonIgnore]
        public int Rolls { get; set; } // For restoring RNG state.
        // Navigation.
        [ForeignKey("Victor")]
        public int? VictorId { get; set; }
        [JsonIgnore]
        public Army Victor { get; set; }
        public Settings Settings { get; set; }
        [InverseProperty("Battle")]
        public ICollection<Army> Armies { get; set; } = new List<Army>(10);
        public ICollection<Attack> Attacks { get; set; } = new List<Attack>(10);
    }
    public class Settings
    {
        public int SettingsId { get; set; }
        public double UnitHealth { get; set; }
        public double UnitAttackDamage { get; set; }
        public double UnitAttackChance { get; set; }
        public TimeSpan UnitAttackTime { get; set; }
        // Navigation.
        [JsonIgnore]
        public int BattleId { get; set; }
        [JsonIgnore]
        public Battle Battle { get; set; }
    }
    public class Army
    {
        public int ArmyId { get; set; }
        [Required]
        public string Name { get; set; }
        [JsonIgnore]
        [Range(80, 100)]
        public int Units { get; set; }
        public int UnitsAlive { get; set; }
        public int UnitsLost { get; set; }
        public AttackStrategy Strategy { get; set; }
        // Battle specific values.
        public double DamageDone { get; set; }
        public double DamageTaken { get; set; }
        [JsonIgnore]
        public TimeSpan NextAttackTime { get; set; }
        // Navigation.
        [JsonIgnore]
        public int BattleId { get; set; }
        [JsonIgnore]
        public Battle Battle { get; set; }
    }
    public class Attack
    {
        public int AttackId { get; set; }
        public int AttackIndex { get; set; } // 1-based index.
        public int AttackChance { get; set; } // Between 1 and 100 inclusive.
        public int AttackRoll { get; set; } // Between 0 and 99 inclusive.
        public bool AttackSuccessful { get; set; } // True when Roll is less than a Chance.
        public double AttackDamage { get; set; } // 0.0 when attack not successful.
        public int UnitsAttacking { get; set; }
        public int UnitsTargeted { get; set; }
        public int UnitsDestroyed { get; set; }
        public TimeSpan BattleTime { get; set; }
        public DateTime TimeStamp { get; set; }
        // Navigation.
        [JsonIgnore]
        public int BattleId { get; set; }
        [JsonIgnore]
        public Battle Battle { get; set; }
        [ForeignKey("Attacker")]
        public int? AttackerId { get; set; }
        [JsonIgnore]
        public Army Attacker { get; set; }
        [ForeignKey("Target")]
        public int? TargetId { get; set; }
        [JsonIgnore]
        public Army Target { get; set; }
    }

    public class BattleSimulatorContext : DbContext
    {
        public BattleSimulatorContext(DbContextOptions<BattleSimulatorContext> options) : base(options) { }
        public DbSet<Army> Armies { get; set; }
        public DbSet<Battle> Battles { get; set; }
        public DbSet<Attack> Attacks { get; set; }
    }
}