using UnityEngine;

namespace GuildSim.World
{
    /// <summary>
    /// ワールドマップ上のイベントポイント 1 件を表す DTO。
    /// _Game レイヤーが組み立て、WorldMapPanel に渡す。
    /// GuildSim.Quest アセンブリへの参照を一切持たない。
    /// </summary>
    public readonly struct MapEventPointMarker
    {
        public string             EventPointId  { get; }
        public string             DisplayName   { get; }
        public EventPointType     PointType     { get; }
        public Vector2            NormalizedPosition { get; }
        public MapQuestMarker[]   LinkedQuests  { get; }

        public MapEventPointMarker(
            string           eventPointId,
            string           displayName,
            EventPointType   pointType,
            Vector2          normalizedPosition,
            MapQuestMarker[] linkedQuests)
        {
            EventPointId       = eventPointId;
            DisplayName        = displayName;
            PointType          = pointType;
            NormalizedPosition = normalizedPosition;
            LinkedQuests       = linkedQuests ?? System.Array.Empty<MapQuestMarker>();
        }
    }
}
