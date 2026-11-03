using System;
using System.Reflection;
using EasternFantasy.Player;
using EasternFantasy.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PortalGroundedArrivalValidation
{
    public static string Validate()
    {
        const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        var scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Scenes/OldTemple/SmogMountain.unity");
        int checks = 0;
        try
        {
            var physics = scene.GetPhysicsScene2D();
            if (physics == Physics2D.defaultPhysicsScene) throw new Exception("Expected isolated physics.");
            PortalManager portal = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var candidate in root.GetComponentsInChildren<PortalManager>(true))
                    if (candidate.name.Trim() == "In_SmogMountain_Portal") portal = candidate;
            if (portal == null) throw new Exception("SmogMountain entrance missing.");
            foreach (string name in new[] { "Player", "MonkPlayer", "SwordswomanPlayer" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/" + name + ".prefab");
                var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                try
                {
                    var movement = player.GetComponent<PlayerMovement2D>();
                    typeof(PlayerMovement2D).GetMethod("Awake", hidden).Invoke(movement, null);
                    var body = player.GetComponent<Rigidbody2D>();
                    var collider = player.GetComponent<CapsuleCollider2D>();
                    Action<bool, string> check = (condition, label) => {
                        if (!condition) throw new Exception(name + ": " + label);
                        checks++;
                    };
                    // Arriving from a distant map with a different altitude and falling velocity.
                    movement.TeleportTo(new Vector3(50, 30), Quaternion.identity);
                    body.linearVelocity = new Vector2(4, -8);
                    var destination = movement.ResolvePortalArrivalPosition(portal.transform);
                    check(Mathf.Abs(destination.x - portal.transform.position.x) < 0.001f, "Portal X preserved");
                    check(destination.y > portal.transform.position.y + 0.3f, "Portal base converted to character origin");
                    movement.TeleportTo(destination, Quaternion.identity);
                    check(Vector3.Distance(player.transform.position, destination) < 0.001f, "Transform immediately at destination");
                    check(Vector2.Distance(body.position, destination) < 0.001f, "Physics pose immediately at destination");
                    check(body.linearVelocity == Vector2.zero, "Old velocity cleared");
                    check(body.interpolation == RigidbodyInterpolation2D.Interpolate, "Interpolation restored");
                    float initialFeet = collider.bounds.min.y;
                    for (int i = 0; i < 20; i++) physics.Simulate(0.02f);
                    check(Mathf.Abs(body.position.y - destination.y) < 0.025f, "No visible upward/downward correction after unpause");
                    check(Mathf.Abs(collider.bounds.min.y - initialFeet) < 0.025f, "Feet stable after physics resumes");
                    // Saved positions use the vector overload and must remain exact, even in midair.
                    var saved = new Vector3(portal.transform.position.x, 10, 0);
                    movement.TeleportTo(saved, Quaternion.identity);
                    check(Vector3.Distance(player.transform.position, saved) < 0.001f, "Saved airborne position not ground-snapped");
                }
                finally { UnityEngine.Object.DestroyImmediate(player); }
            }
            if (MapLocationNames.GetDisplayName("SmogMountain") != "안개 낀 산") throw new Exception("Korean banner missing.");
            checks++;
            string result = checks + " checks passed: three class arrivals on actual SmogMountain support, first-frame pose, velocity/interpolation reset, 0.4s physics stability, exact saved positions and Korean map name.";
            System.IO.Directory.CreateDirectory("Artifacts/PortalGroundedArrival");
            System.IO.File.WriteAllText("Artifacts/PortalGroundedArrival/Validation.txt", result);
            return result;
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
