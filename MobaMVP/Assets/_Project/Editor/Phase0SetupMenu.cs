#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using Moba.Bootstrap;
using Moba.Network;
using Moba.UI;
using Moba.Core;

namespace Moba.EditorTools
{
    /// <summary>
    /// One-click Phase 0 content: Boot / Lobby / Arena scenes, network prefabs, build settings.
    /// Menu: MobaMVP → Setup Phase 0 Scenes And Prefabs
    /// </summary>
    public static class Phase0SetupMenu
    {
        private const string Root = "Assets/_Project";
        private const string ScenesDir = Root + "/Scenes";
        private const string PrefabsDir = Root + "/Prefabs";
        private const string NetworkPrefabsDir = PrefabsDir + "/Network";
        private const string UnitsPrefabsDir = PrefabsDir + "/Units";

        [MenuItem("MobaMVP/Setup Phase 0 Scenes And Prefabs")]
        public static void SetupAll()
        {
            EnsureFolders();
            var playerPrefab = CreateOrUpdatePlayerPrefab();
            var networkPrefab = CreateOrUpdateNetworkPrefab(playerPrefab);
            CreateBootScene(networkPrefab);
            CreateLobbyScene(networkPrefab);
            CreateArenaScene();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "MobaMVP Phase 0",
                "Scenes and prefabs ready.\n\n1) Open Boot and Press Play\n2) Host in one Editor (or build)\n3) Join 127.0.0.1 from second client\n4) Host clicks Start Match\n\nMove with WASD in Arena.",
                "OK");
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(ScenesDir.Replace("Assets", Application.dataPath));
            Directory.CreateDirectory(NetworkPrefabsDir.Replace("Assets", Application.dataPath));
            Directory.CreateDirectory(UnitsPrefabsDir.Replace("Assets", Application.dataPath));
        }

        private static GameObject CreateOrUpdatePlayerPrefab()
        {
            var path = UnitsPrefabsDir + "/PlayerAvatar.prefab";
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "PlayerAvatar";
            Object.DestroyImmediate(capsule.GetComponent<CapsuleCollider>());

            var netObj = capsule.AddComponent<NetworkObject>();
            netObj.AlwaysReplicateAsRoot = true;

            var transformSync = capsule.AddComponent<NetworkTransform>();
            transformSync.Interpolate = true;

            var avatar = capsule.AddComponent<PlayerAvatar>();
            var so = new SerializedObject(avatar);
            so.FindProperty("_bodyRenderer").objectReferenceValue = capsule.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(capsule, path);
            Object.DestroyImmediate(capsule);
            return prefab;
        }

