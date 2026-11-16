using System;
using System.IO;
using System.Linq;
using EasternFantasy.Dialogue;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class SkullMonkIntroSetup
    {
        const string Art = SkullMonkWaveSetup.Art;
        const string Anim = SkullMonkWaveSetup.Anim;
        const string Data = "Assets/_Project/Data/SkullMonkIntro/";
        [MenuItem("Eastern Fantasy/Skull Monk/Configure Meditation Intro")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            Directory.CreateDirectory(Data); AssetDatabase.Refresh();
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "SkullMonkIdle.anim");
            var reference = (Sprite)AnimationUtility.GetObjectReferenceCurve(idle, binding)[0].value;
            float ppu = 416f / reference.bounds.size.y;
            var cells = Enumerable.Range(0,8).Select(i => new Rect(Mathf.Round(i*271.5f), 0,
                Mathf.Round((i+1)*271.5f)-Mathf.Round(i*271.5f),724)).ToArray();
            Copy("해골 승려 명상.png", "SkullMonkMeditate.png");
            Copy("해골 승려 기상.png", "SkullMonkRise.png");
            float[] meditateFloor = {232,232,232,232,232,231,231,231};
            var seatedCells=cells.Select(r=>new Rect(r.x,220,r.width,280)).ToArray();
            SkullMonkMeleeSetup.Slice(Art+"SkullMonkMeditate.png",seatedCells,ppu,
                seatedCells.Select((r,i)=>new Vector2(135f/r.width,(meditateFloor[i]-r.y-reference.bounds.min.y*ppu)/r.height)).ToArray(),true);
            // The seated cloth and staff have many small contours. A rectangular mesh
            // preserves all of them without silhouette triangulation cutting the body.
            var seatedImporter=(TextureImporter)AssetImporter.GetAtPath(Art+"SkullMonkMeditate.png");
            var seatedSettings=new TextureImporterSettings();seatedImporter.ReadTextureSettings(seatedSettings);
            seatedSettings.spriteMeshType=SpriteMeshType.FullRect;seatedImporter.SetTextureSettings(seatedSettings);seatedImporter.SaveAndReimport();
            SkullMonkMeleeSetup.Slice(Art+"SkullMonkRise.png",cells,ppu,
                cells.Select(r=>new Vector2(135f/r.width,(98-reference.bounds.min.y*ppu)/724f)).ToArray(),true);
            var meditation = Clip("SkullMonkMeditate",SkullMonkWaveSetup.Frames("SkullMonkMeditate.png"),
                new[]{0,1,2,3,4,5,6,7,6,5,4,3,2,1},
                new[]{0f,.23f,.46f,.69f,.92f,1.15f,1.38f,1.61f,1.84f,2.07f,2.30f,2.53f,2.76f,2.99f},3.22f,true);
            var rise = Clip("SkullMonkRise",SkullMonkWaveSetup.Frames("SkullMonkRise.png"),
                new[]{0,1,2,3,4,5,6,7}, new[]{0f,.24f,.46f,.58f,.76f,1.0f,1.25f,1.47f},1.8f,false);
            var keys=AnimationUtility.GetObjectReferenceCurve(rise,binding).ToList();
            keys.Add(new ObjectReferenceKeyframe{time=1.67f,value=reference});
            AnimationUtility.SetObjectReferenceCurve(rise,binding,keys.ToArray());
            AnimationUtility.SetAnimationEvents(rise,new[]{new AnimationEvent{time=1.79f,functionName="SkullMonkAwakeningFinished"}});
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim+"SkullMonk.controller");
            var machine=controller.layers[0].stateMachine;
            foreach(var clip in new[]{meditation,rise})
            {
                var state=machine.states.FirstOrDefault(s=>s.state.name==clip.name).state ?? machine.AddState(clip.name);
                state.motion=clip; EditorUtility.SetDirty(state);
            }

            Copy("해진 삿갓을 쓴 해골 승려 무사.png","SkullMonkDialoguePortrait.png");
            var raw=new Texture2D(2,2); ImageConversion.LoadImage(raw,File.ReadAllBytes(Art+"SkullMonkDialoguePortrait.png"));
            int h=raw.height; UnityEngine.Object.DestroyImmediate(raw);
            SkullMonkMeleeSetup.Slice(Art+"SkullMonkDialoguePortrait.png",new[]{new Rect(230,h-830,800,800)},100,
                new[]{new Vector2(.5f,.5f)});
            var character=LoadOrCreate<DialogueCharacter>("SkullMonkCharacter");
            var ch=new SerializedObject(character); ch.FindProperty("displayName").stringValue="해골 승려";
            ch.FindProperty("portrait").objectReferenceValue=SkullMonkWaveSetup.Frames("SkullMonkDialoguePortrait.png")[0];ch.ApplyModifiedPropertiesWithoutUndo();
            var sequence=LoadOrCreate<DialogueSequence>("SkullMonkIntro"); var seq=new SerializedObject(sequence);
            seq.FindProperty("playerCharacter").objectReferenceValue=AssetDatabase.LoadAssetAtPath<DialogueCharacter>("Assets/_Project/Data/Tutorial_DIalogue/DialoguePlayer.asset");
            seq.FindProperty("partnerCharacter").objectReferenceValue=character;
            string[] lines={"이 절의 기운… 당신에게서 흘러나오는 건가?","오랜 침묵을 깨우는 발소리로구나. 이곳에는 어찌 왔느냐.","사람들을 해치는 원인을 찾으러 왔다. 길을 열어라.","아직도 이 문 너머를 원하느냐. 그렇다면 네 뜻을 증명해 보아라."};
            var array=seq.FindProperty("lines");array.arraySize=lines.Length;
            for(int i=0;i<lines.Length;i++){var line=array.GetArrayElementAtIndex(i);line.FindPropertyRelative("speaker").enumValueIndex=i%2;line.FindPropertyRelative("text").stringValue=lines[i];}
            seq.ApplyModifiedPropertiesWithoutUndo();

            const string tutorial="Assets/_Project/Scenes/TutorialMap/MovementPrototype.unity";
            var template=SceneManager.GetSceneByPath(tutorial);bool closeTemplate=!template.isLoaded;
            if(closeTemplate)template=EditorSceneManager.OpenScene(tutorial,OpenSceneMode.Additive);
            var source=template.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DialogueManager>(true)).Single();
            var ui=UnityEngine.Object.Instantiate(source.transform.root.gameObject);ui.name="SkullMonkDialogueUI";
            UnityEngine.Object.DestroyImmediate(ui.GetComponent<PersistentGameplayUI>());
            UnityEngine.Object.DestroyImmediate(ui.transform.Find("Canvas").gameObject);
            var prefab=PrefabUtility.SaveAsPrefabAsset(ui,"Assets/_Project/Prefabs/UI/SkullMonkDialogueUI.prefab");
            UnityEngine.Object.DestroyImmediate(ui);
            if(closeTemplate)EditorSceneManager.CloseScene(template,true);

            var scene=SceneManager.GetSceneByPath(SkullMonkWaveSetup.ScenePath);
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(SkullMonkWaveSetup.ScenePath,OpenSceneMode.Additive);
            var boss=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MON_SkullMonk>(true)).Single();
            var data=new SerializedObject(boss);data.FindProperty("requireIntroDialogue").boolValue=true;
            data.FindProperty("introDialogue").objectReferenceValue=sequence;
            data.FindProperty("introDialogueUiPrefab").objectReferenceValue=prefab;data.ApplyModifiedPropertiesWithoutUndo();
            boss.GetComponent<SpriteRenderer>().sprite=SkullMonkWaveSetup.Frames("SkullMonkMeditate.png")[0];
            var trigger=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Skull Monk Intro Trigger");
            if(trigger==null){trigger=new GameObject("Skull Monk Intro Trigger");SceneManager.MoveGameObjectToScene(trigger,scene);}
            trigger.layer=LayerMask.NameToLayer("Ignore Raycast");
            trigger.transform.position=boss.transform.position+new Vector3(-5f,-.2f,0);
            var box=trigger.GetComponent<BoxCollider2D>();if(box==null)box=trigger.AddComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(3.5f,6f);
            var relay=trigger.GetComponent<SkullMonkIntroTrigger>();if(relay==null)relay=trigger.AddComponent<SkullMonkIntroTrigger>();
            var relayData=new SerializedObject(relay);relayData.FindProperty("boss").objectReferenceValue=boss;relayData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);EditorUtility.SetDirty(machine);AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        static T LoadOrCreate<T>(string name) where T:ScriptableObject
        {var a=AssetDatabase.LoadAssetAtPath<T>(Data+name+".asset");if(a==null){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,Data+name+".asset");}return a;}
        static void Copy(string from,string to){if(!File.Exists(Art+to))File.Copy(Art+from,Art+to);AssetDatabase.ImportAsset(Art+to);}
        static AnimationClip Clip(string name,Sprite[] frames,int[] order,float[] times,float duration,bool loop)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim+name+".anim");
            if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,Anim+name+".anim");}clip.name=name;clip.frameRate=60;
            AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),
                order.Select((n,i)=>new ObjectReferenceKeyframe{time=times[i],value=frames[n]}).ToArray());
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.startTime=0;settings.stopTime=duration;settings.loopTime=loop;
            AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);return clip;
        }
    }
}
