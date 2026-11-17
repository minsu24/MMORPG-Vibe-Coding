using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using EasternFantasy.Player;

if (!UnityEditor.EditorApplication.isPlaying) throw new Exception("Run this reaction validation in Play Mode.");
var scene = SceneManager.CreateScene("Isolated ring hit validation", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
var flags = BindingFlags.Instance | BindingFlags.NonPublic;
var objects = new System.Collections.Generic.List<GameObject>();
var settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
int playerLayer = LayerMask.NameToLayer("Player"), enemyLayer = LayerMask.NameToLayer("Enemy");
bool previousIgnore = Physics2D.GetIgnoreLayerCollision(playerLayer, enemyLayer);
var results = new System.Collections.Generic.List<object>();
GameObject Make(string name)
{
    var go = new GameObject(name);
    SceneManager.MoveGameObjectToScene(go, scene);
    objects.Add(go);
    return go;
}
try
{
    foreach (int direction in new[] { 1, -1 })
    {
        var actor = Make("Test player"); actor.SetActive(false); actor.layer = playerLayer;
        var sprite = actor.AddComponent<SpriteRenderer>();
        var player = actor.AddComponent<PlayerEntity>();
        var movement = actor.GetComponent<PlayerMovement2D>();
        typeof(PlayerMovement2D).GetField("settings", flags).SetValue(movement, settings);
        actor.GetComponent<CapsuleCollider2D>().size = new Vector2(1f, 2f);
        actor.SetActive(true); movement.enabled = false;
        var body = actor.GetComponent<Rigidbody2D>(); body.gravityScale = 0f;
        void Hit()
        {
            var projectile = Make("Test ring"); projectile.transform.position = new Vector3(-3f * direction, 0, 0);
            var renderer = projectile.AddComponent<SpriteRenderer>();
            var ring = projectile.AddComponent<SkullMonkStaffRing>();
            typeof(SkullMonkStaffRing).GetField("visual", flags).SetValue(ring, renderer);
            typeof(SkullMonkStaffRing).GetField("flightFrames", flags).SetValue(ring, new Sprite[8]);
            typeof(SkullMonkStaffRing).GetField("travelDirection", flags).SetValue(ring, Vector2.right * direction);
            typeof(SkullMonkStaffRing).GetField("maxHealthDamageFraction", flags).SetValue(ring, .1f);
            Physics2D.SyncTransforms();
            typeof(SkullMonkStaffRing).GetMethod("Move", flags).Invoke(ring, new object[] { 5f });
            if (!ring.HasImpacted) throw new Exception("Test ring missed");
        }
        Hit();
        if (Mathf.Abs(player.HP - player.maxHP * .9f) > .001f) throw new Exception("Expected 10% damage");
        if (!player.isKnockback || !player.isInvincible || Mathf.Abs(sprite.color.a - .5f) > .001f)
            throw new Exception("Knockback, immunity or half opacity missing");
        if (body.linearVelocity.x * direction <= 0f) throw new Exception("Incorrect knockback direction");
        uint hitVersion = player.HitReactionVersion;
        Hit();
        if (player.HitReactionVersion != hitVersion || Mathf.Abs(player.HP - player.maxHP * .9f) > .001f)
            throw new Exception("A following ring ignored post-hit immunity");
        results.Add(new { direction, damageFraction = .1f, knockback = player.isKnockback, invincible = player.isInvincible, alpha = sprite.color.a, nextRingBlocked = true });
        UnityEngine.Object.DestroyImmediate(actor);
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, previousIgnore);
    }
    var report = new { passed = true, cases = results };
    System.IO.Directory.CreateDirectory("Artifacts/SkullMonkRings");
    System.IO.File.WriteAllText("Artifacts/SkullMonkRings/hit-reaction.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
    return report;
}
finally
{
    foreach (var go in objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
    Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, previousIgnore);
    UnityEngine.Object.DestroyImmediate(settings);
    SceneManager.UnloadSceneAsync(scene);
}
