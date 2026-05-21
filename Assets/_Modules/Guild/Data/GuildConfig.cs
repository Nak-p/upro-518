using UnityEngine;
using GuildSim.Shared;

namespace GuildSim.Guild
{
    [CreateAssetMenu(menuName = "GuildSim/Guild/Guild Config", fileName = "GuildConfig")]
    public sealed class GuildConfig : BaseConfig
    {
        [SerializeField] private GuildRankDefinition[] rankProgression = {};

        public GuildRankDefinition[] RankProgression => rankProgression;
    }
}
