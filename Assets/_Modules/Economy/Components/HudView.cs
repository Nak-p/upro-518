using UnityEngine.UIElements;

namespace GuildSim.Economy
{
    /// <summary>
    /// HUD コントローラー（pure C#、MonoBehaviour 不要）。
    /// UIDocument.rootVisualElement から "hud" 要素を受け取り、
    /// EconomyService のイベントで金貨・名声ラベルをリアルタイム更新する。
    /// </summary>
    public sealed class HudView
    {
        private readonly Label goldLabel;
        private readonly Label repLabel;

        private EconomyService economy;

        public HudView(VisualElement hudRoot)
        {
            goldLabel = hudRoot?.Q<Label>("gold-label");
            repLabel  = hudRoot?.Q<Label>("rep-label");
        }

        public void Initialize(EconomyService economyService)
        {
            economy = economyService;
            economy.GoldChanged       += Refresh;
            economy.ReputationChanged += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            if (goldLabel != null) goldLabel.text = $"💰 {economy.Gold}";
            if (repLabel  != null) repLabel.text  = $"⭐ {economy.Reputation}";
        }

        public void Dispose()
        {
            if (economy == null) return;
            economy.GoldChanged       -= Refresh;
            economy.ReputationChanged -= Refresh;
        }
    }
}
