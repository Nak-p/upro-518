using UnityEngine;
using GuildSim.Shared;

namespace GuildSim.Adventurer
{
    public enum TraitEffectType
    {
        PowerBonus,
        GoldRewardMultiplier,
        SuccessRateBonus,
        UpkeepReduction,
    }

    [CreateAssetMenu(menuName = "GuildSim/Adventurer/Trait Definition", fileName = "TraitDefinition")]
    public sealed class TraitDefinition : BaseDefinition
    {
        [SerializeField] private TraitEffectType effectType;
        [SerializeField] private float effectValue;

        public TraitEffectType EffectType => effectType;
        public float EffectValue => effectValue;
    }
}
