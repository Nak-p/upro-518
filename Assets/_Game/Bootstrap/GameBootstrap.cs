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
using GuildSim.Story;

namespace GuildSim.Game
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameBootstrapConfig config;
        [Tooltip("シーン上の StoryDirector（Yarn DialogueRunner と接続）。未設定ならストーリー無効")]
        [SerializeField] private StoryDirector storyDirector;

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
        public StoryDirector Story => storyDirector;

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
            var questPool = BuildQuestPool();
            questService      = new QuestService(config.QuestConfig, questPool);
            dispatchManager   = new DispatchManager(config.DispatchConfig, adventurerService, questService);
            worldService      = new WorldService(config.WorldConfig);

            var unlockedIds = config.InitiallyUnlockedQuests.Select(q => q.Id)
                .Concat(EventPointQuestIds())
                .Distinct();
            worldService.UnlockQuests(unlockedIds);

            timeManager = gameObject.AddComponent<TimeManager>();
            timeManager.Initialize(config.TimeConfig);

            // Storyモジュール：StoryConfig/StoryDirector 未設定でも他機能に影響しない
            if (config.StoryConfig != null && storyDirector != null)
            {
                var bridge = new StoryConditionBridge(worldService, economyService, guildService, adventurerService);
                storyDirector.Initialize(config.StoryConfig, bridge);
            }
            else
            {
                Debug.LogWarning($"[GameBootstrap] Story disabled. StoryConfig={(config.StoryConfig != null)}, StoryDirector={(storyDirector != null)}");
            }
        }

        private void Start()
        {
            WireEvents();
            storyDirector?.TriggerStory("Story_Welcome");
            Debug.Log("[GameBootstrap] All systems initialized.");
        }

        private void WireEvents()
        {
            EventBus.Subscribe(GameEvents.DayPassed, OnDayPassed);
            EventBus.Subscribe<DispatchResult>(GameEvents.QuestCompleted, OnQuestCompleted);
            EventBus.Subscribe<DispatchResult>(GameEvents.QuestFailed, OnQuestFailed);
            EventBus.Subscribe<string>(StoryEvents.QuestUnlocked, OnStoryQuestUnlocked);
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

                // Story：ペイロード付きトリガー（クエストID一致条件）の通知
                storyDirector?.NotifyEvent(GameEvents.QuestCompleted, defId);
            }
        }

        private void OnStoryQuestUnlocked(string questId)
        {
            Debug.Log($"[GameBootstrap] unlock_quest received: '{questId}'");
            var def = config.StoryUnlockableQuests.FirstOrDefault(q => q != null && q.Id == questId);
            if (def != null)
            {
                questService.UnlockStoryQuest(def);
                Debug.Log($"[GameBootstrap] Quest unlocked on board: '{questId}'");
            }
            else
            {
                Debug.LogWarning($"[GameBootstrap] unlock_quest: '{questId}' not found in StoryUnlockableQuests. " +
                    $"登録済みID: [{string.Join(", ", config.StoryUnlockableQuests.Where(q => q != null).Select(q => $"'{q.Id}'"))}]");
            }
        }

        private void OnQuestFailed(DispatchResult result)
        {
            economyService.RemoveReputation(result.ReputationPenalty);
            if (result.GoldPenalty > 0)
                economyService.TrySpendGold(result.GoldPenalty);
        }

        private QuestDefinition[] BuildQuestPool()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            var list = new System.Collections.Generic.List<QuestDefinition>();

            void Add(QuestDefinition q)
            {
                if (q != null && seen.Add(q.Id)) list.Add(q);
            }

            bool useEventPoints = config.EventPointBindings != null && config.EventPointBindings.Length > 0;

            if (useEventPoints)
            {
                // EventPoint 方式: eventPointId が設定されているクエストのみ使用
                foreach (var q in config.GlobalQuestPool)
                    if (q != null && !string.IsNullOrEmpty(q.EventPointId)) Add(q);

                foreach (var b in config.EventPointBindings)
                    if (b.LinkedQuests != null)
                        foreach (var q in b.LinkedQuests) Add(q);
            }
            else
            {
                // 旧方式: GlobalQuestPool をすべて使用
                foreach (var q in config.GlobalQuestPool) Add(q);
            }

            return list.ToArray();
        }

        private System.Collections.Generic.IEnumerable<string> EventPointQuestIds()
        {
            // GlobalQuestPool の中で eventPointId が設定されているクエストを自動収集
            foreach (var q in config.GlobalQuestPool)
                if (q != null && !string.IsNullOrEmpty(q.EventPointId))
                    yield return q.Id;

            // EventPointBindings に手動で設定された linkedQuests もカバー
            if (config.EventPointBindings == null) yield break;
            foreach (var b in config.EventPointBindings)
                if (b.LinkedQuests != null)
                    foreach (var q in b.LinkedQuests)
                        if (q != null) yield return q.Id;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe(GameEvents.DayPassed, OnDayPassed);
            EventBus.Unsubscribe<DispatchResult>(GameEvents.QuestCompleted, OnQuestCompleted);
            EventBus.Unsubscribe<DispatchResult>(GameEvents.QuestFailed, OnQuestFailed);
            EventBus.Unsubscribe<string>(StoryEvents.QuestUnlocked, OnStoryQuestUnlocked);
            guildService?.Dispose();
            EventBus.Clear();
        }
    }
}
