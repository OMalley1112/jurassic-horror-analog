using UnityEditor;
using UnityEngine;
using AnalogHorror;

namespace AnalogHorror.EditorTools
{
    public static partial class MapBuilder
    {
        // ======================================================= HORROR LAYER
        // Director + tension + storm + dread events, plus the set dressing
        // that sells them: sweeping emergency beacons, moonlight shafts with
        // drifting dust, and cave reverb under the maintenance tunnels.

        static void BuildHorrorSystems()
        {
            var sysRoot = NewRoot("_HORROR SYSTEMS");

            var go = new GameObject("HorrorDirector");
            go.transform.SetParent(sysRoot, false);

            var director = go.AddComponent<HorrorDirector>();
            var tension = go.AddComponent<TensionSystem>();
            var storm = go.AddComponent<StormController>();
            var dread = go.AddComponent<DreadEvents>();

            // ---- find the player rig ----
            var playerGo = GameObject.Find("Player");
            if (playerGo == null)
            {
                Debug.LogWarning("Horror systems: Player not found — wiring skipped.");
                return;
            }

            var controller = playerGo.GetComponent<FirstPersonController>();
            var camGo = controller != null ? controller.camHolder : null;
            var overlay = camGo != null ? camGo.GetComponent<VhsOverlay>() : null;
            var flashlight = camGo != null ? camGo.GetComponent<Flashlight>() : null;
            var ambience = Object.FindObjectOfType<AmbientAudio>();

            director.player = playerGo.transform;
            director.spawnPoint = playerGo.transform.position;
            director.overlay = overlay;
            director.controller = controller;
            director.flashlight = flashlight;
            director.ambience = ambience;

            tension.director = director;
            tension.player = director.player;
            dread.director = director;
            storm.director = director;

            // ---- moon + every practical light the storm can surge ----
            foreach (var l in Object.FindObjectsOfType<Light>())
            {
                if (l.type == LightType.Directional && l.gameObject.name == "MoonLight")
                {
                    storm.moon = l;
                    continue;
                }
                if (playerRoot != null && l.transform.IsChildOf(playerRoot))
                    continue;                       // never touch the flashlight
                lightList.Add(l);
            }
            storm.flickerLights = lightList.ToArray();
            lightList.Clear();

            // ---- set dressing ----
            Emissive("BeaconRed", new Color(0.85f, 0.09f, 0.06f),
                new Color(1.6f, 0.12f, 0.06f), 0.4f);

            BuildBeacon(new Vector3(-70f, 8.34f, 48f), BeaconRotator.Mode.Sweep,
                46f, 150f, sysRoot);                              // watchtower
            BuildBeacon(new Vector3(0f, 8.24f, 22f), BeaconRotator.Mode.Spin,
                70f, 0f, sysRoot);                               // gate arch
            BuildBeacon(new Vector3(13f, 7.42f, -8.5f), BeaconRotator.Mode.Strobe,
                0f, 0f, sysRoot);                                // visitor roof

            BuildMoonShafts();
            BuildDustMotes();
            BuildTunnelReverbZone(sysRoot);
        }

        static readonly System.Collections.Generic.List<Light> lightList =
            new System.Collections.Generic.List<Light>();

        // ------------------------------------------------------------- beacon

        static void BuildBeacon(Vector3 pos, BeaconRotator.Mode mode, float speed,
            float arc, Transform parent)
        {
            var baseGo = Box(parent, "Beacon_Base", pos,
                new Vector3(0.6f, 0.2f, 0.6f), M("MetalDark"));
            StripCollider(baseGo);

            var head = new GameObject("Beacon_Head");
            head.transform.SetParent(parent, false);
            head.transform.position = pos + Vector3.up * 0.28f;

            var bulb = Ball(head.transform, "Beacon_Bulb", Vector3.zero,
                new Vector3(0.36f, 0.3f, 0.36f), M("BeaconRed"));
            StripCollider(bulb);

            var cage = Cyl(head.transform, "Beacon_Cage", Vector3.zero,
                0.44f, 0.42f, M("MetalDark"));
            StripCollider(cage);

            var lightGo = new GameObject("Beacon_Light");
            lightGo.transform.SetParent(head.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0f, 0.2f);
            lightGo.transform.localRotation = Quaternion.identity;

            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = new Color(1f, 0.22f, 0.14f);
            l.intensity = 4.5f;
            l.range = 46f;
            l.spotAngle = 62f;
            l.innerSpotAngle = 26f;
            l.shadows = LightShadows.None;

            var rot = head.AddComponent<BeaconRotator>();
            rot.mode = mode;
            rot.speed = speed;
            rot.arc = arc;
            rot.beaconLight = l;
        }

        // ------------------------------------------------------- moon shafts
        // Translucent volumes hanging under the two roof holes: the "god ray"
        // look without a custom shader. Colliders stripped — you walk through.

        static void BuildMoonShafts()
        {
            var shaft = MatTransparent("MoonShaftVolume",
                new Color(0.62f, 0.72f, 0.95f), 0.045f);

            // main hole (z -5..2) — lands on the T-Rex skeleton
            float[] xsA = { -10f, -4f, 3f, 9.5f };
            foreach (float x in xsA)
            {
                var slab = Box(vcRoot, "LightShaft",
                    new Vector3(x, 3.5f, -1.5f),
                    new Vector3(2.4f, 7f, 5.8f), shaft);
                StripCollider(slab);
            }

            // second hole (z 6..9) over the lobby entrance
            float[] xsB = { -6.5f, 5.5f };
            foreach (float x in xsB)
            {
                var slab = Box(vcRoot, "LightShaft",
                    new Vector3(x, 3.5f, 7.5f),
                    new Vector3(2.8f, 7f, 2.6f), shaft);
                StripCollider(slab);
            }
        }

        static Material MatTransparent(string name, Color color, float alpha)
        {
            string path = MatFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.shader = Shader.Find("Standard");
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            var c = color;
            c.a = alpha;
            mat.SetColor("_Color", c);
            mat.SetFloat("_Glossiness", 0f);
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);

            mats[name] = mat;
            return mat;
        }

        // -------------------------------------------------------- dust motes

        static void BuildDustMotes()
        {
            var go = new GameObject("DustMotes");
            go.transform.SetParent(vcRoot, false);
            go.transform.position = new Vector3(0f, 3.6f, -1.5f);

            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 340;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 13f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.09f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.055f);
            main.startColor = new Color(0.78f, 0.85f, 1f, 0.5f);
            main.gravityModifier = 0f;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.rateOverTime = 42f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(9f, 6.6f, 5.6f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.1f;
            noise.damping = true;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(0.75f, 0.22f),
                        new GradientAlphaKey(0.55f, 0.7f),
                        new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var dustShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (dustShader == null) dustShader = Shader.Find("Sprites/Default");
            var dustMat = new Material(dustShader) { color = new Color(0.8f, 0.87f, 1f, 0.6f) };
            renderer.sharedMaterial = dustMat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingFudge = 2f;
        }

        // ----------------------------------------------------- tunnel reverb

        static void BuildTunnelReverbZone(Transform parent)
        {
            var go = new GameObject("TunnelReverb");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(-10f, -2.3f, -30f);

            var rv = go.AddComponent<TunnelReverb>();
            rv.zoneCenter = Vector3.zero;
            rv.zoneSize = new Vector3(8.2f, 4.4f, 42f);   // covers stairs + corridor
            rv.preset = AudioReverbPreset.Cave;
        }
    }
}
