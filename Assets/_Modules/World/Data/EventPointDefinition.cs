using UnityEngine;
using GuildSim.Shared;

namespace GuildSim.World
{
    [CreateAssetMenu(menuName = "GuildSim/World/Event Point Definition", fileName = "EventPointDefinition")]
    public sealed class EventPointDefinition : BaseDefinition
    {
        [SerializeField] private Vector2        mapPosition;
        [SerializeField] private EventPointType pointType;

        public Vector2        MapPosition => mapPosition;
        public EventPointType PointType   => pointType;
    }
}
