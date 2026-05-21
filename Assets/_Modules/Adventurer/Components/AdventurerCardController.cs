using UnityEngine.UIElements;

namespace GuildSim.Adventurer
{
    /// <summary>
    /// 冒険者カード 1 枚の VisualElement バインド担当（pure C#、MonoBehaviour 不要）。
    /// ListView の makeItem/bindItem から生成・更新される。
    /// </summary>
    public sealed class AdventurerCardController
    {
        private readonly VisualElement root;
        private readonly Label         nameLabel;
        private readonly Label         classLabel;
        private readonly Label         levelLabel;
        private readonly ProgressBar   powerBar;
        private readonly Label         statusLabel;

        public AdventurerState State { get; private set; }

        public AdventurerCardController(VisualElement element)
        {
            root        = element;
            nameLabel   = element.Q<Label>("name-label");
            classLabel  = element.Q<Label>("class-label");
            levelLabel  = element.Q<Label>("level-label");
            powerBar    = element.Q<ProgressBar>("power-bar");
            statusLabel = element.Q<Label>("status-label");
        }

        public void Bind(AdventurerState state)
        {
            State = state;
            Refresh();
        }

        public void Refresh()
        {
            if (State == null) return;

            if (nameLabel  != null) nameLabel.text  = State.Definition.DisplayName;
            if (classLabel != null) classLabel.text = State.Definition.AdventurerClass?.DisplayName ?? "-";
            if (levelLabel != null) levelLabel.text = $"Lv.{State.Level}";

            if (powerBar != null)
            {
                powerBar.highValue = 50f;
                powerBar.value     = State.Stats.Power;
            }

            // ステータスクラスを付け替えてカラーバッジ更新
            root.RemoveFromClassList("status-available");
            root.RemoveFromClassList("status-on-mission");
            root.RemoveFromClassList("status-injured");

            string statusText;
            string statusClass;
            switch (State.Status)
            {
                case AdventurerStatus.OnMission:
                    statusText  = "任務中"; statusClass = "status-on-mission"; break;
                case AdventurerStatus.Injured:
                    statusText  = "負傷中"; statusClass = "status-injured";    break;
                default:
                    statusText  = "待機";   statusClass = "status-available";  break;
            }

            if (statusLabel != null) statusLabel.text = statusText;
            root.AddToClassList(statusClass);
        }
    }
}
