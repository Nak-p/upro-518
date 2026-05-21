using System;
using System.Collections.Generic;
using System.Linq;
using GuildSim.Shared;

namespace GuildSim.Adventurer
{
    public sealed class AdventurerService
    {
        private readonly Dictionary<string, AdventurerState> roster = new();
        private int nextId;

        public IReadOnlyCollection<AdventurerState> Roster => roster.Values;
        public int ActiveCount => roster.Values.Count(a => a.Status != AdventurerStatus.Retired);

        public event Action<AdventurerState> AdventurerAdded;
        public event Action<AdventurerState> AdventurerChanged;

        public AdventurerService(AdventurerDefinition[] starters)
        {
            foreach (var def in starters)
                AddAdventurer(def);
        }

        public AdventurerState AddAdventurer(AdventurerDefinition definition)
        {
            string id = $"adv_{nextId++}";
            var state = new AdventurerState(id, definition);
            roster[id] = state;
            AdventurerAdded?.Invoke(state);
            EventBus.Publish(GameEvents.AdventurerHired, state);
            return state;
        }

        public bool TryGetAdventurer(string id, out AdventurerState state)
            => roster.TryGetValue(id, out state);

        public void DispatchAdventurer(string id, int returnDay)
        {
            if (!roster.TryGetValue(id, out var state)) return;
            state.Dispatch(returnDay);
            AdventurerChanged?.Invoke(state);
            EventBus.Publish(GameEvents.AdventurerDispatched, state);
        }

        public void ReturnAdventurer(string id, bool success)
        {
            if (!roster.TryGetValue(id, out var state)) return;
            state.ReturnFromMission(success);
            AdventurerChanged?.Invoke(state);
            EventBus.Publish(GameEvents.AdventurerReturned, state);
        }

        public void OnDayPassed(int currentDay)
        {
            foreach (var state in roster.Values)
            {
                if (state.Status == AdventurerStatus.OnMission && currentDay >= state.MissionReturnDay)
                    ReturnAdventurer(state.Id, true);
            }
        }
    }
}
