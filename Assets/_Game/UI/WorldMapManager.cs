using UnityEngine;
using UnityEngine.UIElements;
using GuildSim.World;

namespace GuildSim.Game
{
    public sealed class WorldMapManager : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private GameBootstrap bootstrap;
        [SerializeField] private UIDocument uiDocument;

        [Header("テンプレート（UXML）")]
        [SerializeField] private VisualTreeAsset worldMapTemplate;

        private WorldMapPanel worldMapPanel;
        private VisualElement overlay;

        private void Start()
        {
            if (bootstrap == null || uiDocument == null || worldMapTemplate == null)
            {
                Debug.LogError("[WorldMapManager] Required references are not assigned.");
                return;
            }

            var root = uiDocument.rootVisualElement;

            // CloneTree() は TemplateContainer を返すため、
            // 内側の .overlay-panel 要素を直接 root に追加する。
            // これにより position:absolute が root を基準に正しく適用される。
            var container = worldMapTemplate.CloneTree();
            overlay = container.Q(className: "overlay-panel") ?? container;
            overlay.style.display = DisplayStyle.None;
            root.Add(overlay);

            worldMapPanel = new WorldMapPanel(overlay, null);
            worldMapPanel.Initialize(bootstrap.World);

            // HUD の「ワールドマップ」ボタンをフック（GuildHQManager 変更不要）
            root.Q<Button>("world-map-btn")?.RegisterCallback<ClickEvent>(_ => Show());

            var closeBtn = overlay.Q<Button>("close-btn");
            if (closeBtn != null) closeBtn.clicked += Hide;
        }

        public void Show()
        {
            if (overlay != null) overlay.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            if (overlay != null) overlay.style.display = DisplayStyle.None;
        }

        private void OnDestroy()
        {
            worldMapPanel?.Dispose();
        }
    }
}