        private static GameObject CreateOrUpdateNetworkPrefab(GameObject playerPrefab)
        {
            var path = NetworkPrefabsDir + "/MobaNetworkRoot.prefab";
            var root = new GameObject("MobaNetworkRoot");
            var nm = root.AddComponent<NetworkManager>();
            root.AddComponent<UnityTransport>();
            root.AddComponent<MobaNetworkController>();

            nm.NetworkConfig.PlayerPrefab = playerPrefab;
            nm.NetworkConfig.TickRate = (uint)NetworkDefaults.SimulationTickRate;
            nm.NetworkConfig.ConnectionApproval = true;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateBootScene(GameObject networkPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var boot = new GameObject("BootLoader");
            boot.AddComponent<BootLoader>();

            // Persistent network + session live from Boot.
            var session = new GameObject("GameSession");
            session.AddComponent<GameSession>();

            PrefabUtility.InstantiatePrefab(networkPrefab);

            EditorSceneManager.SaveScene(scene, ScenesDir + "/Boot.unity");
        }

        private static void CreateLobbyScene(GameObject networkPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var canvasGo = new GameObject("LobbyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }

            var panel = CreateUiPanel(canvasGo.transform, "Panel");
            var title = CreateText(panel, "Title", "MobaMVP Lobby (LAN)", 36, TextAnchor.UpperCenter);
            SetRect(title.GetComponent<RectTransform>(), 0.1f, 0.82f, 0.9f, 0.95f);

            var address = CreateInput(panel, "Address", NetworkDefaults.DefaultAddress);
            SetRect(address.GetComponent<RectTransform>(), 0.3f, 0.68f, 0.7f, 0.76f);
            CreateText(panel, "AddressLabel", "Host IP", 20, TextAnchor.MiddleRight)
                .GetComponent<RectTransform>();
            SetRect(panel.Find("AddressLabel").GetComponent<RectTransform>(), 0.08f, 0.68f, 0.28f, 0.76f);

            var port = CreateInput(panel, "Port", NetworkDefaults.DefaultPort.ToString());
            SetRect(port.GetComponent<RectTransform>(), 0.3f, 0.58f, 0.7f, 0.66f);
            CreateText(panel, "PortLabel", "Port", 20, TextAnchor.MiddleRight);
            SetRect(panel.Find("PortLabel").GetComponent<RectTransform>(), 0.08f, 0.58f, 0.28f, 0.66f);

            var mode = CreateModeDropdown(panel);
            SetRect(mode.GetComponent<RectTransform>(), 0.3f, 0.48f, 0.7f, 0.56f);
            CreateText(panel, "ModeLabel", "Mode", 20, TextAnchor.MiddleRight);
            SetRect(panel.Find("ModeLabel").GetComponent<RectTransform>(), 0.08f, 0.48f, 0.28f, 0.56f);

            var hostBtn = CreateButton(panel, "HostButton", "Host");
            SetRect(hostBtn.GetComponent<RectTransform>(), 0.2f, 0.34f, 0.48f, 0.44f);
            var joinBtn = CreateButton(panel, "JoinButton", "Join");
            SetRect(joinBtn.GetComponent<RectTransform>(), 0.52f, 0.34f, 0.8f, 0.44f);
            var startBtn = CreateButton(panel, "StartMatchButton", "Start Match (Host)");
            SetRect(startBtn.GetComponent<RectTransform>(), 0.2f, 0.22f, 0.8f, 0.32f);
            var discBtn = CreateButton(panel, "DisconnectButton", "Disconnect");
            SetRect(discBtn.GetComponent<RectTransform>(), 0.2f, 0.12f, 0.8f, 0.2f);

            var status = CreateText(panel, "Status", "Status", 22, TextAnchor.MiddleCenter);
            SetRect(status.GetComponent<RectTransform>(), 0.1f, 0.02f, 0.9f, 0.1f);

            var menu = canvasGo.AddComponent<LobbyMenu>();
            var menuSo = new SerializedObject(menu);
            menuSo.FindProperty("_addressField").objectReferenceValue = address;
            menuSo.FindProperty("_portField").objectReferenceValue = port;
            menuSo.FindProperty("_modeDropdown").objectReferenceValue = mode;
            menuSo.FindProperty("_statusText").objectReferenceValue = status;
            menuSo.FindProperty("_hostButton").objectReferenceValue = hostBtn;
            menuSo.FindProperty("_joinButton").objectReferenceValue = joinBtn;
            menuSo.FindProperty("_startMatchButton").objectReferenceValue = startBtn;
            menuSo.FindProperty("_disconnectButton").objectReferenceValue = discBtn;
            menuSo.ApplyModifiedPropertiesWithoutUndo();

            // Network root may already exist from Boot DontDestroy; still place one for Lobby-only play.
            if (Object.FindFirstObjectByType<NetworkManager>() == null)
            {
                PrefabUtility.InstantiatePrefab(networkPrefab);
            }

            if (Object.FindFirstObjectByType<GameSession>() == null)
            {
                new GameObject("GameSession").AddComponent<GameSession>();
            }

            EditorSceneManager.SaveScene(scene, ScenesDir + "/Lobby.unity");
        }

        private static void CreateArenaScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Ground plane greybox lane.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "LaneGround";
            ground.transform.localScale = new Vector3(4f, 1f, 12f);
            ground.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial(new Color(0.25f, 0.32f, 0.28f));

            CreateSpawnMarker("SpawnPoint_Team0_0", new Vector3(0f, 1f, -40f), new Color(0.2f, 0.45f, 1f));
            CreateSpawnMarker("SpawnPoint_Team1_0", new Vector3(0f, 1f, 40f), new Color(1f, 0.3f, 0.25f));

            var towerBlue = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            towerBlue.name = "Tower_Team0_Tier1";
            towerBlue.transform.position = new Vector3(0f, 1.5f, -25f);
            towerBlue.transform.localScale = new Vector3(2f, 1.5f, 2f);
            towerBlue.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial(new Color(0.2f, 0.45f, 1f));

            var towerRed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            towerRed.name = "Tower_Team1_Tier1";
            towerRed.transform.position = new Vector3(0f, 1.5f, 25f);
            towerRed.transform.localScale = new Vector3(2f, 1.5f, 2f);
            towerRed.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial(new Color(1f, 0.3f, 0.25f));

            var crystalBlue = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crystalBlue.name = "Crystal_Team0";
            crystalBlue.transform.position = new Vector3(0f, 1f, -45f);
            crystalBlue.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial(new Color(0.4f, 0.7f, 1f));

            var crystalRed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crystalRed.name = "Crystal_Team1";
            crystalRed.transform.position = new Vector3(0f, 1f, 45f);
            crystalRed.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial(new Color(1f, 0.5f, 0.4f));

            var arena = new GameObject("ArenaBootstrap");
            arena.AddComponent<ArenaBootstrap>();

            var spawner = new GameObject("PlayerSpawnPlacer");
            spawner.AddComponent<PlayerSpawnPlacer>();

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 28f, -18f);
                cam.transform.LookAt(Vector3.zero);
            }

