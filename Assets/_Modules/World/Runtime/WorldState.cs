namespace GuildSim.World
{
    public sealed class WorldState
    {
        public RegionDefinition[] Regions { get; }
        public int ActiveRegionIndex { get; private set; }
        public RegionDefinition ActiveRegion => Regions[ActiveRegionIndex];

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
    }
}
