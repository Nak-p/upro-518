using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace GuildSim.World
{
    public sealed class WorldMapPanel
    {
        private readonly VisualElement panelRoot;
        private readonly VisualElement mapCanvas;

        private readonly VisualElement detailPanel;
        private readonly Label detailName;
        private readonly Label detailType;
        private readonly Label detailDifficulty;
        private readonly Label detailRewards;
        private readonly Label detailDuration;
        private readonly Label detailStatus;

        private readonly List<VisualElement> activePins      = new();
        private readonly List<VisualElement> activeEventPins = new();
        private readonly List<VisualElement> terrainLabels   = new();
        private bool terrainDrawerRegistered;

        // ワールド投影（タイルマップカメラ方式）：ピンを実際のマップセル位置へ配置
        private Camera   worldCamera;
        private Tilemap  worldTilemap;
        private float    pinCellSpan = 3f;  // ピンの一辺を何セル分の大きさにするか

        private MapQuestMarker[]      currentMarkers      = Array.Empty<MapQuestMarker>();
        private MapEventPointMarker[] currentEventMarkers = Array.Empty<MapEventPointMarker>();

        private static readonly (string text, float nx, float ny)[] TerrainLabels = {
            ("草原",   0.10f, 0.66f),
            ("森",     0.16f, 0.28f),
            ("山脈",   0.50f, 0.30f),
            ("砂漠",   0.60f, 0.76f),
            ("湖",     0.26f, 0.59f),
            ("霧の海", 0.84f, 0.18f),
        };

        public WorldMapPanel(VisualElement root, Sprite mapSprite, bool useTilemap = false)
        {
            panelRoot = root;
            mapCanvas = root?.Q("map-canvas");

            if (mapCanvas != null)
            {
                if (useTilemap)
                {
                    // Tilemap カメラが背景を担当するため、キャンバスと overlay を透明にする
                    mapCanvas.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
                    if (panelRoot != null)
                        panelRoot.AddToClassList("world-map-overlay--tilemap");
                }
                else if (mapSprite != null)
                {
                    mapCanvas.style.backgroundImage = new StyleBackground(mapSprite);
                }
                else
                {
                    WorldMapTerrainDrawer.Register(mapCanvas);
                    terrainDrawerRegistered = true;
                    AddTerrainLabels();
                }
            }

            detailPanel      = root?.Q("quest-detail-panel");
            detailName       = root?.Q<Label>("detail-quest-name");
            detailType       = root?.Q<Label>("detail-quest-type");
            detailDifficulty = root?.Q<Label>("detail-difficulty");
            detailRewards    = root?.Q<Label>("detail-rewards");
            detailDuration   = root?.Q<Label>("detail-duration");
            detailStatus     = root?.Q<Label>("detail-status");

            if (detailPanel != null)
                detailPanel.style.display = DisplayStyle.None;
        }

        public void Initialize(WorldService worldService) { }

        /// <summary>
        /// タイルマップカメラ方式で、ピンを実際のマップセル位置へ投影するための参照を設定。
        /// camera が描画する tilemap のワールド座標を画面→UIパネル座標へ変換してピンを配置する。
        /// </summary>
        public void ConfigureWorldProjection(Camera camera, Tilemap tilemap, float pinSizeInCells = 3f)
        {
            worldCamera  = camera;
            worldTilemap = tilemap;
            pinCellSpan  = Mathf.Max(0.25f, pinSizeInCells);
            UpdatePinPositions();
        }

        public void RefreshMarkers(MapQuestMarker[] markers)
        {
            currentMarkers = markers ?? Array.Empty<MapQuestMarker>();
            RebuildPins();
        }

        public void RefreshEventPoints(MapEventPointMarker[] eventMarkers)
        {
            currentEventMarkers = eventMarkers ?? Array.Empty<MapEventPointMarker>();
            RebuildEventPins();
        }

        /// <summary>
        /// 各ピンをマップのワールド座標 → 画面 → UIパネル座標へ投影して再配置する。
        /// worldCamera / worldTilemap 未設定時は CreatePin の正規化%配置のまま。
        /// カメラが静的でない場合に備え、表示中は毎フレーム呼ぶ想定。
        /// </summary>
        public void UpdatePinPositions()
        {
            if (worldCamera == null || worldTilemap == null) return;
            if (panelRoot?.panel == null) return;

            var cb = worldTilemap.cellBounds;
            Vector3 wMin = worldTilemap.CellToWorld(cb.min);
            Vector3 wMax = worldTilemap.CellToWorld(cb.max);

            // 1セルの画面上サイズ（パネル単位）を実測 → ピンをセル基準でスケール
            float cellWorld    = Mathf.Max(0.0001f, worldTilemap.cellSize.x);
            Vector2 cellPanelA = RuntimePanelUtils.ScreenToPanel(
                panelRoot.panel, worldCamera.WorldToScreenPoint(new Vector3(wMin.x, wMin.y, 0f)));
            Vector2 cellPanelB = RuntimePanelUtils.ScreenToPanel(
                panelRoot.panel, worldCamera.WorldToScreenPoint(new Vector3(wMin.x + cellWorld, wMin.y, 0f)));
            float cellPanelSize = Mathf.Abs(cellPanelB.x - cellPanelA.x);
            float pinSize       = Mathf.Max(4f, cellPanelSize * pinCellSpan);

            ProjectPinList(activePins, currentMarkers.Length,
                i => currentMarkers[i].NormalizedPosition, wMin, wMax, pinSize);
            ProjectPinList(activeEventPins, currentEventMarkers.Length,
                i => currentEventMarkers[i].NormalizedPosition, wMin, wMax, pinSize);
        }

        private void ProjectPinList(
            List<VisualElement> pins,
            int count,
            System.Func<int, Vector2> getPos,
            Vector3 wMin, Vector3 wMax,
            float pinSize)
        {
            for (int i = 0; i < pins.Count && i < count; i++)
            {
                var pin = pins[i];
                var n   = getPos(i);

                // 正規化 (0..1): x=左→右, y=上→下（UI慣習）をワールド座標へ
                float wx = Mathf.Lerp(wMin.x, wMax.x, n.x);
                float wy = Mathf.Lerp(wMax.y, wMin.y, n.y);

                Vector3 screen = worldCamera.WorldToScreenPoint(new Vector3(wx, wy, 0f));
                if (screen.z < 0f) { pin.style.display = DisplayStyle.None; continue; }

                pin.style.display = DisplayStyle.Flex;
                Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(
                    panelRoot.panel, new Vector2(screen.x, screen.y));

                // 位置：セル中心にピン中心が来るよう、サイズの半分だけオフセット
                pin.style.left       = panelPos.x;
                pin.style.top        = panelPos.y;
                pin.style.width      = pinSize;
                pin.style.height     = pinSize;
                pin.style.marginLeft = -pinSize * 0.5f;
                pin.style.marginTop  = -pinSize * 0.5f;

                // アイコン絵文字もピンサイズに追従
                var icon = pin.Q<Label>();
                if (icon != null)
                    icon.style.fontSize = Mathf.Max(6f, pinSize * 0.6f);
            }
        }

        private void RebuildPins()
        {
            foreach (var pin in activePins)
                pin.RemoveFromHierarchy();
            activePins.Clear();

            if (panelRoot == null) return;

            for (int i = 0; i < currentMarkers.Length; i++)
            {
                var pin = CreatePin(currentMarkers[i], i);
                panelRoot.Add(pin);
                activePins.Add(pin);
            }

            UpdatePinPositions();
        }

        private void RebuildEventPins()
        {
            foreach (var pin in activeEventPins)
                pin.RemoveFromHierarchy();
            activeEventPins.Clear();

            if (panelRoot == null) return;

            for (int i = 0; i < currentEventMarkers.Length; i++)
            {
                var pin = CreateEventPin(currentEventMarkers[i], i);
                panelRoot.Add(pin);
                activeEventPins.Add(pin);
            }

            UpdatePinPositions();
        }

        private VisualElement CreateEventPin(MapEventPointMarker marker, int index)
        {
            var pin = new VisualElement();
            pin.AddToClassList("map-pin");
            pin.AddToClassList("map-pin--event");
            pin.AddToClassList($"map-pin--event-{marker.PointType.ToString().ToLower()}");
            pin.pickingMode = PickingMode.Position;
            pin.tooltip     = marker.DisplayName;

            var icon = new Label(EventPointIcon(marker.PointType));
            icon.style.position       = Position.Absolute;
            icon.style.left           = 0;
            icon.style.top            = 0;
            icon.style.right          = 0;
            icon.style.bottom         = 0;
            icon.style.unityTextAlign = TextAnchor.MiddleCenter;
            icon.style.fontSize       = 16;
            icon.pickingMode          = PickingMode.Ignore;
            pin.Add(icon);

            pin.style.position   = Position.Absolute;
            pin.style.left       = new Length(marker.NormalizedPosition.x * 100f, LengthUnit.Percent);
            pin.style.top        = new Length(marker.NormalizedPosition.y * 100f, LengthUnit.Percent);
            pin.style.marginLeft = new Length(-16f, LengthUnit.Pixel);
            pin.style.marginTop  = new Length(-16f, LengthUnit.Pixel);

            int capturedIndex = index;
            pin.RegisterCallback<PointerDownEvent>(evt =>
            {
                evt.StopPropagation();
                ShowEventPointDetail(currentEventMarkers[capturedIndex]);
            });

            return pin;
        }

        private static string EventPointIcon(EventPointType type) => type switch
        {
            EventPointType.Dungeon => "🏰",
            EventPointType.Town    => "🏘",
            EventPointType.Ruin    => "🏛",
            EventPointType.Field   => "🌿",
            EventPointType.Special => "⭐",
            _                      => "📌",
        };

        private void ShowEventPointDetail(MapEventPointMarker marker)
        {
            if (detailPanel == null) return;

            detailPanel.style.display = DisplayStyle.Flex;

            if (detailName != null)
                detailName.text = $"{EventPointIcon(marker.PointType)} {marker.DisplayName}";

            if (detailType != null)
                detailType.text = $"タイプ：{LocalizePointType(marker.PointType)}";

            if (detailDifficulty != null)
            {
                for (int i = 0; i <= 5; i++)
                    detailDifficulty.RemoveFromClassList($"difficulty-{i}");
                detailDifficulty.text = $"関連クエスト：{marker.LinkedQuests.Length}件";
            }

            if (detailRewards != null)
            {
                if (marker.LinkedQuests.Length == 0)
                {
                    detailRewards.text = "（クエストなし）";
                }
                else
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var q in marker.LinkedQuests)
                    {
                        string status = q.IsCompleted ? "✅" : (q.IsUnlocked ? "📋" : "🔒");
                        sb.AppendLine($"{status} {q.DisplayName}");
                    }
                    detailRewards.text = sb.ToString().TrimEnd();
                }
            }

            if (detailDuration != null)
                detailDuration.text = string.Empty;

            if (detailStatus != null)
            {
                int completed = 0;
                int unlocked  = 0;
                foreach (var q in marker.LinkedQuests)
                {
                    if (q.IsCompleted) completed++;
                    else if (q.IsUnlocked) unlocked++;
                }
                detailStatus.text = $"✅ 完了 {completed} / 📋 受注可 {unlocked} / 🔒 未解放 {marker.LinkedQuests.Length - completed - unlocked}";
            }
        }

        private static string LocalizePointType(EventPointType type) => type switch
        {
            EventPointType.Dungeon => "ダンジョン",
            EventPointType.Town    => "町",
            EventPointType.Ruin    => "遺跡",
            EventPointType.Field   => "フィールド",
            EventPointType.Special => "特別",
            _                      => type.ToString(),
        };

        private void AddTerrainLabels()
        {
            foreach (var (text, nx, ny) in TerrainLabels)
            {
                var lbl = new Label(text);
                lbl.AddToClassList("terrain-label");
                lbl.pickingMode         = PickingMode.Ignore;
                lbl.style.position      = Position.Absolute;
                lbl.style.left          = new Length(nx * 100f, LengthUnit.Percent);
                lbl.style.top           = new Length(ny * 100f, LengthUnit.Percent);
                mapCanvas.Add(lbl);
                terrainLabels.Add(lbl);
            }
        }

        private VisualElement CreatePin(MapQuestMarker marker, int index)
        {
            var pin = new VisualElement();
            pin.AddToClassList("map-pin");
            pin.pickingMode = PickingMode.Position;

            var icon = new Label();
            icon.style.position         = Position.Absolute;
            icon.style.left             = 0;
            icon.style.top              = 0;
            icon.style.right            = 0;
            icon.style.bottom           = 0;
            icon.style.unityTextAlign   = TextAnchor.MiddleCenter;
            icon.style.fontSize         = 16;
            icon.pickingMode            = PickingMode.Ignore;

            if (!marker.IsUnlocked)
            {
                pin.AddToClassList("map-pin--locked");
                pin.pickingMode = PickingMode.Ignore;
                icon.text = "🔒";
                pin.tooltip = "未解放";
            }
            else if (marker.IsCompleted)
            {
                pin.AddToClassList("map-pin--completed");
                icon.text = "✅";
                pin.tooltip = marker.DisplayName;
            }
            else
            {
                pin.AddToClassList($"map-pin--difficulty-{marker.DifficultyIndex}");
                icon.text = "📍";
                pin.tooltip = marker.DisplayName;
            }

            pin.Add(icon);

            pin.style.position   = Position.Absolute;
            pin.style.left       = new Length(marker.NormalizedPosition.x * 100f, LengthUnit.Percent);
            pin.style.top        = new Length(marker.NormalizedPosition.y * 100f, LengthUnit.Percent);
            pin.style.marginLeft = new Length(-16f, LengthUnit.Pixel);
            pin.style.marginTop  = new Length(-16f, LengthUnit.Pixel);

            if (marker.IsUnlocked)
            {
                int capturedIndex = index;
                pin.RegisterCallback<PointerDownEvent>(evt =>
                {
                    evt.StopPropagation();
                    OnPinClicked(capturedIndex);
                });
            }

            return pin;
        }

        private void OnPinClicked(int index)
        {
            if (index < 0 || index >= currentMarkers.Length) return;
            ShowDetail(currentMarkers[index]);
        }

        private void ShowDetail(MapQuestMarker marker)
        {
            if (detailPanel == null) return;

            detailPanel.style.display = DisplayStyle.Flex;

            if (detailName != null)
                detailName.text = marker.DisplayName;

            if (detailType != null)
                detailType.text = $"タイプ：{marker.QuestType}";

            if (detailDifficulty != null)
            {
                detailDifficulty.text = $"難易度：{marker.DifficultyLabel}";
                for (int i = 0; i <= 5; i++)
                    detailDifficulty.RemoveFromClassList($"difficulty-{i}");
                detailDifficulty.AddToClassList($"difficulty-{marker.DifficultyIndex}");
            }

            if (detailRewards != null)
                detailRewards.text =
                    $"💰 {marker.RewardGold}G  ⭐ +{marker.RewardReputation}  ✨ +{marker.RewardExperience}XP";

            if (detailDuration != null)
                detailDuration.text =
                    $"⏱ {marker.DurationDays}日  推奨戦力：{marker.RequiredPower}";

            if (detailStatus != null)
                detailStatus.text = marker.IsCompleted ? "✅ 完了済み" : "📋 受注可能";
        }

        public void Dispose()
        {
            if (terrainDrawerRegistered)
            {
                WorldMapTerrainDrawer.Unregister(mapCanvas);
                terrainDrawerRegistered = false;
            }

            foreach (var lbl in terrainLabels)
                lbl.RemoveFromHierarchy();
            terrainLabels.Clear();

            foreach (var pin in activePins)
                pin.RemoveFromHierarchy();
            activePins.Clear();

            foreach (var pin in activeEventPins)
                pin.RemoveFromHierarchy();
            activeEventPins.Clear();
        }
    }
}
