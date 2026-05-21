namespace GuildSim.Adventurer
{
    public readonly struct AdventurerStats
    {
        public int Power { get; }
        public int Endurance { get; }

        public AdventurerStats(int power, int endurance)
        {
            Power = power;
            Endurance = endurance;
        }
    }
}
