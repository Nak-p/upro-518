using System;
using System.Collections.Generic;

namespace GuildSim.Shared
{
    public static class EventBus
    {
        private static readonly Dictionary<string, List<Delegate>> handlers = new();

        public static void Subscribe<T>(string eventKey, Action<T> handler)
        {
            if (!handlers.TryGetValue(eventKey, out var list))
            {
                list = new List<Delegate>();
                handlers[eventKey] = list;
            }
            list.Add(handler);
        }

        public static void Subscribe(string eventKey, Action handler)
        {
            if (!handlers.TryGetValue(eventKey, out var list))
            {
                list = new List<Delegate>();
                handlers[eventKey] = list;
            }
            list.Add(handler);
        }

        public static void Unsubscribe<T>(string eventKey, Action<T> handler)
        {
            if (handlers.TryGetValue(eventKey, out var list))
                list.Remove(handler);
        }

        public static void Unsubscribe(string eventKey, Action handler)
        {
            if (handlers.TryGetValue(eventKey, out var list))
                list.Remove(handler);
        }

        public static void Publish<T>(string eventKey, T payload)
        {
            if (!handlers.TryGetValue(eventKey, out var list)) return;
            foreach (var d in list.ToArray())
            {
                if (d is Action<T> typed) typed(payload);
            }
        }

        public static void Publish(string eventKey)
        {
            if (!handlers.TryGetValue(eventKey, out var list)) return;
            foreach (var d in list.ToArray())
            {
                if (d is Action action) action();
            }
        }

        public static void Clear()
        {
            handlers.Clear();
        }
    }
}
