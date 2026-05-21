using UnityEngine;
using GuildSim.Shared;

namespace GuildSim.Economy
{
    [CreateAssetMenu(menuName = "GuildSim/Economy/Economy Config", fileName = "EconomyConfig")]
    public sealed class EconomyConfig : BaseConfig
    {
        [Header("Starting Resources")]
        [SerializeField] private int startingGold = 500;
        [SerializeField] private int startingReputation = 0;

        [Header("Upkeep")]
        [SerializeField] private int upkeepGoldPerAdventurerPerDay = 5;

        [Header("Reputation")]
        [SerializeField] private int maxReputation = 10000;

        public int StartingGold => Mathf.Max(0, startingGold);
        public int StartingReputation => Mathf.Max(0, startingReputation);
        public int UpkeepGoldPerAdventurerPerDay => Mathf.Max(0, upkeepGoldPerAdventurerPerDay);
        public int MaxReputation => Mathf.Max(1, maxReputation);
    }
}
