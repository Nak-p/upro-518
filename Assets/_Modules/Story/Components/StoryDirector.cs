using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using GuildSim.Shared;

namespace GuildSim.Story
{
    /// <summary>
    /// Yarn の DialogueRunner を GuildSim と接続する司令塔（シーンに配置）。
    /// 役割:
    ///  1. ゲーム状態クエリ関数（is_quest_completed 等）を Yarn に登録
    ///  2. StoryConfig の各 StoryDefinition のトリガーを EventBus に購読
    ///  3. トリガー発火時に DialogueRunner で対象ノードを開始
    ///  4. PlayOnce 制御・StoryEvents 発行
    ///
    /// 想定: 1つの YarnProject に全ノードを入れ、ノード同士を jump で線でつなぐ。
    /// StoryDefinition は「どのノードから始まるエントリーポイントか」を表す。
    /// </summary>
    public sealed class StoryDirector : MonoBehaviour
    {
        [Tooltip("シーン上の Yarn DialogueRunner。全ストーリー共通の YarnProject を割り当てておく")]
        [SerializeField] private DialogueRunner dialogueRunner;

        private StoryConfig config;
        private IStoryConditionContext ctx;
        private readonly StoryState state = new();

        private readonly Dictionary<string, StoryDefinition> byId = new();
        private readonly List<(string key, Action handler)> voidSubs = new();

        private StoryDefinition activeStory;
        private bool initialized;

        public StoryState State => state;
        public bool IsPlaying => dialogueRunner != null && dialogueRunner.IsDialogueRunning;

        /// <summary>GameBootstrap から呼ぶ。条件コンテキストを注入し、関数登録とトリガー購読を行う。</summary>
        public void Initialize(StoryConfig storyConfig, IStoryConditionContext conditionContext)
        {
            if (initialized) return;
            config = storyConfig;
            ctx = conditionContext;

            if (dialogueRunner == null)
            {
                Debug.LogError("[StoryDirector] DialogueRunner is not assigned.");
                return;
            }

            RegisterFunctions();
            IndexStories();
            SubscribeTriggers();

            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
            initialized = true;
        }

        private void RegisterFunctions()
        {
            // .yarn 側で <<if is_quest_completed("Quest_X")>> のように使える
            dialogueRunner.AddFunction<string, bool>("is_quest_completed", id => ctx != null && ctx.IsQuestCompleted(id));
            dialogueRunner.AddFunction<int>("get_gold",             () => ctx?.GetGold() ?? 0);
            dialogueRunner.AddFunction<int>("get_reputation",       () => ctx?.GetReputation() ?? 0);
            dialogueRunner.AddFunction<int>("get_guild_rank",       () => ctx?.GetGuildRankIndex() ?? 0);
            dialogueRunner.AddFunction<int>("get_adventurer_count", () => ctx?.GetAdventurerCount() ?? 0);

            // <<unlock_quest Quest_DaughterSearch>> → GameBootstrap 経由で掲示板に追加
            dialogueRunner.AddCommandHandler<string>("unlock_quest", questId =>
                EventBus.Publish(StoryEvents.QuestUnlocked, questId));
        }

        private void IndexStories()
        {
            if (config == null || config.Stories == null) return;
            foreach (var def in config.Stories)
                if (def != null && !string.IsNullOrEmpty(def.Id))
                    byId[def.Id] = def;
        }

        private void SubscribeTriggers()
        {
            if (config == null || config.Stories == null) return;
            var subscribed = new HashSet<string>();

            foreach (var def in config.Stories)
            {
                if (def == null || def.Triggers == null) continue;
                foreach (var trig in def.Triggers)
                {
                    if (trig == null || trig.Kind != TriggerKind.EventBusKey) continue;
                    if (string.IsNullOrEmpty(trig.EventKey)) continue;
                    // payload条件付きは GameBootstrap が NotifyEvent 経由で通知する
                    if (!string.IsNullOrEmpty(trig.RequiredPayloadQuestId)) continue;
                    if (!subscribed.Add(trig.EventKey)) continue;

                    string key = trig.EventKey;
                    Action h = () => NotifyEvent(key, null);
                    EventBus.Subscribe(key, h);
                    voidSubs.Add((key, h));
                }
            }
        }

        /// <summary>イベントキー＋任意のペイロードIDで、条件に合うストーリーを起動する。</summary>
        public void NotifyEvent(string eventKey, string payloadId)
        {
            if (IsPlaying || config == null || config.Stories == null) return;

            foreach (var def in config.Stories)
            {
                if (def == null || def.Triggers == null) continue;
                foreach (var trig in def.Triggers)
                {
                    if (trig == null || trig.Kind != TriggerKind.EventBusKey) continue;
                    if (trig.EventKey != eventKey) continue;
                    if (!string.IsNullOrEmpty(trig.RequiredPayloadQuestId)
                        && trig.RequiredPayloadQuestId != payloadId) continue;

                    if (TryStart(def)) return;
                }
            }
        }

        /// <summary>ストーリーIDを指定して直接起動する。</summary>
        public bool TriggerStory(string storyId)
        {
            if (IsPlaying) return false;
            return byId.TryGetValue(storyId, out var def) && TryStart(def);
        }

        private bool TryStart(StoryDefinition def)
        {
            if (def.PlayOnce && state.IsStoryCompleted(def.Id)) return false;
            if (string.IsNullOrEmpty(def.StartNode))
            {
                Debug.LogWarning($"[StoryDirector] '{def.Id}' has empty StartNode.");
                return false;
            }

            activeStory = def;
            EventBus.Publish(StoryEvents.StoryStarted, def.Id);
            dialogueRunner.StartDialogue(def.StartNode);
            return true;
        }

        private void OnDialogueComplete()
        {
            if (activeStory == null) return;
            var ended = activeStory;
            state.MarkStoryCompleted(ended.Id);
            activeStory = null;
            EventBus.Publish(StoryEvents.StoryEnded, ended.Id);
        }

        private void OnDestroy()
        {
            foreach (var (key, h) in voidSubs) EventBus.Unsubscribe(key, h);
            voidSubs.Clear();
            if (dialogueRunner != null)
                dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
        }
    }
}
