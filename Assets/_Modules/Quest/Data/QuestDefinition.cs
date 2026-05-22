using UnityEngine;
using GuildSim.Shared;

namespace GuildSim.Quest
{
    public enum QuestDifficulty { E, D, C, B, A, S }
    public enum QuestType { Combat, Escort, Explore, Gather, Deliver }

    [CreateAssetMenu(menuName = "GuildSim/Quest/Quest Definition", fileName = "QuestDefinition")]
    public sealed class QuestDefinition : BaseDefinition
    {
        [Header("Quest Info")]
        [SerializeField] private QuestType questType;
        [SerializeField] private QuestDifficulty difficulty;

        [Header("Requirements")]
        [SerializeField] private int requiredPowerRating = 10;

        [Header("Duration")]
        [SerializeField] private int durationDays = 3;

        [Header("Rewards")]
        [SerializeField] private int rewardGold = 100;
        [SerializeField] private int rewardReputation = 10;
        [SerializeField] private int rewardExperience = 20;

        [Header("Expiry")]
        [SerializeField] private int expiresAfterDays = 7;

        [Header("World Map")]
        [Tooltip("マップ上のピン位置（0–1 正規化座標）")]
        [SerializeField] private Vector2 mapPosition = Vector2.zero;
        [Tooltip("完了すると新エリアが解放される「キークエスト」かどうか")]
        [SerializeField] private bool isKeyQuest = false;

        public QuestType QuestType => questType;
        public QuestDifficulty Difficulty => difficulty;
        public int RequiredPowerRating => Mathf.Max(1, requiredPowerRating);
        public int DurationDays => Mathf.Max(1, durationDays);
        public int RewardGold => Mathf.Max(0, rewardGold);
        public int RewardReputation => Mathf.Max(0, rewardReputation);
        public int RewardExperience => Mathf.Max(0, rewardExperience);
        public int ExpiresAfterDays => Mathf.Max(1, expiresAfterDays);
        public Vector2 MapPosition  => mapPosition;
        public bool IsKeyQuest      => isKeyQuest;
    }
}
