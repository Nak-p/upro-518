using UnityEngine;
using GuildSim.Shared;

namespace GuildSim.Story
{
    [CreateAssetMenu(menuName = "GuildSim/Story/Story Config", fileName = "StoryConfig")]
    public sealed class StoryConfig : BaseConfig
    {
        [Tooltip("ゲームに含めるストーリー一覧。アドオン式に追加するだけで認識される")]
        [SerializeField] private StoryDefinition[] stories = {};

        public StoryDefinition[] Stories => stories;
    }
}
