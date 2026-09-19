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
using Moba.Combat;

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

        [MenuItem("MobaMVP/Setup Phase 0+1+2 Scenes And Prefabs")]
        public static void SetupAll()
        {
            EnsureFolders();
            var playerPrefab = CreateOrUpdatePlayerPrefab();
            var dummyPrefab = CreateOrUpdateDummyPrefab();
            var minionPrefab = CreateOrUpdateMinionPrefab();
            var towerPrefab = CreateOrUpdateTowerPrefab();
            var crystalPrefab = CreateOrUpdateCrystalPrefab();
            var networkPrefab = CreateOrUpdateNetworkPrefab(
                playerPrefab, dummyPrefab, minionPrefab, towerPrefab, crystalPrefab);
            CreateBootScene(networkPrefab);
            CreateLobbyScene(networkPrefab);
            CreateArenaScene(dummyPrefab, minionPrefab, towerPrefab, crystalPrefab);
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "MobaMVP Phase 0–2",
                "Ready.\n\nPlay Boot → Host → Start Match\n" +
                "WASD / J attack / K skill\n" +
                "Waves spawn; destroy enemy tower then crystal to win.\n" +
                "Last-hit minions for gold.",
                "OK");
        }

        [MenuItem("MobaMVP/Setup Phase 0+1 Scenes And Prefabs")]
        public static void SetupPhase01Menu()
        {
            SetupAll();
        }

        [MenuItem("MobaMVP/Setup Phase 0 Scenes And Prefabs")]
        public static void SetupAllLegacyMenu()
        {
            SetupAll();
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

            capsule.AddComponent<HeroCombat>();
            capsule.AddComponent<PlayerCombatInput>();
            AttachHealthBar(capsule);

            var prefab = PrefabUtility.SaveAsPrefabAsset(capsule, path);
            Object.DestroyImmediate(capsule);
            return prefab;
        }

        private static void AttachHealthBar(GameObject owner)
        {
            var barRoot = new GameObject("HealthBar", typeof(RectTransform));
            barRoot.transform.SetParent(owner.transform, false);
            barRoot.transform.localPosition = new Vector3(0f, 2.2f, 0f);

            var canvas = barRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = barRoot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1.6f, 0.2f);
            barRoot.transform.localScale = Vector3.one * 0.02f;

            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(barRoot.transform, false);
            StretchFull(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(barRoot.transform, false);
            StretchFull(fillGo.GetComponent<RectTransform>());
            var fill = fillGo.GetComponent<Image>();
            fill.color = new Color(0.2f, 0.85f, 0.35f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;

            var bar = barRoot.AddComponent<WorldHealthBar>();
            var barSo = new SerializedObject(bar);
            barSo.FindProperty("_targetBehaviour").objectReferenceValue =
                owner.GetComponent<HeroCombat>() as MonoBehaviour
                ?? owner.GetComponent<MinionUnit>() as MonoBehaviour
                ?? owner.GetComponent<DefenseTower>() as MonoBehaviour
                ?? owner.GetComponent<NexusCrystal>() as MonoBehaviour;
            barSo.FindProperty("_fill").objectReferenceValue = fill;
            barSo.FindProperty("_canvas").objectReferenceValue = canvas;
            barSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AttachHealthBar(GameObject owner, Vector3 localOffset)
        {
            AttachHealthBar(owner);
            var bar = owner.transform.Find("HealthBar");
            if (bar != null)
            {
                bar.localPosition = localOffset;
                var wh = bar.GetComponent<WorldHealthBar>();
                if (wh != null)
                {
                    var so = new SerializedObject(wh);
                    so.FindProperty("_offset").vector3Value = Vector3.zero;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static GameObject CreateOrUpdateDummyPrefab()
        {
            var path = UnitsPrefabsDir + "/TrainingDummy.prefab";
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "TrainingDummy";
            Object.DestroyImmediate(capsule.GetComponent<CapsuleCollider>());

            var netObj = capsule.AddComponent<NetworkObject>();
            netObj.AlwaysReplicateAsRoot = true;
            capsule.AddComponent<NetworkTransform>();

            var combat = capsule.AddComponent<HeroCombat>();
            var combatSo = new SerializedObject(combat);
            combatSo.FindProperty("_useFixedTeam").boolValue = true;
            combatSo.FindProperty("_fixedTeam").enumValueIndex = (int)TeamId.Red;
            combatSo.ApplyModifiedPropertiesWithoutUndo();

            var dummy = capsule.AddComponent<TrainingDummy>();
            var dummySo = new SerializedObject(dummy);
            dummySo.FindProperty("_bodyRenderer").objectReferenceValue = capsule.GetComponent<Renderer>();
            dummySo.ApplyModifiedPropertiesWithoutUndo();

            AttachHealthBar(capsule);

            var prefab = PrefabUtility.SaveAsPrefabAsset(capsule, path);
            Object.DestroyImmediate(capsule);
            return prefab;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject CreateOrUpdateNetworkPrefab(
            GameObject playerPrefab,
            GameObject dummyPrefab,
            GameObject minionPrefab,
            GameObject towerPrefab,
            GameObject crystalPrefab)
        {
            var path = NetworkPrefabsDir + "/MobaNetworkRoot.prefab";
            var root = new GameObject("MobaNetworkRoot");
            var nm = root.AddComponent<NetworkManager>();
            root.AddComponent<UnityTransport>();
            root.AddComponent<MobaNetworkController>();

            nm.NetworkConfig.PlayerPrefab = playerPrefab;
            nm.NetworkConfig.TickRate = (uint)NetworkDefaults.SimulationTickRate;
            nm.NetworkConfig.ConnectionApproval = true;

            void AddPrefab(GameObject go)
            {
                if (go != null)
                {
                    nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = go });
                }
            }

            AddPrefab(dummyPrefab);
            AddPrefab(minionPrefab);
            AddPrefab(towerPrefab);
            AddPrefab(crystalPrefab);

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

        private static void CreateArenaScene(
            GameObject dummyPrefab,
            GameObject minionPrefab,
            GameObject towerPrefab,
            GameObject crystalPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "LaneGround";
            ground.transform.localScale = new Vector3(4f, 1f, 12f);
            ground.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial(new Color(0.25f, 0.32f, 0.28f));

            CreateSpawnMarker("SpawnPoint_Team0_0", new Vector3(0f, 1f, -40f), new Color(0.2f, 0.45f, 1f));
            CreateSpawnMarker("SpawnPoint_Team1_0", new Vector3(0f, 1f, 40f), new Color(1f, 0.3f, 0.25f));

            // Visual placeholders (non-networked) until server spawns real buildings.
            CreateVisualMarker("TowerMarker_Blue", PrimitiveType.Cylinder, new Vector3(0f, 1.5f, -25f),
                new Vector3(2f, 1.5f, 2f), new Color(0.2f, 0.45f, 1f, 0.35f));
            CreateVisualMarker("TowerMarker_Red", PrimitiveType.Cylinder, new Vector3(0f, 1.5f, 25f),
                new Vector3(2f, 1.5f, 2f), new Color(1f, 0.3f, 0.25f, 0.35f));
            CreateVisualMarker("CrystalMarker_Blue", PrimitiveType.Cube, new Vector3(0f, 1f, -45f),
                Vector3.one, new Color(0.4f, 0.7f, 1f, 0.35f));
            CreateVisualMarker("CrystalMarker_Red", PrimitiveType.Cube, new Vector3(0f, 1f, 45f),
                Vector3.one, new Color(1f, 0.5f, 0.4f, 0.35f));

            var arena = new GameObject("ArenaBootstrap");
            arena.AddComponent<ArenaBootstrap>();

            var spawner = new GameObject("PlayerSpawnPlacer");
            spawner.AddComponent<PlayerSpawnPlacer>();

            var dummySpawnerGo = new GameObject("ArenaDummySpawner");
            var dummySpawner = dummySpawnerGo.AddComponent<ArenaDummySpawner>();
            var dummySo = new SerializedObject(dummySpawner);
            dummySo.FindProperty("_dummyPrefab").objectReferenceValue = dummyPrefab != null
                ? dummyPrefab.GetComponent<NetworkObject>()
                : null;
            dummySo.FindProperty("_spawnOnStart").boolValue = false;
            dummySo.ApplyModifiedPropertiesWithoutUndo();

            var structureGo = new GameObject("ArenaStructureSpawner");
            var structures = structureGo.AddComponent<ArenaStructureSpawner>();
            var stSo = new SerializedObject(structures);
            stSo.FindProperty("_towerPrefab").objectReferenceValue =
                towerPrefab != null ? towerPrefab.GetComponent<NetworkObject>() : null;
            stSo.FindProperty("_crystalPrefab").objectReferenceValue =
                crystalPrefab != null ? crystalPrefab.GetComponent<NetworkObject>() : null;
            stSo.ApplyModifiedPropertiesWithoutUndo();

            var waveGo = new GameObject("LaneWaveSpawner");
            var wave = waveGo.AddComponent<LaneWaveSpawner>();
            var waveSo = new SerializedObject(wave);
            waveSo.FindProperty("_minionPrefab").objectReferenceValue = minionPrefab != null
                ? minionPrefab.GetComponent<NetworkObject>()
                : null;
            waveSo.ApplyModifiedPropertiesWithoutUndo();

            CreateMatchResultUi();
            CreateCombatHudOverlay();

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 28f, -18f);
                cam.transform.LookAt(Vector3.zero);
            }

            EditorSceneManager.SaveScene(scene, ScenesDir + "/Arena_1Lane.unity");
        }

        private static GameObject CreateOrUpdateMinionPrefab()
        {
            var path = UnitsPrefabsDir + "/Minion.prefab";
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Minion";
            capsule.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            Object.DestroyImmediate(capsule.GetComponent<Collider>());

            var netObj = capsule.AddComponent<NetworkObject>();
            netObj.AlwaysReplicateAsRoot = true;
            capsule.AddComponent<NetworkTransform>();

            var minion = capsule.AddComponent<MinionUnit>();
            var so = new SerializedObject(minion);
            so.FindProperty("_bodyRenderer").objectReferenceValue = capsule.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();

            AttachHealthBar(capsule);

            var prefab = PrefabUtility.SaveAsPrefabAsset(capsule, path);
            Object.DestroyImmediate(capsule);
            return prefab;
        }

        private static void CreateVisualMarker(
            string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial(color);
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }
        }

        private static GameObject CreateOrUpdateTowerPrefab()
        {
            var path = UnitsPrefabsDir + "/DefenseTower.prefab";
            var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "DefenseTower";
            tower.transform.localScale = new Vector3(2f, 1.5f, 2f);
            Object.DestroyImmediate(tower.GetComponent<Collider>());

            var net = tower.AddComponent<NetworkObject>();
            net.AlwaysReplicateAsRoot = true;
            tower.AddComponent<NetworkTransform>();
            var defense = tower.AddComponent<DefenseTower>();
            var so = new SerializedObject(defense);
            so.FindProperty("_bodyRenderer").objectReferenceValue = tower.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            AttachHealthBar(tower, new Vector3(0f, 3.5f, 0f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(tower, path);
            Object.DestroyImmediate(tower);
            return prefab;
        }

        private static GameObject CreateOrUpdateCrystalPrefab()
        {
            var path = UnitsPrefabsDir + "/NexusCrystal.prefab";
            var crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crystal.name = "NexusCrystal";
            Object.DestroyImmediate(crystal.GetComponent<Collider>());

            var net = crystal.AddComponent<NetworkObject>();
            net.AlwaysReplicateAsRoot = true;
            crystal.AddComponent<NetworkTransform>();
            var nexus = crystal.AddComponent<NexusCrystal>();
            var so = new SerializedObject(nexus);
            so.FindProperty("_bodyRenderer").objectReferenceValue = crystal.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            AttachHealthBar(crystal, new Vector3(0f, 2.5f, 0f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(crystal, path);
            Object.DestroyImmediate(crystal);
            return prefab;
        }

        private static void CreateMatchResultUi()
        {
            var root = new GameObject("MatchResult", typeof(NetworkObject));
            var controller = root.AddComponent<MatchResultController>();

            var canvasGo = new GameObject("ResultCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var panel = CreateUiPanel(canvasGo.transform, "Panel");
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            panel.SetActive(false);

            var title = CreateText(panel.transform, "ResultText", "对局结束", 48, TextAnchor.MiddleCenter);
            SetRect(title.GetComponent<RectTransform>(), 0.1f, 0.45f, 0.9f, 0.7f);

            var back = CreateButton(panel.transform, "BackButton", "返回大厅");
            SetRect(back.GetComponent<RectTransform>(), 0.35f, 0.25f, 0.65f, 0.38f);

            var so = new SerializedObject(controller);
            so.FindProperty("_panel").objectReferenceValue = panel;
            so.FindProperty("_resultText").objectReferenceValue = title;
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCombatHudOverlay()
        {
            var canvasGo = new GameObject("CombatHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var hint = CreateText(canvasGo.transform, "Hint", "J/LMB attack | K/Q skill | WASD move", 28, TextAnchor.LowerCenter);
            SetRect(hint.GetComponent<RectTransform>(), 0.15f, 0.02f, 0.85f, 0.08f);

            var status = CreateText(canvasGo.transform, "Status", "HP --", 26, TextAnchor.UpperLeft);
            SetRect(status.GetComponent<RectTransform>(), 0.02f, 0.88f, 0.55f, 0.98f);

            var hud = canvasGo.AddComponent<CombatHud>();
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("_hintText").objectReferenceValue = hint;
            hudSo.FindProperty("_statusText").objectReferenceValue = status;
            hudSo.ApplyModifiedPropertiesWithoutUndo();
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
