using UnityEngine.UIElements;
using GuildSim.Shared.UI;

namespace GuildSim.Quest
{
    /// <summary>
    /// クエストカード 1 枚の VisualElement バインド担当（pure C#、MonoBehaviour 不要）。
    /// ListView の makeItem/bindItem から生成・更新される。
    /// </summary>
    public sealed class QuestCardController
    {
        private readonly VisualElement root;
        private readonly Label         nameLabel;
        private readonly Label         typeLabel;
        private readonly Label         difficultyLabel;
        private readonly Label         rewardLabel;
        private readonly Label         durationLabel;
        private readonly Label         powerLabel;
        private readonly Label         statusBadge;

        public QuestState State { get; private set; }

        public QuestCardController(VisualElement element)
        {
            root            = element;
            nameLabel       = element.Q<Label>("quest-name");
            typeLabel       = element.Q<Label>("quest-type");
            difficultyLabel = element.Q<Label>("difficulty-label");
            rewardLabel     = element.Q<Label>("reward-label");
            durationLabel   = element.Q<Label>("duration-label");
            powerLabel      = element.Q<Label>("power-label");
            statusBadge     = element.Q<Label>("status-badge");
        }

        public void Bind(QuestState state)
        {
            State = state;
            var def = state.Definition;

            if (nameLabel != null) nameLabel.text = def.DisplayName;
            if (typeLabel != null) typeLabel.text = def.QuestType.ToString();

            int diffIdx = (int)def.Difficulty;
            if (difficultyLabel != null)
                difficultyLabel.text = DifficultyColorTable.GetLabel(diffIdx);

            // 難易度クラスを付け替えて色バッジ更新（CSS が色決定）
            for (int i = 0; i <= 5; i++)
                root.RemoveFromClassList($"difficulty-{i}");
            root.AddToClassList($"difficulty-{diffIdx}");

            if (rewardLabel   != null) rewardLabel.text   = $"💰 {def.RewardGold}G  ⭐ +{def.RewardReputation}";
            if (durationLabel != null) durationLabel.text = $"⏱ {def.DurationDays}日";
            if (powerLabel    != null) powerLabel.text    = $"推奨戦力: {def.RequiredPowerRating}";

            // 受注済みバッジ
            bool inProgress = state.Status == QuestStatus.InProgress;
            if (statusBadge != null)
            {
                statusBadge.text = "派遣中";
                if (inProgress)
                    statusBadge.RemoveFromClassList("status-badge--hidden");
                else
                    statusBadge.AddToClassList("status-badge--hidden");
            }

            // 受注済みはグレーアウト（選択不可）、Available のみ操作可能
            root.SetEnabled(state.Status == QuestStatus.Available);
        }
    }
}
