using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using AnalogHorror;

namespace AnalogHorror.EditorTools
{
    /// <summary>
    /// Procedurally builds the whole analog-horror Jurassic Park map
    /// (plus player) out of primitives. Menu: Jurassic Horror → Build.
    /// </summary>
    public static partial class MapBuilder
    {
        const string ScenePath = "Assets/Scenes/JurassicHorror.unity";
        const string MatFolder = "Assets/Resources/Materials";
        const int Seed = 1993;

        static System.Random rng;
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        static Transform envRoot, vcRoot, padRoot, jungRoot, tunRoot;
        static Transform propsRoot, lightRoot, playerRoot, audioRoot;

        // ------------------------------------------------------------------ menu

        [MenuItem("Jurassic Horror/Build Complete Map && Player")]
        public static void BuildAll()
        {
            try
            {
                BuildAllInner();
                System.IO.File.WriteAllText("Temp/build_error.txt", "OK");
            }
            catch (System.Exception ex)
            {
                // the editor log can be stolen by a second Unity instance —
                // always leave the failure where we can read it
                System.IO.File.WriteAllText("Temp/build_error.txt", ex.ToString());
                Debug.LogException(ex);
            }
        }

        static void BuildAllInner()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            rng = new System.Random(Seed);
            mats.Clear();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateRoots();
            CreateAllMaterials();
            BuildLighting();

            BuildGroundAndRoads();
            BuildJungle();
            BuildVisitorCenter();
            BuildPaddocks();
            BuildTunnelSystem();
            BuildPropsAndDebris();
            BuildPlayer();
            BuildAmbience();
            BuildDinosaurs();
            BuildHorrorSystems();
            BuildCreepyFigures();
            BuildPropAnimation();
            BuildGameLoop();
            BuildExpansion();       // bigger world: river, plant, cargo, aviary
            BuildNpcs();            // survivors you can actually talk to
            BuildMenu();            // title screen — gates the run
            BuildMovieScenes();     // JP III corner: plane wreck, spinosaurus
            BuildRanch();           // far north: Ranch 04, the hatching yard
            BuildIslandWorld();     // island two: Jurassic World, across the channel

            SaveAndRegisterScene();
        }

        [MenuItem("Jurassic Horror/Show Map Guide")]
        public static void ShowGuide()
        {
            string full = System.IO.Path.GetFullPath("MAP_GUIDE.md");
            if (System.IO.File.Exists(full))
                EditorUtility.RevealInFinder(full);
            else
                Debug.LogWarning("MAP_GUIDE.md not found at " + full);
        }

        /// <summary>
        /// Compiles the game into a standalone Windows 64-bit build you can
        /// ship: Builds/Windows/JurassicHorror.exe (no Unity needed to play).
        /// </summary>
        [MenuItem("Jurassic Horror/Build Windows Player (.exe)")]
        public static void BuildWindowsPlayer()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // the scene must be in the build settings or the exe starts empty
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            const string dir = "Builds/Windows";
            System.IO.Directory.CreateDirectory(dir);
            string exe = System.IO.Path.Combine(dir, "JurassicHorror.exe");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            var s = report.summary;
            if (s.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log("WINDOWS BUILD OK: " + exe +
                          " (" + (s.totalSize / (1024 * 1024)) + " MB, " +
                          s.totalTime.TotalSeconds.ToString("F0") + "s)");
            else
                Debug.LogError("WINDOWS BUILD FAILED: " + s.result +
                               " errors=" + s.totalErrors);
        }

        // ----------------------------------------------------------------- roots

        static void CreateRoots()
        {
            envRoot = NewRoot("_ENVIRONMENT");
            vcRoot = NewRoot("_VISITOR CENTER");
            padRoot = NewRoot("_PADDOCKS");
            jungRoot = NewRoot("_JUNGLE");
            tunRoot = NewRoot("_TUNNELS");
            propsRoot = NewRoot("_PROPS");
            lightRoot = NewRoot("_LIGHTING");
            playerRoot = NewRoot("_PLAYER");
            audioRoot = NewRoot("_AUDIO");
        }

        static Transform NewRoot(string name)
        {
            var go = new GameObject(name);
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            return go.transform;
        }

        // ------------------------------------------------------------- materials

        static void CreateAllMaterials()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(MatFolder);

            // ground / nature
            Mat("GrassDark", new Color(0.055f, 0.09f, 0.05f), 0.08f);
            Mat("GrassJungle", new Color(0.04f, 0.075f, 0.04f), 0.06f);
            Mat("DirtRoad", new Color(0.16f, 0.12f, 0.08f), 0.1f);
            Mat("Trunk", new Color(0.14f, 0.105f, 0.075f), 0.15f);
            Mat("LeafDark", new Color(0.025f, 0.065f, 0.03f), 0.12f);
            Mat("LeafMid", new Color(0.045f, 0.1f, 0.045f), 0.12f);
            Mat("Hedge", new Color(0.02f, 0.05f, 0.025f), 0.1f);
            Mat("Rock", new Color(0.22f, 0.22f, 0.21f), 0.15f);

