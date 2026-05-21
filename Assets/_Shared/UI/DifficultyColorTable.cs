using UnityEngine;

namespace GuildSim.Shared.UI
{
    /// <summary>難易度ごとの色を一元管理（ハードコード禁止のため static テーブルで管理）</summary>
    public static class DifficultyColorTable
    {
        private static readonly Color[] Colors =
        {
            new Color(0.6f, 0.9f, 0.6f), // E: 薄緑
            new Color(0.6f, 0.8f, 1.0f), // D: 薄青
            new Color(1.0f, 0.9f, 0.4f), // C: 黄
            new Color(1.0f, 0.6f, 0.2f), // B: オレンジ
            new Color(1.0f, 0.3f, 0.3f), // A: 赤
            new Color(0.8f, 0.3f, 1.0f), // S: 紫
        };

        private static readonly string[] Labels = { "E", "D", "C", "B", "A", "S" };

        public static Color GetColor(int difficultyIndex)
            => Colors[Mathf.Clamp(difficultyIndex, 0, Colors.Length - 1)];

        public static string GetLabel(int difficultyIndex)
            => Labels[Mathf.Clamp(difficultyIndex, 0, Labels.Length - 1)];
    }
}
