using GuildSim.Story;
using GuildSim.World;
using GuildSim.Economy;
using GuildSim.Guild;
using GuildSim.Adventurer;

namespace GuildSim.Game
{
    /// <summary>
    /// GuildSim.Story の IStoryConditionContext を、各モジュールのServiceに委譲する実装。
    /// 依存方向：Game → Story（抽象）/ Game → 各モジュール（具象）。
    /// Storyモジュールは具象モジュールを一切importしない。
    /// </summary>
    internal sealed class StoryConditionBridge : IStoryConditionContext
    {
        private readonly WorldService      world;
        private readonly EconomyService    economy;
        private readonly GuildService      guild;
        private readonly AdventurerService adventurers;

        internal StoryConditionBridge(
            WorldService world,
            EconomyService economy,
            GuildService guild,
            AdventurerService adventurers)
        {
            this.world       = world;
            this.economy     = economy;
            this.guild       = guild;
            this.adventurers = adventurers;
        }

        public bool IsQuestCompleted(string questId)
            => world != null && world.IsQuestCompleted(questId);

        public int GetGold()            => economy?.Gold ?? 0;
        public int GetReputation()      => economy?.Reputation ?? 0;
        public int GetGuildRankIndex()  => guild?.CurrentRankIndex ?? 0;
        public int GetAdventurerCount() => adventurers?.ActiveCount ?? 0;
    }
}
