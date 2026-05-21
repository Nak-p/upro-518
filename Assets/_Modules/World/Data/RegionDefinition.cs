using UnityEngine;
using GuildSim.Shared;

namespace GuildSim.World
{
    // QuestDefinition の参照は持たない（World は Shared のみ依存）。
    // クエスト紐づけは GameBootstrapConfig / QuestService が担当する。
    [CreateAssetMenu(menuName = "GuildSim/World/Region Definition", fileName = "RegionDefinition")]
    public sealed class RegionDefinition : BaseDefinition
    {
        [Header("Danger")]
        [SerializeField] private int dangerLevel = 1;

        [Header("Quest Influence")]
        [Tooltip("クエスト生成密度の倍率。Quest モジュール側で参照する。")]
        [SerializeField] private float questDensity = 1f;

        [Tooltip("このリージョンで出現するクエスト難易度の下限（0=E, 5=S）")]
        [SerializeField] private int minDifficultyIndex = 0;

        [Tooltip("このリージョンで出現するクエスト難易度の上限（0=E, 5=S）")]
        [SerializeField] private int maxDifficultyIndex = 2;

        public int DangerLevel => Mathf.Max(1, dangerLevel);
        public float QuestDensity => Mathf.Max(0f, questDensity);
        public int MinDifficultyIndex => Mathf.Clamp(minDifficultyIndex, 0, 5);
        public int MaxDifficultyIndex => Mathf.Clamp(maxDifficultyIndex, MinDifficultyIndex, 5);
    }
}
