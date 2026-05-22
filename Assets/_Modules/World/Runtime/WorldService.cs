using System.Collections.Generic;
using GuildSim.Shared;

namespace GuildSim.World
{
    public sealed class WorldService
    {
        private readonly WorldState state;

        public WorldState State => state;
        public RegionDefinition ActiveRegion => state.ActiveRegion;
        public RegionDefinition[] Regions => state.Regions;

        public WorldService(WorldConfig config)
        {
            state = new WorldState(config);
        }

        public void SelectRegion(int index)
        {
            state.SetActiveRegion(index);
            EventBus.Publish(WorldEvents.RegionSelected, state.ActiveRegion);
        }

        /// <summary>初期化時の一括解放（イベント発行なし）</summary>
        public void UnlockQuests(IEnumerable<string> questDefinitionIds)
        {
            foreach (var id in questDefinitionIds)
                state.UnlockQuest(id);
        }

        /// <summary>プレイ中の単件解放（MapUnlockChanged を発行）</summary>
        public void UnlockQuest(string questDefinitionId)
        {
            state.UnlockQuest(questDefinitionId);
            EventBus.Publish(WorldEvents.MapUnlockChanged);
        }

        /// <summary>クエスト完了マーク（MapUnlockChanged を発行）</summary>
        public void MarkQuestCompleted(string questDefinitionId)
        {
            state.MarkQuestCompleted(questDefinitionId);
            EventBus.Publish(WorldEvents.MapUnlockChanged);
        }

        public bool IsQuestUnlocked(string questDefinitionId)  => state.IsQuestUnlocked(questDefinitionId);
        public bool IsQuestCompleted(string questDefinitionId) => state.IsQuestCompleted(questDefinitionId);
    }
}
