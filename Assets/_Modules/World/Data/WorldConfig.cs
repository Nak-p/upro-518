using GuildSim.Shared;

namespace GuildSim.World
{
    using UnityEngine;

    [CreateAssetMenu(menuName = "GuildSim/World/World Config", fileName = "WorldConfig")]
    public sealed class WorldConfig : BaseConfig
    {
        [SerializeField] private RegionDefinition[] regions = {};

        public RegionDefinition[] Regions => regions;
    }
}
