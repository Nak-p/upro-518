using UnityEngine;
using GuildSim.Shared;

namespace GuildSim.Adventurer
{
    [CreateAssetMenu(menuName = "GuildSim/Adventurer/Class Definition", fileName = "ClassDefinition")]
    public sealed class ClassDefinition : BaseDefinition
    {
        [Header("Base Stats")]
        [SerializeField] private int basePower = 10;
        [SerializeField] private int baseEndurance = 10;

        [Header("Growth")]
        [SerializeField] private float powerGrowthPerLevel = 1.5f;
        [SerializeField] private float enduranceGrowthPerLevel = 1f;

        [Header("Economy")]
        [SerializeField] private int hireCost = 100;
        [SerializeField] private int upkeepCostModifier = 0;

        [Header("Traits")]
        [SerializeField] private TraitDefinition[] classTraits = {};

        public int BasePower => Mathf.Max(1, basePower);
        public int BaseEndurance => Mathf.Max(1, baseEndurance);
        public float PowerGrowthPerLevel => Mathf.Max(0f, powerGrowthPerLevel);
        public float EnduranceGrowthPerLevel => Mathf.Max(0f, enduranceGrowthPerLevel);
        public int HireCost => Mathf.Max(0, hireCost);
        public int UpkeepCostModifier => upkeepCostModifier;
        public TraitDefinition[] ClassTraits => classTraits;
    }
}
