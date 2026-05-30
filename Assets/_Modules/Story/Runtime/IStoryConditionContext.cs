namespace GuildSim.Story
{
    /// <summary>
    /// Yarnスクリプトから参照されるGuildSimゲーム状態の抽象。
    /// 実装はGuildSim.Game層でStoryConditionBridgeとして提供される。
    /// この抽象により、GuildSim.StoryはQuest/Economy/Guild等を直接参照せずに済む。
    /// </summary>
    public interface IStoryConditionContext
    {
        bool IsQuestCompleted(string questId);
        int  GetGold();
        int  GetReputation();
        int  GetGuildRankIndex();
        int  GetAdventurerCount();
    }
}
