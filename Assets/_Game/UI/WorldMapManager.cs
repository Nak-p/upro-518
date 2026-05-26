using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
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
        [Tooltip("クエストピンの大きさを何セル分にするか")]
        [SerializeField] private float pinSizeInCells = 3f;

        private WorldMapPanel worldMapPanel;
        private VisualElement overlay;
        private VisualElement guildRoot;
        private bool useTilemapMode;
        private bool isMapVisible;

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
            useTilemapMode = useTilemap;
            if (useTilemap) tilemapCamera.enabled = false;

            // Tilemap カメラ方式では、マップ表示中にギルドUIルート（不透明背景を持つ）を
            // 隠して、カメラ（島）が UI Toolkit の透明部分から見えるようにする
            guildRoot = root.Q(className: "guild-hq");

            worldMapPanel = new WorldMapPanel(overlay, mapSprite, useTilemap);
            worldMapPanel.Initialize(bootstrap.World);

            // タイルマップ方式：ピンを実際のマップセル位置へ投影するためカメラ＋タイルマップを渡す
            if (useTilemap)
            {
                var painter = FindFirstObjectByType<WorldMapTilemapPainter>();
                if (painter != null && painter.BaseTilemap != null)
                    worldMapPanel.ConfigureWorldProjection(tilemapCamera, painter.BaseTilemap, pinSizeInCells);
                else
                    Debug.LogWarning("[WorldMapManager] WorldMapTilemapPainter/BaseTilemap が見つからず、ピンのセル投影は無効です。");
            }

            worldMapPanel.RefreshMarkers(BuildMarkers());
            worldMapPanel.RefreshEventPoints(BuildEventPointMarkers());

            root.Q<Button>("world-map-btn")?.RegisterCallback<ClickEvent>(_ => Show());
            overlay.Q<Button>("back-btn")?.RegisterCallback<ClickEvent>(_ => Hide());

            EventBus.Subscribe(WorldEvents.MapUnlockChanged, OnUnlockChanged);
        }

        private void OnUnlockChanged()
        {
            worldMapPanel?.RefreshMarkers(BuildMarkers());
            worldMapPanel?.RefreshEventPoints(BuildEventPointMarkers());
        }

        private MapQuestMarker[] BuildMarkers()
        {
            if (bootstrapConfig == null) return System.Array.Empty<MapQuestMarker>();

            // イベントポイント方式が有効な場合は個別クエストピンを非表示にする
            if (bootstrapConfig.EventPointBindings != null && bootstrapConfig.EventPointBindings.Length > 0)
                return System.Array.Empty<MapQuestMarker>();

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

        private MapEventPointMarker[] BuildEventPointMarkers()
        {
            if (bootstrapConfig == null) return System.Array.Empty<MapEventPointMarker>();

            var bindings = bootstrapConfig.EventPointBindings;
            if (bindings == null || bindings.Length == 0) return System.Array.Empty<MapEventPointMarker>();

            var ws     = bootstrap.World;
            var result = new MapEventPointMarker[bindings.Length];

            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding.EventPoint == null) continue;

                var ep = binding.EventPoint;
                var questDefs = binding.LinkedQuests ?? System.Array.Empty<QuestDefinition>();
                var questMarkers = new MapQuestMarker[questDefs.Length];

                for (int j = 0; j < questDefs.Length; j++)
                {
                    var def = questDefs[j];
                    if (def == null) continue;
                    questMarkers[j] = new MapQuestMarker(
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

                result[i] = new MapEventPointMarker(
                    eventPointId:      ep.Id,
                    displayName:       ep.DisplayName,
                    pointType:         ep.PointType,
                    normalizedPosition: ep.MapPosition,
                    linkedQuests:      questMarkers);
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
            // タイルマップ方式では背後のギルドUI（不透明背景）を隠してカメラを見せる
            if (useTilemapMode && guildRoot != null)
                guildRoot.style.display = DisplayStyle.None;
            // overlay を先に表示してから pin を構築し panelRoot.panel が null にならないようにする
            if (overlay != null) overlay.style.display = DisplayStyle.Flex;
            isMapVisible = true;
            worldMapPanel?.RefreshMarkers(BuildMarkers());
            worldMapPanel?.RefreshEventPoints(BuildEventPointMarkers());
        }

        private void LateUpdate()
        {
            // 表示中はピンをマップのワールド座標へ追従させる（カメラ変更にも対応）
            if (isMapVisible && useTilemapMode)
                worldMapPanel?.UpdatePinPositions();
        }

        public void Hide()
        {
            if (tilemapCamera != null) tilemapCamera.enabled = false;
            if (guildRoot != null)
                guildRoot.style.display = DisplayStyle.Flex;
            if (overlay != null) overlay.style.display = DisplayStyle.None;
            isMapVisible = false;
        }

        private void OnDestroy()
        {
            if (tilemapCamera != null) tilemapCamera.enabled = false;
            EventBus.Unsubscribe(WorldEvents.MapUnlockChanged, OnUnlockChanged);
            worldMapPanel?.Dispose();
        }
    }
}
