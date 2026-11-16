using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EasternFantasy.Player;
using EasternFantasy.Inventory;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class HumanFacedBirdValidation
    {
        private const string Folder = "Artifacts/HumanFacedBird";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly List<string> results = new List<string>();
        private static GameObject fixture;
        private static MON_HumanFacedBird bird;
        private static PlayerEntity player;
        private static Rigidbody2D body;
        private static Camera camera;
        private static int hits;
        private static void Set(object owner, string name, object value) =>
            (owner is EnemyController ? typeof(EnemyController).GetField(name, Hidden) ?? owner.GetType().GetField(name, Hidden)
                : owner.GetType().GetField(name, Hidden)).SetValue(owner, value);

        public static void Run()
        {
            if (!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("EasternFantasy.SaveTestDirectory", "")))
                throw new InvalidOperationException("Use Play mode with isolated save storage.");
            Directory.CreateDirectory(Folder); results.Clear(); hits = 0;
            ValidateAppearance();
            EasternFantasy.Saving.SaveGameService.BeginAdminSession();
            fixture = new GameObject("HumanFacedBirdValidation");
            var floor = new GameObject("ValidationFloor", typeof(BoxCollider2D), typeof(SpriteRenderer));
            floor.transform.SetParent(fixture.transform);
            floor.transform.position = new Vector3(1000, -.5f, 0);
            floor.transform.localScale = new Vector3(30, 1, 1);
            var groundArt = floor.GetComponent<SpriteRenderer>();
            groundArt.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1f);
            groundArt.color = new Color(.12f, .15f, .18f);
            groundArt.sortingOrder = -5;
            var actor = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab"));
            actor.transform.SetParent(fixture.transform); actor.transform.position = new Vector3(1003, .75f, 0);
            foreach (var script in actor.GetComponents<MonoBehaviour>())
                if (script is PlayerMovement2D || script is PlayerInputReader || script is PlayerAdminController || script is PlayerDeathController)
                    script.enabled = false;
            var actorBody = actor.GetComponent<Rigidbody2D>(); actorBody.bodyType = RigidbodyType2D.Kinematic;
            player = actor.GetComponent<PlayerEntity>(); player.Damaged += CountHit;
            var enemy = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HumanFacedBirdSetup.PrefabPath));
            enemy.transform.SetParent(fixture.transform); enemy.transform.position = new Vector3(1000, .05f, 0);
            bird = enemy.GetComponent<MON_HumanFacedBird>(); body = enemy.GetComponent<Rigidbody2D>();
            Set(bird, "player", actor); Set(bird, "playerController", actor.GetComponent<PlayerMovement2D>());
            Set(bird, "playerEntity", player); Set(bird, "playerProgression", actor.GetComponent<PlayerProgression>());
            Set(bird, "playerCurrency", actor.GetComponent<PlayerCurrency>()); Set(bird, "patrolRange", 0f);
            Set(bird, "_rewardYeopjeon", 0); Set(bird, "_reward_EXP", 0f);
            var view = new GameObject("ValidationCamera", typeof(Camera)); view.transform.SetParent(fixture.transform);
            camera = view.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 2.2f;
            camera.transform.position = new Vector3(1001, 1.6f, -10);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.08f, .1f, .15f);
            camera.enabled = false;
            Physics2D.SyncTransforms();
            player.StartCoroutine(Routine());
        }

        private static void CountHit(float _) => hits++;
        private static void ValidateAppearance()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanFacedBirdSetup.PrefabPath);
            Check(Vector3.Distance(prefab.transform.localScale, Vector3.one * 1.3f) < .001f,
                "Prefab is 30 percent larger with no cumulative enlargement");
            foreach (string action in new[] { "Idle", "Walk", "Attack", "Dead" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(HumanFacedBirdSetup.AnimationFolder + "HumanFacedBird" + action + ".anim");
                var frames = AnimationUtility.GetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"));
                var sprite = (Sprite)frames[0].value;
                float bottom = float.PositiveInfinity, top = float.NegativeInfinity;
                foreach (var vertex in sprite.vertices) { bottom = Mathf.Min(bottom, vertex.y); top = Mathf.Max(top, vertex.y); }
                Check(Mathf.Abs((top - bottom) * 1.3f - 2.561f) < .005f, action + " matches common upright body height");
                var flip = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(SpriteRenderer), "m_FlipX"));
                float expected = action == "Idle" || action == "Dead" ? 1f : 0f;
                bool correct = flip != null;
                foreach (var frame in frames) correct &= flip != null && Mathf.Approximately(flip.Evaluate(frame.time), expected);
                Check(correct, action + " normalizes source facing in every frame");
            }
        }
        private static void Check(bool passed, string message)
        {
            results.Add((passed ? "PASS " : "FAIL ") + message);
            File.WriteAllLines(Folder + "/Validation.txt", results);
            if (!passed) throw new InvalidOperationException(message);
        }
        private static IEnumerator WaitForAttack()
        {
            float deadline = Time.time + 2f;
            while (!bird.IsAttacking && Time.time < deadline) yield return null;
            Check(bird.IsAttacking, "Nearby grounded player starts an attack");
        }
        private static IEnumerator WaitForLanding()
        {
            float deadline = Time.time + 2f;
            while (bird.IsAttacking && Time.time < deadline) yield return null;
            yield return new WaitForSeconds(.15f);
            Check(!bird.IsAttacking && Mathf.Abs(body.position.y) < .1f && Mathf.Approximately(body.gravityScale, 1f),
                "Attack lands on the floor and restores gravity");
        }
        private static void AimPlayer(float direction)
        {
            player.transform.position = new Vector3(body.position.x + direction * 1.3f, .75f, 0);
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            Set(bird, "nextAttackAt", Time.time);
            Physics2D.SyncTransforms();
        }
        private static void Capture(string name)
        {
            var rt = new RenderTexture(960, 640, 24);
            var image = new Texture2D(960, 640, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = rt; camera.aspect = 1.5f; camera.Render(); RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 960, 640), 0, 0); image.Apply();
                File.WriteAllBytes(Folder + "/" + name + ".png", image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(image);
            }
        }

        private static IEnumerator Routine()
        {
            yield return new WaitForSeconds(.25f);
            Check(body.position.x > 1000.1f, "Uses common monster detection and walking chase");
            bird.Speed = 0f; body.linearVelocity = Vector2.zero; body.position = new Vector2(1000, .03f);
            AimPlayer(1f);
            yield return WaitForAttack();
            Check(bird.transform.localScale.x < 0, "Faces right for a player on the right");
            yield return new WaitForSeconds(.3f);
            Check(body.position.y > .4f && hits == 0, "Rises before claw damage; windup does not damage");
            Capture("Takeoff");
            yield return new WaitForSeconds(.3f);
            Check(hits == 1, "Claw animation event damages player once");
            Capture("Claw");
            bird.HumanFacedBirdClawHit(); bird.HumanFacedBirdClawHit();
            Check(hits == 1, "Duplicate hit events cannot damage twice in one attack");
            yield return WaitForLanding();
            Capture("Landing");
            AimPlayer(-1f);
            yield return WaitForAttack();
            Check(bird.transform.localScale.x > 0, "Faces left for a player on the left");
            yield return new WaitForSeconds(.6f);
            Check(hits == 2, "Mirrored claw attack damages on the left");
            yield return WaitForLanding();
            player.transform.position = new Vector3(1010, .75f, 0); Physics2D.SyncTransforms();
            Set(bird, "nextAttackAt", Time.time);
            yield return new WaitForSeconds(.3f);
            Check(!bird.IsAttacking, "Does not attack a distant player");
            AimPlayer(1f); yield return WaitForAttack();
            yield return new WaitForSeconds(.2f);
            bird.ApplyKnockback(-1f); bird.HumanFacedBirdClawHit();
            Check(!bird.IsAttacking && body.gravityScale == 1f && hits == 2, "Knockback cancels attack, restores gravity and suppresses late hit");
            yield return new WaitForSeconds(.6f);
            AimPlayer(1f); yield return WaitForAttack();
            yield return new WaitForSeconds(.2f);
            bird.enabled = false; bird.HumanFacedBirdClawHit();
            Check(body.gravityScale == 1f && !bird.IsAttacking && hits == 2, "Disable cancels airborne attack without lingering damage");
            bird.enabled = true;
            yield return new WaitForSeconds(.6f);
            AimPlayer(1f); yield return WaitForAttack();
            yield return new WaitForSeconds(.2f);
            bird.TakeDamage(bird.maxHP + 1, false, false); bird.HumanFacedBirdClawHit();
            yield return null;
            Check(bird.HP == 0 && !bird.IsAttacking && !body.simulated && body.gravityScale == 1f && hits == 2,
                "Death cancels flight and prevents delayed claw damage");
            Check(bird.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("HumanFacedBirdDead"), "Death plays supplied death animation");
            Capture("Death");
            yield return new WaitForSeconds(1.3f);
            Check(bird == null, "Death animation completes before monster removal");
            player.Damaged -= CountHit;
            UnityEngine.Object.Destroy(fixture);
            results.Add("COMPLETE"); File.WriteAllLines(Folder + "/Validation.txt", results);
        }
    }
}
