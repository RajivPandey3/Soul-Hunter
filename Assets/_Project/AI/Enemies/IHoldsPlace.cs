namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// Marks an enemy that stands where it was placed for one life (pile shooters, mines, still enemies).
    /// Such an enemy is never moved ahead of a fleeing player as a straggler (Bug MIS AI-09, owner choice A, 2026-10-21).
    /// </summary>
    public interface IHoldsPlace
    {
    }
}
