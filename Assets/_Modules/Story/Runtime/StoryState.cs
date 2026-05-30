using System.Collections.Generic;

namespace GuildSim.Story
{
    /// <summary>
    /// C#側で保持する最小限のストーリー進行状態。
    /// ストーリー本文の変数（フラグ）はYarnの変数ストレージ（$variable）が管理するため、
    /// ここではPlayOnce判定用の「クリア済みストーリーID」のみ追跡する。
    /// </summary>
    public sealed class StoryState
    {
        private readonly HashSet<string> completedStories = new();

        public bool IsStoryCompleted(string storyId) => completedStories.Contains(storyId);
        public void MarkStoryCompleted(string storyId) => completedStories.Add(storyId);

        public IReadOnlyCollection<string> CompletedStories => completedStories;
    }
}
