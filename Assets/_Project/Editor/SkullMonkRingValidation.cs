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
    public static class SkullMonkRingValidation
    {
        public const string Output="Artifacts/SkullMonkRings/";
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static void Call(MON_SkullMonk boss,string method,params object[] args)=>typeof(MON_SkullMonk).GetMethod(method,Flags).Invoke(boss,args);
        public static object Probe()
        {
            var boss=UnityEngine.Object.FindFirstObjectByType<MON_SkullMonk>();
            var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab"));
            var target=player.GetComponent<PlayerEntity>();var body=player.GetComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;
            var temporary=new List<SkullMonkStaffRing>();
            try
            {
                typeof(EnemyController).GetField("playerEntity",Flags).SetValue(boss,target);
                typeof(EnemyController).GetField("player",Flags).SetValue(boss,player);
                var floor=(Collider2D)typeof(MON_SkullMonk).GetField("waveFloor",Flags).GetValue(boss);
                boss.transform.position+=Vector3.up*(floor.bounds.max.y-boss.GetComponent<Collider2D>().bounds.min.y);
                boss.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;Physics2D.SyncTransforms();
                var spin=SkullMonkRingSetup.Frames("SkullMonkRingSpin.png");var flight=SkullMonkRingSetup.Frames("SkullMonkRingFlight.png");
                var gesture=SkullMonkRingSetup.Frames("SkullMonkRingGesture.png");
                foreach(string name in new[]{"SkullMonkRingLift","SkullMonkRingHold","SkullMonkRingRecover"})
                {
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(SkullMonkRingSetup.Anim+name+".anim");
                    Require(!AnimationUtility.GetObjectReferenceCurve(clip,AnimationUtility.GetObjectReferenceCurveBindings(clip)[0]).Any(k=>k.value==gesture[3]),"Excluded fourth gesture frame was used.");
                }
                var counts=new List<int>();
                foreach(float side in new[]{1f,-1f})
                {
                    target.HP=target.maxHP;target.isInvincible=false;player.transform.position=boss.transform.position+new Vector3(side*6,0,0);Physics2D.SyncTransforms();
                    typeof(MON_SkullMonk).GetField("nextWaveAt",Flags).SetValue(boss,float.PositiveInfinity);
                    typeof(MON_SkullMonk).GetField("nextRingAt",Flags).SetValue(boss,0f);
                    typeof(MON_SkullMonk).GetField("nextAttackAt",Flags).SetValue(boss,0f);
                    Call(boss,"MonsterAbility");Require(boss.IsRingAttacking,"AI did not choose the ring pattern.");
                    var animator=boss.GetComponent<Animator>();animator.Update(0);animator.Update(1.32f);animator.Update(0);
                    var rings=(SkullMonkStaffRing[])typeof(MON_SkullMonk).GetField("heldRings",Flags).GetValue(boss);
                    Require(rings.Count(r=>r!=null)==4,"Charge transition did not replace exactly four rings.");
                    boss.SkullMonkRingChargeFinished();Require(boss.GetComponentsInChildren<SkullMonkStaffRing>().Length==4,"Duplicate charge event created rings.");
                    var start=rings.Select(r=>r.transform.position).ToArray();
                    animator.Update(.49f);Call(boss,"TickRingPattern",.66f);Call(boss,"UpdateStaffRings");
                    Require(rings.Select((r,i)=>r.transform.position.y-start[i].y).All(d=>d>.5f),"Rings did not rise overhead.");
                    animator.Update(.67f);animator.Update(0);Call(boss,"TickRingPattern",.25f);
                    Require(boss.RingsLaunched==1,"First ring launch timing failed.");counts.Add(boss.RingsLaunched);
                    for(int i=1;i<4;i++)
                    {
                        Call(boss,"TickRingPattern",.20f);Require(boss.RingsLaunched==i,"Rings fired together.");
                        Call(boss,"TickRingPattern",.36f);Require(boss.RingsLaunched==i+1,"Sequential ring launch failed.");counts.Add(boss.RingsLaunched);
                    }
                    Require(rings.All(r=>r.HasLaunched&&r.transform.parent==null),"Not all four ring instances detached for flight.");
                    Call(boss,"TickRingPattern",.31f);animator.Update(0);animator.Update(.91f);
                    Require(!boss.IsAttacking&&!boss.IsRingAttacking,"Monk did not recover after four launches.");
                    foreach(var r in rings)if(r!=null)r.Cancel();
                }
                // Tracking phase changes direction; after the cutoff even a reversing target cannot bend the shot.
                foreach(float side in new[]{1f,-1f})
                {
                    target.HP=target.maxHP;player.transform.position=new Vector3(15+side*12,2,0);Physics2D.SyncTransforms();
                    var ring=SkullMonkStaffRing.Create(boss,spin,flight,0,boss.GetComponent<SpriteRenderer>());temporary.Add(ring);
                    ring.transform.position=new Vector3(15,2,0);ring.Launch(target,.1f,.4f);
                    Vector2 original=ring.TravelDirection;
                    player.transform.position+=Vector3.up*5;Physics2D.SyncTransforms();ring.Advance(.15f);
                    Require(Vector2.Angle(original,ring.TravelDirection)>1f&&!ring.TrackingLocked,"Initial bounded tracking did not steer.");
                    ring.Advance(.25f);Require(ring.TrackingLocked,"Tracking did not lock at 0.4 seconds.");
                    Vector2 locked=ring.TravelDirection;
                    player.transform.position=new Vector3(15-side*12,-2,0);Physics2D.SyncTransforms();ring.Advance(.2f);
                    Require(Vector2.Angle(locked,ring.TravelDirection)<.01f,"Ring kept homing after the cutoff.");ring.Cancel();
                }
                target.HP=target.maxHP;target.isInvincible=false;player.transform.position=new Vector3(21,1,0);Physics2D.SyncTransforms();
                var shot=SkullMonkStaffRing.Create(boss,spin,flight,0,boss.GetComponent<SpriteRenderer>());temporary.Add(shot);
                shot.transform.position=new Vector3(15,1,0);shot.Launch(target,.1f,0);float before=target.HP;
                shot.Advance(.6f);float after=target.HP;Require(shot.HasImpacted&&after<before,"Swept ring collision did not damage player.");
                shot.Advance(.02f);Require(target.HP==after,"One ring applied damage twice.");
                var cleanup=SkullMonkStaffRing.Create(boss,spin,flight,0,boss.GetComponent<SpriteRenderer>());temporary.Add(cleanup);
                cleanup.transform.position=new Vector3(15,2,0);cleanup.Launch(target,.1f,.4f);
                boss.gameObject.SetActive(false);cleanup.Advance(.02f);Require(!cleanup.gameObject.activeSelf,"Owner disable did not cancel ring.");
                var report=new{fourthGestureFrameExcluded=true,launchCounts=counts,risesOverhead=true,exactlyFour=true,
                    earlyTrackingSeconds=.4f,locksAndFliesStraight=true,directions=2,oneHitPerRing=true,recovers=true,cancelsOnDisable=true};
                Directory.CreateDirectory(Output);File.WriteAllText(Output+"runtime.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
            }
            finally
            {
                foreach(var ring in temporary)if(ring!=null)UnityEngine.Object.DestroyImmediate(ring.gameObject);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        public static void Render()
        {
            var boss=UnityEngine.Object.FindFirstObjectByType<MON_SkullMonk>();boss.HP=boss.maxHP;
            var root=new GameObject("Ring pattern preview");root.layer=31;root.transform.localScale=new Vector3(-1,1,1);
            root.transform.position=new Vector3(15,0,0); // Keep the presentation away from the scene's left wall.
            var actor=root.AddComponent<SpriteRenderer>();actor.flipX=true;
            var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab"));
            var target=player.GetComponent<PlayerEntity>();var body=player.GetComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;
            foreach(var tr in player.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=31;
            var hitbox=player.GetComponent<Collider2D>();
            var clips=new[]{"SkullMonkRingCharge","SkullMonkRingLift","SkullMonkRingHold","SkullMonkRingRecover","SkullMonkIdle"}
                .Select(n=>AssetDatabase.LoadAssetAtPath<AnimationClip>(SkullMonkRingSetup.Anim+n+".anim")).ToArray();
            var curves=clips.Select(c=>AnimationUtility.GetObjectReferenceCurve(c,AnimationUtility.GetObjectReferenceCurveBindings(c)[0])).ToArray();
            var spin=SkullMonkRingSetup.Frames("SkullMonkRingSpin.png");var flight=SkullMonkRingSetup.Frames("SkullMonkRingFlight.png");
            var cameraObject=new GameObject("Ring pattern camera");var camera=cameraObject.AddComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=4.65f;camera.transform.position=new Vector3(16.7f,.55f,-10);camera.cullingMask=1<<31;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.065f,.09f,.12f);
            var rt=new RenderTexture(1100,650,24);camera.targetTexture=rt;var capture=new Texture2D(1100,650,TextureFormat.RGBA32,false);
            var rings=new SkullMonkStaffRing[4];int launched=0;Directory.CreateDirectory(Output+"frames");
            try
            {
                for(int i=0;i<201;i++)
                {
                    float t=i/30f;int state;float local;
                    if(t<1.32f){state=0;local=t;}else if(t<2.47f){state=1;local=t-1.32f;}
                    else if(t<4.67f){state=2;local=Mathf.Repeat(t-2.47f,1f);}else if(t<5.57f){state=3;local=t-4.67f;}
                    else{state=4;local=t-5.57f;}
                    actor.sprite=(Sprite)curves[state].Last(k=>k.time<=local+.0001f).value;
                    float jump=Mathf.Max(0,Mathf.Sin((t-2.8f)*3.4f))*1.3f;
                    player.transform.position=new Vector3(21,-1.6f+jump,0);Physics2D.SyncTransforms();
                    player.transform.position+=Vector3.up*(-2.54624f+jump-hitbox.bounds.min.y);Physics2D.SyncTransforms();target.HP=target.maxHP;
                    if(t>=1.32f&&rings[0]==null&&launched==0)
                    {
                        for(int j=0;j<4;j++){rings[j]=SkullMonkStaffRing.Create(boss,spin,flight,j,actor);rings[j].transform.SetParent(root.transform,false);}
                    }
                    for(int j=0;j<4;j++)if(rings[j]!=null)
                    {
                        if(rings[j].HasLaunched)rings[j].Advance(1f/30f);
                        else
                        {
                            float lift=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-1.8f)/.65f));
                            var p=Vector2.Lerp(MON_SkullMonk.StaffRingStart(j),MON_SkullMonk.StaffRingOverhead(j),lift);
                            p.y+=Mathf.Sin(t*3+j*1.7f)*.035f*lift;rings[j].SetHover(new Vector2(-p.x,p.y),t);
                            if(j==launched&&t+.0001f>=2.72f+j*.55f){rings[j].Launch(target,.1f,.4f);launched++;}
                        }
                        if(rings[j]!=null)foreach(var tr in rings[j].GetComponentsInChildren<Transform>())tr.gameObject.layer=31;
                    }
                    camera.Render();RenderTexture.active=rt;capture.ReadPixels(new Rect(0,0,1100,650),0,0);capture.Apply();
                    File.WriteAllBytes(Output+"frames/"+i.ToString("D3")+".png",capture.EncodeToPNG());
                    if(i==72)File.WriteAllBytes(Output+"overhead.png",capture.EncodeToPNG());
                    if(i==89)File.WriteAllBytes(Output+"launch.png",capture.EncodeToPNG());
                    if(i==38)File.WriteAllBytes(Output+"charged.png",capture.EncodeToPNG());
                }
            }
            finally
            {
                foreach(var ring in rings)if(ring!=null)UnityEngine.Object.DestroyImmediate(ring.gameObject);
                RenderTexture.active=null;camera.targetTexture=null;
                UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(player);UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(capture);
            }
        }
    }
}
