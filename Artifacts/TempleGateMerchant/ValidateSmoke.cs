using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using EasternFantasy.Shop;

if (!Application.isPlaying) throw new Exception("Play mode required");
var npc = UnityEngine.Object.FindObjectsByType<MonkeyMerchantAmbientAnimation>(FindObjectsSortMode.None).Single();
var animator = npc.GetComponent<Animator>();
var renderer = npc.GetComponent<SpriteRenderer>();
var data = new SerializedObject(npc);
var frames = data.FindProperty("smokeFrames");
if (frames.arraySize != 8) throw new Exception("Expected 8 smoke frames");
npc.SendMessage("OnEnable");
npc.Advance(15f); animator.Update(0f);
float duration = data.FindProperty("pipeDuration").floatValue;
float handoff = duration * 7f / 8f;
npc.Advance(handoff - .01f); animator.Update(handoff - .01f);
if (!npc.IsSmoking || !renderer.sprite.name.EndsWith("06")) throw new Exception("Seventh pipe frame missing: " + renderer.sprite.name);
var existing = UnityEngine.Object.FindObjectsByType<MerchantPipeSmoke>(FindObjectsSortMode.None);
npc.Advance(.02f); animator.Update(0f);
if (npc.IsSmoking || !animator.GetCurrentAnimatorStateInfo(0).IsName(MonkeyMerchantAmbientAnimation.IdleState)) throw new Exception("Merchant did not return to idle");
var smoke = UnityEngine.Object.FindObjectsByType<MerchantPipeSmoke>(FindObjectsSortMode.None).Except(existing).Single();
if (smoke.transform.parent != null || smoke.gameObject.scene != npc.gameObject.scene) throw new Exception("Smoke must be independent in merchant scene");
var origin = smoke.transform.position;
if (smoke.GetComponent<SpriteRenderer>().sprite != frames.GetArrayElementAtIndex(0).objectReferenceValue) throw new Exception("Smoke must start at frame one");
var expected = npc.transform.TransformPoint(data.FindProperty("smokeLocalOffset").vector2Value);
if (Vector3.Distance(origin, expected) > .001f) throw new Exception("Smoke position mismatch");
var originalPosition = npc.transform.position;
try
{
    npc.transform.position += Vector3.left * 3;
    smoke.Advance(.01f);
    var drift = npc.transform.TransformVector(data.FindProperty("smokeDrift").vector2Value);
    if (Vector3.Distance(smoke.transform.position, origin + drift * .01f) > .0001f) throw new Exception("First update must move directly up-right");
    smoke.Advance(.39f);
    if (smoke.transform.position.x <= origin.x || smoke.transform.position.y <= origin.y) throw new Exception("Smoke did not drift independently up-right");
    for (int i = 0; i < 4; i++)
    {
        var previousPosition = smoke.transform.position;
        smoke.Advance(.2f);
        if (Vector3.Distance(smoke.transform.position - previousPosition, drift * .2f) > .0001f) throw new Exception("Smoke trajectory jumped during animation");
    }
    var alpha = smoke.GetComponent<SpriteRenderer>().color.a;
    if (alpha <= 0 || alpha >= 1) throw new Exception("Smoke did not fade");
    smoke.Advance(.5f);
    if (!smoke.IsFinished || smoke.gameObject.activeSelf) throw new Exception("Smoke not removed");
    npc.Advance(0f);
    return new { passed = true, frames = frames.arraySize, idleAfterFrame = 8, independentDrift = true, fadesAndRemoves = true };
}
finally { npc.transform.position = originalPosition; npc.SendMessage("OnEnable"); }
