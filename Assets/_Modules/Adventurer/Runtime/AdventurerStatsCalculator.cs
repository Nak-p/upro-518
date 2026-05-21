using UnityEngine;

namespace GuildSim.Adventurer
{
    public static class AdventurerStatsCalculator
    {
        public static AdventurerStats Compute(ClassDefinition cls, int level)
        {
            int power = Mathf.RoundToInt(cls.BasePower + cls.PowerGrowthPerLevel * (level - 1));
            int endurance = Mathf.RoundToInt(cls.BaseEndurance + cls.EnduranceGrowthPerLevel * (level - 1));
            return new AdventurerStats(power, endurance);
        }
    }
}
