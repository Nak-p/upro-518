using UnityEngine.UIElements;
using GuildSim.Adventurer;
using GuildSim.Quest;
using GuildSim.Shared;

namespace GuildSim.Dispatch
{
    /// <summary>
    /// 派遣操作パネル（pure C#、MonoBehaviour 不要）。
    /// UIDocument.rootVisualElement から "dispatch-panel" 要素を受け取り、
    /// 冒険者・クエスト選択 → 成功率表示 → 派遣ボタン → 結果表示を管理する。
    /// </summary>
    public sealed class DispatchPanelView
    {
        private readonly Label  adventurerLabel;
        private readonly Label  questLabel;
        private readonly Label  rateLabel;
        private readonly Button dispatchButton;
        private readonly Label  resultLabel;

        private DispatchManager dispatchManager;
        private DispatchConfig  dispatchConfig;
        private AdventurerState selectedAdventurer;
        private QuestState      selectedQuest;
        private int             currentDay;

        public DispatchPanelView(VisualElement panelRoot)
        {
            adventurerLabel = panelRoot?.Q<Label>("adventurer-label");
            questLabel      = panelRoot?.Q<Label>("quest-label");
            rateLabel       = panelRoot?.Q<Label>("rate-label");
            dispatchButton  = panelRoot?.Q<Button>("dispatch-btn");
            resultLabel     = panelRoot?.Q<Label>("result-label");
        }

        public void Initialize(DispatchManager manager, DispatchConfig config)
        {
            dispatchManager = manager;
            dispatchConfig  = config;
            dispatchManager.DispatchResolved += OnDispatchResolved;
            EventBus.Subscribe(GameEvents.DayPassed, OnDayPassed);
            dispatchButton?.RegisterCallback<ClickEvent>(_ => OnDispatchClicked());
            RefreshButton();
        }

        public void OnAdventurerSelected(AdventurerState state)
        {
            selectedAdventurer = state;
            if (adventurerLabel != null)
                adventurerLabel.text = state != null
                    ? $"冒険者: {state.Definition.DisplayName} (戦力{state.Stats.Power})"
                    : "冒険者を選択してください";
            RefreshSuccessRate();
            RefreshButton();
        }

        public void OnQuestSelected(QuestState state)
        {
            selectedQuest = state;
            if (questLabel != null)
                questLabel.text = state != null
                    ? $"クエスト: {state.Definition.DisplayName}"
                    : "クエストを選択してください";
            RefreshSuccessRate();
            RefreshButton();
        }

        private void RefreshSuccessRate()
        {
            if (selectedAdventurer == null || selectedQuest == null || rateLabel == null) return;
            float rate = DispatchCalculator.ComputeSuccessRate(
                selectedAdventurer, selectedQuest.Definition, dispatchConfig);
            rateLabel.text = $"成功率: {rate * 100f:F0}%";
        }

        private void RefreshButton()
        {
            bool canDispatch = selectedAdventurer != null
                && selectedQuest != null
                && selectedAdventurer.Status == AdventurerStatus.Available
                && selectedQuest.Status      == QuestStatus.Available;
            dispatchButton?.SetEnabled(canDispatch);
        }

        private void OnDispatchClicked()
        {
            if (selectedAdventurer == null || selectedQuest == null) return;

            bool ok = dispatchManager.TryDispatch(
                selectedAdventurer.Id, selectedQuest.InstanceId, currentDay);

            if (resultLabel != null)
                resultLabel.text = ok ? "✓ 派遣しました！" : "× 派遣できませんでした";

            selectedAdventurer = null;
            selectedQuest      = null;
            if (adventurerLabel != null) adventurerLabel.text = "冒険者を選択してください";
            if (questLabel      != null) questLabel.text      = "クエストを選択してください";
            if (rateLabel       != null) rateLabel.text       = "";
            RefreshButton();
        }

        private void OnDispatchResolved(DispatchResult result)
        {
            if (resultLabel == null) return;
            resultLabel.text = result.Success
                ? $"✓ 成功！ +{result.GoldReward}G +{result.ReputationGain}名声"
                : $"× 失敗… -{result.ReputationPenalty}名声";
        }

        private void OnDayPassed() => currentDay++;

        public void Dispose()
        {
            if (dispatchManager != null)
                dispatchManager.DispatchResolved -= OnDispatchResolved;
            EventBus.Unsubscribe(GameEvents.DayPassed, OnDayPassed);
        }
    }
}