            // structures
            Mat("Concrete", new Color(0.3f, 0.3f, 0.285f), 0.22f);
            Mat("ConcreteDark", new Color(0.17f, 0.175f, 0.17f), 0.2f);
            Mat("ConcreteDirty", new Color(0.23f, 0.22f, 0.19f), 0.16f);
            Mat("WoodPlank", new Color(0.24f, 0.16f, 0.09f), 0.18f);
            Mat("WoodDark", new Color(0.13f, 0.085f, 0.05f), 0.15f);
            Mat("RoofMetal", new Color(0.16f, 0.17f, 0.16f), 0.35f, 0.6f);

            // metal
            Mat("MetalGray", new Color(0.35f, 0.36f, 0.37f), 0.45f, 0.75f);
            Mat("MetalDark", new Color(0.11f, 0.11f, 0.12f), 0.4f, 0.6f);
            Mat("MetalRust", new Color(0.3f, 0.15f, 0.08f), 0.35f, 0.4f);
            Mat("ChainLink", new Color(0.4f, 0.42f, 0.43f), 0.5f, 0.85f);

            // signs / paint
            Mat("SignYellow", new Color(0.55f, 0.42f, 0.05f), 0.3f);
            Mat("SignRed", new Color(0.45f, 0.06f, 0.05f), 0.3f);
            Mat("SignWhite", new Color(0.7f, 0.7f, 0.66f), 0.3f);
            Mat("SignBlack", new Color(0.05f, 0.05f, 0.05f), 0.3f);
            Mat("PaintWhite", new Color(0.65f, 0.65f, 0.6f), 0.3f);

            // props
            Mat("Crate", new Color(0.32f, 0.23f, 0.13f), 0.2f);
            Mat("BarrelBlue", new Color(0.07f, 0.13f, 0.25f), 0.4f, 0.5f);
            Mat("BarrelRed", new Color(0.3f, 0.07f, 0.06f), 0.4f, 0.5f);
            Mat("JeepGreen", new Color(0.1f, 0.14f, 0.08f), 0.35f, 0.3f);
            Mat("Tire", new Color(0.04f, 0.04f, 0.04f), 0.45f);
            Mat("Chrome", new Color(0.6f, 0.6f, 0.62f), 0.65f, 0.9f);
            Mat("Bone", new Color(0.68f, 0.65f, 0.55f), 0.35f);
            Mat("Egg", new Color(0.72f, 0.68f, 0.55f), 0.4f);
            Mat("BloodDark", new Color(0.12f, 0.015f, 0.01f), 0.5f);
            Mat("Puddle", new Color(0.03f, 0.045f, 0.05f), 0.92f, 0.15f);
            Mat("ShadowBlack", new Color(0.015f, 0.015f, 0.02f), 0.2f);
            Mat("Litter", new Color(0.5f, 0.47f, 0.4f), 0.3f);

            // dinosaurs
            Mat("DinoHide", new Color(0.075f, 0.085f, 0.065f), 0.25f);
            Mat("DinoBelly", new Color(0.13f, 0.135f, 0.1f), 0.25f);
            Mat("DinoSpine", new Color(0.045f, 0.05f, 0.042f), 0.3f);

            // player
            Mat("PlayerKhaki", new Color(0.35f, 0.3f, 0.18f), 0.25f);
            Mat("PlayerDenim", new Color(0.11f, 0.14f, 0.2f), 0.3f);
            Mat("PlayerSkin", new Color(0.55f, 0.4f, 0.31f), 0.35f);
            Mat("PlayerBoot", new Color(0.11f, 0.07f, 0.045f), 0.4f);
            Mat("PlayerHat", new Color(0.45f, 0.36f, 0.2f), 0.3f);
            Mat("PlayerDark", new Color(0.08f, 0.08f, 0.09f), 0.35f);
            Mat("PlayerOlive", new Color(0.2f, 0.22f, 0.13f), 0.3f);

            // emissive
            Emissive("LightWarm", new Color(0.9f, 0.82f, 0.6f), new Color(1.1f, 0.95f, 0.6f), 0.5f);
            Emissive("LightCold", new Color(0.7f, 0.8f, 0.9f), new Color(0.5f, 0.7f, 1f), 0.5f);
            Emissive("ScreenGlow", new Color(0.1f, 0.9f, 0.35f), new Color(0.05f, 0.6f, 0.2f), 0.6f);
            Emissive("HeadlightGlow", new Color(1f, 0.95f, 0.8f), new Color(0.9f, 0.8f, 0.55f), 0.6f);
            Emissive("EyeGlow", new Color(0.9f, 0.95f, 1f), new Color(1.4f, 1.5f, 1.6f), 0.6f);
            Emissive("WarningLight", new Color(0.9f, 0.15f, 0.1f), new Color(1.2f, 0.12f, 0.06f), 0.5f);

