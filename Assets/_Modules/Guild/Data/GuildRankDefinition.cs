using UnityEngine;
using GuildSim.Shared;

namespace GuildSim.Guild
{
    [CreateAssetMenu(menuName = "GuildSim/Guild/Guild Rank Definition", fileName = "GuildRankDefinition")]
    public sealed class GuildRankDefinition : BaseDefinition
    {
        [Header("Rank Threshold")]
        [SerializeField] private int requiredReputation = 0;
        [SerializeField] private int maxMembers = 5;

        [Header("Unlocks")]
        [SerializeField] private string[] unlockedFeatureIds = {};

        public int RequiredReputation => Mathf.Max(0, requiredReputation);
        public int MaxMembers => Mathf.Max(1, maxMembers);
        public string[] UnlockedFeatureIds => unlockedFeatureIds;
    }
}
