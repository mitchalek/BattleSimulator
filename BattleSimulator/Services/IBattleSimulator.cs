namespace BattleSimulator.Services
{
    public interface IBattleSimulator
    {
        bool AddArmy(Models.Army army);
        bool StartSimulation();
        bool ResetSimulation();
        Models.Battle GetBattle(int battleId);
        System.Collections.Generic.ICollection<Models.Battle> GetBattles();
    }
}