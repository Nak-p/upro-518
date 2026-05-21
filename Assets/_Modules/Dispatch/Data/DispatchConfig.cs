using UnityEngine;
using GuildSim.Shared;

namespace GuildSim.Dispatch
{
    [CreateAssetMenu(menuName = "GuildSim/Dispatch/Dispatch Config", fileName = "DispatchConfig")]
    public sealed class DispatchConfig : BaseConfig
    {
        [Header("Success Formula")]
        [SerializeField] private float baseSuccessRate = 0.5f;
        [SerializeField] private float powerDifferenceCoefficient = 0.05f;

        [Header("Failure Penalty")]
        [SerializeField] private int failureGoldPenalty = 0;
        [SerializeField] private int failureRepPenalty = 5;

        [Header("Experience")]
        [SerializeField] private int experienceOnSuccess = 20;
        [SerializeField] private int experienceOnFail = 5;

        public float BaseSuccessRate => Mathf.Clamp01(baseSuccessRate);
        public float PowerDifferenceCoefficient => Mathf.Max(0f, powerDifferenceCoefficient);
        public int FailureGoldPenalty => Mathf.Max(0, failureGoldPenalty);
        public int FailureRepPenalty => Mathf.Max(0, failureRepPenalty);
        public int ExperienceOnSuccess => Mathf.Max(0, experienceOnSuccess);
        public int ExperienceOnFail => Mathf.Max(0, experienceOnFail);
    }
}
