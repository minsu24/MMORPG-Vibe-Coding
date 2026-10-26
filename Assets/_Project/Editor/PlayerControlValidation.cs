using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EasternFantasy.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.EditorTools
{
    public static class PlayerControlValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly List<string> results = new List<string>();
        private static void Call(object target, string name) => target.GetType().GetMethod(name, Private).Invoke(target, null);
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
        private static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
            results.Add("PASS: " + message);
        }

        [MenuItem("Eastern Fantasy/Controls/Validate Player Controls")]
        public static void RunMenu()
        {
            Directory.CreateDirectory("Artifacts/Controls");
            try
            {
                string report = Run();
                File.WriteAllText("Artifacts/Controls/Validation.txt", report);
                Debug.Log(report);
            }
            catch (Exception error)
            {
                File.WriteAllText("Artifacts/Controls/Validation.txt", string.Join("\n", results) + "\nFAIL: " + error);
                Debug.LogException(error);
            }
        }

        public static string Run()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run outside Play mode.");
            results.Clear();
            TestAttackBuffer();
            foreach (string name in new[] { "Player", "MonkPlayer", "SwordswomanPlayer" }) TestPrefab(name);
            return string.Join("\n", results) + "\nAll " + results.Count + " checks passed. Fixtures used isolated preview scenes; no scene or prefab saved.";
        }

        private static void TestAttackBuffer()
        {
            var buffer = new PlayerAttackInputBuffer();
            Check(!buffer.Read(false, false, true, 0f, .15f), "Idle does not invent an attack");
            Check(buffer.Read(true, true, false, 1f, .15f), "Attack press is accepted immediately");
            Check(buffer.Read(false, false, false, 1.14f, .15f), "Released tap survives inside the reservation window");
            Check(!buffer.Read(false, false, false, 1.16f, .15f), "Expired tap does not cause a late attack");
            buffer.Read(true, true, false, 2f, .15f);
            buffer.Consume();
            Check(!buffer.Read(false, false, false, 2.01f, .15f), "Successful attack consumes the reservation exactly once");
            Check(buffer.Read(false, true, true, 3f, .15f), "Held attack repeats when enabled");
            Check(!buffer.Read(false, true, false, 3f, .15f), "Held attack respects the repeat setting");
            buffer.Read(true, true, true, 4f, .15f);
            buffer.Suppress();
            Check(!buffer.Read(false, true, true, 4.01f, .15f), "Menu or hit stun discards the queue and blocks a held key");
            Check(!buffer.Read(false, false, true, 4.02f, .15f), "Releasing the blocked key does not attack");
            Check(buffer.Read(true, true, true, 4.03f, .15f), "A fresh press works after releasing the blocked key");
            buffer.Consume();
            buffer.Read(true, true, false, 5f, .15f);
            Check(buffer.Read(false, false, false, 5f, .15f), "Frozen gameplay time preserves input during hit stop");
        }

        private static void TestPrefab(string name)
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            PlayerInputReader input = null;
            try
            {
                PhysicsScene2D physics = scene.GetPhysicsScene2D();
                Check(physics != Physics2D.defaultPhysicsScene, name + ": isolated physics fixture");
                var floor = new GameObject("Control validation floor", typeof(BoxCollider2D));
                SceneManager.MoveGameObjectToScene(floor, scene);
                floor.transform.position = new Vector3(0f, -.5f);
                floor.GetComponent<BoxCollider2D>().size = new Vector2(30f, 1f);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/" + name + ".prefab");
                var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var entity = player.GetComponent<PlayerEntity>();
                var movement = player.GetComponent<PlayerMovement2D>();
                var animation = player.GetComponent<PlayerIdleAnimation>();
                var animator = player.GetComponentInChildren<Animator>();
                var body = player.GetComponent<Rigidbody2D>();
                input = player.GetComponent<PlayerInputReader>();
                Call(entity, "Awake");
                Call(movement, "Awake");
                animator.Rebind(); animator.Update(0f);
                Call(animation, "Awake");
                Call(input, "Awake");
                Call(input, "OnEnable");
                if (player.GetComponent<MonkEnergy>() != null) Call(player.GetComponent<MonkEnergy>(), "Awake");
                MonoBehaviour basic = null;
                foreach (MonoBehaviour component in player.GetComponents<MonoBehaviour>())
                    if (component.enabled && component is IPlayerBasicAttack) { basic = component; break; }
                Check(basic != null, name + ": enabled basic attack handler exists");
                Call(basic, "Awake");
                TestJump(name, movement, body, physics);

                entity.isKnockback = true;
                Check(!((IPlayerBasicAttack)basic).TryAttack(), name + ": hit stun rejects basic attack");
                entity.isKnockback = false;
                movement.SetMovementLocked(true);
                Check(!((IPlayerBasicAttack)basic).TryAttack(), name + ": movement lock rejects basic attack");
                movement.SetMovementLocked(false);
                object first = new object(), second = new object();
                input.SetInputBlocked(first, true); input.SetInputBlocked(second, true);
                input.SetInputBlocked(first, false);
                Check(input.IsInputBlocked && !animation.CanPerformCombatAction, name + ": overlapping dialogue locks remain owned independently");
                input.SetInputBlocked(second, false);
                Check(animation.CanPerformCombatAction, name + ": final owner releases gameplay");
                movement.SetMoveInput(-1f); animation.RefreshAttackFacing();
                Check(animation.FacingDirectionX == -1f, name + ": same-frame left input updates attack facing");
                movement.SetMoveInput(1f); animation.RefreshAttackFacing();
                Check(animation.FacingDirectionX == 1f, name + ": same-frame right input updates attack facing");

                // Sample the real controller at known times, independently of editor wall-clock speed.
                string attackState = Get<string>(animation, "attackStateName");
                Set(animation, "<IsAttacking>k__BackingField", true);
                Set(animation, "<IsBasicAttackActive>k__BackingField", true);
                animator.Play("Base Layer." + attackState, 0, .01f); animator.Update(0f);
                Check(!animation.HasReachedBasicAttackHit(.18f), name + ": wind-up does not hit early");
                animator.Play("Base Layer." + attackState, 0, .7f); animator.Update(0f);
                Check(animation.HasReachedBasicAttackHit(.18f), name + ": hit threshold follows animator progress");
                animation.MarkBasicAttackHit(.28f);
                Check(!animation.CanChainBasicAttack, name + ": immediate follow-through cannot be cancelled");
                Set(animation, "basicAttackChainAt", Time.time - .01f);
                Check(animation.CanChainBasicAttack, name + ": late recovery permits skill chaining");
                string skillState = name == "Player" ? "PlayerThrowTails"
                    : name == "MonkPlayer" ? "MonkStab" : "SwordDoubleSlash";
                int oldSequence = animation.BasicAttackSequence;
                Check(animation.TryRequestSkillAnimation(skillState), name + ": a real skill animation can replace late basic recovery");
                Check(animation.IsAttacking && !animation.IsBasicAttackActive
                    && animation.BasicAttackSequence != oldSequence, name + ": chained skill owns the animation and invalidates the old swing");
                animation.FinishSkillAnimation();
                Set(animation, "<IsAttacking>k__BackingField", true);
                Set(animation, "<IsBasicAttackActive>k__BackingField", true);
                int sequence = animation.BasicAttackSequence;
                IEnumerator pendingHit = null;
                if (!(basic is PlayerProjectileShooter))
                {
                    Set(basic, "attackSequence", sequence);
                    pendingHit = (IEnumerator)basic.GetType().GetMethod("AttackRoutine", Private).Invoke(basic, null);
                }
                input.SetInputBlocked(first, true);
                Check(!animation.IsAttacking && !animation.IsBasicAttackActive
                    && animation.BasicAttackSequence != sequence, name + ": menu cancels the active swing and invalidates pending damage");
                if (pendingHit != null) Check(!pendingHit.MoveNext(), name + ": cancelled melee coroutine exits without delayed damage");
                input.SetInputBlocked(first, false);
                entity.HP = 0f;
                Check(!((IPlayerBasicAttack)basic).TryAttack(), name + ": death rejects basic attack");
                entity.HP = entity.maxHP;
                if (!(basic is PlayerProjectileShooter)) TestMeleeHit(name, basic, entity, movement, animation, animator, scene);
            }
            finally
            {
                if (input != null)
                {
                    Call(input, "OnDisable");
                    var actions = Get<UnityEngine.InputSystem.InputActionAsset>(input, "runtimeActions");
                    Set(input, "runtimeActions", null);
                    if (actions != null) UnityEngine.Object.DestroyImmediate(actions);
                }
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void TestMeleeHit(string name, MonoBehaviour basic, PlayerEntity entity,
            PlayerMovement2D movement, PlayerIdleAnimation animation, Animator animator, Scene scene)
        {
            var enemies = new List<EnemyController>();
            var behind = new GameObject("Target behind player", typeof(BoxCollider2D), typeof(MON_Goblin));
            SceneManager.MoveGameObjectToScene(behind, scene);
            behind.layer = LayerMask.NameToLayer("Enemy");
            behind.transform.position = entity.transform.position + Vector3.left;
            var rearEnemy = behind.GetComponent<EnemyController>();
            typeof(EnemyController).GetField("_maxHP", Private).SetValue(rearEnemy, 1000f);
            typeof(Entity).GetMethod("Setup", Private).Invoke(rearEnemy, null);
            for (int i = 0; i < 20; i++)
            {
                var target = new GameObject("Crowded target " + i, typeof(BoxCollider2D), typeof(MON_Goblin));
                SceneManager.MoveGameObjectToScene(target, scene);
                target.layer = LayerMask.NameToLayer("Enemy");
                target.transform.position = entity.transform.position + Vector3.right;
                var duplicate = new GameObject("Second hurtbox", typeof(BoxCollider2D));
                duplicate.transform.SetParent(target.transform, false); duplicate.layer = target.layer;
                var enemy = target.GetComponent<EnemyController>();
                typeof(EnemyController).GetField("_maxHP", Private).SetValue(enemy, 1000f);
                typeof(Entity).GetMethod("Setup", Private).Invoke(enemy, null);
                enemies.Add(enemy);
            }
            Set(entity, "criticalChance", 0f);
            if (basic is SwordswomanBasicAttack) Set(basic, "slashEffectPrefab", null);
            movement.SetMoveInput(1f); animation.RefreshAttackFacing();
            Set(animation, "<IsAttacking>k__BackingField", true);
            Set(animation, "<IsBasicAttackActive>k__BackingField", true);
            Set(basic, "attackSequence", animation.BasicAttackSequence);
            animator.Play("Base Layer." + Get<string>(animation, "attackStateName"), 0, .7f);
            animator.Update(0f); Physics2D.SyncTransforms();
            var routine = (IEnumerator)basic.GetType().GetMethod("AttackRoutine", Private).Invoke(basic, null);
            float savedTimeScale = Time.timeScale;
            try
            {
                // Suppress only global camera/time feedback in this isolated damage test.
                Time.timeScale = 0f;
                routine.MoveNext();
                float expected = entity.Attack_Power * Get<float>(basic, "damageMultiplier");
                bool allHitOnce = enemies.TrueForAll(enemy => Mathf.Abs(enemy.HP - (1000f - expected)) < .001f);
                Check(allHitOnce, name + ": twenty crowded enemies each take one hit despite duplicate hurtboxes");
                Check(rearEnemy.HP == 1000f, name + ": forward melee attack does not damage targets behind the player");
                routine.MoveNext();
                Check(enemies.TrueForAll(enemy => Mathf.Abs(enemy.HP - (1000f - expected)) < .001f),
                    name + ": recovery cannot repeat melee damage");
                animation.CancelBasicAttack();
                Check(!routine.MoveNext(), name + ": the damage coroutine releases recovery after cancellation");
            }
            finally { Time.timeScale = savedTimeScale; }
        }

        private static void TestJump(string name, PlayerMovement2D movement, Rigidbody2D body, PhysicsScene2D physics)
        {
            Action<Vector2> place = position =>
            {
                body.position = position; body.transform.position = position;
                body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
            };
            place(new Vector2(0f, 2f));
            for (int i = 0; i < 80; i++) physics.Simulate(.02f);
            Call(movement, "FixedUpdate");
            Check(movement.IsGrounded, name + ": real floor contact grants grounding");
            Vector2 groundedPosition = body.position;
            movement.RequestJump(); Call(movement, "FixedUpdate");
            Check(body.linearVelocity.y > 0f && !movement.IsGrounded, name + ": grounded jump launches immediately");
            body.linearVelocity = Vector2.zero;
            place(new Vector2(0f, 4f));
            movement.RequestJump(); Call(movement, "FixedUpdate");
            Check(body.linearVelocity.y == 0f, name + ": consumed coyote allowance cannot grant a double jump");
            Check(Get<bool>(movement, "jumpRequested"), name + ": airborne press is reserved briefly");
            place(groundedPosition); Call(movement, "FixedUpdate");
            Check(body.linearVelocity.y > 0f, name + ": landing consumes a valid buffered jump");
            place(new Vector2(0f, 4f)); movement.RequestJump();
            Set(movement, "jumpExpiresAt", Time.time - 1f); Call(movement, "FixedUpdate");
            place(groundedPosition); Call(movement, "FixedUpdate");
            Check(body.linearVelocity.y == 0f, name + ": expired jump does not fire on landing");
            place(new Vector2(50f, 4f)); movement.RequestJump(); Call(movement, "FixedUpdate");
            Check(body.linearVelocity.y > 0f, name + ": recent ledge departure accepts a coyote jump");
            movement.ClearInput(); place(new Vector2(50f, 4f));
            movement.RequestJump(); Set(movement, "lastGroundedAt", Time.time - 1f); Call(movement, "FixedUpdate");
            Check(body.linearVelocity.y == 0f, name + ": coyote jump expires away from ground");
            movement.ClearInput(); place(groundedPosition); Call(movement, "FixedUpdate");
            place(new Vector2(50f, 4f)); movement.RequestJump(true); Call(movement, "FixedUpdate");
            Check(body.linearVelocity.y == 0f, name + ": down-jump cannot use coyote time to jump upward");
            movement.ClearInput(); place(groundedPosition); Call(movement, "FixedUpdate");
            movement.RequestJump(true); Call(movement, "FixedUpdate");
            Check(body.linearVelocity.y > 0f, name + ": solid floors remain solid on down-jump");
            movement.ClearInput(); place(groundedPosition); Call(movement, "FixedUpdate");
            var platform = new GameObject("One-way test platform", typeof(BoxCollider2D), typeof(PlatformEffector2D));
            SceneManager.MoveGameObjectToScene(platform, body.gameObject.scene);
            platform.transform.position = new Vector3(65f, -.5f);
            var platformCollider = platform.GetComponent<BoxCollider2D>();
            platformCollider.size = new Vector2(8f, 1f);
            platformCollider.usedByEffector = true;
            platform.GetComponent<PlatformEffector2D>().useOneWay = true;
            place(new Vector2(65f, groundedPosition.y)); Call(movement, "FixedUpdate");
            Check(movement.IsGrounded, name + ": one-way platform supports the player");
            movement.RequestJump(true); Call(movement, "FixedUpdate");
            Check(body.linearVelocity.y < 0f && Physics2D.GetIgnoreCollision(body.GetComponent<CapsuleCollider2D>(), platformCollider),
                name + ": down-jump drops through a one-way platform");
            for (int i = 0; i < 35; i++) { physics.Simulate(.02f); Call(movement, "FixedUpdate"); }
            Check(!Physics2D.GetIgnoreCollision(body.GetComponent<CapsuleCollider2D>(), platformCollider),
                name + ": platform collision is restored after clearing the platform");
            movement.ClearInput(); place(groundedPosition); Call(movement, "FixedUpdate");
        }
    }
}
