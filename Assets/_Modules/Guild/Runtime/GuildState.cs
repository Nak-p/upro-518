namespace GuildSim.Guild
{
    public sealed class GuildState
    {
        public int CurrentRankIndex { get; private set; }
        public GuildRankDefinition CurrentRank { get; private set; }

        public GuildState(GuildRankDefinition startRank)
        {
            CurrentRank = startRank;
            CurrentRankIndex = 0;
        }

        public void SetRank(int index, GuildRankDefinition rank)
        {
            CurrentRankIndex = index;
            CurrentRank = rank;
        }
    }
}
