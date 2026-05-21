namespace GuildSim.Shared
{
    public static class GameEvents
    {
        public const string DayPassed          = "time.day_passed";
        public const string SeasonChanged      = "time.season_changed";

        public const string QuestCompleted     = "quest.completed";
        public const string QuestFailed        = "quest.failed";
        public const string QuestBoardRefreshed = "quest.board_refreshed";

        public const string AdventurerHired    = "adventurer.hired";
        public const string AdventurerRetired  = "adventurer.retired";
        public const string AdventurerDispatched = "adventurer.dispatched";
        public const string AdventurerReturned = "adventurer.returned";

        public const string GoldChanged        = "economy.gold_changed";
        public const string ReputationChanged  = "economy.reputation_changed";

        public const string GuildRankUp        = "guild.rank_up";
        public const string GuildUpgraded      = "guild.upgraded";
    }
}
