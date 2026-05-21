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
    }
}
