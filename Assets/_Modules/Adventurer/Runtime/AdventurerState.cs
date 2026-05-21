using System;

namespace GuildSim.Adventurer
{
    public enum AdventurerStatus { Available, OnMission, Injured, Retired }

    public sealed class AdventurerState
    {
        public string Id { get; }
        public AdventurerDefinition Definition { get; }
        public int Level { get; private set; }
        public int Experience { get; private set; }
        public AdventurerStatus Status { get; private set; }
        public int MissionReturnDay { get; private set; }

        public AdventurerStats Stats => AdventurerStatsCalculator.Compute(Definition.AdventurerClass, Level);

        public AdventurerState(string id, AdventurerDefinition definition)
        {
            Id = id;
            Definition = definition;
            Level = definition.StartingLevel;
            Status = AdventurerStatus.Available;
        }

        public void Dispatch(int returnDay)
        {
            Status = AdventurerStatus.OnMission;
            MissionReturnDay = returnDay;
        }

        public void ReturnFromMission(bool success)
        {
            Status = AdventurerStatus.Available;
            if (success) GainExperience(10);
        }

        public void GainExperience(int amount)
        {
            Experience += amount;
            int expForNextLevel = Level * 100;
            if (Experience >= expForNextLevel)
            {
                Experience -= expForNextLevel;
                Level++;
            }
        }

        public void Retire() => Status = AdventurerStatus.Retired;
    }
}
