using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.Player;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class SkullMonkWaveValidation
    {
        public const string Output = "Artifacts/SkullMonkWave/";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        public static object Probe()
        {
            var boss = UnityEngine.Object.FindFirstObjectByType<MON_SkullMonk>();
            if (boss == null) throw new Exception("Center boss is not loaded.");
            var player = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab"));
            var victim = player.GetComponent<PlayerEntity>();
            player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var collider = player.GetComponent<Collider2D>();
            var created = new List<SkullMonkGroundWave>();
            try
            {
                typeof(EnemyController).GetField("playerEntity", Flags).SetValue(boss, victim);
                typeof(EnemyController).GetField("player", Flags).SetValue(boss, player);
                var support = (Collider2D)typeof(MON_SkullMonk).GetField("waveFloor", Flags).GetValue(boss);
                var body = boss.GetComponent<Rigidbody2D>();
                boss.transform.position += Vector3.up * (support.bounds.max.y - boss.GetComponent<Collider2D>().bounds.min.y);
                body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
                player.transform.position = boss.transform.position + Vector3.left * 5;
                typeof(MON_SkullMonk).GetField("nextWaveAt", Flags).SetValue(boss, 0f);
                typeof(MON_SkullMonk).GetField("nextAttackAt", Flags).SetValue(boss, 0f);
                typeof(MON_SkullMonk).GetMethod("MonsterAbility", Flags).Invoke(boss, null);
                Require(boss.IsWaveAttacking, "AI did not select wave pattern.");
                var animator = boss.GetComponent<Animator>();
                animator.Update(0); animator.Update(1.01f);
                Require((bool)typeof(MON_SkullMonk).GetField("staffPlanted", Flags).GetValue(boss), "No staff plant event.");
                bool smoke = boss.gameObject.scene.GetRootGameObjects().Any(g => g.name == "Giant Smash Dust");
                Require(smoke, "No supplied ground smoke.");
                animator.Update(.20f); animator.Update(0);
                var counts = new List<int>();
                for (int i = 0; i < 5; i++)
                {
                    animator.Update(.41f);
                    Require(boss.WavesReleased == i + 1, "Bow/wave count mismatch.");
                    boss.SkullMonkReleaseWave();
                    Require(boss.WavesReleased == i + 1, "Duplicate event emitted an extra wave.");
                    counts.Add(boss.WavesReleased);
                    animator.Update(.60f); animator.Update(0);
                }
                Require(boss.WavesReleased == 5, "Not exactly five waves.");
                animator.Update(1.01f); animator.Update(0);
                bool recovered = !boss.IsWaveAttacking && !boss.IsAttacking;
                Require(recovered, "Ritual did not recover to idle.");
                boss.SkullMonkReleaseWave(); Require(boss.WavesReleased == 5, "Sixth wave emitted.");
                var floor = (Collider2D)typeof(MON_SkullMonk).GetField("waveFloor", Flags).GetValue(boss);
                float ground = floor.bounds.max.y;
                var source = boss.GetComponent<SpriteRenderer>();
                float max = victim.maxHP;
                foreach (float side in new[] { 1f, -1f })
                {
                    victim.HP = max; victim.isInvincible = false;
                    PositionFeet(player, collider, new Vector2(side * 4f, ground + .03f));
                    var wave = SkullMonkGroundWave.Create(boss, new Vector2(0, ground), -10, 10, 12, .45f, LayerMask.GetMask("Player"), source); created.Add(wave);
                    wave.Advance(.34f);
                    var crest = wave.transform.Find("Expanding circular crest Front").GetComponent<LineRenderer>();
                    var rear = wave.transform.Find("Expanding circular crest Back").GetComponent<LineRenderer>();
                    Require(Mathf.Abs(wave.RippleCenter.x) < .001f && Mathf.Abs(wave.Radius - 4.08f) < .001f,
                        "Ripple center moved or radius did not expand.");
                    Require(!crest.loop && !rear.loop && crest.positionCount == 97 && rear.positionCount == 97
                        && Mathf.Abs(crest.GetPosition(0).x - wave.Radius) < .001f
                        && Mathf.Abs(crest.GetPosition(96).x + wave.Radius) < .001f,
                        "Wave is not one continuous expanding circle.");
                    Require(crest.sortingOrder > source.sortingOrder && rear.sortingOrder < source.sortingOrder
                        && Mathf.Abs(Mathf.Abs(crest.GetPosition(48).y) - wave.Radius * .14f) < .001f
                        && Mathf.Abs(rear.GetPosition(48).y - wave.Radius * .105f) < .001f
                        && SkullMonkGroundWave.GroundDepth(20f) > SkullMonkGroundWave.GroundDepth(10f),
                        "Side-view depth or front/back occlusion is incorrect.");
                    Require(Mathf.Abs(victim.HP - max * .7f) < .001f, "Ground hit was not exactly 30% maximum health.");
                    Require(victim.isInvincible && victim.isKnockback, "Wave hit did not use normal knockback and invincibility.");
                    // A separate following pulse must also respect the normal post-hit immunity.
                    var protectedWave = SkullMonkGroundWave.Create(boss, new Vector2(0, ground), -10, 10, 12, .45f, LayerMask.GetMask("Player"), source);
                    created.Add(protectedWave); protectedWave.Advance(.34f);
                    Require(Mathf.Abs(victim.HP - max * .7f) < .001f, "A following pulse ignored post-hit invincibility.");
                    wave.Advance(.02f);
                    Require(Mathf.Abs(victim.HP - max * .7f) < .001f, "One pulse hit twice.");
                    wave.Advance(1f);
                    Require(wave.LeftFront == -10 && wave.RightFront == 10 && !wave.gameObject.activeSelf, "Wave did not finish at both map edges.");
                    victim.HP = max; victim.isInvincible = false;
                    PositionFeet(player, collider, new Vector2(side * 4f, ground + .6f));
                    wave = SkullMonkGroundWave.Create(boss, new Vector2(0, ground), -10, 10, 12, .45f, LayerMask.GetMask("Player"), source); created.Add(wave);
                    wave.Advance(.34f); Require(victim.HP == max, "Airborne player was hit.");
                }
                int deathEvents = 0; victim.Died += () => deathEvents++;
                victim.isInvincible = true; victim.TakeMaxHealthDamage(.3f); Require(victim.HP == max, "Immunity was ignored.");
                victim.isInvincible = false; victim.HP = max * .2f; victim.TakeMaxHealthDamage(.3f);
                Require(victim.IsDead && deathEvents == 1, "Percentage damage skipped death handling.");
                var report = new { waveCounts = counts, smoke, recovered, directions = 2, damageFraction = .3f,
                    centeredExpandingCircle = true, sideViewDepthAndOcclusion = true,
                    singleHitPerPulse = true, jumpingEvades = true, reachesBothEdges = true,
                    normalKnockbackAndPostHitInvincibility = true, followingPulseBlockedDuringInvincibility = true, immunity = true, deathEvents };
                Directory.CreateDirectory(Output); File.WriteAllText(Output + "runtime.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
                return report;
            }
            finally
            {
                foreach (var wave in created) if (wave != null) UnityEngine.Object.DestroyImmediate(wave.gameObject);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }
        static void PositionFeet(GameObject actor, Collider2D collider, Vector2 point)
        {
            actor.transform.position = new Vector3(point.x, point.y, 0); Physics2D.SyncTransforms();
            actor.transform.position += Vector3.up * (point.y - collider.bounds.min.y); Physics2D.SyncTransforms();
        }
        static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }

        public static void Render()
        {
            var boss = UnityEngine.Object.FindFirstObjectByType<MON_SkullMonk>();
            boss.HP = boss.maxHP; // Nonserialized runtime stats only; needed by the wave preview.
            var root = new GameObject("Wave ritual preview"); root.layer = 31;
            var actor = root.AddComponent<SpriteRenderer>();
            var smokeRoot = new GameObject("Ground smoke preview"); smokeRoot.layer = 31;
            var smoke = smokeRoot.AddComponent<SpriteRenderer>(); smoke.sortingOrder = 1;
            var smokeFrames = SkullMonkWaveSetup.Frames("SkullMonkWaveSmoke.png");
            var clips = new[] { "SkullMonkWavePlant", "SkullMonkWavePulse", "SkullMonkWaveRecover", "SkullMonkIdle" }
                .Select(n => AssetDatabase.LoadAssetAtPath<AnimationClip>(SkullMonkWaveSetup.Anim + n + ".anim")).ToArray();
            var curves = clips.Select(c => AnimationUtility.GetObjectReferenceCurve(c,
                AnimationUtility.GetObjectReferenceCurveBindings(c)[0])).ToArray();
            var cameraObject = new GameObject("Wave ritual camera");
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 4.4f;
            camera.transform.position = new Vector3(0, -.1f, -10); camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.065f, .09f, .12f);
            var rt = new RenderTexture(1000,600,24); camera.targetTexture = rt;
            var capture = new Texture2D(1000,600,TextureFormat.RGBA32,false);
            var waves = new List<SkullMonkGroundWave>(); int released = 0;
            Directory.CreateDirectory(Output + "frames");
            float ground = -2.54624f;
            try
            {
                for (int i = 0; i < 240; i++)
                {
                    float t = i / 30f; int clip; float local;
                    if (t < 1.2f) { clip=0; local=t; }
                    else if (t < 6.2f) { clip=1; local=Mathf.Repeat(t-1.2f,1f); }
                    else if (t < 7.2f) { clip=2; local=t-6.2f; }
                    else { clip=3; local=t-7.2f; }
                    actor.sprite=(Sprite)curves[clip].Last(k=>k.time<=local+.0001f).value;
                    float smokeAge=t-1f; smoke.enabled=smokeAge>=0 && smokeAge<.4f;
                    if(smoke.enabled)
                    {
                        int frame=Mathf.Min(3,Mathf.FloorToInt(smokeAge*10)); smoke.sprite=smokeFrames[frame];
                        float size=1.7f/smokeFrames[0].bounds.size.y; smokeRoot.transform.localScale=Vector3.one*size;
                        smokeRoot.transform.position=new Vector3(.65f,ground-smoke.sprite.bounds.min.y*size,0);
                        smoke.color=new Color(1,1,1,Mathf.Lerp(1,.15f,frame/4f));
                    }
                    foreach(var wave in waves.ToArray()) if(wave!=null) wave.Advance(1f/30f);
                    if(released<5 && t+.0001f>=1.6f+released)
                    {
                        var wave=SkullMonkGroundWave.Create(boss,new Vector2(.65f,ground),-22f,22f,12,.45f,0,actor);
                        foreach(var tr in wave.GetComponentsInChildren<Transform>()) tr.gameObject.layer=31;
                        waves.Add(wave); released++;
                    }
                    camera.Render(); RenderTexture.active=rt; capture.ReadPixels(new Rect(0,0,1000,600),0,0);capture.Apply();
                    File.WriteAllBytes(Output+"frames/"+i.ToString("D3")+".png",capture.EncodeToPNG());
                    if(i==60)File.WriteAllBytes(Output+"wave.png",capture.EncodeToPNG());
                    if(i==32)File.WriteAllBytes(Output+"plant-smoke.png",capture.EncodeToPNG());
                }
            }
            finally
            {
                foreach(var wave in waves)if(wave!=null)UnityEngine.Object.DestroyImmediate(wave.gameObject);
                RenderTexture.active=null;camera.targetTexture=null;
                UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(smokeRoot);
                UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(capture);
            }
        }
    }
}
