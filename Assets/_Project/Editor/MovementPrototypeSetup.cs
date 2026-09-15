using System.IO;
using EasternFantasy.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace EasternFantasy.Editor
{
    public static class MovementPrototypeSetup
    {
        public const string ScenePath = "Assets/_Project/Scenes/MovementPrototype.unity";

        [MenuItem("Eastern Fantasy/Create Movement Prototype")]
        public static void Create()
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
            if (File.Exists(ScenePath)) throw new System.InvalidOperationException("Prototype already exists; open its scene instead.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Save the current scene first.");

            foreach (string folder in new[] { "Scenes", "Data/Player", "Prefabs/Player", "Art/Prototype" })
                Directory.CreateDirectory("Assets/_Project/" + folder);
            AssetDatabase.Refresh();

            var texture = new Texture2D(8, 8);
            var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            const string spritePath = "Assets/_Project/Art/Prototype/WhiteSquare.png";
            File.WriteAllBytes(spritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(spritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 8;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            var settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/_Project/Data/Player/DefaultMovement.asset");
            var material = new PhysicsMaterial2D("Player Frictionless") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, "Assets/_Project/Data/Player/PlayerFrictionless.physicsMaterial2D");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.14f, 0.19f);
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            var directional = new GameObject("Main Directional Light").AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 0f; // Sprite lighting is supplied by Global Light 2D.

            var terrain = new GameObject("Test Terrain");
            Surface("Ground", new Vector2(0f, -2f), new Vector2(21f, 1f), sprite, terrain.transform);
            Surface("Step 1", new Vector2(0f, -0.9f), new Vector2(2.5f, 0.35f), sprite, terrain.transform);
            Surface("Step 2", new Vector2(3.4f, 0.3f), new Vector2(2.5f, 0.35f), sprite, terrain.transform);
            Surface("Step 3", new Vector2(6.8f, 1.5f), new Vector2(2.5f, 0.35f), sprite, terrain.transform);
            Surface("Left Boundary", new Vector2(-10.7f, 1f), new Vector2(0.4f, 8f), sprite, terrain.transform);
            Surface("Right Boundary", new Vector2(10.7f, 1f), new Vector2(0.4f, 8f), sprite, terrain.transform);

            var player = new GameObject("Player");
            player.transform.position = new Vector3(-6f, -0.6f, 0f);
            var body = player.AddComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.gravityScale = settings.GravityScale;
            var collider = player.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.7f, 1.6f);
            collider.sharedMaterial = material;
            var movement = player.AddComponent<PlayerMovement2D>();
            var movementFields = new SerializedObject(movement);
            movementFields.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>("Assets/_Project/Data/Player/DefaultMovement.asset");
            movementFields.FindProperty("groundLayers").intValue = 1; // Default: only terrain has colliders here.
            movementFields.ApplyModifiedPropertiesWithoutUndo();
            var input = player.AddComponent<PlayerInputReader>();
            var inputFields = new SerializedObject(input);
            inputFields.FindProperty("inputActions").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
            inputFields.ApplyModifiedPropertiesWithoutUndo();
            Visual("Robe", new Vector2(0f, -0.1f), new Vector2(0.65f, 1f), new Color(0.2f, 0.73f, 0.66f), sprite, player.transform, 2);
            Visual("Head", new Vector2(0f, 0.55f), new Vector2(0.46f, 0.46f), new Color(0.96f, 0.8f, 0.59f), sprite, player.transform, 3);
            Visual("Hat", new Vector2(0f, 0.82f), new Vector2(0.75f, 0.12f), new Color(0.11f, 0.2f, 0.24f), sprite, player.transform, 4);
            Visual("Belt", new Vector2(0f, -0.12f), new Vector2(0.7f, 0.12f), new Color(0.96f, 0.77f, 0.38f), sprite, player.transform, 4);
            Visual("Left Foot", new Vector2(-0.19f, -0.7f), new Vector2(0.24f, 0.2f), Color.gray, sprite, player.transform, 3);
            Visual("Right Foot", new Vector2(0.19f, -0.7f), new Vector2(0.24f, 0.2f), Color.gray, sprite, player.transform, 3);
            PrefabUtility.SaveAsPrefabAssetAndConnect(player, "Assets/_Project/Prefabs/Player/Player.prefab", InteractionMode.AutomatedAction);
            var title = new GameObject("Controls").AddComponent<TextMesh>();
            title.transform.position = new Vector3(-9.5f, 5.5f, 0f);
            title.text = "MOVEMENT PROTOTYPE\nA / D  or  Arrow Keys     |     SPACE : Jump";
            title.fontSize = 48;
            title.characterSize = 0.065f;
            title.color = new Color(0.91f, 0.88f, 0.75f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            Debug.Log("Movement prototype created and saved: " + ScenePath);
        }

        private static void Surface(string name, Vector2 position, Vector2 size, Sprite sprite, Transform parent)
        {
            var item = Visual(name, position, size, new Color(0.35f, 0.43f, 0.4f), sprite, parent, 0);
            item.AddComponent<BoxCollider2D>();
        }

        private static GameObject Visual(string name, Vector2 position, Vector2 size, Color color, Sprite sprite, Transform parent, int order)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return item;
        }
    }
}
