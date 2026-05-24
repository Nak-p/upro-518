using System.Linq;
using UnityEngine;
using GuildSim.Shared;
using GuildSim.Time;
using GuildSim.Economy;
using GuildSim.Adventurer;
using GuildSim.Quest;
using GuildSim.Dispatch;
using GuildSim.Guild;
using GuildSim.World;

namespace GuildSim.Game
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameBootstrapConfig config;

        private TimeManager timeManager;
        private EconomyService economyService;
        private AdventurerService adventurerService;
        private QuestService questService;
        private DispatchManager dispatchManager;
        private GuildService guildService;
        private WorldService worldService;

        public EconomyService Economy => economyService;
        public AdventurerService Adventurers => adventurerService;
        public QuestService Quests => questService;
        public DispatchManager Dispatch => dispatchManager;
        public DispatchConfig DispatchConfig => config?.DispatchConfig;
        public GuildService Guild => guildService;
        public WorldService World => worldService;
        public TimeManager Time => timeManager;

        /// <summary>
        /// Awake でサービスを生成することで、他の MonoBehaviour の Start() より
        /// 必ずサービスが準備済みになる（実行順序問題を構造的に解決）。
        /// </summary>
        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[GameBootstrap] GameBootstrapConfig is not assigned.");
                return;
            }

            EventBus.Clear();

            economyService    = new EconomyService(config.EconomyConfig);
            guildService      = new GuildService(config.GuildConfig, economyService);
            adventurerService = new AdventurerService(config.StarterAdventurers);
            questService      = new QuestService(config.QuestConfig, config.GlobalQuestPool);
            dispatchManager   = new DispatchManager(config.DispatchConfig, adventurerService, questService);
            worldService      = new WorldService(config.WorldConfig);
            worldService.UnlockQuests(config.InitiallyUnlockedQuests.Select(q => q.Id));

            timeManager = gameObject.AddComponent<TimeManager>();
            timeManager.Initialize(config.TimeConfig);
        }

        private void Start()
        {
            WireEvents();
            Debug.Log("[GameBootstrap] All systems initialized.");
        }

        private void WireEvents()
        {
            EventBus.Subscribe(GameEvents.DayPassed, OnDayPassed);
            EventBus.Subscribe<DispatchResult>(GameEvents.QuestCompleted, OnQuestCompleted);
            EventBus.Subscribe<DispatchResult>(GameEvents.QuestFailed, OnQuestFailed);
        }

        private void OnDayPassed()
        {
            adventurerService.OnDayPassed(timeManager.Clock.TotalDays);
            questService.OnDayPassed();
            economyService.OnDayPassed(adventurerService.ActiveCount);
        }

        private void OnQuestCompleted(DispatchResult result)
        {
            economyService.AddGold(result.GoldReward);
            economyService.AddReputation(result.ReputationGain);

            if (questService.TryGetQuest(result.QuestInstanceId, out var questState))
            {
                var defId = questState.Definition.Id;
                worldService.MarkQuestCompleted(defId);
                foreach (var binding in config.QuestUnlockBindings)
                {
                    if (binding.KeyQuest == null || binding.KeyQuest.Id != defId) continue;
                    foreach (var q in binding.UnlocksQuests)
                        if (q != null) worldService.UnlockQuest(q.Id);
                }
            }
        }

        private void OnQuestFailed(DispatchResult result)
        {
            economyService.RemoveReputation(result.ReputationPenalty);
            if (result.GoldPenalty > 0)
                economyService.TrySpendGold(result.GoldPenalty);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe(GameEvents.DayPassed, OnDayPassed);
            EventBus.Unsubscribe<DispatchResult>(GameEvents.QuestCompleted, OnQuestCompleted);
            EventBus.Unsubscribe<DispatchResult>(GameEvents.QuestFailed, OnQuestFailed);
            guildService?.Dispose();
            EventBus.Clear();
        }
    }
}
