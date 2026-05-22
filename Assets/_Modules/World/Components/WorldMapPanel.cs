using System;
using System.Collections.Generic;
using UnityEngine;
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

        private readonly List<VisualElement> activePins    = new();
        private readonly List<VisualElement> terrainLabels = new();
        private bool terrainDrawerRegistered;

        private MapQuestMarker[] currentMarkers = Array.Empty<MapQuestMarker>();

        private static readonly (string text, float nx, float ny)[] TerrainLabels = {
            ("草原",   0.10f, 0.66f),
            ("森",     0.16f, 0.28f),
            ("山脈",   0.50f, 0.30f),
            ("砂漠",   0.60f, 0.76f),
            ("湖",     0.26f, 0.59f),
            ("霧の海", 0.84f, 0.18f),
        };

        public WorldMapPanel(VisualElement root, Sprite mapSprite)
        {
            panelRoot = root;
            mapCanvas = root?.Q("map-canvas");

            if (mapCanvas != null)
            {
                if (mapSprite != null)
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

        public void RefreshMarkers(MapQuestMarker[] markers)
        {
            currentMarkers = markers ?? Array.Empty<MapQuestMarker>();
            RebuildPins();
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
        }

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
        }
    }
}
