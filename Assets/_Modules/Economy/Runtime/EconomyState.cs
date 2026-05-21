using UnityEngine;

namespace GuildSim.Economy
{
    public sealed class EconomyState
    {
        private readonly EconomyConfig config;

        public int Gold { get; private set; }
        public int Reputation { get; private set; }

        public EconomyState(EconomyConfig config)
        {
            this.config = config;
            Gold = config.StartingGold;
            Reputation = config.StartingReputation;
        }

        public bool TrySpendGold(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            return true;
        }

        public void AddGold(int amount) => Gold += Mathf.Max(0, amount);

        public void AddReputation(int amount)
            => Reputation = Mathf.Clamp(Reputation + amount, 0, config.MaxReputation);

        public void RemoveReputation(int amount)
            => Reputation = Mathf.Max(0, Reputation - Mathf.Max(0, amount));
    }
}
