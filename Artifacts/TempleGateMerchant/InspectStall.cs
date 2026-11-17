using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
var scene = SceneManager.GetSceneByPath(EasternFantasy.Editor.TempleGateMerchantSetup.ScenePath);
return new { playing = Application.isPlaying, loaded = scene.isLoaded, renderers = scene.isLoaded ? scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SpriteRenderer>(true)).Where(r => r.name.Contains("Merchant") || (r.sprite != null && AssetDatabase.GetAssetPath(r.sprite).Contains("NPC/"))).Select(r => new { r.name, parent = r.transform.parent != null ? r.transform.parent.name : "root", position = r.transform.position.ToString(), sprite = r.sprite != null ? AssetDatabase.GetAssetPath(r.sprite) : "", size = r.bounds.size.ToString(), order = r.sortingOrder }).ToArray() : null };
