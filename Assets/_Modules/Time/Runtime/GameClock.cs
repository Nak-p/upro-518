using System;

namespace GuildSim.Time
{
    public sealed class GameClock
    {
        private readonly TimeConfig config;
        private float accumulatedSeconds;

        public int TotalDays { get; private set; }
        public int DayOfSeason => TotalDays % config.DaysPerSeason;
        public int Season => (TotalDays / config.DaysPerSeason) % config.SeasonsPerYear;
        public int Year => TotalDays / config.DaysPerYear;

        public event Action DayPassed;
        public event Action<int> SeasonChanged;

        public GameClock(TimeConfig config)
        {
            this.config = config;
        }

        public void Tick(float deltaTime)
        {
            accumulatedSeconds += deltaTime;
            while (accumulatedSeconds >= config.RealSecondsPerDay)
            {
                accumulatedSeconds -= config.RealSecondsPerDay;
                AdvanceDay();
            }
        }

        private void AdvanceDay()
        {
            int prevSeason = Season;
            TotalDays++;
            DayPassed?.Invoke();
            if (Season != prevSeason)
                SeasonChanged?.Invoke(Season);
        }
    }
}
