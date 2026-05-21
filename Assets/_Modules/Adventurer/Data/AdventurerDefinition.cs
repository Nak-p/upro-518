using UnityEngine;
using GuildSim.Shared;

namespace GuildSim.Adventurer
{
    [CreateAssetMenu(menuName = "GuildSim/Adventurer/Adventurer Definition", fileName = "AdventurerDefinition")]
    public sealed class AdventurerDefinition : BaseDefinition
    {
        [SerializeField] private ClassDefinition adventurerClass;
        [SerializeField] private int startingLevel = 1;
        [SerializeField] private TraitDefinition[] personalTraits = {};

        public ClassDefinition AdventurerClass => adventurerClass;
        public int StartingLevel => Mathf.Max(1, startingLevel);
        public TraitDefinition[] PersonalTraits => personalTraits;
    }
}
