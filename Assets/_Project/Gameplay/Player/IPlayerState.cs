namespace SoulHunter.Gameplay.Player
{
    /// <summary>
    /// Learning Comment:
    /// Blueprint (player-controller.md) ke anusar ye interface State Machine ka base hai.
    /// Isse hum bina bohot saare 'if/else' statements ke alag-alag behaviors likh sakte hain.
    /// </summary>
    public interface IPlayerState
    {
        void Enter();
        void UpdateLogic();
        void UpdatePhysics(ref SoulHunter.Gameplay.Physics.EnvironmentData envData);
        void Exit();
    }
}
