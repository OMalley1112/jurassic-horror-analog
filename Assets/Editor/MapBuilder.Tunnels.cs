using UnityEngine;
using AnalogHorror;

namespace AnalogHorror.EditorTools
{
    public static partial class MapBuilder
    {
        // Shaft: x[-14,-6] z[-10,-4] cut into the ground (ground planes skip it).
        // Stairs drop to y = -4; corridor runs from z=-10 to z=-50 at y=-4..-0.6.

        static void BuildTunnelSystem()
        {
            var concrete = M("ConcreteDark");
            var dirty = M("ConcreteDirty");
            var metal = M("MetalDark");
            var rust = M("MetalRust");
            var puddle = M("Puddle");
            var bone = M("Bone");

            BuildShaftWalls(concrete);
            BuildStairs(dirty);
            BuildCorridor(concrete);
            BuildCorridorDressing(metal, rust, puddle, bone);
        }

        static void BuildShaftWalls(Material wallMat)
        {
            // vertical earth/concrete walls around the opening (y -4 .. 0)
            Box(tunRoot, "Shaft_Wall_W", new Vector3(-14.25f, -2f, -7f),
                new Vector3(0.5f, 4.2f, 6.6f), wallMat);
            Box(tunRoot, "Shaft_Wall_E", new Vector3(-5.75f, -2f, -7f),
                new Vector3(0.5f, 4.2f, 6.6f), wallMat);
            // header beam closing the slot above the corridor mouth
            Box(tunRoot, "Tunnel_HeadBeam", new Vector3(-10f, -0.3f, -10.1f),
                new Vector3(8.6f, 0.7f, 0.7f), wallMat);
        }

        static void BuildStairs(Material stepMat)
        {
            // 12 steps: 8m wide, from lobby floor (y0, z=-4) down to y=-4 (z=-10)
            for (int i = 0; i < 12; i++)
            {
                float topY = -(i + 1) * (4f / 12f);
                float z = -4f - 0.5f * i - 0.25f;
                float bottom = -4.2f;
                float height = topY - bottom;
                Box(tunRoot, "Shaft_Step", new Vector3(-10f, (topY + bottom) * 0.5f, z),
                    new Vector3(8f, height, 0.5f), stepMat);
            }
        }

        static void BuildCorridor(Material wallMat)
        {
            // interior: x[-13,-7], y[-4,-0.6], z[-10,-50]
            Box(tunRoot, "Tunnel_Floor", new Vector3(-10f, -4.15f, -30f),
                new Vector3(8.6f, 0.3f, 41f), wallMat);
            Box(tunRoot, "Tunnel_Ceiling", new Vector3(-10f, -0.75f, -30f),
                new Vector3(8.6f, 0.3f, 41f), wallMat);
            Box(tunRoot, "Tunnel_Wall_W", new Vector3(-13.25f, -2.3f, -30f),
                new Vector3(0.5f, 3.4f, 41f), wallMat);
            Box(tunRoot, "Tunnel_Wall_E", new Vector3(-6.75f, -2.3f, -30f),
                new Vector3(0.5f, 3.4f, 41f), wallMat);
            Box(tunRoot, "Tunnel_Wall_End", new Vector3(-10f, -2.3f, -50.3f),
                new Vector3(8.6f, 3.4f, 0.5f), wallMat);
            // floor trim along the walls (reads as a gutter edge)
            Box(tunRoot, "Tunnel_Trim_W", new Vector3(-12.9f, -3.9f, -30f),
                new Vector3(0.3f, 0.2f, 40f), M("ConcreteDirty"));
            Box(tunRoot, "Tunnel_Trim_E", new Vector3(-7.1f, -3.9f, -30f),
                new Vector3(0.3f, 0.2f, 40f), M("ConcreteDirty"));
        }

