using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using GuildSim.Shared;

namespace GuildSim.Quest
{
    /// <summary>
    /// クエスト掲示板パネル（pure C#、MonoBehaviour 不要）。
    /// UIDocument.rootVisualElement から "quest-board" 要素を受け取り、
    /// ListView で QuestCardController を仮想スクロール表示する。
    /// </summary>
    public sealed class QuestBoardPanel
    {
        private readonly ListView         listView;
        private readonly VisualTreeAsset  cardTemplate;
        private readonly List<QuestState> boardList = new();

        private QuestService service;

        public event Action<QuestState> QuestSelected;

        public QuestBoardPanel(VisualElement panelRoot, VisualTreeAsset questCardTemplate)
        {
            cardTemplate = questCardTemplate;
            listView     = panelRoot?.Q<ListView>("quest-list");
            SetupListView();
        }

        private void SetupListView()
        {
            if (listView == null) return;

            listView.makeItem = () =>
            {
                var item      = cardTemplate.CloneTree();
                var cardRoot  = item.Q(className: "quest-card") ?? item;
                item.userData = new QuestCardController(cardRoot);
                return item;
            };

            listView.bindItem = (element, index) =>
            {
                if (index >= boardList.Count) return;
                var ctrl = element.userData as QuestCardController;
                ctrl?.Bind(boardList[index]);
            };

            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.itemsSource          = boardList;
            listView.selectionType        = SelectionType.Single;
            listView.selectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(IEnumerable<object> selected)
        {
            var state = selected.FirstOrDefault() as QuestState;
            QuestSelected?.Invoke(state);
        }

        public void Initialize(QuestService questService)
        {
            service = questService;
            service.BoardRefreshed += Rebuild;
            EventBus.Subscribe(GameEvents.QuestBoardRefreshed, Rebuild);
            Rebuild();
        }

        private void Rebuild()
        {
            boardList.Clear();
            boardList.AddRange(service.Board);
            listView?.RefreshItems();
        }

        public void Dispose()
        {
            if (service == null) return;
            service.BoardRefreshed -= Rebuild;
            EventBus.Unsubscribe(GameEvents.QuestBoardRefreshed, Rebuild);
        }
    }
}
