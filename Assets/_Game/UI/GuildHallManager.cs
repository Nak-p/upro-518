using UnityEngine;
using UnityEngine.UIElements;
using GuildSim.Guild;

namespace GuildSim.Game
{
    public sealed class GuildHallManager : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private GameBootstrap bootstrap;
        [SerializeField] private UIDocument uiDocument;

        [Header("テンプレート（UXML）")]
        [SerializeField] private VisualTreeAsset guildHallTemplate;

        private GuildHallPanel guildHallPanel;
        private VisualElement overlay;

        private void Start()
        {
            if (bootstrap == null || uiDocument == null || guildHallTemplate == null)
            {
                Debug.LogError("[GuildHallManager] Required references are not assigned.");
                return;
            }

            var root = uiDocument.rootVisualElement;
            overlay = guildHallTemplate.CloneTree();
            overlay.style.display = DisplayStyle.None;
            root.Add(overlay);

            guildHallPanel = new GuildHallPanel(overlay);
            guildHallPanel.Initialize(bootstrap.Guild);

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
            guildHallPanel?.Dispose();
        }
    }
}
