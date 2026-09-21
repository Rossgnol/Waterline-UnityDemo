using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Waterline.Core;

namespace Waterline.Editor
{
    public static class ExplorationBuilder
    {
        public const string ScenePath = "Assets/Waterline/Scenes/Day02_Exploration.unity";
        private static Transform root;
        private static Material concrete, dark, steel, amber, teal;

        [MenuItem("Waterline/06 Open exploration level")]
        public static void Open()
        {
            if (!File.Exists(ScenePath)) Build();
            else if (Application.isBatchMode || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        public static void Build()
        {
            if (File.Exists(ScenePath)) { Open(); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(PrototypeBuilder.ScenePath)) PrototypeBuilder.Build();
            else EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath);
            root = new GameObject("DAY 02 - Lower dock exploration").transform;
            concrete = Material("Concrete"); dark = Material("Dark steel"); steel = Material("Deck steel");
            amber = Material("Safety amber"); teal = Material("Signal teal");
            var session = Object.FindFirstObjectByType<DockSession>();
            var player = Object.FindFirstObjectByType<FirstPersonController>();
            session.explorationLayout = true; session.playerTransform = player.transform;
            // Shared Day 01 settings are preserved, so comparisons remain reproducible.
            var tuning = Object.Instantiate(session.tuning);
            AssetDatabase.CreateAsset(tuning, "Assets/Waterline/Settings/ExplorationTuning.asset");
            session.tuning = tuning;
            Object.DestroyImmediate(GameObject.Find("WEST / CONTROL outer wall"));
            Object.DestroyImmediate(GameObject.Find("EAST / WINCH outer wall"));
            foreach (int side in new[] { -1, 1 })
            {
                Cube("Upper outer wall with stair doorway", new Vector3(side * 11, 1.7f, 1), new Vector3(0.3f, 3.4f, 8), concrete);
                Stair(side);
            }
            GameObject.Find("Dry dock floor").transform.localScale = new Vector3(29, 0.4f, 10);
            GameObject.Find("Dry dock floor").transform.position = new Vector3(0, -3.7f, 0.5f);
            GameObject.Find("North seawall").transform.localScale = new Vector3(29.5f, 6.6f, 0.5f);
            Cube("Lower south seawall", new Vector3(0, -1.8f, -4.65f), new Vector3(29, 3.6f, 0.3f), concrete);
            foreach (int side in new[] { -1, 1 })
                Cube("Lower outer seawall", new Vector3(side * 14.6f, -1.8f, 0.5f), new Vector3(0.3f, 3.6f, 10.3f), concrete);

            // Tool room below the west room; two doorways retain a short internal loop.
            Cube("Workshop east wall north", new Vector3(-5, -2, 3.5f), new Vector3(0.25f, 3, 3), concrete);
            Cube("Workshop east wall south", new Vector3(-5, -2, -3.25f), new Vector3(0.25f, 3, 2.5f), concrete);
            Cube("Workshop doorway lintel", new Vector3(-5, -0.6f, 0), new Vector3(0.25f, 0.4f, 4), amber);
            Cube("Workshop rubber path", new Vector3(-8, -3.485f, 0.5f), new Vector3(2, 0.025f, 7.7f), teal, false);
            Cube("North crossing paint", new Vector3(0, -3.485f, 4.2f), new Vector3(22, 0.025f, 0.45f), amber, false);
            Cube("South crossing paint", new Vector3(0, -3.485f, -3.6f), new Vector3(22, 0.025f, 0.45f), teal, false);
            Cube("Lower east equipment", new Vector3(8, -2.85f, 0), new Vector3(2, 1.3f, 2.4f), dark);
            var table = GameObject.Find("Workbench"); table.transform.position = new Vector3(-8, -3.05f, 0);
            var pin = Object.FindObjectsByType<DockInteractable>(FindObjectsSortMode.None).Single(i => i.action == DockAction.TakePin);
            pin.transform.position = new Vector3(-8, -2.38f, 0);
            pin.description = "取走后穿过坞底，走东侧橙色楼梯上绞盘间。";
            foreach (var item in Object.FindObjectsByType<DockInteractable>(FindObjectsSortMode.None))
            {
                if (item.action == DockAction.StartFlood) item.description = "注水后两侧楼梯将封闭，下层不可返回。长按确认。";
                if (item.action == DockAction.InstallPin) item.description = "把下层工具间找到的联轴销安装到泵上。";
                if (item.action == DockAction.UnlockGate) item.description = "解除坞门机械锁，同时打开南侧隔离门。";
            }
            GameObject.Find("Sign PUMP INTERLOCK").GetComponent<TextMesh>().text = "UNLOCK IN EAST ROOM";
            GameObject.Find("Sign PUMP INTERLOCK").GetComponent<TextMesh>().characterSize = 0.028f;
            Label("WEST STAIRS / DOWN", new Vector3(-10.8f, 2.4f, -4), -90, 0.03f);
            Label("EAST STAIRS / DOWN", new Vector3(10.8f, 2.4f, -4), 90, 0.03f);
            Label("03  TOOL ROOM", new Vector3(-8, -1, 4.8f), 0, 0.06f);
            Label("EAST STAIRS >", new Vector3(8, -1.3f, 5.25f), 0, 0.045f);
            Label("FLOOD LINE", new Vector3(-4.82f, -0.6f, 3.5f), -90, 0.028f);
            foreach (var position in new[] { new Vector3(-8, -0.5f, 0), new Vector3(8, -0.5f, 2), new Vector3(0, -0.4f, 4) })
            {
                var lamp = new GameObject("Lower maintenance lamp"); lamp.transform.SetParent(root); lamp.transform.position = position;
                var light = lamp.AddComponent<Light>(); light.type = LightType.Point; light.range = 11; light.intensity = 5;
                light.color = new Color(0.6f, 0.84f, 1);
            }
            var routes = root.gameObject.AddComponent<DockRoutes>(); routes.session = session; routes.player = player.transform;
            routes.westFloodGate = FloodGate(-1); routes.eastFloodGate = FloodGate(1);
            root.gameObject.AddComponent<DockMap>().routes = routes;
            session.water.localScale = new Vector3(28.7f, 0.06f, 9.75f);
            // Keep dry water below walkable ground, then rise across the entire lower deck.
            session.dryWaterHeight = -3.62f;
            ApplyEntryGuide();
            ApplyLabelDepth();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("[Waterline] Exploration scene generated.");
        }

        private static void Stair(int side)
        {
            float x = side * 12.65f;
            Cube("Stair upper landing", new Vector3(x, -0.2f, -4), new Vector3(3.5f, 0.4f, 2), concrete);
            Cube("Stair landing south guard", new Vector3(x, 0.7f, -5), new Vector3(3.5f, 1.4f, 0.15f), dark);
            Cube("Stair landing outer guard", new Vector3(side * 14.35f, 0.7f, -4), new Vector3(0.15f, 1.4f, 2), dark);
            for (int step = 0; step < 20; step++)
            {
                float height = -(step + 1) * 0.175f;
                Cube("Stair tread " + side + "/" + step, new Vector3(x, height - 0.09f, -3 + (step + 0.5f) * 0.35f),
                    new Vector3(3.1f, 0.18f, 0.355f), steel);
                Cube("Stair nosing", new Vector3(x, height + 0.005f, -3 + step * 0.35f + 0.02f),
                    new Vector3(3.1f, 0.01f, 0.05f), side < 0 ? teal : amber, false);
            }
            foreach (float edge in new[] { -1.6f, 1.6f })
            {
                var rail = Cube("Stair sloping guard", new Vector3(x + edge, -1.05f, 0.5f), new Vector3(0.12f, 1.4f, 7.83f), dark);
                rail.transform.rotation = Quaternion.Euler(26.565f, 0, 0);
                var handrail = Cube("Stair sloping handrail", new Vector3(x + edge, -0.5f, 0.5f), new Vector3(0.15f, 0.12f, 7.83f), side < 0 ? teal : amber);
                handrail.transform.rotation = Quaternion.Euler(26.565f, 0, 0);
            }
        }

        private static GameObject FloodGate(int side)
        {
            var barrier = Cube("Flood safety gate " + side, new Vector3(side * 12.65f, 0.9f, -3.05f), new Vector3(3.25f, 1.8f, 0.18f), amber);
            barrier.SetActive(false);
            return barrier;
        }

        private static Material Material(string name)
        { return AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/" + name + ".mat"); }

        public static void UpgradeLabelDepth()
        {
            Open(); ApplyLabelDepth();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        public static void UpgradeEntryGuide()
        {
            Open(); ApplyEntryGuide(); ApplyLabelDepth();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        private static void ApplyEntryGuide()
        {
            var player = Object.FindFirstObjectByType<FirstPersonController>();
            player.transform.position = new Vector3(-8, 0.08f, -4);
            player.transform.rotation = Quaternion.Euler(0, -90, 0);
            foreach (int side in new[] { -1, 1 })
            {
                var sign = GameObject.Find("Route sign " + (side < 0 ? "WEST" : "EAST") + " STAIRS / DOWN");
                sign.transform.position = new Vector3(side * 10.8f, 2.4f, -4);
                sign.transform.rotation = Quaternion.Euler(0, side * 90, 0);
                sign.GetComponent<TextMesh>().characterSize = 0.03f;
            }
        }

        private static void ApplyLabelDepth()
        {
            const string path = "Assets/Waterline/Generated/World text.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Waterline/Depth Text");
                if (shader == null) throw new System.InvalidOperationException("Depth text shader is not imported.");
                material = new Material(shader); AssetDatabase.CreateAsset(material, path);
            }
            foreach (var mesh in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                var label = mesh.GetComponent<WorldLabel>();
                if (label == null) label = mesh.gameObject.AddComponent<WorldLabel>();
                label.template = material; label.Refresh();
                EditorUtility.SetDirty(label);
            }
        }

        private static GameObject Cube(string name, Vector3 p, Vector3 size, Material material, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(root);
            go.transform.position = p; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Label(string value, Vector3 p, float yaw, float size)
        {
            var go = new GameObject("Route sign " + value); go.transform.SetParent(root); go.transform.position = p;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var text = go.AddComponent<TextMesh>(); text.text = value; text.fontSize = 72; text.characterSize = size;
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); go.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        }

        public static void Validate()
        {
            PrototypeValidation.Validate();
            var routes = Object.FindFirstObjectByType<DockRoutes>();
            if (routes == null || routes.player == null || routes.westFloodGate == null || routes.eastFloodGate == null ||
                !routes.session.explorationLayout || routes.session.playerTransform != routes.player)
                throw new System.InvalidOperationException("Exploration route references missing.");
            var pin = Object.FindObjectsByType<DockInteractable>(FindObjectsSortMode.None).Single(i => i.action == DockAction.TakePin);
            if (pin.transform.position.y > -2) throw new System.InvalidOperationException("Repair pin must be below deck.");
        }

        [MenuItem("Waterline/07 Build exploration demo")]
        public static void BuildWindows()
        {
            Open(); Validate(); Directory.CreateDirectory("Builds/Exploration");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = "Builds/Exploration/Waterline.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded) throw new System.InvalidOperationException("Exploration build failed.");
            Debug.Log("[Waterline] Exploration Windows build ready.");
        }
    }
}
