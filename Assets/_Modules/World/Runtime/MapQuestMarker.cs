using UnityEngine;

namespace GuildSim.World
{
    /// <summary>
    /// ワールドマップ上の 1 ピンを表す DTO。
    /// _Game レイヤーが組み立て、WorldMapPanel に渡す。
    /// GuildSim.Quest アセンブリへの参照を一切持たない。
    /// </summary>
    public readonly struct MapQuestMarker
    {
        public string  QuestDefinitionId   { get; }
        public string  DisplayName         { get; }
        public string  QuestType           { get; }
        public string  DifficultyLabel     { get; }
        public int     DifficultyIndex     { get; }
        public int     RewardGold          { get; }
        public int     RewardReputation    { get; }
        public int     RewardExperience    { get; }
        public int     DurationDays        { get; }
        public int     RequiredPower       { get; }
        public Vector2 NormalizedPosition  { get; }
        public bool    IsUnlocked          { get; }
        public bool    IsCompleted         { get; }
        public Sprite  Icon               { get; }

        public MapQuestMarker(
            string  questDefinitionId,
            string  displayName,
            string  questType,
            string  difficultyLabel,
            int     difficultyIndex,
            int     rewardGold,
            int     rewardReputation,
            int     rewardExperience,
            int     durationDays,
            int     requiredPower,
            Vector2 normalizedPosition,
            bool    isUnlocked,
            bool    isCompleted,
            Sprite  icon)
        {
            QuestDefinitionId  = questDefinitionId;
            DisplayName        = displayName;
            QuestType          = questType;
            DifficultyLabel    = difficultyLabel;
            DifficultyIndex    = difficultyIndex;
            RewardGold         = rewardGold;
            RewardReputation   = rewardReputation;
            RewardExperience   = rewardExperience;
            DurationDays       = durationDays;
            RequiredPower      = requiredPower;
            NormalizedPosition = normalizedPosition;
            IsUnlocked         = isUnlocked;
            IsCompleted        = isCompleted;
            Icon               = icon;
        }
    }
}
