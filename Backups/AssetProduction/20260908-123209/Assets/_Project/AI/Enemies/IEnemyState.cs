namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// Learning Comment:
    /// State machine interface for Enemy AI. 
    /// Ensures we don't have massive if-else chains in the EnemyController.
    /// </summary>
    public interface IEnemyState
    {
        void Enter();
        void UpdateLogic();
        void UpdatePhysics(ref SoulHunter.Gameplay.Physics.EnvironmentData envData);
        void Exit();
    }
}
