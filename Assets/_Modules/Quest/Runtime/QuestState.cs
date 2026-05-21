using System;

namespace GuildSim.Quest
{
    public enum QuestStatus { Available, InProgress, Completed, Failed, Expired }

    public sealed class QuestState
    {
        public string InstanceId { get; }
        public QuestDefinition Definition { get; }
        public QuestStatus Status { get; private set; }
        public int PostedDay { get; }
        public string AssignedAdventurerId { get; private set; }

        public QuestState(string instanceId, QuestDefinition definition, int postedDay)
        {
            InstanceId = instanceId;
            Definition = definition;
            PostedDay = postedDay;
            Status = QuestStatus.Available;
        }

        public void Assign(string adventurerId)
        {
            Status = QuestStatus.InProgress;
            AssignedAdventurerId = adventurerId;
        }

        public void Complete() => Status = QuestStatus.Completed;
        public void Fail() => Status = QuestStatus.Failed;
        public void Expire() => Status = QuestStatus.Expired;

        public bool IsExpired(int currentDay)
            => Status == QuestStatus.Available
               && currentDay - PostedDay >= Definition.ExpiresAfterDays;
    }
}
