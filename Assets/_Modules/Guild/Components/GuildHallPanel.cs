using UnityEngine.UIElements;
using GuildSim.Shared;

namespace GuildSim.Guild
{
    public sealed class GuildHallPanel
    {
        private readonly Label rankLabel;
        private readonly Label maxMembersLabel;
        private readonly Label reputationLabel;
        private readonly ProgressBar reputationBar;

        private GuildService service;
        private int currentReputation;

        public GuildHallPanel(VisualElement panelRoot)
        {
            rankLabel       = panelRoot?.Q<Label>("rank-label");
            maxMembersLabel = panelRoot?.Q<Label>("max-members-label");
            reputationLabel = panelRoot?.Q<Label>("reputation-label");
            reputationBar   = panelRoot?.Q<ProgressBar>("reputation-bar");
        }

        public void Initialize(GuildService guildService)
        {
            service = guildService;

            EventBus.Subscribe<GuildRankDefinition>(GameEvents.GuildRankUp, OnRankUp);
            EventBus.Subscribe<int>(GameEvents.ReputationChanged, OnReputationChanged);

            Refresh(service.CurrentRank, 0);
        }

        private void OnRankUp(GuildRankDefinition rank) => Refresh(rank, currentReputation);

        private void OnReputationChanged(int reputation)
        {
            currentReputation = reputation;
            Refresh(service.CurrentRank, reputation);
        }

        private void Refresh(GuildRankDefinition rank, int reputation)
        {
            if (rank == null) return;

            if (rankLabel != null)       rankLabel.text       = $"ギルドランク: {rank.DisplayName}";
            if (maxMembersLabel != null) maxMembersLabel.text = $"最大人数: {rank.MaxMembers} 人";
            if (reputationLabel != null) reputationLabel.text = $"名声: {reputation}";

            if (reputationBar != null)
            {
                int threshold = rank.RequiredReputation;
                reputationBar.value = threshold > 0 ? (float)reputation / threshold * 100f : 100f;
            }
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<GuildRankDefinition>(GameEvents.GuildRankUp, OnRankUp);
            EventBus.Unsubscribe<int>(GameEvents.ReputationChanged, OnReputationChanged);
        }
    }
}
