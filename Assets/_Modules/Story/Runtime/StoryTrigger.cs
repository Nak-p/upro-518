using UnityEngine;

namespace GuildSim.Story
{
    public enum TriggerKind
    {
        Manual = 0,
        EventBusKey = 1,
    }

    [System.Serializable]
    public sealed class StoryTrigger
    {
        [SerializeField] private TriggerKind kind = TriggerKind.Manual;
        [Tooltip("EventBusのイベントキー（GameEvents.QuestCompleted 等）。Kind=EventBusKeyの時のみ使用")]
        [SerializeField] private string eventKey;
        [Tooltip("ペイロードのクエストIDがこれと一致した場合のみ発火（任意・空ならフィルタなし）")]
        [SerializeField] private string requiredPayloadQuestId;

        public TriggerKind Kind                   => kind;
        public string      EventKey               => eventKey;
        public string      RequiredPayloadQuestId => requiredPayloadQuestId;
    }
}
