namespace GuildSim.Dispatch
{
    public readonly struct DispatchResult
    {
        public bool Success { get; }
        public int GoldReward { get; }
        public int GoldPenalty { get; }
        public int ReputationGain { get; }
        public int ReputationPenalty { get; }
        public int ExperienceGain { get; }
        public string AdventurerId { get; }
        public string QuestInstanceId { get; }

        public DispatchResult(
            bool success,
            int goldReward,
            int goldPenalty,
            int reputationGain,
            int reputationPenalty,
            int experienceGain,
            string adventurerId,
            string questInstanceId)
        {
            Success = success;
            GoldReward = goldReward;
            GoldPenalty = goldPenalty;
            ReputationGain = reputationGain;
            ReputationPenalty = reputationPenalty;
            ExperienceGain = experienceGain;
            AdventurerId = adventurerId;
            QuestInstanceId = questInstanceId;
        }
    }
}
