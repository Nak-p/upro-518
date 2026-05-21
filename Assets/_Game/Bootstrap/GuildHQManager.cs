using UnityEngine;
using UnityEngine.UIElements;
using GuildSim.Economy;
using GuildSim.Adventurer;
using GuildSim.Quest;
using GuildSim.Dispatch;

namespace GuildSim.Game
{
    /// <summary>
    /// Scene Organism: UI Toolkit 版 GuildHQ ルートコントローラー。
    /// UIDocument 1本 + GameBootstrap 参照だけで全 UI を初期化・配線する。
    /// Inspector 接続は 4 箇所のみ（bootstrap, uiDocument, 2枚のカードテンプレート）。
    /// </summary>
    public sealed class GuildHQManager : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private GameBootstrap  bootstrap;
        [SerializeField] private UIDocument     uiDocument;

        [Header("カードテンプレート（UXML）")]
        [SerializeField] private VisualTreeAsset adventurerCardTemplate;
        [SerializeField] private VisualTreeAsset questCardTemplate;

        // ----- pure C# コントローラー（MonoBehaviour 不要） -----
        private HudView          hudView;
        private GuildRosterPanel rosterPanel;
        private QuestBoardPanel  questBoardPanel;
        private DispatchPanelView dispatchPanel;

        private void Start()
        {
            if (bootstrap == null)
            {
                Debug.LogError("[GuildHQManager] GameBootstrap is not assigned.");
                return;
            }
            if (uiDocument == null)
            {
                Debug.LogError("[GuildHQManager] UIDocument is not assigned.");
                return;
            }

            var root = uiDocument.rootVisualElement;

            // HUD
            hudView = new HudView(root.Q("hud"));
            hudView.Initialize(bootstrap.Economy);

            // 冒険者一覧
            rosterPanel = new GuildRosterPanel(root.Q("roster-panel"), adventurerCardTemplate);
            rosterPanel.Initialize(bootstrap.Adventurers);

            // クエスト掲示板
            questBoardPanel = new QuestBoardPanel(root.Q("quest-board"), questCardTemplate);
            questBoardPanel.Initialize(bootstrap.Quests);

            // 派遣パネル
            dispatchPanel = new DispatchPanelView(root.Q("dispatch-panel"));
            dispatchPanel.Initialize(bootstrap.Dispatch, bootstrap.DispatchConfig);

            // 選択イベントを派遣パネルへ配線
            rosterPanel.AdventurerSelected  += dispatchPanel.OnAdventurerSelected;
            questBoardPanel.QuestSelected   += dispatchPanel.OnQuestSelected;

            Debug.Log("[GuildHQManager] All UI panels initialized via UI Toolkit.");
        }

        private void OnDestroy()
        {
            rosterPanel?.Dispose();
            questBoardPanel?.Dispose();
            dispatchPanel?.Dispose();
            hudView?.Dispose();
        }
    }
}
