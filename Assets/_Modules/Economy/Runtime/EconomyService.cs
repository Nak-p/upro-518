using System;
using GuildSim.Shared;

namespace GuildSim.Economy
{
    public sealed class EconomyService
    {
        private readonly EconomyState state;
        private readonly EconomyConfig config;

        public int Gold => state.Gold;
        public int Reputation => state.Reputation;

        public event Action GoldChanged;
        public event Action ReputationChanged;

        public EconomyService(EconomyConfig config)
        {
            this.config = config;
            state = new EconomyState(config);
        }

        public bool TrySpendGold(int amount)
        {
            bool result = state.TrySpendGold(amount);
            if (result)
            {
                GoldChanged?.Invoke();
                EventBus.Publish(GameEvents.GoldChanged, state.Gold);
            }
            return result;
        }

        public void AddGold(int amount)
        {
            state.AddGold(amount);
            GoldChanged?.Invoke();
            EventBus.Publish(GameEvents.GoldChanged, state.Gold);
        }

        public void AddReputation(int amount)
        {
            state.AddReputation(amount);
            ReputationChanged?.Invoke();
            EventBus.Publish(GameEvents.ReputationChanged, state.Reputation);
        }

        public void RemoveReputation(int amount)
        {
            state.RemoveReputation(amount);
            ReputationChanged?.Invoke();
            EventBus.Publish(GameEvents.ReputationChanged, state.Reputation);
        }

        public void OnDayPassed(int activeAdventurerCount)
        {
            int upkeep = config.UpkeepGoldPerAdventurerPerDay * activeAdventurerCount;
            state.TrySpendGold(upkeep);
            GoldChanged?.Invoke();
            EventBus.Publish(GameEvents.GoldChanged, state.Gold);
        }
    }
}
