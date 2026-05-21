using UnityEngine;
using GuildSim.Shared;

namespace GuildSim.Quest
{
    [CreateAssetMenu(menuName = "GuildSim/Quest/Quest Config", fileName = "QuestConfig")]
    public sealed class QuestConfig : BaseConfig
    {
        [Header("Board")]
        [SerializeField] private int boardSize = 6;
        [SerializeField] private int refreshIntervalDays = 3;

        [Header("Failure Penalty")]
        [SerializeField] private int failureRepPenalty = 5;

        public int BoardSize => Mathf.Max(1, boardSize);
        public int RefreshIntervalDays => Mathf.Max(1, refreshIntervalDays);
        public int FailureRepPenalty => Mathf.Max(0, failureRepPenalty);
    }
}
