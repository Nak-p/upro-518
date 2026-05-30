using UnityEngine;
using Yarn.Unity;
using GuildSim.Shared;

namespace GuildSim.Story
{
    /// <summary>
    /// 1つのストーリー（= YarnProject内の特定の開始ノード）を表す定義。
    /// 複数のStoryDefinitionが同じYarnProjectを共有し、開始ノードだけ変えてよい。
    /// → 1つのプロジェクト内でノードを線でつなぎ、大きな物語に育てられる。
    /// </summary>
    [CreateAssetMenu(menuName = "GuildSim/Story/Story Definition", fileName = "Story_New")]
    public sealed class StoryDefinition : BaseDefinition
    {
        [Tooltip("Yarnのコンパイル済みプロジェクト（.yarnファイル群を束ねたもの）")]
        [SerializeField] private YarnProject yarnProject;

        [Tooltip("このストーリーの開始ノード名（Yarnの title:）")]
        [SerializeField] private string startNode = "Start";

        [Tooltip("このストーリーを起動するトリガー（複数指定可）")]
        [SerializeField] private StoryTrigger[] triggers = {};

        [Tooltip("一度クリアしたら再起動しない")]
        [SerializeField] private bool playOnce = true;

        public YarnProject    YarnProject => yarnProject;
        public string         StartNode   => startNode;
        public StoryTrigger[] Triggers    => triggers;
        public bool           PlayOnce    => playOnce;
    }
}
