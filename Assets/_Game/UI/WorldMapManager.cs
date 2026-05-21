using UnityEngine;
using UnityEngine.UIElements;
using GuildSim.World;

namespace GuildSim.Game
{
    public sealed class WorldMapManager : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private WorldConfig worldConfig;
        [SerializeField] private UIDocument uiDocument;

        [Header("テンプレート（UXML）")]
        [SerializeField] private VisualTreeAsset worldMapTemplate;

        private WorldService worldService;
        private WorldMapPanel worldMapPanel;
        private VisualElement overlay;

        private void Awake()
        {
            if (worldConfig == null)
            {
                Debug.LogError("[WorldMapManager] WorldConfig is not assigned.");
                return;
            }
            worldService = new WorldService(worldConfig);
        }

        private void Start()
        {
            if (uiDocument == null || worldMapTemplate == null) return;

            var root = uiDocument.rootVisualElement;
            overlay = worldMapTemplate.CloneTree();
            overlay.style.display = DisplayStyle.None;
            root.Add(overlay);

            worldMapPanel = new WorldMapPanel(overlay, null);
            worldMapPanel.Initialize(worldService);

            var closeBtn = overlay.Q<Button>("close-btn");
            if (closeBtn != null) closeBtn.clicked += Hide;
        }

        public void Show() => overlay?.SetEnabled(true);

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
