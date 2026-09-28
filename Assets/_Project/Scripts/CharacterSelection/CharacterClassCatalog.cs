using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasternFantasy.CharacterSelection
{
    [CreateAssetMenu(
        fileName = "CharacterClassCatalog",
        menuName = "Eastern Fantasy/Character/Class Catalog")]
    public sealed class CharacterClassCatalog : ScriptableObject
    {
        [SerializeField] private CharacterClassDefinition[] classes =
            Array.Empty<CharacterClassDefinition>();

        public IReadOnlyList<CharacterClassDefinition> Classes => classes;

        public CharacterClassDefinition Find(CharacterClassId classId)
        {
            if (classes == null)
                return null;

            foreach (CharacterClassDefinition definition in classes)
                if (definition != null && definition.ClassId == classId)
                    return definition;

            return null;
        }

        private void OnValidate()
        {
            if (classes == null)
                classes = Array.Empty<CharacterClassDefinition>();
        }
    }
}
