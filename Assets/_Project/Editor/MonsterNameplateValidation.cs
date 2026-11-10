using System;
using System.IO;
using System.Reflection;
using EasternFantasy.Player;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class MonsterNameplateValidation
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        static string report;
        static int checks;
        static void Check(bool value,string message)
        {
            report+=(value?"PASS ":"FAIL ")+message+"\n";
            if(!value)throw new Exception(message);
            checks++;
        }
        static void SetEnemy(EnemyController enemy,string name,object value) => typeof(EnemyController).GetField(name,Hidden).SetValue(enemy,value);
        static void Tick(MonsterNameplateUI ui)
        {
            Physics2D.SyncTransforms();
            typeof(MonsterNameplateUI).GetMethod("LateUpdate",Hidden).Invoke(ui,null);
        }
        static MonsterNameplateView View(MonsterNameplateUI ui) => (MonsterNameplateView)typeof(MonsterNameplateUI).GetField("view",Hidden).GetValue(ui);

        public static string Run()
        {
            if(!Application.isPlaying)throw new Exception("Run in Play Mode.");
            report="Native Unity monster nameplate validation\n";checks=0;
            var holder=new GameObject("Nameplate test fixtures");holder.SetActive(false);
            GameObject monster=null,cameraObject=null;
            try
            {
                // Inactive player probe supplies health/position without touching the live player or saves.
                var probe=new GameObject("Nameplate player probe");probe.SetActive(false);probe.transform.SetParent(holder.transform);
                var player=probe.AddComponent<PlayerEntity>();player.HP=player.maxHP;
                var camera=Camera.main;
                if(camera==null){cameraObject=new GameObject("Nameplate camera",typeof(Camera));cameraObject.tag="MainCamera";camera=cameraObject.GetComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;}
                monster=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/Ginseng.prefab"));
                var enemy=monster.GetComponent<EnemyController>();
                monster.transform.position=camera.ViewportToWorldPoint(new Vector3(.5f,.65f,10));
                enemy.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
                SetEnemy(enemy,"playerEntity",player);
                var ui=monster.GetComponent<MonsterNameplateUI>();
                Check(ui!=null,"Spawned monster automatically receives nameplate behaviour");
                player.transform.position=enemy.transform.position+Vector3.right*10;
                Check(!enemy.ShouldShowNameplate,"Distant unhit monster is hidden");Tick(ui);
                Check(View(ui)==null,"Distant monster does not allocate a UI panel");
                player.transform.position=enemy.transform.position+Vector3.right*3.9f;
                Check(enemy.ShouldShowNameplate,"Approaching within four units reveals name");Tick(ui);
                var view=View(ui);Check(view!=null && view.gameObject.activeSelf,"Nearby monster creates and displays its UI");
                var text=view.GetComponentInChildren<TMP_Text>();
                Check(text.text=="인삼 요괴","Ginseng displays Korean monster name");
                Check(text.font!=null && text.fontSize==18 && text.color==Color.white,"Readable Korean font with white 18px text");
                var background=view.GetComponent<Image>();
                Check(background.color==new Color(0,0,0,.65f),"Rectangular black background has 65 percent opacity");
                Check(!background.raycastTarget && !text.raycastTarget && view.GetComponentInParent<GraphicRaycaster>()==null,"Nameplates cannot intercept mouse input");
                Check(((RectTransform)view.transform).rect.width>=text.GetPreferredValues(text.text).x+23.9f,"Background fits name plus horizontal padding");
                var before=((RectTransform)view.transform).anchoredPosition;
                var scale=monster.transform.localScale;scale.x=-scale.x;monster.transform.localScale=scale;Tick(ui);
                Check(view.transform.lossyScale.x>0 && view.transform.rotation==Quaternion.identity,"Facing flips do not mirror or rotate text");
                var expected=camera.WorldToScreenPoint(enemy.NameplatePosition);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)view.transform.parent,expected,null,out var anchor);
                Check(Vector2.Distance(((RectTransform)view.transform).anchoredPosition,anchor)<.01f && ((RectTransform)view.transform).pivot.y==1,"Top of nameplate follows position below monster feet");
                monster.transform.position+=Vector3.right*.5f;Tick(ui);
                Check(Vector2.Distance(before,((RectTransform)view.transform).anchoredPosition)>1,"Nameplate follows moving monster");
                player.transform.position=enemy.transform.position+Vector3.right*10;
                Tick(ui);Check(!view.gameObject.activeSelf,"Leaving proximity hides name");
                enemy.TakeDamage(0,false,false);Check(!enemy.ShouldShowNameplate,"Zero damage does not reveal name");
                float health=enemy.HP;enemy.TakeDamage(1,false,false);
                Check(enemy.HP==health-1 && enemy.ShouldShowNameplate,"Actual distant hit reveals name without changing damage behaviour");Tick(ui);
                Check(view.gameObject.activeSelf && View(ui)==view,"Hit reuses the same UI panel");
                float until=(float)typeof(EnemyController).GetField("nameplateHitUntil",Hidden).GetValue(enemy);
                Check(Mathf.Abs(until-Time.time-5)<.01f,"Hit keeps name visible for five seconds");
                SetEnemy(enemy,"nameplateHitUntil",Time.time-1);Tick(ui);
                Check(!view.gameObject.activeSelf,"Name hides when distant hit timer expires");
                player.transform.position=enemy.transform.position;
                player.HP=0;Check(!enemy.ShouldShowNameplate,"Dead player does not reveal nearby names");
                player.HP=player.maxHP;enemy.HP=0;Tick(ui);
                Check(!view.gameObject.activeSelf,"Defeated monster name is hidden immediately");
                enemy.HP=enemy.maxHP;Tick(ui);monster.SetActive(false);
                Check(!view.gameObject.activeSelf,"Disabling monster immediately hides detached UI");
                report+="COMPLETE "+checks+" checks passed\n";
                return report;
            }
            finally
            {
                if(monster!=null)UnityEngine.Object.DestroyImmediate(monster);
                UnityEngine.Object.DestroyImmediate(holder);
                if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);
                Directory.CreateDirectory("Artifacts/MonsterNameplate");File.WriteAllText("Artifacts/MonsterNameplate/Validation.txt",report);
            }
        }
    }
}
