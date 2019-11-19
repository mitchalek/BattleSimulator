using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Hangfire;
using BattleSimulator.Models;
using System.Collections.Generic;

namespace BattleSimulator.Services
{
    public class BattleSimulatorService : IBattleSimulator
    {
        private readonly BattleSimulatorContext _context;
        private readonly IConfiguration _configuration;
        public BattleSimulatorService(BattleSimulatorContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        #region Interface implementation.
        public bool AddArmy(Army army)
        {
            var battle = _context.Battles.Where(b => !b.Completed).Include(b => b.Armies).SingleOrDefault();
            // Create and configure new battle.
            if (battle == null)
            {
                battle = new Battle();
                ConfigureBattle(battle);
                _context.Add(battle);
                _context.SaveChanges();
            }
            // Add army.
            if (!battle.Started && !battle.Armies.Any(a => a.Name == army.Name))
            {
                ResetArmy(army);
                battle.Armies.Add(army);
                battle.ArmyCount++;
                _context.SaveChanges();
                return true;
            }
            return false;
        }
        public bool StartSimulation()
        {
            var battle = _context.Battles.Where(b => !b.Started).Include(b => b.Settings).Include(b => b.Armies).Include(b => b.Attacks).SingleOrDefault();
            if (battle != null && battle.Armies.Count >= GetMinimumArmies())
            {
                // Prepare data.
                ResetBattle(battle);
                ConfigureBattle(battle);
                // Change state to started and save changes before job enqueue.
                battle.Started = true;
                _context.SaveChanges();
                // Start simulation.
                string simulationId = BackgroundJob.Enqueue(() => Simulate(JobCancellationToken.Null));
                // Update simulation background job identifier for the battle, so it can be canceled when requested.
                battle.SimulationId = simulationId;
                _context.SaveChanges();
                return true;
            }
            return false;
        }
        public bool ResetSimulation()
        {
            var battle = _context.Battles.SingleOrDefault(b => b.Started && !b.Completed);
            if (battle != null)
            {
                string simulationId = battle.SimulationId;
                bool isDeleted = BackgroundJob.Delete(simulationId);
                battle.Started = false;
                battle.Reset = true;
                battle.SimulationId = null;
                _context.SaveChanges();
                return isDeleted;
            }
            return false;
        }
        public Battle GetBattle(int battleId)
        {
            // Get battle with all related data.
            var battle = _context.Battles.AsNoTracking().Where(b => b.BattleId == battleId).Include(b => b.Settings).Include(b => b.Armies).Include(b => b.Attacks).FirstOrDefault();
            // Sort attack log.
            if (battle != null)
            {
                battle.Attacks = battle.Attacks.OrderBy(a => a.AttackIndex).ToList();
            }
            return battle;
        }
        public ICollection<Battle> GetBattles()
        {
            return _context.Battles.AsNoTracking().ToList();
        }
        #endregion

        #region Private methods.
        // Reset battle simulation data.
        private void ResetBattle(Battle battle)
        {
            // Restore armies to default state.
            foreach (var army in battle.Armies)
            {
                ResetArmy(army);
            }
            // Delete attack logs.
            battle.Attacks.Clear();
            // Restore battle to default state.
            battle.AttackCount = 0;
            battle.SimulationId = null;
            battle.Victor = null;
            battle.Rolls = 0;
            battle.Time = TimeSpan.Zero;
            battle.Reset = false;
            battle.Completed = false;
            battle.Started = false;
        }
        // Reset army simulation data.
        private void ResetArmy(Army army)
        {
            army.UnitsAlive = army.Units;
            army.UnitsLost = 0;
            army.DamageDone = 0.0;
            army.DamageTaken = 0.0;
            army.NextAttackTime = TimeSpan.Zero;
        }
        // Update battle settings with configuration data.
        // If not configured or cannot parse default values will be used.
        // If configured values are too low, minimal allowed values will be used.
        private void ConfigureBattle(Battle battle)
        {
            if (battle.Settings == null)
            {
                battle.Settings = new Settings();
            }
            try { battle.Settings.UnitHealth = Math.Max(_configuration.GetValue("BattleSimulator:UnitHealth", 1.0), double.Epsilon); }
            catch { battle.Settings.UnitHealth = 1.0; }
            try { battle.Settings.UnitAttackDamage = Math.Max(_configuration.GetValue("BattleSimulator:UnitAttackDamage", 0.5), 0.0); }
            catch { battle.Settings.UnitAttackDamage = 0.5; }
            try { battle.Settings.UnitAttackChance = Math.Max(_configuration.GetValue("BattleSimulator:UnitAttackChance", 1.0), 0.0); }
            catch { battle.Settings.UnitAttackChance = 1.0; }
            try { battle.Settings.UnitAttackTime = TimeSpan.FromTicks(Math.Max(_configuration.GetValue("BattleSimulator:UnitAttackTime", TimeSpan.FromSeconds(0.01)).Ticks, 0)); }
            catch { battle.Settings.UnitAttackTime = TimeSpan.FromSeconds(0.01); }
        }
        // Get minimum armies from config or default.
        private int GetMinimumArmies()
        {
            try { return Math.Max(_configuration.GetValue("BattleSimulator:MinimumArmies", 10), 1); }
            catch { return 10; }
        }
        // Create new random number generator and initialize with specific seed value and number of rolls.
        private Random CreateRNG(int seed, int rolls = 0)
        {
            var random = new Random(seed);
            if (rolls > 0)
            {
                for (int i = 0; i < rolls; i++)
                {
                    random.Next();
                }
            }
            return random;
        }
        // Return next random int between min(inclusive) and max(exclusive), and update battle roll counter.
        private int Roll(Random rng, int min, int max, Battle battle)
        {
            battle.Rolls++;
            return rng.Next(min, max);
        }
        // Battle simulation procedure.
        // Hangfire library is used to execute this method from a background thread.
        // Real time is used for measuring battle time in ticks.
        // Simulation procedure is safe to be interrupted and will continue from previous state.
        public void Simulate(IJobCancellationToken cancellationToken)
        {
            try
            {
                // Get battle and related armies data from database that will be used during simulation.
                var battle = _context.Battles.Where(b => b.Started && !b.Completed).Include(b => b.Settings).Include(b => b.Armies).Single();
                // Current battle time to continue from.
                long simulationBaseTimeTicks = battle.Time.Ticks;
                // Current time stamp for calculating simulation time.
                long simulationStartTimeTicks = DateTime.UtcNow.Ticks;
                // Setup random number generator.
                var random = CreateRNG(battle.BattleId, battle.Rolls);
                // Begin simulation loop.
                while (true)
                {
                    // User requested restart or background worker shutdown.
                    cancellationToken.ThrowIfCancellationRequested();
                    // Recalculate battle time.
                    long currentBattleTimeTicks = simulationBaseTimeTicks + (DateTime.UtcNow.Ticks - simulationStartTimeTicks);
                    battle.Time = TimeSpan.FromTicks(currentBattleTimeTicks);
                    // Get armies that are able to attack.
                    foreach (var attacker in battle.Armies.Where(a => a.UnitsAlive > 0 && a.NextAttackTime <= battle.Time))
                    {
                        // Defeated by previous attackers.
                        if (attacker.UnitsAlive < 1)
                        {
                            continue;
                        }
                        // Target selection.
                        Army target = null;
                        var targets = battle.Armies.Where(a => a.UnitsAlive > 0 && a.ArmyId != attacker.ArmyId);
                        switch (attacker.Strategy)
                        {
                            case AttackStrategy.Random:
                                var targetArray = targets.ToArray();
                                int randomIndex = Roll(random, 0, targetArray.Length, battle);
                                target = targets.ElementAtOrDefault(randomIndex);
                                break;
                            case AttackStrategy.Weakest:
                                target = targets.OrderBy(a => a.UnitsAlive).FirstOrDefault();
                                break;
                            case AttackStrategy.Strongest:
                                target = targets.OrderBy(a => a.UnitsAlive).LastOrDefault();
                                break;
                            default:
                                break;
                        }
                        // !!! VICTORY DETECTED !!!
                        // No valid target means attacker is the winner.
                        // Proclaim the victor, complete the battle and update database.
                        if (target == null)
                        {
                            battle.Victor = attacker;
                            battle.Completed = true;
                            _context.SaveChanges();
                            System.Diagnostics.Debug.WriteLine("--- Battle simulation completed successfully.");
                            return;
                        }
                        // Calculate success chance and roll.
                        int unitsAttacking = attacker.UnitsAlive;
                        int unitsAttacked = target.UnitsAlive;
                        int unitsDestroyed = 0;
                        int attackChance = Math.Clamp((int)(battle.Settings.UnitAttackChance * unitsAttacking + 0.5), 0, 100);
                        int attackRoll = Roll(random, 0, 100, battle);
                        bool attackSuccessful = attackRoll < attackChance;
                        double damage = 0.0;
                        // Apply damage if attack successful.
                        if (attackSuccessful)
                        {
                            damage = battle.Settings.UnitAttackDamage * unitsAttacking;
                            attacker.DamageDone += damage;
                            target.DamageTaken += damage;
                            // Recalculate number of units.
                            int unitsLostNew = Math.Min((int)(target.DamageTaken / battle.Settings.UnitHealth), target.Units);
                            unitsDestroyed = unitsLostNew - target.UnitsLost;
                            target.UnitsLost = unitsLostNew;
                            target.UnitsAlive = target.Units - unitsLostNew;
                        }
                        // Set next attack time for the attacker.
                        attacker.NextAttackTime = TimeSpan.FromTicks(currentBattleTimeTicks + battle.Settings.UnitAttackTime.Ticks * unitsAttacking);
                        // Increment attack counter.
                        battle.AttackCount++;
                        // Log the attack.
                        Attack attack = new Attack
                        {
                            Attacker = attacker,
                            Target = target,
                            UnitsAttacking = unitsAttacking,
                            UnitsTargeted = unitsAttacked,
                            UnitsDestroyed = unitsDestroyed,
                            AttackChance = attackChance,
                            AttackRoll = attackRoll,
                            AttackSuccessful = attackSuccessful,
                            AttackDamage = damage,
                            AttackIndex = battle.AttackCount,
                            BattleTime = battle.Time,
                            TimeStamp = DateTime.Now
                        };
                        battle.Attacks.Add(attack);
                    }
                    // Save changes to database.
                    _context.SaveChanges();
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("--- Battle simulation has been canceled gracefully.");
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("--- Battle simulation has experienced an unexpected error.");
            }
        }
        #endregion
    }
}
