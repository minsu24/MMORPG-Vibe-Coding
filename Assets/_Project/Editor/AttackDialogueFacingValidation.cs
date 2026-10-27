using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.EditorTools
{
    public static class AttackDialogueFacingValidation
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Hidden).SetValue(target, value);
        private static void Call(object target, string name) => target.GetType().GetMethod(name, Hidden).Invoke(target, null);
        public static void Run(bool fixedVersion) => SaveGameService.Instance.StartCoroutine(Validate(fixedVersion));
        private static IEnumerator Validate(bool fixedVersion)
        {
            var results = new List<string>();
            string folder = "Artifacts/AttackDialogueFacing";
            Directory.CreateDirectory(folder);
            string path = folder + (fixedVersion ? "/After.txt" : "/Before.txt");
            Action<bool, string> check = (ok, message) =>
            {
                results.Add((ok ? "PASS " : "FAIL ") + message);
                File.WriteAllLines(path, results);
            };
            foreach (var id in new[] { CharacterClassId.Monk, CharacterClassId.Swordswoman, CharacterClassId.Dosa })
            {
                SaveGameService.PrepareSelection();
                PlayerPrefs.SetInt("Character.SelectedClass", (int)id);
                yield return SceneManager.LoadSceneAsync("MovementPrototype");
                yield return null;
                while (SaveGameService.IsRestoring) yield return null;
                var entity = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
                var input = entity.GetComponent<PlayerInputReader>();
                var movement = entity.GetComponent<PlayerMovement2D>();
                var animation = entity.GetComponent<PlayerIdleAnimation>();
                var animator = entity.GetComponentInChildren<Animator>();
                var body = entity.GetComponent<Rigidbody2D>();
                var dialogue = DialogueManager.Instance;
                dialogue.EndDialogue();
                typeof(SaveGameService).GetMethod("RestoreWorld", Hidden).Invoke(null, new object[] { new SaveGameData
                {
                    inventoryTutorial = true, statsTutorial = true, mapTutorial = true,
                    shopTutorial = true, dungeonTutorial = true, upgradeTutorial = true,
                    playedDialogues = Resources.Load<SaveAssetCatalog>("SaveAssetCatalog").entries
                        .Where(e => e.asset is DialogueSequence).Select(e => e.id).ToArray()
                } });
                Set(input, "hasFocus", true);
                entity.isInvincible = true;
                var goblin = UnityEngine.Object.FindFirstObjectByType<MON_Goblin>();
                goblin.enabled = false; goblin.HP = 1f;
                float hitDirection = id == CharacterClassId.Monk &&
                    (bool)typeof(MonkBasicAttack).GetField("invertFacingDirection", Hidden).GetValue(entity.GetComponent<MonkBasicAttack>()) ? -1f : 1f;
                entity.GetComponent<PlayerLocationSetter>().TeleportToAndRetarget(goblin.transform.position - Vector3.right * hitDirection, Quaternion.identity);
                body.constraints = RigidbodyConstraints2D.FreezeAll;
                movement.SetMoveInput(1f);
                animation.RefreshAttackFacing(); movement.ClearInput();
                IPlayerBasicAttack attack = id == CharacterClassId.Monk
                    ? (IPlayerBasicAttack)entity.GetComponent<MonkBasicAttack>()
                    : id == CharacterClassId.Swordswoman ? entity.GetComponent<SwordswomanBasicAttack>()
                    : (IPlayerBasicAttack)entity.GetComponent<PlayerProjectileShooter>();
                check(attack.TryAttack(), id + ": real basic attack starts");
                float deadline = Time.realtimeSinceStartup + 4f;
                while (!dialogue.IsDialogueActive && Time.realtimeSinceStartup < deadline) yield return null;
                check(goblin.HP <= 0f && dialogue.IsDialogueActive, id + ": lethal hit opens actual tutorial dialogue");
                check(!animation.IsBasicAttackActive && !animation.IsAttacking, id + ": dialogue cancels attack");
                check(!(bool)typeof(PlayerIdleAnimation).GetField("lockAttackFacing", Hidden).GetValue(animation), id + ": cancelled hit does not relock facing");
                dialogue.EndDialogue();
                yield return new WaitForSecondsRealtime(.2f); // Let the lethal-hit stop expire.
                input.enabled = false; // Supply deterministic movement without keyboard/UI automation.
                movement.SetMoveInput(-1f); Call(animation, "LateUpdate");
                float leftScale = animator.transform.localScale.x;
                check(animation.FacingDirectionX == -1f, id + ": turns left after dialogue");
                movement.SetMoveInput(1f); Call(animation, "LateUpdate");
                check(animation.FacingDirectionX == 1f && leftScale * animator.transform.localScale.x < 0f, id + ": moving right flips the actual sprite");
                movement.ClearInput(); input.enabled = true;
                Set(input, "hasFocus", true);
                check(attack.TryAttack(), id + ": next basic attack starts after dialogue");
                animation.CancelBasicAttack();
                animation.MarkBasicAttackHit(.28f);
                check(!(bool)typeof(PlayerIdleAnimation).GetField("lockAttackFacing", Hidden).GetValue(animation), id + ": delayed hit callback cannot relock a cancelled attack");
                animation.FinishSkillAnimation();
                check(animation.TryRequestAttack(), id + ": uninterrupted attack starts");
                animation.MarkBasicAttackHit(.28f);
                check((bool)typeof(PlayerIdleAnimation).GetField("lockAttackFacing", Hidden).GetValue(animation), id + ": live attack retains intended follow-through lock");
                animation.CancelBasicAttack();
            }
            results.Add("Validation complete.");
            File.WriteAllLines(path, results);
        }
    }
}
