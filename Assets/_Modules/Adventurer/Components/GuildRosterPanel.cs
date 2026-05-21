using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using GuildSim.Shared;

namespace GuildSim.Adventurer
{
    /// <summary>
    /// 冒険者一覧パネル（pure C#、MonoBehaviour 不要）。
    /// UIDocument.rootVisualElement から "roster-panel" 要素を受け取り、
    /// ListView で AdventurerCardController を仮想スクロール表示する。
    /// </summary>
    public sealed class GuildRosterPanel
    {
        private readonly ListView            listView;
        private readonly VisualTreeAsset     cardTemplate;
        private readonly List<AdventurerState> rosterList = new();

        private AdventurerService service;

        public event Action<AdventurerState> AdventurerSelected;

        public GuildRosterPanel(VisualElement panelRoot, VisualTreeAsset adventurerCardTemplate)
        {
            cardTemplate = adventurerCardTemplate;
            listView     = panelRoot?.Q<ListView>("roster-list");
            SetupListView();
        }

        private void SetupListView()
        {
            if (listView == null) return;

            listView.makeItem = () =>
            {
                var item       = cardTemplate.CloneTree();
                var cardRoot   = item.Q(className: "adventurer-card") ?? item;
                item.userData  = new AdventurerCardController(cardRoot);
                return item;
            };

            listView.bindItem = (element, index) =>
            {
                if (index >= rosterList.Count) return;
                var ctrl = element.userData as AdventurerCardController;
                ctrl?.Bind(rosterList[index]);
            };

            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.itemsSource          = rosterList;
            listView.selectionType        = SelectionType.Single;
            listView.selectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(IEnumerable<object> selected)
        {
            var state = selected.FirstOrDefault() as AdventurerState;
            AdventurerSelected?.Invoke(state);
            if (state != null)
                EventBus.Publish(AdventurerEvents.DetailOpened, state);
        }

        public void Initialize(AdventurerService adventurerService)
        {
            service = adventurerService;
            service.AdventurerAdded   += OnAdventurerAdded;
            service.AdventurerChanged += OnAdventurerChanged;
            EventBus.Subscribe(GameEvents.AdventurerReturned, Rebuild);
            Rebuild();
        }

        private void Rebuild()
        {
            rosterList.Clear();
            rosterList.AddRange(
                service.Roster.Where(s => s.Status != AdventurerStatus.Retired));
            listView?.RefreshItems();
        }

        private void OnAdventurerAdded(AdventurerState _) => Rebuild();

        private void OnAdventurerChanged(AdventurerState state)
        {
            int idx = rosterList.FindIndex(s => s.Id == state.Id);
            if (idx >= 0) listView?.RefreshItem(idx);
        }

        public void Dispose()
        {
            if (service == null) return;
            service.AdventurerAdded   -= OnAdventurerAdded;
            service.AdventurerChanged -= OnAdventurerChanged;
            EventBus.Unsubscribe(GameEvents.AdventurerReturned, Rebuild);
        }
    }
}
