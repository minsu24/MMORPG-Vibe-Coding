using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    // Play-mode regressions for immediate airborne entry and deferred dialogue ownership.
    public static class ShopkeeperTutorialValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static string Result { get; private set; } = "Not run";
        private static void Set(object target,string name,object value) => target.GetType().GetField(name,Private).SetValue(target,value);
        private static void Enter(DialogueTrigger trigger,Collider2D player) => typeof(DialogueTrigger).GetMethod("OnTriggerEnter2D",Private).Invoke(trigger,new object[]{player});
        private static void Exit(DialogueTrigger trigger,Collider2D player) => typeof(DialogueTrigger).GetMethod("OnTriggerExit2D",Private).Invoke(trigger,new object[]{player});
        private static bool Pending(DialogueTrigger trigger) => typeof(DialogueTrigger).GetField("pendingEntryDialogue",Private).GetValue(trigger)!=null;
        private static void Ground(PlayerMovement2D player,bool grounded) => typeof(PlayerMovement2D).GetField("<IsGrounded>k__BackingField",Private).SetValue(player,grounded);
        private static DialogueSequence Current(DialogueManager manager) => (DialogueSequence)typeof(DialogueManager).GetField("currentSequence",Private).GetValue(manager);
        private static void Check(bool condition,string message)
        { if(!condition) { Result="FAILED: "+message; throw new Exception(Result); } }

        public static IEnumerator Run()
        {
            Check(Application.isPlaying,"Run in Play mode");
            var manager=DialogueManager.Instance;
            Check(manager!=null && !manager.IsDialogueActive,"A ready dialogue manager is required");
            Check(!EasternFantasy.UI.MapTutorialUI.IsRunning,"Finish the map guide before validation");
            Result="Running";
            var pauseOwner=new object();
            var fixture=new GameObject("Shopkeeper Tutorial Validation"); fixture.SetActive(false);
            var definitions=new List<DialogueSequence>();
            var settings=ScriptableObject.CreateInstance<PlayerMovementSettings>();
            GameTimeController.SetPaused(pauseOwner,true);
            try
            {
                var playerObject=new GameObject("Airborne Player"); playerObject.SetActive(false);
                playerObject.transform.SetParent(fixture.transform,false); playerObject.tag="Player";
                var player=playerObject.AddComponent<PlayerMovement2D>(); Set(player,"settings",settings);
                var collider=playerObject.GetComponent<Collider2D>();
                fixture.transform.position=new Vector3(10000,10000,0);
                fixture.SetActive(true); playerObject.SetActive(true);
                var source=AssetDatabase.LoadAssetAtPath<DialogueSequence>("Assets/_Project/Data/Tutorial_DIalogue/ShopkeeperTutorialDialogue.asset");
                Func<bool,DialogueTrigger> makeTrigger=retain =>
                {
                    var go=new GameObject("Entry Trigger"); go.transform.SetParent(fixture.transform,false);
                    var trigger=go.AddComponent<DialogueTrigger>();
                    var sequence=UnityEngine.Object.Instantiate(source); definitions.Add(sequence);
                    Set(trigger,"sequence",sequence); Set(trigger,"startOnPlayerEnter",true);
                    Set(trigger,"playOnlyOnce",true); Set(trigger,"finishEntryAfterExit",retain);
                    return trigger;
                };
                var immediate=makeTrigger(false);
                Ground(player,false); Enter(immediate,collider);
                Check(Current(manager)==definitions[0] && manager.IsDialogueActive && !Pending(immediate),"Airborne entry did not start immediately");
                Exit(immediate,collider);
                Check(Current(manager)==definitions[0],"Trigger exit stopped an already started dialogue");
                manager.EndDialogue(); Enter(immediate,collider);
                Check(!manager.IsDialogueActive && !Pending(immediate),"Once-only immediate dialogue replayed");

                var busySequence=UnityEngine.Object.Instantiate(source);definitions.Add(busySequence);
                Check(manager.StartDialogue(busySequence),"Could not start competing dialogue");
                var tutorial=makeTrigger(true);var expected=definitions[definitions.Count-1];
                Ground(player,false);Enter(tutorial,collider);Exit(tutorial,collider);
                Check(Pending(tutorial),"Busy tutorial exit discarded queued entry");
                yield return null;yield return null;
                Check(Pending(tutorial) && Current(manager)==busySequence,"Queued tutorial interrupted active dialogue");
                manager.EndDialogue();
                yield return null;yield return null;
                Check(Current(manager)==expected && !Pending(tutorial) && !player.IsGrounded,"Queued tutorial still waited for landing");
                manager.EndDialogue();Enter(tutorial,collider);Exit(tutorial,collider);
                Check(!manager.IsDialogueActive && !Pending(tutorial),"Once-only queued dialogue replayed");

                Check(manager.StartDialogue(busySequence),"Could not start competing proximity dialogue");
                var ordinary=makeTrigger(false);
                Enter(ordinary,collider);Exit(ordinary,collider);
                Check(!Pending(ordinary),"Ordinary pending proximity dialogue did not cancel on exit");
                manager.EndDialogue();
                yield return null;yield return null;
                Check(!manager.IsDialogueActive,"Cancelled proximity dialogue started later");

                Check(manager.StartDialogue(busySequence),"Could not start competing disable dialogue");
                var cancelled=makeTrigger(true);
                Enter(cancelled,collider);cancelled.gameObject.SetActive(false);
                Check(!Pending(cancelled),"Disabled trigger retained pending dialogue");
                manager.EndDialogue();
                yield return null;yield return null;
                Check(!manager.IsDialogueActive,"Disabled trigger started later");
                Result="PASS: immediate airborne playback, exit keeps active dialogue, busy dialogue queues without landing, once-only replay blocked, normal exit cancellation, disable cleanup.";
                Debug.Log(Result);
            }
            finally
            {
                if(manager!=null && manager.IsDialogueActive && definitions.Contains(Current(manager))) manager.EndDialogue();
                UnityEngine.Object.Destroy(fixture);
                var played=(HashSet<DialogueSequence>)typeof(DialogueTrigger).GetField("playedSequences",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                foreach(var sequence in definitions) { played.Remove(sequence); UnityEngine.Object.Destroy(sequence); }
                UnityEngine.Object.Destroy(settings);
                GameTimeController.SetPaused(pauseOwner,false);
            }
        }
    }
}
