using UnityEngine;
using GuildSim.Shared;

namespace GuildSim.Time
{
    public sealed class TimeManager : MonoBehaviour
    {
        private GameClock clock;
        private bool running;

        public GameClock Clock => clock;

        public void Initialize(TimeConfig config)
        {
            clock = new GameClock(config);
            clock.DayPassed += () => EventBus.Publish(GameEvents.DayPassed);
            clock.SeasonChanged += s => EventBus.Publish(GameEvents.SeasonChanged, s);
            running = true;
        }

        private void Update()
        {
            if (running) clock?.Tick(UnityEngine.Time.deltaTime);
        }

        public void SetPaused(bool paused) => running = !paused;

        private void OnDestroy()
        {
            running = false;
        }
    }
}
