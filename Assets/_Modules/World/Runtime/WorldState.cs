using System.Collections.Generic;

namespace GuildSim.World
{
    public sealed class WorldState
    {
        public RegionDefinition[] Regions { get; }
        public int ActiveRegionIndex { get; private set; }
        public RegionDefinition ActiveRegion => Regions[ActiveRegionIndex];

        private readonly HashSet<string> unlockedQuestIds  = new();
        private readonly HashSet<string> completedQuestIds = new();

        public WorldState(WorldConfig config)
        {
            Regions = config.Regions;
            ActiveRegionIndex = 0;
        }

        public void SetActiveRegion(int index)
        {
            if (index >= 0 && index < Regions.Length)
                ActiveRegionIndex = index;
        }

        public void UnlockQuest(string questDefinitionId)       => unlockedQuestIds.Add(questDefinitionId);
        public void MarkQuestCompleted(string questDefinitionId) => completedQuestIds.Add(questDefinitionId);
        public bool IsQuestUnlocked(string questDefinitionId)   => unlockedQuestIds.Contains(questDefinitionId);
        public bool IsQuestCompleted(string questDefinitionId)  => completedQuestIds.Contains(questDefinitionId);
    }
}
