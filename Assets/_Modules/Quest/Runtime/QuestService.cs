using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
            // board からは削除しない → InProgress として掲示板に残す
            BoardRefreshed?.Invoke();
            EventBus.Publish(GameEvents.QuestBoardRefreshed);
            return true;
        }

        public void CompleteQuest(string questInstanceId)
        {
            if (!allQuests.TryGetValue(questInstanceId, out var quest)) return;
            quest.Complete();
            board.Remove(quest);
            BoardRefreshed?.Invoke();
            EventBus.Publish(GameEvents.QuestBoardRefreshed);
        }

        public void FailQuest(string questInstanceId)
        {
            if (!allQuests.TryGetValue(questInstanceId, out var quest)) return;
            quest.Fail();
            board.Remove(quest);
            BoardRefreshed?.Invoke();
            EventBus.Publish(GameEvents.QuestBoardRefreshed);
        }

        public bool TryGetQuest(string instanceId, out QuestState state)
            => allQuests.TryGetValue(instanceId, out state);

        /// <summary>ストーリーコマンドから直接掲示板に追加する（日次リフレッシュを待たない）</summary>
        public void UnlockStoryQuest(QuestDefinition def)
        {
            if (def == null) return;
            if (board.Any(q => q.Definition.Id == def.Id)) return;
            var newState = new QuestState($"q_{nextId++}", def, currentDay);
            board.Add(newState);
            allQuests[newState.InstanceId] = newState;
            BoardRefreshed?.Invoke();
            EventBus.Publish(GameEvents.QuestBoardRefreshed);
        }

        private void RefreshBoard()
        {
            // 既に掲示板に出ているクエスト定義を除いた候補からのみ追加する
            var onBoard = new HashSet<string>(board.Select(q => q.Definition.Id));
            var candidates = questPool.Where(q => !onBoard.Contains(q.Id)).ToList();
            // Fisher-Yates shuffle
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            foreach (var def in candidates)
            {
                if (board.Count >= config.BoardSize) break;
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
