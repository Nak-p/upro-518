using UnityEngine;

namespace GuildSim.Shared
{
    public abstract class BaseDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
    }
}