            EditorSceneManager.SaveScene(scene, ScenesDir + "/Arena_1Lane.unity");
        }

        private static void CreateSpawnMarker(string name, Vector3 pos, Color color)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = name;
            marker.transform.position = pos;
            marker.transform.localScale = Vector3.one * 0.8f;
            marker.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial(color);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
        }

        private static Material CreateColorMaterial(Color color)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Standard") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Sprites/Default");
            var mat = new Material(shader) { color = color };
            return mat;
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenesDir + "/Boot.unity", true),
                new EditorBuildSettingsScene(ScenesDir + "/Lobby.unity", true),
                new EditorBuildSettingsScene(ScenesDir + "/Arena_1Lane.unity", true)
            };
        }

        private static GameObject CreateUiPanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.12f, 0.92f);
            return go;
        }

        private static Text CreateText(Transform parent, string name, string content, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private static InputField CreateInput(Transform parent, string name, string value)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.22f, 1f);

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGo.transform.SetParent(go.transform, false);
            var placeholder = placeholderGo.GetComponent<Text>();
            placeholder.text = value;
            placeholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            placeholder.fontSize = 22;
            Stretch(placeholder.GetComponent<RectTransform>());

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = Color.white;
            text.fontSize = 22;
            text.supportRichText = false;
            Stretch(text.GetComponent<RectTransform>());

            var field = go.GetComponent<InputField>();
            field.textComponent = text;
            field.placeholder = placeholder;
            field.text = value;
            return field;
        }

        private static Dropdown CreateModeDropdown(Transform parent)
        {
            var go = new GameObject("ModeDropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.22f, 1f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.color = Color.white;
            label.fontSize = 22;
            Stretch(label.GetComponent<RectTransform>());

            var template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(go.transform, false);
            template.SetActive(false);
            var templateRt = template.GetComponent<RectTransform>();
            templateRt.anchorMin = new Vector2(0f, 0f);
            templateRt.anchorMax = new Vector2(1f, 0f);
            templateRt.pivot = new Vector2(0.5f, 1f);
            templateRt.sizeDelta = new Vector2(0f, 100f);
            template.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.16f, 1f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(template.transform, false);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = Color.white;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 28f);

            var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            Stretch(item.GetComponent<RectTransform>());
            var itemLabel = new GameObject("Item Label", typeof(RectTransform), typeof(Text));
            itemLabel.transform.SetParent(item.transform, false);
            var itemText = itemLabel.GetComponent<Text>();
            itemText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemText.color = Color.white;
            itemText.fontSize = 20;
            Stretch(itemLabel.GetComponent<RectTransform>());

            var scroll = template.GetComponent<ScrollRect>();
            scroll.content = contentRt;
            scroll.viewport = viewport.GetComponent<RectTransform>();

            var dropdown = go.GetComponent<Dropdown>();
            dropdown.captionText = label;
            dropdown.itemText = itemText;
            dropdown.template = templateRt;
            dropdown.options.Clear();
            dropdown.options.Add(new Dropdown.OptionData("1v1"));
            dropdown.options.Add(new Dropdown.OptionData("3v3"));
            dropdown.value = 0;
            dropdown.RefreshShownValue();
            return dropdown;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.18f, 0.45f, 0.55f, 1f);
            var text = CreateText(go.transform, "Text", label, 24, TextAnchor.MiddleCenter);
            Stretch(text.GetComponent<RectTransform>());
            return go.GetComponent<Button>();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 4f);
            rt.offsetMax = new Vector2(-8f, -4f);
        }

        private static void SetRect(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
#endif
