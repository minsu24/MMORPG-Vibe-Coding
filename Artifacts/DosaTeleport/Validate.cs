using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using EasternFantasy.Player;

if (UnityEditor.EditorApplication.isPlaying) throw new Exception("Run this isolated validation outside Play Mode.");
var previousScene = SceneManager.GetActiveScene();
var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
var origin = new Vector3(1000f, 1000f, 0f);
var created = new List<GameObject>();
var results = new List<object>();
GameObject Make(string name, Vector2 position)
{
    var go = new GameObject(name);
    SceneManager.MoveGameObjectToScene(go, scene);
    go.transform.position = origin + (Vector3)position;
    created.Add(go);
    return go;
}
try
{
    var actor = Make("Teleport body", Vector2.zero);
    actor.layer = LayerMask.NameToLayer("Player");
    var body = actor.AddComponent<Rigidbody2D>();
    body.bodyType = RigidbodyType2D.Kinematic;
    var capsule = actor.AddComponent<CapsuleCollider2D>();
    capsule.size = new Vector2(1f, 2f);
    var movement = actor.AddComponent<PlayerMovement2D>();
    movement.enabled = false;
    var flags = BindingFlags.Instance | BindingFlags.NonPublic;
    typeof(PlayerMovement2D).GetField("bodyCollider", flags).SetValue(movement, capsule);
    typeof(PlayerMovement2D).GetField("dashEnemyMask", flags).SetValue(movement, LayerMask.GetMask("Enemy"));
    movement.body = body;
    var wall = Make("Invisible thin wall", new Vector2(3f, 0f)).AddComponent<BoxCollider2D>();
    wall.size = new Vector2(.05f, 20f);
    void Check(string name, Vector3 request, Func<Vector3, bool> valid)
    {
        Physics2D.SyncTransforms();
        var resolved = movement.ResolveSkillTeleportPosition(origin + request) - origin;
        if (!valid(resolved)) throw new Exception(name + " failed: " + resolved);
        results.Add(new { test = name, destination = resolved.ToString("F4") });
    }
    Check("Stops before a thin invisible right wall", new Vector3(8, 0, 0), p => p.x > 2.40f && p.x < 2.475f);
    wall.transform.position = origin + new Vector3(-3f, 0f);
    Check("Stops before a left wall", new Vector3(-8, 0, 0), p => p.x < -2.40f && p.x > -2.475f);
    wall.transform.position = origin + new Vector3(0f, 4f);
    wall.size = new Vector2(20f, .05f);
    Check("Stops below a solid ceiling", new Vector3(0, 8, 0), p => p.y > 2.90f && p.y < 2.975f);
    wall.transform.position = origin + new Vector3(3f, 0f);
    wall.size = new Vector2(.05f, 20f);
    wall.isTrigger = true;
    Check("Nonblocking triggers do not shorten teleport", new Vector3(8, 0, 0), p => Mathf.Abs(p.x - 8f) < .001f);
    wall.isTrigger = false;
    wall.gameObject.layer = LayerMask.NameToLayer("Enemy");
    Check("Enemy layer remains passable", new Vector3(8, 0, 0), p => Mathf.Abs(p.x - 8f) < .001f);
    wall.gameObject.layer = 0;
    wall.transform.position = origin + new Vector3(0f, 4f);
    wall.size = new Vector2(20f, .05f);
    var effector = wall.gameObject.AddComponent<PlatformEffector2D>();
    effector.useOneWay = true;
    wall.usedByEffector = true;
    Check("Upward teleport passes through a one-way platform", new Vector3(0, 8, 0), p => Mathf.Abs(p.y - 8f) < .001f);
    wall.gameObject.SetActive(false);
    var floor = Make("Supporting floor", new Vector2(0, -1.5f)).AddComponent<BoxCollider2D>();
    floor.size = new Vector2(30f, 1f);
    Check("Standing on the floor does not block horizontal teleport", new Vector3(8, 0, 0), p => Mathf.Abs(p.x - 8f) < .001f);
    floor.gameObject.SetActive(false);
    wall.usedByEffector = false;
    effector.enabled = false;
    wall.transform.position = origin + new Vector3(3, 0);
    wall.size = new Vector2(.05f, 20f);
    wall.gameObject.SetActive(true);
    capsule.offset = new Vector2(.35f, .1f);
    Check("Collider offset is included in wall clearance", new Vector3(8, 0, 0), p => p.x > 2.05f && p.x < 2.125f);
    capsule.offset = Vector2.zero;
    wall.transform.position = origin + new Vector3(.535f, 0);
    Check("Wall-adjacent cast makes no progress", new Vector3(8, 0, 0), p => p.sqrMagnitude <= .0001f);
    wall.gameObject.SetActive(false);
    Check("Clear path preserves full range", new Vector3(8, 0, 0), p => Mathf.Abs(p.x - 8f) < .001f);
    var report = new { passed = true, cases = results };
    System.IO.File.WriteAllText("Artifacts/DosaTeleport/validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
    return report;
}
finally
{
    foreach (var go in created) if (go != null) UnityEngine.Object.DestroyImmediate(go);
    EditorSceneManager.CloseScene(scene, true);
    SceneManager.SetActiveScene(previousScene);
}