            // VHS post material
            CreateVhsMaterial();

            AssetDatabase.SaveAssets();
        }

        static void CreateVhsMaterial()
        {
            string path = MatFolder + "/VhsOverlay.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("AnalogHorror/VHSOverlay");
            if (shader == null)
            {
                Debug.LogWarning("VHS shader not found — is Assets/Shaders/VHSOverlay.shader imported?");
                return;
            }
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mats["VhsOverlay"] = mat;
        }

        static Material Mat(string name, Color color, float gloss = 0.2f, float metal = 0f)
        {
            if (mats.TryGetValue(name, out var cached) && cached != null) return cached;

            string path = MatFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.shader = Shader.Find("Standard");
            mat.SetColor("_Color", color);
            mat.SetFloat("_Glossiness", gloss);
            mat.SetFloat("_Metallic", metal);

            mats[name] = mat;
            return mat;
        }

        static Material Emissive(string name, Color color, Color emission, float gloss = 0.4f)
        {
            var mat = Mat(name, color, gloss);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material M(string name)
        {
            if (mats.TryGetValue(name, out var m) && m != null) return m;
            return Mat(name, new Color(0.5f, 0.5f, 0.5f));
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // ------------------------------------------------------------- lighting

        static void BuildLighting()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.021f;
            RenderSettings.fogColor = new Color(0.035f, 0.045f, 0.055f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.075f, 0.085f, 0.115f);
            RenderSettings.skybox = null;

            var moon = new GameObject("MoonLight");
            moon.transform.SetParent(lightRoot, false);
            moon.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            var light = moon.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.55f, 0.63f, 0.82f);
            light.intensity = 0.34f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.85f;
        }

        // --------------------------------------------------------------- player

        static void BuildPlayer()
        {
            var player = new GameObject("Player");
            player.transform.SetParent(playerRoot, false);
            player.transform.position = new Vector3(0f, 0.15f, 6f);
            player.transform.rotation = Quaternion.identity;
            player.tag = "Player";

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.35f;

            // camera
            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.62f, 0f);

            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 72f;
            cam.nearClipPlane = 0.04f;
            cam.farClipPlane = 320f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.012f, 0.015f, 0.02f);
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();

            // flashlight spot
            var spotGo = new GameObject("FlashlightSpot");
            spotGo.transform.SetParent(camGo.transform, false);
            spotGo.transform.localPosition = new Vector3(0.16f, -0.14f, 0.06f);
            spotGo.transform.localRotation = Quaternion.identity;
            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1f, 0.94f, 0.82f);
            spot.intensity = 2.6f;
            spot.range = 30f;
            spot.spotAngle = 58f;
            spot.innerSpotAngle = 24f;
            spot.shadows = LightShadows.Soft;
            spot.shadowStrength = 0.7f;

            // VHS overlay
            var overlay = camGo.AddComponent<VhsOverlay>();
            if (mats.TryGetValue("VhsOverlay", out var vhsMat) && vhsMat != null)
                overlay.effectMaterial = vhsMat;
            else
                Debug.LogWarning("VHS overlay material missing — screen will have no tape effect.");
            overlay.cameraLabel = "CAM 04  VISITOR CENTER";

            // flashlight battery logic
            var flashlight = camGo.AddComponent<Flashlight>();
            flashlight.spot = spot;

            // controller + body
            var controller = player.AddComponent<FirstPersonController>();
            controller.camHolder = camGo.transform;
            controller.body = PlayerModelBuilder.Build(player.transform);
        }

        static void BuildAmbience()
        {
            var go = new GameObject("AmbientAudio");
            go.transform.SetParent(audioRoot, false);
            go.AddComponent<AmbientAudio>();
        }

        // ---------------------------------------------------------------- save

        static void SaveAndRegisterScene()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            foreach (var s in scenes)
                if (s.path == ScenePath) { found = true; break; }
            if (!found)
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            Selection.activeGameObject = GameObject.Find("Player");
            Debug.Log("<color=lime>Jurassic Horror map built and saved to " + ScenePath +
                      "</color> — close this dialog and press <b>Play</b>. Objective: recover " +
                      "3 power cells (E) and escape through the main gate. Controls: WASD, mouse, " +
                      "Shift run, Ctrl crouch, Space jump, F flashlight, R reload cell, E interact.");
        }
    }
}
