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
            overlay = worldMapTemplate.CloneTree();
            overlay.style.display = DisplayStyle.None;
            root.Add(overlay);

            worldMapPanel = new WorldMapPanel(overlay, null);
            worldMapPanel.Initialize(bootstrap.World);

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
