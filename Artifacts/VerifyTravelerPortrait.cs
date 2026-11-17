using System;
using System.Linq;
using UnityEditor;
using EasternFantasy.Dialogue;
const string path = "Assets/_Project/Data/Quest/GinsengCollection/TravelerCharacter.asset";
const string image = "Assets/_Project/Art/Old Temple/NPC/나그네 일러스트.png";
AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
var character = AssetDatabase.LoadAssetAtPath<DialogueCharacter>(path);
if (character == null || character.Portrait == null || AssetDatabase.GetAssetPath(character.Portrait) != image)
    throw new Exception("Traveler portrait reference mismatch");
var sequences = AssetDatabase.FindAssets("t:DialogueSequence", new[]{"Assets/_Project/Data/Quest/GinsengCollection"})
    .Select(id => AssetDatabase.LoadAssetAtPath<DialogueSequence>(AssetDatabase.GUIDToAssetPath(id)))
    .Where(sequence => new SerializedObject(sequence).FindProperty("partnerCharacter").objectReferenceValue == character).ToArray();
return new { portrait = AssetDatabase.GetAssetPath(character.Portrait), dialogues = sequences.Length, verified = true };
