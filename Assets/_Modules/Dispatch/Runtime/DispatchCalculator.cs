using UnityEngine;
using GuildSim.Adventurer;
using GuildSim.Quest;

namespace GuildSim.Dispatch
{
    public static class DispatchCalculator
    {
        public static float ComputeSuccessRate(
            AdventurerState adventurer,
            QuestDefinition quest,
            DispatchConfig config)
        {
            int powerDiff = adventurer.Stats.Power - quest.RequiredPowerRating;
            float rate = config.BaseSuccessRate + powerDiff * config.PowerDifferenceCoefficient;
            return Mathf.Clamp01(rate);
        }

        public static DispatchResult Resolve(
            AdventurerState adventurer,
            QuestState quest,
            DispatchConfig config)
        {
            float rate = ComputeSuccessRate(adventurer, quest.Definition, config);
            bool success = Random.value < rate;

            return new DispatchResult(
                success: success,
                goldReward: success ? quest.Definition.RewardGold : 0,
                goldPenalty: success ? 0 : config.FailureGoldPenalty,
                reputationGain: success ? quest.Definition.RewardReputation : 0,
                reputationPenalty: success ? 0 : config.FailureRepPenalty,
                experienceGain: success ? config.ExperienceOnSuccess : config.ExperienceOnFail,
                adventurerId: adventurer.Id,
                questInstanceId: quest.InstanceId
            );
        }
    }
}
