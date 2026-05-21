using System;
using GuildSim.Shared;
using GuildSim.Economy;

namespace GuildSim.Guild
{
    public sealed class GuildService
    {
        private readonly GuildConfig config;
        private readonly EconomyService economy;
        private readonly GuildState state;

        public GuildRankDefinition CurrentRank => state.CurrentRank;
        public int MaxMembers => state.CurrentRank?.MaxMembers ?? 1;

        public event Action<GuildRankDefinition> RankedUp;

        public GuildService(GuildConfig config, EconomyService economy)
        {
            this.config = config;
            this.economy = economy;

            var startRank = config.RankProgression.Length > 0 ? config.RankProgression[0] : null;
            state = new GuildState(startRank);

            EventBus.Subscribe(GameEvents.ReputationChanged, OnReputationChanged);
        }

        private void OnReputationChanged()
        {
            TryRankUp(economy.Reputation);
        }

        private void TryRankUp(int reputation)
        {
            var ranks = config.RankProgression;
            for (int i = ranks.Length - 1; i > state.CurrentRankIndex; i--)
            {
                if (reputation >= ranks[i].RequiredReputation)
                {
                    state.SetRank(i, ranks[i]);
                    RankedUp?.Invoke(ranks[i]);
                    EventBus.Publish(GameEvents.GuildRankUp, ranks[i]);
                    break;
                }
            }
        }

        public void Dispose()
        {
            EventBus.Unsubscribe(GameEvents.ReputationChanged, OnReputationChanged);
        }
    }
}
