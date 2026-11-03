using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GinsengMonsterSetup
{
    private const string Art = "Assets/_Project/Art/Old Temple/Monster/";
    private const string Anim = "Assets/_Project/Animations/Old Temple/Ginseng";
    private const string Prefab = "Assets/_Project/Prefabs/Enemy/Ginseng.prefab";
    private const string Scene = "Assets/_Project/Scenes/OldTemple/SmogMountain.unity";

    [MenuItem("Eastern Fantasy/Monsters/Set Up Ginseng In SmogMountain")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var original = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(Scene);
        bool opened = !scene.isLoaded;
        if (!opened && scene.isDirty) throw new InvalidOperationException("Save SmogMountain before setup.");
        var idle = Slice("인삼 몬스터 대기", "GinsengIdle", new[] { 160, 437, 700, 973, 1238, 1506, 1773, 2043 });
        var walk = Slice("인삼 몬스터 걷기", "GinsengWalk", new[] { 153, 424, 693, 952, 1220, 1486, 1754, 2025 });
        if (!AssetDatabase.IsValidFolder(Anim)) AssetDatabase.CreateFolder("Assets/_Project/Animations/Old Temple", "Ginseng");
        var idleClip = Clip("GinsengIdle", idle, 6);
        var walkClip = WalkClip(walk);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim + "/Ginseng.controller");
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(Anim + "/Ginseng.controller");
        if (!controller.parameters.Any(p => p.name == "isMoving")) controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);
        if (!controller.parameters.Any(p => p.name == "isDead")) controller.AddParameter("isDead", AnimatorControllerParameterType.Trigger);
        var machine = controller.layers[0].stateMachine;
        var idleState = State(machine, "GinsengIdle", idleClip);
        var walkState = State(machine, "GinsengWalk", walkClip);
        ConfigureWalkSpeed(controller, walkState);
        machine.defaultState = idleState;
        Transition(idleState, walkState, AnimatorConditionMode.If, "isMoving");
        Transition(walkState, idleState, AnimatorConditionMode.IfNot, "isMoving");
        // No death sprite has been supplied; fade the idle pose instead of leaving a walking corpse.
        var defeatedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "/GinsengDefeated.anim");
        if (defeatedClip == null)
        {
            defeatedClip = new AnimationClip { name = "GinsengDefeated", frameRate = 10 };
            AnimationUtility.SetObjectReferenceCurve(defeatedClip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
                new[] { new ObjectReferenceKeyframe { time = 0, value = idle[0] } });
            defeatedClip.SetCurve("", typeof(SpriteRenderer), "m_Color.a", AnimationCurve.Linear(0, 1, 0.4f, 0));
            AssetDatabase.CreateAsset(defeatedClip, Anim + "/GinsengDefeated.anim");
        }
        var deadState = State(machine, "GinsengDefeated", defeatedClip);
        if (!machine.anyStateTransitions.Any(t => t.destinationState == deadState))
        {
            var transition = machine.AddAnyStateTransition(deadState);
            transition.hasExitTime = false;
            transition.duration = 0;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0, "isDead");
        }
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        var go = new GameObject("Ginseng");
        try
        {
            go.layer = LayerMask.NameToLayer("Enemy");
            go.tag = "Monster";
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = idle[0];
            renderer.sortingOrder = 4;
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 3;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = go.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.62f, 1.3f);
            collider.offset = new Vector2(0, 0.66f);
            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var monster = go.AddComponent<MON_Ginseng>();
            var data = new SerializedObject(monster);
            data.FindProperty("_maxHP").floatValue = 60;
            data.FindProperty("_attackPower").floatValue = 10;
            data.FindProperty("_moveSpeed").floatValue = 1.4f;
            data.FindProperty("_detectRange").floatValue = 5;
            data.FindProperty("detectionShape").enumValueIndex = 1;
            data.FindProperty("detectionHalfHeight").floatValue = 1.4f;
            data.FindProperty("patrolRange").floatValue = 2.5f;
            data.FindProperty("patrolMinWait").floatValue = 1.3f;
            data.FindProperty("patrolMaxWait").floatValue = 2.5f;
            data.FindProperty("_playerLayer").intValue = LayerMask.GetMask("Player");
            data.FindProperty("_reward_EXP").floatValue = 25;
            data.FindProperty("_rewardYeopjeon").intValue = 5;
            data.FindProperty("currencyPickupPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/WorldCurrencyPickup.prefab").GetComponent<EasternFantasy.Inventory.WorldCurrencyPickup>();
            data.FindProperty("hitFlashMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Material/HitFlashMaterial.mat");
            data.FindProperty("damageTextPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Text (TMP).prefab");
            var text = new GameObject("DamageTextAnchor");
            text.transform.SetParent(go.transform, false);
            text.transform.localPosition = new Vector3(0, 2.2f);
            data.FindProperty("textSpawnPoint").objectReferenceValue = text.transform;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(go, Prefab);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
        if (System.IO.File.Exists(Art + "인삼 몬스터 공격.png")) GinsengAttackSetup.Apply();
        if (System.IO.File.Exists(Art + "인삼 몬스터 사망.png")) GinsengDeathSetup.Apply();
        if (opened) scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Additive);
        try
        {
            if (!scene.GetRootGameObjects().Any(r => r.GetComponentInChildren<MON_Ginseng>(true) != null))
            {
                var monster = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), scene);
                float ground = scene.GetRootGameObjects().First(r => r.name == "Floor").GetComponent<Collider2D>().bounds.max.y;
                monster.transform.position = new Vector3(0.8f, ground + 0.005f);
                EditorSceneManager.MarkSceneDirty(scene);
            }
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static Sprite[] Slice(string filename, string prefix, int[] centers)
    {
        string path = Art + filename + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var existing = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
        var rects = new SpriteRect[8];
        var pairs = new SpriteNameFileIdPair[8];
        var texture = new Texture2D(2, 2);
        try
        {
            texture.LoadImage(System.IO.File.ReadAllBytes(path));
            var pixels = texture.GetPixels32();
            for (int i = 0; i < 8; i++)
            {
                string name = prefix + "_" + i.ToString("00");
                var id = existing.TryGetValue(name, out var old) ? old : GUID.Generate();
                int left = centers[i] - 128;
                // Keep the beige torso centered despite the leaves changing silhouette.
                double sum = 0, weight = 0;
                for (int y = 300; y < 410; y++)
                for (int x = left; x < left + 256; x++)
                {
                    var color = pixels[y * texture.width + x];
                    if (color.a < 180 || color.r < 145 || color.r < color.g * 1.03f || color.g < 100) continue;
                    sum += x; weight++;
                }
                float anchor = weight > 0 ? (float)(sum / weight) : centers[i];
                rects[i] = new SpriteRect { name = name, rect = new Rect(left, 186, 256, 424), spriteID = id,
                    alignment = SpriteAlignment.Custom, pivot = new Vector2((anchor - left) / 256f, 4f / 424f) };
                pairs[i] = new SpriteNameFileIdPair(name, id);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 200;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
        provider.Apply(); importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
    }

    private static AnimationClip Clip(string name, Sprite[] frames, int fps)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "/" + name + ".anim");
        bool create = clip == null;
        if (create) clip = new AnimationClip { name = name };
        clip.frameRate = fps;
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < keys.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / (float)fps, value = frames[i % frames.Length] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        if (create) AssetDatabase.CreateAsset(clip, Anim + "/" + name + ".anim");
        else { EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip); }
        return clip;
    }

    private static AnimationClip WalkClip(Sprite[] frames)
    {
        var clip = Clip("GinsengWalk", frames, 50);
        // Two planted poses (0/4), with shorter passing poses (2/6).
        float[] holds = { 0.14f, 0.10f, 0.06f, 0.10f, 0.14f, 0.10f, 0.06f, 0.10f };
        var keys = new ObjectReferenceKeyframe[9];
        float time = 0;
        for (int i = 0; i < 8; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = Mathf.Round(time * 50f) / 50f, value = frames[i] };
            time += holds[i];
        }
        // Sprite curves include the final sample's duration. Extend the last pose
        // through 0.8s without adding a duplicate first-frame hold at the seam.
        keys[8] = new ObjectReferenceKeyframe { time = 0.78f, value = frames[7] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var loopSettings = AnimationUtility.GetAnimationClipSettings(clip);
        loopSettings.startTime = 0;
        loopSettings.stopTime = 0.8f;
        loopSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, loopSettings);
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssetIfDirty(clip);
        return clip;
    }

    private static void ConfigureWalkSpeed(AnimatorController controller, AnimatorState walkState)
    {
        if (!controller.parameters.Any(p => p.name == "walkCycleSpeed"))
            controller.AddParameter(new AnimatorControllerParameter { name = "walkCycleSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
        walkState.speedParameter = "walkCycleSpeed";
        walkState.speedParameterActive = true;
    }

    [MenuItem("Eastern Fantasy/Monsters/Refresh Ginseng Walking Rhythm")]
    public static void RefreshWalking()
    {
        var frames = AssetDatabase.LoadAllAssetsAtPath(Art + "인삼 몬스터 걷기.png").OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (frames.Length != 8) throw new InvalidOperationException("Expected eight walking sprites.");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim + "/Ginseng.controller");
        var walk = controller.layers[0].stateMachine.states.First(s => s.state.name == "GinsengWalk").state;
        walk.motion = WalkClip(frames);
        ConfigureWalkSpeed(controller, walk);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
    }

    private static AnimatorState State(AnimatorStateMachine machine, string name, Motion motion)
    {
        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name) ?? machine.AddState(name);
        state.motion = motion;
        return state;
    }

    private static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, string parameter)
    {
        if (from.transitions.Any(t => t.destinationState == to)) return;
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0;
        transition.AddCondition(mode, 0, parameter);
    }
}
