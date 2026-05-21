using System;
using System.Collections.Generic;
using System.Linq;
using GuildSim.Shared;

namespace GuildSim.Quest
{
    public sealed class QuestService
    {
        private readonly QuestConfig config;
        private readonly QuestDefinition[] questPool;
        private readonly List<QuestState> board = new();
        private readonly Dictionary<string, QuestState> allQuests = new();
        private int nextId;
        private int currentDay;

        public IReadOnlyList<QuestState> Board => board;

        public event Action BoardRefreshed;

        public QuestService(QuestConfig config, QuestDefinition[] questPool)
        {
            this.config = config;
            this.questPool = questPool;
            RefreshBoard();
        }

        public void OnDayPassed()
        {
            currentDay++;
            ExpireOldQuests();
            if (currentDay % config.RefreshIntervalDays == 0)
                RefreshBoard();
        }

        public bool TryAssignQuest(string questInstanceId, string adventurerId)
        {
            if (!allQuests.TryGetValue(questInstanceId, out var quest)) return false;
            if (quest.Status != QuestStatus.Available) return false;
            quest.Assign(adventurerId);
            board.Remove(quest);
            return true;
        }

        public void CompleteQuest(string questInstanceId)
        {
            if (allQuests.TryGetValue(questInstanceId, out var quest))
                quest.Complete();
        }

        public void FailQuest(string questInstanceId)
        {
            if (allQuests.TryGetValue(questInstanceId, out var quest))
                quest.Fail();
        }

        public bool TryGetQuest(string instanceId, out QuestState state)
            => allQuests.TryGetValue(instanceId, out state);

        private void RefreshBoard()
        {
            while (board.Count < config.BoardSize && questPool.Length > 0)
            {
                var def = RandomUtility.Pick(questPool);
                var state = new QuestState($"q_{nextId++}", def, currentDay);
                board.Add(state);
                allQuests[state.InstanceId] = state;
            }
            BoardRefreshed?.Invoke();
            EventBus.Publish(GameEvents.QuestBoardRefreshed);
        }

        private void ExpireOldQuests()
        {
            foreach (var q in board.ToList())
            {
                if (q.IsExpired(currentDay))
                {
                    q.Expire();
                    board.Remove(q);
                }
            }
        }
    }
}
