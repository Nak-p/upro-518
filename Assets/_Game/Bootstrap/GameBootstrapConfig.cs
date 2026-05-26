using UnityEngine;
using GuildSim.Time;
using GuildSim.Economy;
using GuildSim.Adventurer;
using GuildSim.Quest;
using GuildSim.Dispatch;
using GuildSim.Guild;
using GuildSim.World;

namespace GuildSim.Game
{
    [System.Serializable]
    public sealed class QuestUnlockBinding
    {
        [Tooltip("このクエストを完了すると…")]
        [SerializeField] private QuestDefinition keyQuest;
        [Tooltip("…これらのクエストピンが解放される")]
        [SerializeField] private QuestDefinition[] unlocksQuests = {};

        public QuestDefinition   KeyQuest      => keyQuest;
        public QuestDefinition[] UnlocksQuests => unlocksQuests;
    }

    [System.Serializable]
    public sealed class RegionQuestBinding
    {
        [SerializeField] private RegionDefinition region;
        [SerializeField] private QuestDefinition[] questPool = {};

        public RegionDefinition Region => region;
        public QuestDefinition[] QuestPool => questPool;
    }

    [System.Serializable]
    public sealed class EventPointQuestBinding
    {
        [Tooltip("マップ上のイベントポイント")]
        [SerializeField] private EventPointDefinition eventPoint;
        [Tooltip("このポイントに紐づくクエスト一覧")]
        [SerializeField] private QuestDefinition[] linkedQuests = {};

        public EventPointDefinition EventPoint    => eventPoint;
        public QuestDefinition[]    LinkedQuests  => linkedQuests;
    }

    [CreateAssetMenu(menuName = "GuildSim/Game Bootstrap Config", fileName = "GameBootstrapConfig")]
    public sealed class GameBootstrapConfig : ScriptableObject
    {
        [Header("Core")]
        [SerializeField] private TimeConfig timeConfig;
        [SerializeField] private EconomyConfig economyConfig;
        [SerializeField] private GuildConfig guildConfig;

        [Header("Adventurers")]
        [SerializeField] private AdventurerDefinition[] starterAdventurers = {};

        [Header("Quests")]
        [SerializeField] private QuestConfig questConfig;
        [Tooltip("全地域共通のグローバルクエストプール")]
        [SerializeField] private QuestDefinition[] globalQuestPool = {};

        [Header("Dispatch")]
        [SerializeField] private DispatchConfig dispatchConfig;

        [Header("World")]
        [SerializeField] private WorldConfig worldConfig;
        [Tooltip("地域ごとのクエストプール紐づけ（_Game のみ Quest+World 両方参照可）")]
        [SerializeField] private RegionQuestBinding[] regionQuestBindings = {};

        [Header("World Map")]
        [Tooltip("ゲーム開始時点でアンロック済みのクエストピン")]
        [SerializeField] private QuestDefinition[] initiallyUnlockedQuests = {};
        [Tooltip("キークエスト完了時のアンロック連鎖定義")]
        [SerializeField] private QuestUnlockBinding[] questUnlockBindings = {};
        [Tooltip("ワールドマップの背景画像")]
        [SerializeField] private Sprite worldMapSprite;
        [Tooltip("マップ上のイベントポイントとクエストの紐づけ")]
        [SerializeField] private EventPointQuestBinding[] eventPointBindings = {};

        public TimeConfig TimeConfig => timeConfig;
        public EconomyConfig EconomyConfig => economyConfig;
        public GuildConfig GuildConfig => guildConfig;
        public AdventurerDefinition[] StarterAdventurers => starterAdventurers;
        public QuestConfig QuestConfig => questConfig;
        public QuestDefinition[] GlobalQuestPool => globalQuestPool;
        public DispatchConfig DispatchConfig => dispatchConfig;
        public WorldConfig WorldConfig => worldConfig;
        public RegionQuestBinding[] RegionQuestBindings => regionQuestBindings;
        public QuestDefinition[]    InitiallyUnlockedQuests => initiallyUnlockedQuests;
        public QuestUnlockBinding[] QuestUnlockBindings     => questUnlockBindings;
        public Sprite                   WorldMapSprite     => worldMapSprite;
        public EventPointQuestBinding[] EventPointBindings => eventPointBindings;

        private void OnValidate()
        {
            if (timeConfig == null) Debug.LogWarning("[GameBootstrapConfig] TimeConfig is missing.");
            if (economyConfig == null) Debug.LogWarning("[GameBootstrapConfig] EconomyConfig is missing.");
            if (guildConfig == null) Debug.LogWarning("[GameBootstrapConfig] GuildConfig is missing.");
            if (questConfig == null) Debug.LogWarning("[GameBootstrapConfig] QuestConfig is missing.");
            if (dispatchConfig == null) Debug.LogWarning("[GameBootstrapConfig] DispatchConfig is missing.");
            if (worldConfig == null) Debug.LogWarning("[GameBootstrapConfig] WorldConfig is missing.");
        }
    }
}
