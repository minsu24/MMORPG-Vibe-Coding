using System;
using System.Linq;
using System.Reflection;
using EasternFantasy.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.EditorTools
{
    public static class MonkJumpLandingValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Call(object instance, string method) => instance.GetType().GetMethod(method, Private).Invoke(instance, null);
        private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

        public static string Validate()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try {
                var physics = scene.GetPhysicsScene2D();
                Check(physics != Physics2D.defaultPhysicsScene, "Fixture must have isolated physics");
                var floor = new GameObject("Floor", typeof(BoxCollider2D));
                SceneManager.MoveGameObjectToScene(floor, scene);
                floor.transform.position = new Vector3(0, -.5f);
                floor.GetComponent<BoxCollider2D>().size = new Vector2(30, 1);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/MonkPlayer.prefab");
                var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var body = player.GetComponent<Rigidbody2D>();
                var collider = player.GetComponent<CapsuleCollider2D>();
                var movement = player.GetComponent<PlayerMovement2D>();
                var idle = player.GetComponent<PlayerIdleAnimation>();
                var jump = player.GetComponent<MonkJumpAnimation>();
                var animator = player.GetComponentInChildren<Animator>();
                var renderer = animator.GetComponent<SpriteRenderer>();
                var source = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(((Sprite)AnimationUtility.GetObjectReferenceCurve(
                    AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Monk/MonkLand.anim"), EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"))[0].value))).OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
                Call(player.GetComponent<PlayerEntity>(), "Awake");
                Call(movement, "Awake");
                animator.Rebind(); animator.Update(0);
                Call(idle, "Awake"); Call(jump, "Awake");
                player.transform.position = new Vector3(0, .66f); body.position = player.transform.position;
                for (int i = 0; i < 15; i++) physics.Simulate(.02f);
                Action tick = () => { Call(movement, "FixedUpdate"); physics.Simulate(.02f); Call(jump, "Update"); Call(idle, "LateUpdate"); animator.Update(.02f); };
                tick(); Check(movement.IsGrounded, "Initial player did not settle");
                movement.RequestJump();
                bool sawRising = false, sawFalling = false, landed = false;
                int airTicks = 0;
                for (int i = 0; i < 120; i++) {
                    tick();
                    bool touching = collider.IsTouching(floor.GetComponent<BoxCollider2D>());
                    if (!touching) {
                        airTicks++;
                        Check(renderer.sprite != source[5], "Landing frame six played in midair");
                        if (body.linearVelocity.y < 0f) {
                            Check(renderer.sprite == source[4], "Falling did not immediately show frame five"); sawFalling = true;
                        } else {
                            Check(renderer.sprite != source[4], "Falling frame shown while rising"); sawRising = true;
                        }
                    } else if (airTicks > 0) {
                        Check(renderer.sprite == source[5], "First ground contact did not show frame six");
                        landed = true; break;
                    }
                }
                Check(sawRising && sawFalling && landed, "Missing rising, falling or landing phase");
                for (int i = 0; i < 2; i++) { tick(); Check(renderer.sprite == source[5], "Landing pose ended too early"); }
                for (int i = 0; i < 12; i++) tick();
                Check(animator.GetCurrentAnimatorStateInfo(0).IsName("MonkIdle"), "Did not return to idle after landing");
                movement.SetMoveInput(1); movement.RequestJump(); airTicks = 0; landed = false;
                for (int i = 0; i < 120; i++) {
                    tick();
                    if (!collider.IsTouching(floor.GetComponent<BoxCollider2D>())) {
                        airTicks++; Check(renderer.sprite != source[5], "Running jump showed landing frame six in air");
                        if (body.linearVelocity.y < 0f) Check(renderer.sprite == source[4], "Running jump missed falling frame five");
                        else Check(renderer.sprite != source[4], "Running jump showed falling frame while rising");
                    } else if (airTicks > 0) { Check(renderer.sprite == source[5], "Running jump missed landing frame six"); landed = true; break; }
                }
                Check(landed, "Running jump did not land");
                for (int i = 0; i < 20; i++) tick();
                Check(animator.GetCurrentAnimatorStateInfo(0).IsName("MonkMove"), "Did not return to walking");
                movement.SetMoveInput(0);
                // A ledge fall has no jump request, but must still use the contact-timed landing.
                body.position = new Vector2(0, 9); body.linearVelocity = Vector2.zero;
                player.transform.position = body.position;
                animator.Play("Base Layer.MonkIdle", 0, 0); animator.Update(0); airTicks = 0; landed = false;
                for (int i = 0; i < 180; i++) {
                    tick();
                    if (!collider.IsTouching(floor.GetComponent<BoxCollider2D>())) {
                        airTicks++; Check(renderer.sprite != source[5], "Ledge fall showed landing frame before contact");
                        if (body.linearVelocity.y < 0f) Check(renderer.sprite == source[4], "Ledge fall missed frame five");
                    } else if (airTicks > 0) { Check(renderer.sprite == source[5], "Ledge fall missed landing frame six"); landed = true; break; }
                }
                Check(landed, "Ledge fall did not land");
                // Jumping again during recovery immediately returns to airborne animation.
                movement.RequestJump(); tick();
                Check(renderer.sprite != source[4] && renderer.sprite != source[5] && !movement.IsGrounded, "Landing animation blocked the next jump");
                var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Monk/MonkIdle.anim");
                var idleFrame = (Sprite)AnimationUtility.GetObjectReferenceCurve(idleClip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"))[0].value;
                Check(Mathf.Abs(source[5].bounds.size.y - idleFrame.bounds.size.y) < .001f, "Landing sprite scale does not match idle");
                return "PASS: real isolated physics; frames 1-4 while rising; frame 5 immediately on descent and throughout falling; frame 6 only on floor contact; idle/walk return; ledge falls; immediate re-jump; PPU " + source[5].pixelsPerUnit.ToString("F3") + " matches idle height.";
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
