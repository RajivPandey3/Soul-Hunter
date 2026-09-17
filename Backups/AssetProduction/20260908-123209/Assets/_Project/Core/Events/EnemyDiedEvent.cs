namespace SoulHunter.Core.Events
{
    public struct EnemyDiedEvent : IGameEvent
    {
        public int ExperienceReward { get; }

        public EnemyDiedEvent(int experienceReward)
        {
            ExperienceReward = experienceReward;
        }
    }
}