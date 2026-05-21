using System;
using GuildSim.Shared;
using GuildSim.Adventurer;
using GuildSim.Quest;

namespace GuildSim.Dispatch
{
    public sealed class DispatchManager
    {
        private readonly DispatchConfig config;
        private readonly AdventurerService adventurerService;
        private readonly QuestService questService;

        public event Action<DispatchResult> DispatchResolved;

        public DispatchManager(DispatchConfig config, AdventurerService adventurerService, QuestService questService)
        {
            this.config = config;
            this.adventurerService = adventurerService;
            this.questService = questService;
        }

        public bool TryDispatch(string adventurerId, string questInstanceId, int currentDay)
        {
            if (!adventurerService.TryGetAdventurer(adventurerId, out var adventurer)) return false;
            if (adventurer.Status != AdventurerStatus.Available) return false;
            if (!questService.TryGetQuest(questInstanceId, out var quest)) return false;
            if (quest.Status != QuestStatus.Available) return false;

            int returnDay = currentDay + quest.Definition.DurationDays;
            adventurerService.DispatchAdventurer(adventurerId, returnDay);
            questService.TryAssignQuest(questInstanceId, adventurerId);

            ScheduleResolution(adventurer, quest, returnDay);
            return true;
        }

        private void ScheduleResolution(
            AdventurerState adventurer,
            QuestState quest,
            int returnDay)
        {
            var result = DispatchCalculator.Resolve(adventurer, quest, config);
            EventBus.Subscribe<int>(GameEvents.DayPassed, dayCount => OnDayCheck(result, returnDay));
        }

        private void OnDayCheck(DispatchResult result, int returnDay)
        {
            if (!adventurerService.TryGetAdventurer(result.AdventurerId, out var adv)) return;
            if (adv.Status != AdventurerStatus.OnMission) return;
            if (adv.MissionReturnDay > returnDay) return;

            Resolve(result);
            EventBus.Unsubscribe<int>(GameEvents.DayPassed, dayCount => OnDayCheck(result, returnDay));
        }

        private void Resolve(DispatchResult result)
        {
            if (result.Success)
                questService.CompleteQuest(result.QuestInstanceId);
            else
                questService.FailQuest(result.QuestInstanceId);

            adventurerService.ReturnAdventurer(result.AdventurerId, result.Success);

            DispatchResolved?.Invoke(result);
            string eventKey = result.Success ? GameEvents.QuestCompleted : GameEvents.QuestFailed;
            EventBus.Publish(eventKey, result);
        }
    }
}
