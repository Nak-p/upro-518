using UnityEngine.UIElements;
using GuildSim.Shared;

namespace GuildSim.Adventurer
{
    public sealed class AdventurerDetailPanel
    {
        private readonly VisualElement root;
        private readonly Label nameLabel;
        private readonly Label classLabel;
        private readonly Label levelLabel;
        private readonly Label statusLabel;
        private readonly Label powerLabel;
        private readonly Label enduranceLabel;
        private readonly VisualElement traitsContainer;

        public AdventurerDetailPanel(VisualElement panelRoot)
        {
            root             = panelRoot;
            nameLabel        = panelRoot?.Q<Label>("name-label");
            classLabel       = panelRoot?.Q<Label>("class-label");
            levelLabel       = panelRoot?.Q<Label>("level-label");
            statusLabel      = panelRoot?.Q<Label>("status-label");
            powerLabel       = panelRoot?.Q<Label>("power-label");
            enduranceLabel   = panelRoot?.Q<Label>("endurance-label");
            traitsContainer  = panelRoot?.Q("traits-container");
        }

        public void Initialize()
        {
            if (root != null) root.style.display = DisplayStyle.None;
            EventBus.Subscribe<AdventurerState>(AdventurerEvents.DetailOpened, Show);
        }

        private void Show(AdventurerState state)
        {
            if (root == null || state == null) return;

            var def   = state.Definition;
            var stats = state.Stats;

            if (nameLabel      != null) nameLabel.text      = def.DisplayName;
            if (classLabel     != null) classLabel.text     = $"クラス: {def.AdventurerClass?.DisplayName ?? "----"}";
            if (levelLabel     != null) levelLabel.text     = $"Lv: {state.Level}";
            if (statusLabel    != null) statusLabel.text    = $"状態: {StatusText(state.Status)}";
            if (powerLabel     != null) powerLabel.text     = $"戦闘力: {stats.Power}";
            if (enduranceLabel != null) enduranceLabel.text = $"耐久力: {stats.Endurance}";

            BuildTraits(def);

            root.style.display = DisplayStyle.Flex;
        }

        private void BuildTraits(AdventurerDefinition def)
        {
            if (traitsContainer == null) return;
            traitsContainer.Clear();

            if (def.AdventurerClass != null)
            {
                foreach (var trait in def.AdventurerClass.ClassTraits)
                    AddTraitChip(trait.DisplayName, "class-trait");
            }
            foreach (var trait in def.PersonalTraits)
                AddTraitChip(trait.DisplayName, "personal-trait");
        }

        private void AddTraitChip(string traitName, string className)
        {
            var chip = new Label(traitName);
            chip.AddToClassList("trait-chip");
            chip.AddToClassList(className);
            traitsContainer.Add(chip);
        }

        private static string StatusText(AdventurerStatus status) => status switch
        {
            AdventurerStatus.Available  => "待機中",
            AdventurerStatus.OnMission  => "任務中",
            AdventurerStatus.Injured    => "負傷",
            AdventurerStatus.Retired    => "引退",
            _                           => "不明"
        };

        public void Dispose()
        {
            EventBus.Unsubscribe<AdventurerState>(AdventurerEvents.DetailOpened, Show);
        }
    }
}