        static void BuildCorridorDressing(Material metal, Material rust, Material puddle, Material bone)
        {
            // ---- ceiling pipes ----
            Cyl(tunRoot, "Pipe_1", new Vector3(-12.3f, -1.15f, -30f), 0.28f, 39f, rust,
                new Vector3(90f, 0f, 0f));
            Cyl(tunRoot, "Pipe_2", new Vector3(-7.7f, -1.1f, -30f), 0.2f, 39f, metal,
                new Vector3(90f, 0f, 0f));
            Cyl(tunRoot, "Pipe_3", new Vector3(-12.3f, -1.55f, -30f), 0.14f, 39f, metal,
                new Vector3(90f, 0f, 0f));

            for (float z = -13f; z >= -47f; z -= 6f)
            {
                Box(tunRoot, "Pipe_Bracket", new Vector3(-12.3f, -0.95f, z),
                    new Vector3(0.5f, 0.12f, 0.12f), metal);
            }

            // ---- caged wall lamps ----
            var lampZ = new[] { -14f, -22f, -30f, -38f, -46f };
            for (int i = 0; i < lampZ.Length; i++)
            {
                // alternate walls
                float x = i % 2 == 0 ? -12.6f : -7.4f;
                Vector3 p = new Vector3(x, -1.5f, lampZ[i]);
                Box(tunRoot, "Lamp_Cage", p, new Vector3(0.3f, 0.4f, 0.3f), metal);
                var bulb = Ball(tunRoot, "Lamp_Bulb", p + new Vector3(i % 2 == 0 ? 0.14f : -0.14f, 0f, 0f),
                    new Vector3(0.2f, 0.2f, 0.2f), i == 3 ? M("WarningLight") : M("LightWarm"));
                StripCollider(bulb);

                bool flicker = (i == 1 || i == 3);
                var lightGo = NewPointLight(p + new Vector3(i % 2 == 0 ? 0.5f : -0.5f, 0.2f, 0f),
                    i == 3 ? new Color(1f, 0.25f, 0.15f) : new Color(1f, 0.85f, 0.6f),
                    1.3f, 11f, flicker);
                lightGo.transform.SetParent(tunRoot, true);
                if (flicker)
                {
                    var fl = lightGo.GetComponent<FlickeringLight>();
                    if (fl != null) fl.blackoutChance = 0.012f;
                }
            }

            // ---- puddles ----
            for (int i = 0; i < 6; i++)
            {
                var p = FlatQuad(tunRoot, "Tunnel_Puddle",
                    new Vector3(rf(-11.6f, -8.4f), -3.98f, rf(-47f, -13f)),
                    new Vector2(rf(0.8f, 2f), rf(1.2f, 3f)), puddle, rf(-30f, 30f));
                StripCollider(p);
            }

            // ---- broken security grate (passable on the right half) ----
            for (float x = -13.6f; x <= -10.6f; x += 0.3f)
            {
                float bend = rf(-6f, 6f);
                Box(tunRoot, "Grate_Bar", new Vector3(x, -2.3f, -44f),
                    new Vector3(0.05f, 3.2f, 0.05f), rust, new Vector3(0f, 0f, bend));
            }
            // a couple of bent, torn-out bars on the floor
            Box(tunRoot, "Grate_BrokenBar", new Vector3(-7.6f, -3.95f, -43.4f),
                new Vector3(0.05f, 0.05f, 1.6f), rust, new Vector3(0f, 40f, 0f));
            Box(tunRoot, "Grate_BrokenBar", new Vector3(-8.4f, -3.95f, -44.6f),
                new Vector3(0.05f, 0.05f, 1.9f), rust, new Vector3(0f, -25f, 0f));

            // ---- end door + warning light ----
            Box(tunRoot, "BunkerDoor", new Vector3(-10f, -2.7f, -49.95f),
                new Vector3(2.4f, 2.6f, 0.18f), metal);
            Box(tunRoot, "BunkerDoor_Frame", new Vector3(-10f, -1.3f, -49.9f),
                new Vector3(3f, 0.3f, 0.3f), rust);
            Box(tunRoot, "BunkerDoor_Handle", new Vector3(-9.2f, -2.7f, -49.8f),
                new Vector3(0.1f, 0.5f, 0.1f), M("MetalGray"));
            var warnBulb = Ball(tunRoot, "BunkerDoor_WarnBulb", new Vector3(-10f, -1.1f, -49.7f),
                new Vector3(0.24f, 0.24f, 0.24f), M("WarningLight"));
            StripCollider(warnBulb);

            var warnLight = NewPointLight(new Vector3(-10f, -1.1f, -49.3f),
                new Color(1f, 0.2f, 0.12f), 1.6f, 9f, false);
            warnLight.transform.SetParent(tunRoot, true);
            var warnFlicker = warnLight.AddComponent<FlickeringLight>();
            warnFlicker.baseIntensity = 1.6f;
            warnFlicker.flickerAmount = 0.85f;
            warnFlicker.buzzSpeed = 3.2f;
            warnFlicker.blackoutChance = 0.001f;

            Sign("SECTOR B\nNO ENTRY", tunRoot, "Sign_SectorB",
                new Vector3(-10f, -1.05f, -49.6f), 2.6f, 1f,
                M("SignRed"), new Color(0.85f, 0.85f, 0.8f),
                FaceToward(Vector3.forward), 0.2f);

            // ---- bones in the dark ----
            for (int i = 0; i < 6; i++)
            {
                Capsule(tunRoot, "Tunnel_Bone",
                    new Vector3(rf(-11.8f, -8.2f), -3.9f, rf(-46f, -16f)),
                    rf(0.08f, 0.14f), rf(0.5f, 0.9f), bone,
                    new Vector3(90f, rf(0f, 360f), 0f));
            }
        }

        // ------------------------------------------------------ shared helper

        /// <summary>Creates a world-space point light GO (unparented — caller parents it).</summary>
        static GameObject NewPointLight(Vector3 worldPos, Color color, float intensity,
            float range, bool flicker)
        {
            var go = new GameObject(flicker ? "PointLight_Flicker" : "PointLight");
            go.transform.position = worldPos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;

            if (flicker)
            {
                var fl = go.AddComponent<FlickeringLight>();
                fl.baseIntensity = intensity;
                fl.flickerAmount = 0.6f;
            }
            return go;
        }
    }
}
