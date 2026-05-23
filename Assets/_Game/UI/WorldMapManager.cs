using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using GuildSim.Quest;
using GuildSim.World;
using GuildSim.Shared;

namespace GuildSim.Game
{
    public sealed class WorldMapManager : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private GameBootstrap       bootstrap;
        [SerializeField] private UIDocument          uiDocument;
        [SerializeField] private GameBootstrapConfig bootstrapConfig;

        [Header("テンプレート（UXML）")]
        [SerializeField] private VisualTreeAsset worldMapTemplate;

        [Header("タイルマップ（任意）")]
        [Tooltip("アサインするとタイルマップカメラ方式を使用。未アサインは従来方式。")]
        [SerializeField] private Camera tilemapCamera;

        private WorldMapPanel worldMapPanel;
        private VisualElement overlay;

        private void Start()
        {
            if (bootstrap == null || uiDocument == null || worldMapTemplate == null)
            {
                Debug.LogError("[WorldMapManager] bootstrap / uiDocument / worldMapTemplate が未アサインです。");
                return;
            }

            var root = uiDocument.rootVisualElement;

            var container = worldMapTemplate.CloneTree();
            overlay = container.Q(className: "overlay-panel") ?? container;
            overlay.style.display = DisplayStyle.None;
            root.Add(overlay);

            var mapSprite  = bootstrapConfig != null ? bootstrapConfig.WorldMapSprite : null;
            bool useTilemap = tilemapCamera != null;
            if (useTilemap) tilemapCamera.enabled = false;
            worldMapPanel = new WorldMapPanel(overlay, mapSprite, useTilemap);
            worldMapPanel.Initialize(bootstrap.World);
            worldMapPanel.RefreshMarkers(BuildMarkers());

            root.Q<Button>("world-map-btn")?.RegisterCallback<ClickEvent>(_ => Show());
            overlay.Q<Button>("back-btn")?.RegisterCallback<ClickEvent>(_ => Hide());

            EventBus.Subscribe(WorldEvents.MapUnlockChanged, OnUnlockChanged);
        }

        private void OnUnlockChanged()
        {
            worldMapPanel?.RefreshMarkers(BuildMarkers());
        }

        private MapQuestMarker[] BuildMarkers()
        {
            if (bootstrapConfig == null) return System.Array.Empty<MapQuestMarker>();

            var ws   = bootstrap.World;
            var defs = CollectAllMapQuests();
            var result = new MapQuestMarker[defs.Count];

            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                result[i] = new MapQuestMarker(
                    questDefinitionId:  def.Id,
                    displayName:        def.DisplayName,
                    questType:          LocalizeQuestType(def.QuestType),
                    difficultyLabel:    def.Difficulty.ToString(),
                    difficultyIndex:    (int)def.Difficulty,
                    rewardGold:         def.RewardGold,
                    rewardReputation:   def.RewardReputation,
                    rewardExperience:   def.RewardExperience,
                    durationDays:       def.DurationDays,
                    requiredPower:      def.RequiredPowerRating,
                    normalizedPosition: def.MapPosition,
                    isUnlocked:         ws.IsQuestUnlocked(def.Id),
                    isCompleted:        ws.IsQuestCompleted(def.Id),
                    icon:               def.Icon);
            }
            return result;
        }

        private List<QuestDefinition> CollectAllMapQuests()
        {
            var seen = new HashSet<string>();
            var list = new List<QuestDefinition>();

            void Add(QuestDefinition def)
            {
                if (def != null && seen.Add(def.Id))
                    list.Add(def);
            }

            foreach (var q in bootstrapConfig.InitiallyUnlockedQuests)
                Add(q);

            foreach (var binding in bootstrapConfig.QuestUnlockBindings)
            {
                Add(binding.KeyQuest);
                foreach (var q in binding.UnlocksQuests)
                    Add(q);
            }

            return list;
        }

        private static string LocalizeQuestType(QuestType type) => type switch
        {
            QuestType.Combat  => "戦闘",
            QuestType.Escort  => "護衛",
            QuestType.Explore => "探索",
            QuestType.Gather  => "採集",
            QuestType.Deliver => "配達",
            _                 => type.ToString()
        };

        public void Show()
        {
            if (tilemapCamera != null) tilemapCamera.enabled = true;
            worldMapPanel?.RefreshMarkers(BuildMarkers());
            if (overlay != null) overlay.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            if (tilemapCamera != null) tilemapCamera.enabled = false;
            if (overlay != null) overlay.style.display = DisplayStyle.None;
        }

        private void OnDestroy()
        {
            if (tilemapCamera != null) tilemapCamera.enabled = false;
            EventBus.Unsubscribe(WorldEvents.MapUnlockChanged, OnUnlockChanged);
            worldMapPanel?.Dispose();
        }
    }
}
