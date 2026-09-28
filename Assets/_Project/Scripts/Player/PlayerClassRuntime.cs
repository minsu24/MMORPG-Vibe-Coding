using EasternFantasy.CharacterSelection;
using EasternFantasy.Advancement;
using EasternFantasy.Skill;
using UnityEngine;

namespace EasternFantasy.Player
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class PlayerClassRuntime : MonoBehaviour
    {
        [SerializeField] private CharacterClassCatalog catalog;
        [SerializeField] private CharacterClassId representedClass = CharacterClassId.Dosa;

        private static PlayerClassRuntime activeInstance;

        public static CharacterClassDefinition ActiveDefinition { get; private set; }
        public CharacterClassId RepresentedClass => representedClass;

        private void Awake()
        {
            CharacterClassId selectedClass = CharacterSelectionState.SelectedClass;

            if (activeInstance != null && activeInstance != this)
            {
                if (activeInstance.representedClass == selectedClass)
                {
                    gameObject.SetActive(false);
                    Destroy(gameObject);
                    return;
                }

                GameObject previousPlayer = activeInstance.gameObject;
                activeInstance = null;
                ActiveDefinition = null;
                previousPlayer.SetActive(false);
                Destroy(previousPlayer);
            }

            CharacterClassDefinition definition = catalog != null
                ? catalog.Find(selectedClass)
                : null;

            if (definition == null)
            {
                Debug.LogError($"No class definition exists for '{selectedClass}'.", this);
                RegisterCurrent(null);
                return;
            }

            if (representedClass != selectedClass)
            {
                ReplaceWithSelectedPrefab(definition);
                return;
            }

            RegisterCurrent(definition);
        }

        private void ReplaceWithSelectedPrefab(CharacterClassDefinition definition)
        {
            GameObject targetPrefab = definition.PlayerPrefab;
            PlayerClassRuntime targetRuntime = targetPrefab != null
                ? targetPrefab.GetComponent<PlayerClassRuntime>()
                : null;

            if (targetRuntime == null || targetRuntime.representedClass != definition.ClassId)
            {
                Debug.LogError(
                    $"Class '{definition.DisplayName}' needs a player prefab with " +
                    $"PlayerClassRuntime set to '{definition.ClassId}'.",
                    definition);
                RegisterCurrent(definition);
                return;
            }

            Transform cachedTransform = transform;
            Instantiate(
                targetPrefab,
                cachedTransform.position,
                cachedTransform.rotation,
                cachedTransform.parent);

            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void RegisterCurrent(CharacterClassDefinition definition)
        {
            activeInstance = this;
            ActiveDefinition = definition;

            if (definition == null)
                return;

            PlayerEntity entity = GetComponent<PlayerEntity>();
            if (entity != null)
                entity.ConfigureClass(definition.StatGrowth);

            PlayerSkillSystem skillSystem = GetComponent<PlayerSkillSystem>();
            if (skillSystem != null)
                skillSystem.ConfigureClass(definition.AvailableSkills);

            PlayerAdvancementSystem advancementSystem =
                GetComponent<PlayerAdvancementSystem>();
            if (advancementSystem != null)
                advancementSystem.ConfigureClass(definition.AutomaticAdvancement);
        }

        private void OnDestroy()
        {
            if (activeInstance != this)
                return;

            activeInstance = null;
            ActiveDefinition = null;
        }
    }
}
