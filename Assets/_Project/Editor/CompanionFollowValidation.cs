using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class CompanionFollowValidation
    {
        private const string Report = "Artifacts/CompanionFollow/Validation.txt";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        public static void RunPathChecks()
        {
            Directory.CreateDirectory("Artifacts/CompanionFollow");
            File.WriteAllText(Report, "Delayed companion path validation\n");
            foreach (int fps in new[] { 30, 60, 120 })
            {
                float dt = 1f / fps;
                var trail = new CompanionFollowTrail();
                trail.Reset(Vector3.left * 1.1f, Vector3.zero, true, 0f, .4f);
                float firstAir = -1f, launchX = -1f;
                Vector3 pet = Vector3.zero;
                for (int frame = 1; frame <= fps * 6; frame++)
                {
                    float t = frame * dt;
                    bool air = t > 1f && t < 1.7f;
                    float phase = Mathf.Clamp01((t - 1f) / .7f);
                    var owner = new Vector3(Mathf.Min(t, 2f) * 4f, air ? 8f * phase * (1f - phase) : 0f);
                    trail.Record(owner, !air, t);
                    pet = trail.Advance(t, dt, .4f, 1.1f);
                    if (firstAir < 0f && pet.y > .02f) { firstAir = t; launchX = pet.x; }
                }
                Check(firstAir >= 1.4f - dt && firstAir <= 1.4f + dt * 2f, fps + " fps: jump delayed approximately 0.4 seconds");
                Check(launchX >= 3.95f && launchX <= 4f + dt * 9f, fps + " fps: same takeoff location, without horizontal sprite offset");
                Check(Mathf.Abs(pet.y) < .001f && Mathf.Abs(8f - pet.x - 1.1f) < .03f, fps + " fps: grounded idle spacing after landing");
                for (int frame = fps * 6 + 1; frame <= fps * 120; frame++)
                {
                    float t = frame * dt;
                    trail.Record(new Vector3(8f, 0f), true, t);
                    pet = trail.Advance(t, dt, .4f, 1.1f);
                }
                Check(trail.Count < 150, fps + " fps: long idle history stays bounded");
                bool resumed = false;
                for (int frame = 1; frame <= fps * 2; frame++)
                {
                    float t = 120f + frame * dt;
                    trail.Record(new Vector3(8f + frame * dt * 4f, 0f), true, t);
                    pet = trail.Advance(t, dt, .4f, 1.1f);
                    resumed |= pet.x > 8.2f;
                }
                Check(resumed, fps + " fps: resumes without replaying the long idle wait");
            }
            var hop = new CompanionFollowTrail();
            hop.Reset(Vector3.left * 1.1f, Vector3.zero, true, 0f, .4f);
            bool hopped = false;
            Vector3 final = Vector3.zero;
            for (int i = 1; i < 300; i++)
            {
                float t = i / 60f;
                bool air = t > 1f && t < 1.3f;
                float phase = Mathf.Clamp01((t - 1f) / .3f);
                hop.Record(new Vector3(0f, air ? .6f * phase * (1f - phase) : 0f), !air, t);
                final = hop.Advance(t, 1f / 60f, .4f, 1.1f);
                hopped |= final.y > .01f;
            }
            Check(hopped && Mathf.Abs(final.y) < .001f, "short in-place hops replay completely instead of stopping in midair");
            Check(hop.IsCaughtUp, "completed in-place jump remains ready to enter combat");
            var turn = new CompanionFollowTrail();
            turn.Reset(Vector3.left * 1.1f, Vector3.zero, true, 0f, .4f);
            float turnedAt = -1f;
            Vector3 previous = Vector3.left * 1.1f;
            for (int i = 1; i <= 180; i++)
            {
                float t = i / 60f;
                float x = t <= 1f ? t * 4f : 4f - (t - 1f) * 4f;
                turn.Record(new Vector3(x, 0f), true, t);
                Vector3 position = turn.Advance(t, 1f / 60f, .4f, 1.1f);
                if (t > 1f && turnedAt < 0f && position.x < previous.x - .001f) turnedAt = t;
                Check(position.y == 0f && Mathf.Abs(position.x - previous.x) < .15f, "continuous ground route through reversal", false);
                previous = position;
            }
            Check(turnedAt >= 1.39f && turnedAt <= 1.46f, "direction reversal follows recorded path with delay, without swapping sides");
        }

        public static void RunNative()
        {
            Application.runInBackground = true;
            SaveGameService.Instance.StartCoroutine(ValidateActor());
        }

        private static IEnumerator ValidateActor()
        {
            while (SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible) yield return null;
            SaveGameService.BeginAdminSession();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            var movement = player.GetComponent<PlayerMovement2D>();
            player.isInvincible = true;
            player.GetComponent<PlayerInputReader>().enabled = false;
            typeof(PlayerIdleAnimation).GetField("inputReader", Hidden).SetValue(player.GetComponent<PlayerIdleAnimation>(), null);
            var platform = new GameObject("Companion Follow Test Platform");
            PetCompanionActor actor = null;
            try
            {
                int mask = ((LayerMask)typeof(PlayerMovement2D).GetField("groundLayers", Hidden).GetValue(movement)).value;
                platform.layer = Enumerable.Range(0, 32).First(i => (mask & (1 << i)) != 0);
                movement.body.constraints = RigidbodyConstraints2D.FreezeRotation;
                player.GetComponent<PlayerLocationSetter>().TeleportToAndRetarget(new Vector3(10000f, 30f, 0f), Quaternion.identity);
                platform.transform.position = new Vector3(10000f, player.GetComponent<CapsuleCollider2D>().bounds.min.y - .52f);
                platform.AddComponent<BoxCollider2D>().size = new Vector2(150f, 1f);
                Physics2D.SyncTransforms();
                yield return new WaitForSeconds(.2f);
                var prefab = AssetDatabase.LoadAssetAtPath<PetCompanionActor>("Assets/_Project/Prefabs/Companions/GoblinCompanion.prefab");
                actor = UnityEngine.Object.Instantiate(prefab);
                actor.Initialize(player, JsonUtility.FromJson<PetCompanionProgress>("{\"level\":1,\"equipped\":true}"));
                movement.SetMoveInput(1f);
                yield return new WaitForSeconds(1f);
                float floor = actor.transform.position.y;
                float launchX = player.transform.position.x;
                movement.RequestJump();
                yield return new WaitForFixedUpdate();
                float jumpAt = Time.time;
                Check(!movement.IsGrounded, "native player jump started");
                float firstAir = -1f, petLaunchX = 0f;
                float until = Time.time + 2.5f;
                while (Time.time < until)
                {
                    yield return null;
                    if (firstAir < 0f && actor.transform.position.y > floor + .04f)
                    {
                        firstAir = Time.time;
                        petLaunchX = actor.transform.position.x;
                    }
                }
                float delay = firstAir - jumpAt;
                File.AppendAllText(Report, "Native jump delay: " + delay + " seconds; takeoff delta X: " + (petLaunchX - launchX) + "\n");
                Check(firstAir > 0f && delay > .3f && delay < .65f, "real prefab jumps after player, not simultaneously");
                Check(Mathf.Abs(petLaunchX - launchX) < .65f, "real prefab reaches the player's takeoff location before rising");
                movement.SetMoveInput(0f);
                yield return new WaitForSeconds(1.5f);
                Check(Mathf.Abs(actor.transform.position.y - floor) < .05f, "real prefab settles on ground after stopping");
                Vector3 before = actor.transform.position;
                GameTimeController.SetPaused(actor, true);
                yield return new WaitForSecondsRealtime(.15f);
                Check((actor.transform.position - before).sqrMagnitude < .00001f, "pause freezes path playback");
                GameTimeController.SetPaused(actor, false);
                player.GetComponent<PlayerLocationSetter>().TeleportToAndRetarget(player.transform.position + Vector3.right * 100f, Quaternion.identity);
                yield return null;
                yield return null;
                Check(Vector2.Distance(actor.transform.position, player.transform.position) < 3f, "teleport resets trail near owner");
                Check(actor.GetComponentsInChildren<Collider2D>().Length == 0, "companion still cannot block or be attacked");
                File.AppendAllText(Report, "COMPLETE\n");
            }
            finally
            {
                if (actor != null) { GameTimeController.SetPaused(actor, false); UnityEngine.Object.Destroy(actor.gameObject); }
                movement.ClearInput();
                movement.body.constraints = RigidbodyConstraints2D.FreezeAll;
                UnityEngine.Object.Destroy(platform);
            }
        }

        private static void Check(bool ok, string message, bool logSuccess = true)
        {
            if (logSuccess || !ok) File.AppendAllText(Report, (ok ? "PASS " : "FAIL ") + message + "\n");
            if (!ok) throw new InvalidOperationException(message);
        }
    }
}
