using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Waterline.Core;

namespace Waterline.Editor
{
    // Builds a real editable scene once. Reopening the project never overwrites it.
    [InitializeOnLoad]
    public static class PrototypeBuilder
    {
        public const string ScenePath = "Assets/Waterline/Scenes/Day01_Dock.unity";
        private const string Generated = "Assets/Waterline/Generated";
        private const string Settings = "Assets/Waterline/Settings";
        private static Transform root;
        private static Material concrete, dark, steel, rust, teal, yellow, waterMaterial, rubber;

        static PrototypeBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists(ScenePath) && !EditorSceneManager.GetActiveScene().isDirty) Build();
            };
        }

        [MenuItem("Waterline/01 Open prototype")]
        public static void Open()
        {
            if (!File.Exists(ScenePath)) { Build(); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Waterline/02 Build first scene")]
        public static void Build()
        {
            if (File.Exists(ScenePath))
            {
                if (Application.isBatchMode) throw new InvalidOperationException("Scene already exists; use validation/build commands, not scene generation.");
                EditorUtility.DisplayDialog("场景已存在", "为保留你的编辑，生成器不会覆盖已有场景。请选择 Open prototype。", "知道了");
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Generated);
            Directory.CreateDirectory(Settings);
            Directory.CreateDirectory("Assets/Waterline/Scenes");
            AssetDatabase.Refresh();
            ConfigureProject();
            CreateMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            root = new GameObject("WATERLINE - Day 01").transform;
            var session = new GameObject("Dock Session").AddComponent<DockSession>();
            session.transform.SetParent(root);
            session.tuning = AssetDatabase.LoadAssetAtPath<DockTuning>(Settings + "/DockTuning.asset");
            if (session.tuning == null)
            {
                session.tuning = ScriptableObject.CreateInstance<DockTuning>();
                AssetDatabase.CreateAsset(session.tuning, Settings + "/DockTuning.asset");
            }

            // Upper level: two rooms, a south path, and a bridge across the dock.
            Room("WEST / CONTROL", -8, concrete);
            Room("EAST / WINCH", 8, concrete);
            Cube("South perimeter", new Vector3(0, -0.2f, -6), new Vector3(22, 0.4f, 3), concrete);
            Cube("South rubber strip", new Vector3(0, 0.015f, -6), new Vector3(21, 0.025f, 1.3f), rubber, false);
            Rail(new Vector3(0, 0.65f, -7.4f), new Vector3(22, 1.3f, 0.15f));
            Rail(new Vector3(0, 0.65f, -4.45f), new Vector3(10, 1.3f, 0.15f));
            Rail(new Vector3(-10.9f, 0.65f, -6), new Vector3(0.15f, 1.3f, 3));
            Rail(new Vector3(10.9f, 0.65f, -6), new Vector3(0.15f, 1.3f, 3));
            Cube("Dry dock floor", new Vector3(0, -3.7f, 0.5f), new Vector3(10, 0.4f, 10), dark);
            Cube("North seawall", new Vector3(0, -0.4f, 5.6f), new Vector3(22, 6.6f, 0.5f), dark);
            for (int i = 0; i < 6; i++)
            {
                Cube("Old tide mark " + i, new Vector3(-4.98f, -3f + i * 0.5f, 0), new Vector3(0.02f, 0.07f, 8.7f), yellow, false);
                Cube("Old tide mark east " + i, new Vector3(4.98f, -3f + i * 0.5f, 0), new Vector3(0.02f, 0.07f, 8.7f), yellow, false);
            }
            // Permanent safety boundaries keep the first exercise on the upper level.
            for (int side = -1; side <= 1; side += 2)
            {
                Rail(new Vector3(side * 5f, 0.65f, -1.1f), new Vector3(0.14f, 1.3f, 6.5f));
                // Bridge openings have temporary invisible barriers until deployment.
            }

            var bridge = Cube("Retractable inspection bridge", new Vector3(0, -0.12f, 3.5f), new Vector3(10, 0.24f, 2), steel);
            session.bridge = bridge.transform;
            var bridgeRailA = Cube("Bridge north handrail", new Vector3(0, 0.8f, 4.43f), new Vector3(10, 0.1f, 0.1f), yellow);
            var bridgeRailB = Cube("Bridge south handrail", new Vector3(0, 0.8f, 2.57f), new Vector3(10, 0.1f, 0.1f), yellow);
            // Their collision remains useful even while the deck is retracted.
            var guards = new GameObject("Bridge safety gates").AddComponent<BridgeSafety>();
            guards.transform.SetParent(root); guards.session = session;
            guards.west = Cube("West bridge barrier", new Vector3(-5, 1f, 3.5f), new Vector3(0.15f, 2, 2), yellow);
            guards.east = Cube("East bridge barrier", new Vector3(5, 1f, 3.5f), new Vector3(0.15f, 2, 2), yellow);

            session.water = Cube("Water surface", new Vector3(0, -3.3f, 0.5f), new Vector3(9.85f, 0.06f, 9.75f), waterMaterial, false).transform;
            var boat = new GameObject("Inspection boat"); boat.transform.SetParent(root);
            boat.transform.position = new Vector3(0, -2.5f, 0);
            var hull = Cube("Hull", Vector3.zero, new Vector3(3.0f, 0.8f, 5.5f), rust);
            hull.transform.SetParent(boat.transform, false); hull.transform.localPosition = new Vector3(0, -0.4f, 0);
            var deck = Cube("Deck", Vector3.zero, new Vector3(2.7f, 0.12f, 5.1f), steel);
            deck.transform.SetParent(boat.transform, false); deck.transform.localPosition = Vector3.zero;
            var cabin = Cube("Cabin", Vector3.zero, new Vector3(1.8f, 1.2f, 1.6f), teal);
            cabin.transform.SetParent(boat.transform, false); cabin.transform.localPosition = new Vector3(0, 0.6f, -0.6f);
            var glass = Cube("Cabin window", Vector3.zero, new Vector3(1.5f, 0.5f, 0.03f), dark, false);
            glass.transform.SetParent(boat.transform, false); glass.transform.localPosition = new Vector3(0, 0.8f, 0.215f);
            session.boat = boat.transform;

            // Closed south gate makes the first condition physically meaningful.
            session.serviceDoor = Cube("Service shutter - opens after pump repair", new Vector3(4f, 1.35f, -6), new Vector3(0.22f, 2.7f, 2.9f), rust).transform;
            Cube("Service shutter lintel", new Vector3(4, 3.0f, -6), new Vector3(0.5f, 0.5f, 3.2f), dark);
            Sign("PUMP INTERLOCK", new Vector3(3.82f, 1.8f, -6), 90, 0.065f);

            Cube("Workbench", new Vector3(-9, 0.45f, 0), new Vector3(2.4f, 0.9f, 1.2f), dark);
            Interactable(session, "联轴销", DockAction.TakePin, new Vector3(-9, 1.12f, 0), new Vector3(0.45f, 0.35f, 0.45f), yellow,
                "泵的缺失零件。取走后安装到右侧控制台。", true);
            Pedestal(session, "安装联轴销", DockAction.InstallPin, new Vector3(-6.5f, 0, 0.8f), "修复泵，同时解除南侧检修门的供电联锁。", teal);
            Pedestal(session, "启动注水", DockAction.StartFlood, new Vector3(-6.5f, 0, 2.3f), "注水会让检修艇浮起。长按确认启动。", yellow, true);
            Pedestal(session, "解除坞门锁", DockAction.UnlockGate, new Vector3(8.5f, 0, 1.2f), "先解除机械锁，待水位平衡后即可出坞。", rust);
            Pedestal(session, "展开检修桥", DockAction.DeployBridge, new Vector3(7.0f, 0, 2.4f), "打开跨坞近路，返回西侧启动注水。", teal);
            Pedestal(session, "登艇撤离", DockAction.BoardBoat, new Vector3(5.65f, 0, -1f), "水位到达浮航线后登艇。", yellow);

            Sign("01  CONTROL", new Vector3(-8, 2.5f, 4.72f), 0, 0.07f);
            Sign("02  WINCH", new Vector3(8, 2.5f, 4.72f), 0, 0.07f);
            Sign("WATERLINE / 07", new Vector3(0, 2.1f, 5.25f), 0, 0.055f);
            Sign("SOUTH WALKWAY", new Vector3(-2.5f, 0.055f, -6), 0, 0.06f, true);

            var player = new GameObject("Player"); player.transform.SetParent(root);
            player.transform.position = new Vector3(-8, 0.08f, -3.3f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f; controller.radius = 0.3f; controller.center = new Vector3(0, 0.9f, 0);
            controller.stepOffset = 0.3f; controller.skinWidth = 0.03f;
            var cameraObject = new GameObject("Player Camera"); cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 1.62f, 0);
            var camera = cameraObject.AddComponent<Camera>(); camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 150; camera.backgroundColor = new Color(0.1f, 0.16f, 0.2f);
            camera.clearFlags = CameraClearFlags.SolidColor; cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
            var input = player.AddComponent<FirstPersonController>(); input.session = session; input.view = camera;
            var hud = session.gameObject.AddComponent<DockHud>(); hud.session = session; hud.player = input;
            Lighting();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Selection.activeObject = session.tuning;
            Debug.Log("[Waterline] Scene ready. Press Play. Settings: " + Settings + "/DockTuning.asset");
        }

        private static void ConfigureProject()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Settings + "/DockRenderer.asset");
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, Settings + "/DockRenderer.asset");
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Settings + "/DockURP.asset");
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "Dock URP";
                pipeline.shadowDistance = 50; pipeline.msaaSampleCount = 4;
                AssetDatabase.CreateAsset(pipeline, Settings + "/DockURP.asset");
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            PlayerSettings.productName = "Waterline";
            PlayerSettings.companyName = "Waterline Portfolio";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var inputMode = settings.FindProperty("activeInputHandler");
            if (inputMode != null) { inputMode.intValue = 1; settings.ApplyModifiedPropertiesWithoutUndo(); }
        }

        private static Material Mat(string name, Color color, float metallic = 0, float smoothness = 0.3f)
        {
            string path = Generated + "/" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result != null) return result;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP shader is not ready. Wait for package import and retry.");
            result = new Material(shader) { name = name };
            result.SetColor("_BaseColor", color); result.SetFloat("_Metallic", metallic); result.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(result, path); return result;
        }

        private static void CreateMaterials()
        {
            concrete = Mat("Concrete", new Color(0.25f, 0.31f, 0.33f));
            dark = Mat("Dark steel", new Color(0.045f, 0.075f, 0.095f), 0.5f);
            steel = Mat("Deck steel", new Color(0.22f, 0.36f, 0.39f), 0.65f);
            rust = Mat("Rust red", new Color(0.5f, 0.16f, 0.075f), 0.45f);
            teal = Mat("Signal teal", new Color(0.07f, 0.55f, 0.45f), 0.3f);
            yellow = Mat("Safety amber", new Color(0.92f, 0.58f, 0.1f), 0.25f);
            waterMaterial = Mat("Harbor water", new Color(0.025f, 0.24f, 0.29f), 0.5f, 0.9f);
            rubber = Mat("Rubber", new Color(0.04f, 0.055f, 0.06f), 0, 0.1f);
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 size, Material material, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(root); go.transform.position = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Room(string name, float x, Material material)
        {
            Cube(name + " floor", new Vector3(x, -0.2f, 0), new Vector3(6, 0.4f, 10), material);
            Cube(name + " outer wall", new Vector3(x + Mathf.Sign(x) * 3, 1.7f, 0), new Vector3(0.3f, 3.4f, 10), material);
            Cube(name + " north wall", new Vector3(x, 1.7f, 5), new Vector3(6, 3.4f, 0.3f), material);
            Cube(name + " canopy", new Vector3(x, 3.5f, 0), new Vector3(6, 0.2f, 10), dark);
            for (int i = 0; i < 3; i++)
                Cube(name + " roof beam " + i, new Vector3(x, 3.3f, -3 + i * 3), new Vector3(6, 0.22f, 0.2f), yellow);
        }

        private static void Rail(Vector3 position, Vector3 size)
        {
            // Solid low barriers avoid falling; top rail reads as a safety boundary.
            Cube("Safety curb", new Vector3(position.x, 0.2f, position.z), new Vector3(size.x, 0.4f, size.z), dark);
            Cube("Handrail", new Vector3(position.x, 1.15f, position.z), new Vector3(size.x, 0.12f, size.z), yellow);
            var boundary = Cube("Safety collision", position, size, dark);
            boundary.GetComponent<Renderer>().enabled = false;
        }

        private static void Interactable(DockSession session, string title, DockAction action, Vector3 position, Vector3 size,
            Material material, string description, bool hide = false, bool hold = false)
        {
            var go = Cube(title, position, size, material);
            var item = go.AddComponent<DockInteractable>(); item.session = session; item.action = action;
            item.displayName = title; item.description = description; item.hideAfterSuccess = hide; item.needsHold = hold;
        }

        private static void Pedestal(DockSession session, string title, DockAction action, Vector3 position,
            string description, Material material, bool hold = false)
        {
            Cube(title + " stand", position + Vector3.up * 0.5f, new Vector3(0.65f, 1, 0.55f), dark);
            Interactable(session, title, action, position + Vector3.up * 1.1f, new Vector3(0.75f, 0.35f, 0.65f), material, description, false, hold);
            Sign(ControlLabel(action), position + new Vector3(0, 1.42f, -0.34f), 0, 0.025f);
        }

        internal static string ControlLabel(DockAction action)
        {
            switch (action)
            {
                case DockAction.InstallPin: return "REPAIR";
                case DockAction.StartFlood: return "FLOOD";
                case DockAction.UnlockGate: return "UNLOCK";
                case DockAction.DeployBridge: return "BRIDGE";
                case DockAction.BoardBoat: return "EXIT";
                default: return "PIN";
            }
        }

        private static void Sign(string label, Vector3 position, float yaw, float size, bool floor = false)
        {
            var go = new GameObject("Sign " + label); go.transform.SetParent(root); go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(floor ? 90 : 0, yaw, 0);
            var text = go.AddComponent<TextMesh>(); text.text = label; text.fontSize = 72; text.characterSize = size;
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = new Color(0.8f, 0.94f, 0.9f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        }

        private static void Lighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.4f, 0.48f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.12f, 0.19f, 0.23f); RenderSettings.fogDensity = 0.017f;
            var sun = new GameObject("Overcast daylight"); sun.transform.SetParent(root);
            sun.transform.rotation = Quaternion.Euler(45, -30, 0);
            var light = sun.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.5f;
            light.color = new Color(0.65f, 0.79f, 1f); light.shadows = LightShadows.Soft;
            foreach (float x in new[] { -8f, 8f })
            {
                var go = new GameObject("Warm work light"); go.transform.SetParent(root); go.transform.position = new Vector3(x, 2.7f, 0);
                var workLight = go.AddComponent<Light>(); workLight.type = LightType.Point;
                workLight.range = 12; workLight.intensity = 5; workLight.color = new Color(1f, 0.77f, 0.49f);
            }
        }
    }
}
