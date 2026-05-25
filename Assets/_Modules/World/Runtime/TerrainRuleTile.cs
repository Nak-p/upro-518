using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GuildSim.World
{
    /// <summary>
    /// 隣接判定で「仲間（siblings）」に登録したタイルを自分と同一視する RuleTile。
    ///
    /// 例: 砂浜の RuleTile に水(RT_Water)を sibling として登録すると、
    /// 砂浜は「水に対しては縁取らず、草など仲間以外に対してだけ縁取る」ようになる。
    /// これにより、複数地形に接する境界でも単層で破綻なくオートタイルできる。
    /// </summary>
    public class TerrainRuleTile : RuleTile
    {
        [Tooltip("自分と同一視する隣接タイル（この方向には境界スプライトを出さない）")]
        public List<TileBase> siblings = new();

        public override bool RuleMatch(int neighbor, TileBase other)
        {
            bool same = other == this || (other != null && siblings.Contains(other));

            switch (neighbor)
            {
                case TilingRuleOutput.Neighbor.This:    return same;
                case TilingRuleOutput.Neighbor.NotThis: return !same;
            }
            return base.RuleMatch(neighbor, other);
        }
    }
}
