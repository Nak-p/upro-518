using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using GuildSim.Shared;

namespace GuildSim.World
{
    public sealed class WorldMapPanel
    {
        private readonly VisualElement root;
        private readonly ListView regionList;
        private readonly List<RegionDefinition> regions = new();
        private WorldService service;

        public WorldMapPanel(VisualElement panelRoot, VisualTreeAsset template)
        {
            root = panelRoot;
            regionList = root?.Q<ListView>("region-list");
            SetupListView(template);
        }

        private void SetupListView(VisualTreeAsset template)
        {
            if (regionList == null) return;

            regionList.makeItem = () =>
            {
                if (template != null) return template.CloneTree();

                // テンプレート未設定時はコードで要素を生成
                var item = new VisualElement();
                item.AddToClassList("region-item");

                var nameLabel = new Label { name = "region-name" };
                nameLabel.AddToClassList("region-name");

                var dangerLabel = new Label { name = "danger-label" };
                dangerLabel.AddToClassList("region-danger");

                item.Add(nameLabel);
                item.Add(dangerLabel);
                return item;
            };

            regionList.bindItem = (element, index) =>
            {
                if (index >= regions.Count) return;
                var region = regions[index];
                var label = element.Q<Label>("region-name") ?? element as Label;
                if (label != null) label.text = region.DisplayName;
                var danger = element.Q<Label>("danger-label");
                if (danger != null) danger.text = $"危険度 {region.DangerLevel}";
                element.EnableInClassList("active-region",
                    service != null && service.State.ActiveRegionIndex == index);
            };

            regionList.selectionType = SelectionType.Single;
            regionList.selectionChanged += OnSelectionChanged;
            regionList.itemsSource = regions;
        }

        private void OnSelectionChanged(IEnumerable<object> selected)
        {
            if (service == null) return;
            int idx = regionList.selectedIndex;
            if (idx >= 0) service.SelectRegion(idx);
        }

        public void Initialize(WorldService worldService)
        {
            service = worldService;
            regions.Clear();
            regions.AddRange(service.Regions);
            regionList?.RefreshItems();

            EventBus.Subscribe<RegionDefinition>(WorldEvents.RegionSelected, OnRegionSelected);
        }

        private void OnRegionSelected(RegionDefinition region)
        {
            regionList?.RefreshItems();
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<RegionDefinition>(WorldEvents.RegionSelected, OnRegionSelected);
        }
    }
}
