using UnityEngine;
using GuildSim.Shared;

namespace GuildSim.Time
{
    [CreateAssetMenu(menuName = "GuildSim/Time/Time Config", fileName = "TimeConfig")]
    public sealed class TimeConfig : BaseConfig
    {
        [Header("Day Settings")]
        [SerializeField] private float realSecondsPerDay = 5f;
        [SerializeField] private int daysPerSeason = 30;
        [SerializeField] private int seasonsPerYear = 4;

        [Header("Tick Settings")]
        [SerializeField] private float minRealSecondsPerDay = 1f;
        [SerializeField] private float maxRealSecondsPerDay = 60f;

        public float RealSecondsPerDay => Mathf.Clamp(realSecondsPerDay, minRealSecondsPerDay, maxRealSecondsPerDay);
        public int DaysPerSeason => Mathf.Max(1, daysPerSeason);
        public int SeasonsPerYear => Mathf.Max(1, seasonsPerYear);
        public int DaysPerYear => DaysPerSeason * SeasonsPerYear;
    }
}
