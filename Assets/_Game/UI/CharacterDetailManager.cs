using UnityEngine;
using UnityEngine.UIElements;
using GuildSim.Adventurer;

namespace GuildSim.Game
{
    public sealed class CharacterDetailManager : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private UIDocument uiDocument;

        [Header("テンプレート（UXML）")]
        [SerializeField] private VisualTreeAsset detailTemplate;

        private AdventurerDetailPanel detailPanel;

        private void Start()
        {
            if (uiDocument == null || detailTemplate == null)
            {
                Debug.LogError("[CharacterDetailManager] Required references are not assigned.");
                return;
            }

            var root    = uiDocument.rootVisualElement;
            var overlay = detailTemplate.CloneTree();
            root.Add(overlay);

            detailPanel = new AdventurerDetailPanel(overlay);
            detailPanel.Initialize();

            var closeBtn = overlay.Q<Button>("close-btn");
            if (closeBtn != null)
                closeBtn.clicked += () => overlay.style.display = DisplayStyle.None;
        }

        private void OnDestroy()
        {
            detailPanel?.Dispose();
        }
    }
}
