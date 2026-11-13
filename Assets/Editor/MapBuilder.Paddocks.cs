using System.Collections.Generic;
using UnityEngine;
using AnalogHorror;

namespace AnalogHorror.EditorTools
{
    public static partial class MapBuilder
    {
        // T-Rex paddock:  x[-75,-25] z[8,52]   — huge breach on east fence (z≈31)
        // Raptor paddock: x[25,65]   z[30,70]  — gate on west fence (z≈50) + breach north

        static void BuildPaddocks()
        {
            BuildTRexPaddock();
            BuildRaptorPaddock();
        }

        // ------------------------------------------------------------- fences

        static void FenceRect(string tag, float x0, float z0, float x1, float z1,
            float height, int railCount, float postSize, float railSize,
            Material postMat, Material railMat,
            float gapSide_Axis /* which side: use strings instead */, string gapAt,
            float gapCenter, float gapWidth, float damageChance, bool barbArms)
        {
            // side definitions (a → b)
            Vector3[] a =
            {
                new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1),   // S, E
                new Vector3(x1, 0f, z0), new Vector3(x0, 0f, z0),   // N, W
            };
            Vector3[] b =
            {
                new Vector3(x1, 0f, z1), new Vector3(x1, 0f, z0),   // S, E
                new Vector3(x0, 0f, z0), new Vector3(x0, 0f, z1),   // N, W
            };
            string[] names = { "S", "E", "N", "W" };

            for (int s = 0; s < 4; s++)
            {
                float t0 = -1f, t1 = -1f;
                if (names[s] == gapAt)
                {
                    float len = Vector3.Distance(a[s], b[s]);
                    float half = gapWidth * 0.5f;
                    float centerT;
                    switch (names[s])
                    {
                        case "S": centerT = (gapCenter - x0) / (x1 - x0); break;
                        case "N": centerT = (x1 - gapCenter) / (x1 - x0); break;
                        case "E": centerT = (z1 - gapCenter) / (z1 - z0); break;
                        default:  centerT = (gapCenter - z0) / (z1 - z0); break;
                    }
                    t0 = centerT - half / len;
                    t1 = centerT + half / len;
                }

                FenceLine(tag + "_" + names[s], a[s], b[s], height, railCount,
                    postSize, railSize, postMat, railMat, t0, t1, damageChance, barbArms);
            }
        }

        static void FenceLine(string tag, Vector3 a, Vector3 b, float height, int railCount,
            float postSize, float railSize, Material postMat, Material railMat,
            float gapT0, float gapT1, float damageChance, bool barbArms)
        {
            Vector3 ab = b - a;
            float len = ab.magnitude;
            float yaw = Mathf.Atan2(ab.x, ab.z) * Mathf.Rad2Deg;
            int posts = Mathf.Max(2, Mathf.RoundToInt(len / 3f));

            bool InGap(float t) => t > gapT0 && t < gapT1;

            var postPos = new List<Vector3>();
            var postT = new List<float>();
            for (int i = 0; i <= posts; i++)
            {
                float t = i / (float)posts;
                if (InGap(t)) continue;

                Vector3 p = a + ab * t;
                postPos.Add(p);
                postT.Add(t);

                bool damaged = chance(damageChance);
                Vector3 euler = damaged
                    ? new Vector3(rf(-9f, 9f), yaw, rf(-9f, 9f))
                    : new Vector3(0f, yaw, 0f);
                float sink = damaged ? rf(0.1f, 0.3f) : 0f;
                Vector3 drift = damaged ? new Vector3(rf(-0.2f, 0.2f), 0f, rf(-0.2f, 0.2f)) : Vector3.zero;

                Box(padRoot, tag + "_Post", p + drift + Vector3.up * (height * 0.5f - sink),
                    new Vector3(postSize, height, postSize), postMat, euler);

                if (barbArms && i % 2 == 0 && !damaged)
                {
                    // angled arm along the top of the fence
                    Vector3 inward = Vector3.Cross(ab.normalized, Vector3.up); // either side is fine visually
                    Box(padRoot, tag + "_Arm",
                        p + Vector3.up * (height + 0.18f) + inward * 0.12f,
                        new Vector3(0.07f, 0.55f, 0.07f), postMat,
                        new Vector3(24f, yaw, 0f));
                }
            }

            // rails between consecutive standing posts
            for (int level = 0; level < railCount; level++)
            {
                float rh = railCount == 1
                    ? height * 0.65f
                    : 0.45f + level * (height - 0.75f) / (railCount - 1);

                for (int i = 0; i < postPos.Count - 1; i++)
                {
                    // never bridge the gate opening
                    if (InGap((postT[i] + postT[i + 1]) * 0.5f)) continue;
                    float segLen = Vector3.Distance(postPos[i], postPos[i + 1]);
                    if (chance(damageChance * 1.6f)) continue;   // missing rail

                    Vector3 mid = (postPos[i] + postPos[i + 1]) * 0.5f;
                    float segYaw = Mathf.Atan2((postPos[i + 1] - postPos[i]).x,
                                               (postPos[i + 1] - postPos[i]).z) * Mathf.Rad2Deg;
                    bool bent = chance(damageChance * 0.8f);
                    Vector3 euler = bent
                        ? new Vector3(rf(-7f, 7f), segYaw, rf(-12f, 12f))
                        : new Vector3(0f, segYaw, 0f);

                    Box(padRoot, tag + "_Rail",
                        mid + Vector3.up * (bent ? rh * 0.85f : rh),
                        new Vector3(railSize, railSize, segLen * 1.04f), railMat, euler);
                }
            }

            // a fallen rail lying on the ground near the fence
            if (chance(0.6f))
            {
                Vector3 mid = (a + b) * 0.5f + ab.normalized * rf(-len * 0.3f, len * 0.3f);
                Vector3 inward = Vector3.Cross(ab.normalized, Vector3.up).normalized;
                Box(padRoot, tag + "_FallenRail",
                    mid + inward * rf(0.6f, 1.8f) + Vector3.up * (railSize * 0.5f),
                    new Vector3(railSize, railSize, rf(2.5f, 4.5f)), railMat,
                    new Vector3(rf(-4f, 4f), yaw + rf(-25f, 25f), rf(-4f, 4f)));
            }
        }

        static void BuildGatePanel(Vector3 hingePos, float yaw, float width, float height,
            Material frameMat, Material meshMat)
        {
            // simple swing-open gate: frame + two cross bars, rotated off its hinges
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 outDir = rot * Vector3.right;

            Vector3 center = hingePos + outDir * (width * 0.5f) + Vector3.up * (height * 0.5f);
            var pieces = new System.Collections.Generic.List<GameObject>();

            pieces.Add(Box(padRoot, "Gate_Top", center + Vector3.up * (height * 0.5f - 0.1f),
                new Vector3(width, 0.12f, 0.12f), frameMat, new Vector3(0f, yaw, 0f)));
            pieces.Add(Box(padRoot, "Gate_Bottom", center - Vector3.up * (height * 0.5f - 0.1f),
                new Vector3(width, 0.12f, 0.12f), frameMat, new Vector3(0f, yaw, 0f)));
            for (int i = 0; i <= 3; i++)
            {
                Vector3 p = hingePos + outDir * (i * width / 3f) + Vector3.up * (height * 0.5f);
                pieces.Add(Box(padRoot, "Gate_Vert", p, new Vector3(0.1f, height, 0.1f), frameMat,
                    new Vector3(0f, yaw, 0f)));
            }
            // mesh panels (thin plates)
            var panel = Box(padRoot, "Gate_Mesh", center,
                new Vector3(width - 0.2f, height - 0.4f, 0.03f), meshMat,
                new Vector3(0f, yaw, 0f));
            StripCollider(panel);
            pieces.Add(panel);

            // hung on its hinges — the wind works it back and forth, slowly
            var hinge = Pivot(padRoot, "Gate_Hinge", hingePos);
            foreach (var p in pieces) p.transform.SetParent(hinge, true);

            var sw = hinge.gameObject.AddComponent<Swing>();
            sw.amplitude = 1.6f;
            sw.speed = 0.18f;               // one lazy rock every ~5.5 s
            sw.axis = Vector3.up;
            sw.phase = rf(0f, 6.28f);
            sw.wobbleAmount = 0.5f;
        }

        // --------------------------------------------------------- T-Rex paddock

        static void BuildTRexPaddock()
        {
            var postMat = M("MetalRust");
            var railMat = M("MetalGray");

            // massive breach on the east fence (facing the road)
            FenceRect("TRex_Fence", -75f, 8f, -25f, 52f,
                4.2f, 4, 0.28f, 0.16f, postMat, railMat,
                0f, "E", 31f, 10f, 0.06f, true);

            // extra carnage around the breach: leaning posts and torn rails
            for (int i = 0; i < 4; i++)
            {
                float z = 25.5f + i * 3.6f;
                Box(padRoot, "TRex_BreachLeanPost", new Vector3(-25.4f, 1.7f, z),
                    new Vector3(0.28f, 4.4f, 0.28f), postMat,
                    new Vector3(rf(-28f, -8f), 12f, rf(-14f, 14f)));
            }
            for (int i = 0; i < 3; i++)
            {
                Box(padRoot, "TRex_BreachRail",
                    new Vector3(-26.6f - i * 0.5f, 0.9f + i * 0.7f, 27f + i * 3.5f),
                    new Vector3(0.16f, 0.16f, rf(3f, 5f)), railMat,
                    new Vector3(rf(-30f, 30f), rf(20f, 70f), rf(-40f, 40f)));
            }

            // gate posts flanking the breach
            Box(padRoot, "TRex_GatePost_A", new Vector3(-25f, 2.2f, 26f),
                new Vector3(0.45f, 4.4f, 0.45f), postMat);
            Box(padRoot, "TRex_GatePost_B", new Vector3(-25f, 2.2f, 36f),
                new Vector3(0.45f, 4.4f, 0.45f), postMat);

            // big road-facing warning sign
            Sign("PENS 01\nT. REX\nEXTREME DANGER", padRoot, "Sign_TRex",
                new Vector3(-24.2f, 2.4f, 22f), 3f, 2.6f,
                M("SignRed"), new Color(0.9f, 0.88f, 0.8f),
                FaceToward(Vector3.right), 0.24f, 2f);

            Sign("DANGER  HIGH VOLTAGE", padRoot, "Sign_TRex_HV",
                new Vector3(-48f, 2.6f, 7.7f), 4.4f, 0.9f,
                M("SignYellow"), new Color(0.05f, 0.05f, 0.05f),
                FaceToward(Vector3.back), 0.22f, -2f);

            // warning lamp on the north-west corner
            BuildWarningLamp(new Vector3(-74f, 0f, 9f));

            // drag marks + giant footprints trailing out of the breach
            var dark = M("ShadowBlack");
            var drag = Box(padRoot, "TRex_DragMark", new Vector3(-30f, 0.045f, 34f),
                new Vector3(0.7f, 0.02f, 7f), dark, new Vector3(0f, 28f, 0f));
            StripCollider(drag);
            var drag2 = Box(padRoot, "TRex_DragMark", new Vector3(-38f, 0.045f, 41f),
                new Vector3(0.6f, 0.02f, 6f), dark, new Vector3(0f, 40f, 0f));
            StripCollider(drag2);

            Vector3[] prints =
            {
                new Vector3(-27f, 0.05f, 33f),
                new Vector3(-31f, 0.05f, 37f),
                new Vector3(-35f, 0.05f, 41f),
                new Vector3(-40f, 0.05f, 45f),
                new Vector3(-30f, 0.05f, 27f),
            };
            for (int i = 0; i < prints.Length; i++)
            {
                var footprint = Ball(padRoot, "TRex_Print", prints[i],
                    new Vector3(1.15f, 0.07f, 2f), dark, new Vector3(0f, rf(-40f, 40f), 0f));
                StripCollider(footprint);
            }

            BuildWatchtower(new Vector3(-70f, 0f, 48f));
            BuildBonePile(padRoot, new Vector3(-56f, 0f, 20f), 1.6f, 42f);
            BuildInteriorScrub(-75f, 8f, -25f, 52f, 46);
        }

        // -------------------------------------------------------- raptor paddock

        static void BuildRaptorPaddock()
        {
            var postMat = M("MetalGray");
            var railMat = M("ChainLink");

            // main gate (west fence) + a torn breach on the north fence
            FenceRect("Raptor_Fence", 25f, 30f, 65f, 70f,
                3.1f, 3, 0.16f, 0.12f, postMat, railMat,
                0f, "W", 50f, 5f, 0.05f, false);

            FenceRect("Raptor_Breach", 25f, 30f, 65f, 70f,
                3.1f, 3, 0.16f, 0.12f, postMat, railMat,
                0f, "N", 45f, 3.5f, 0.05f, false);

            // gate posts + swung-open gate panel
            Box(padRoot, "Raptor_GatePost_A", new Vector3(25f, 1.7f, 47.4f),
                new Vector3(0.3f, 3.4f, 0.3f), postMat);
            Box(padRoot, "Raptor_GatePost_B", new Vector3(25f, 1.7f, 52.6f),
                new Vector3(0.3f, 3.4f, 0.3f), postMat);
            BuildGatePanel(new Vector3(25f, 0f, 47.6f), 62f, 4.8f, 2.8f, postMat, railMat);

            // bent fence section at the north breach
            for (int i = 0; i < 3; i++)
            {
                Box(padRoot, "Raptor_BreachBar", new Vector3(43.6f + i * 0.8f, 1.1f, 30.4f),
                    new Vector3(0.12f, 2.6f, 0.12f), railMat,
                    new Vector3(rf(-35f, -15f), 8f, rf(-20f, 20f)));
            }

            Sign("PENS 04\nVELOCIRAPTOR\nDANGER", padRoot, "Sign_Raptor",
                new Vector3(24.2f, 1.9f, 43f), 2.8f, 2.1f,
                M("SignYellow"), new Color(0.05f, 0.05f, 0.05f),
                FaceToward(Vector3.left), 0.22f, -2.5f);

            BuildWarningLamp(new Vector3(25.6f, 0f, 46.6f));
            BuildRaptorNest(new Vector3(56f, 0f, 62f));
            BuildBonePile(padRoot, new Vector3(33f, 0f, 64f), 0.9f, 17f);
            BuildMaintenanceHut(new Vector3(30f, 0f, 35f));
            BuildInteriorScrub(25f, 30f, 65f, 70f, 52);
        }

        // --------------------------------------------------------------- pieces

        static void BuildWarningLamp(Vector3 basePos)
        {
            Cyl(padRoot, "WarnLamp_Post", basePos + Vector3.up * 1.6f, 0.14f, 3.2f, M("MetalDark"));
            var bulb = Ball(padRoot, "WarnLamp_Bulb", basePos + Vector3.up * 3.3f,
                new Vector3(0.3f, 0.3f, 0.3f), M("WarningLight"));
            StripCollider(bulb);

            var lightGo = NewPointLight(basePos + Vector3.up * 3.4f,
                new Color(1f, 0.22f, 0.14f), 1.4f, 10f, true);
            lightGo.transform.SetParent(padRoot, true);
            var fl = lightGo.GetComponent<FlickeringLight>();
            if (fl != null) fl.buzzSpeed = 2.4f;
        }

        static void BuildWatchtower(Vector3 pos)
        {
            var wood = M("WoodDark");
            var metal = M("MetalGray");

            // legs
            for (int i = 0; i < 4; i++)
            {
                float dx = (i & 1) == 0 ? -1.6f : 1.6f;
                float dz = (i & 2) == 0 ? -1.6f : 1.6f;
                Box(padRoot, "Tower_Leg", pos + new Vector3(dx, 3f, dz),
                    new Vector3(0.28f, 6f, 0.28f), wood,
                    new Vector3(dz * 1.6f, 0f, -dx * 1.6f));
            }
            // braces
            Box(padRoot, "Tower_Brace", pos + new Vector3(0f, 2.6f, -1.6f),
                new Vector3(3.4f, 0.18f, 0.18f), wood, new Vector3(0f, 0f, 45f));
            Box(padRoot, "Tower_Brace", pos + new Vector3(0f, 2.6f, 1.6f),
                new Vector3(3.4f, 0.18f, 0.18f), wood, new Vector3(0f, 0f, -45f));

            // platform + railing
            Box(padRoot, "Tower_Deck", pos + Vector3.up * 6.1f,
                new Vector3(4f, 0.25f, 4f), M("WoodPlank"));
            for (int i = 0; i < 8; i++)
            {
                Vector3 off = new Vector3(
                    i < 4 ? -1.85f : 1.85f, 0f,
                    -1.85f + (i % 4) * 1.23f);
                if (i == 4) continue;   // gap where the ladder arrives
                Box(padRoot, "Tower_RailPost", pos + off + Vector3.up * 6.75f,
                    new Vector3(0.1f, 1.1f, 0.1f), metal);
            }
            Box(padRoot, "Tower_Rail", pos + new Vector3(-1.85f, 7.3f, 0f),
                new Vector3(0.1f, 0.1f, 4f), metal);
            Box(padRoot, "Tower_Rail", pos + new Vector3(0f, 7.3f, -1.85f),
                new Vector3(4f, 0.1f, 0.1f), metal);
            Box(padRoot, "Tower_Rail", pos + new Vector3(1.85f, 7.3f, 0f),
                new Vector3(0.1f, 0.1f, 4f), metal);

            // ladder
            for (int i = 0; i < 11; i++)
            {
                Box(padRoot, "Tower_LadderRung", pos + new Vector3(0f, 0.5f + i * 0.55f, 2.1f),
                    new Vector3(1.1f, 0.09f, 0.09f), metal);
            }
            Box(padRoot, "Tower_LadderRail", pos + new Vector3(-0.55f, 3.1f, 2.1f),
                new Vector3(0.1f, 6.4f, 0.1f), metal);
            Box(padRoot, "Tower_LadderRail", pos + new Vector3(0.55f, 3.1f, 2.1f),
                new Vector3(0.1f, 6.4f, 0.1f), metal);

            // roof + floodlight aimed at the breach
            for (int i = 0; i < 4; i++)
            {
                float dx = (i & 1) == 0 ? -1.7f : 1.7f;
                float dz = (i & 2) == 0 ? -1.7f : 1.7f;
                Box(padRoot, "Tower_RoofPost", pos + new Vector3(dx, 7.15f, dz),
                    new Vector3(0.12f, 1.6f, 0.12f), metal);
            }
            Box(padRoot, "Tower_Roof", pos + new Vector3(0f, 8.1f, 0f),
                new Vector3(4.6f, 0.2f, 4.6f), M("RoofMetal"), new Vector3(0f, 0f, 4f));

            var spotGo = new GameObject("Tower_Floodlight");
            spotGo.transform.SetParent(padRoot, false);
            spotGo.transform.position = pos + new Vector3(1.2f, 7.6f, 1.6f);
            spotGo.transform.rotation = Quaternion.Euler(34f, 130f, 0f);
            var light = spotGo.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(0.95f, 0.9f, 0.75f);
            light.intensity = 2.6f;
            light.range = 34f;
            light.spotAngle = 70f;
            light.shadows = LightShadows.Soft;
            var fl = spotGo.AddComponent<FlickeringLight>();
            fl.baseIntensity = 2.6f;
            fl.flickerAmount = 0.45f;
        }

        static void BuildMaintenanceHut(Vector3 pos)
        {
            var wood = M("WoodPlank");
            var roof = M("RoofMetal");
            float yaw = -14f;
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

            // 6 x 5 walls with a doorway on the west (road) side
            Box(padRoot, "Hut_Wall_N", pos + rot * new Vector3(0f, 1.5f, -2.5f),
                new Vector3(6.2f, 3f, 0.2f), wood, new Vector3(0f, yaw, 0f));
            Box(padRoot, "Hut_Wall_S", pos + rot * new Vector3(0f, 1.5f, 2.5f),
                new Vector3(6.2f, 3f, 0.2f), wood, new Vector3(0f, yaw, 0f));
            Box(padRoot, "Hut_Wall_E", pos + rot * new Vector3(3f, 1.5f, 0f),
                new Vector3(0.2f, 3f, 5.2f), wood, new Vector3(0f, yaw, 0f));
            // west wall split around the door
            Box(padRoot, "Hut_Wall_W_L", pos + rot * new Vector3(-3f, 1.5f, -1.9f),
                new Vector3(0.2f, 3f, 1.6f), wood, new Vector3(0f, yaw, 0f));
            Box(padRoot, "Hut_Wall_W_R", pos + rot * new Vector3(-3f, 1.5f, 1.9f),
                new Vector3(0.2f, 3f, 1.6f), wood, new Vector3(0f, yaw, 0f));
            Box(padRoot, "Hut_DoorHeader", pos + rot * new Vector3(-3f, 2.7f, 0f),
                new Vector3(0.2f, 0.6f, 2.4f), wood, new Vector3(0f, yaw, 0f));

            // slanted corrugated roof
            Box(padRoot, "Hut_Roof", pos + rot * new Vector3(0f, 3.35f, 0f),
                new Vector3(6.8f, 0.18f, 5.8f), roof, new Vector3(7f, yaw, 0f));
            // a hole: one panel slid aside
            Box(padRoot, "Hut_RoofPanel_Slipped", pos + rot * new Vector3(1.4f, 3.5f, -1.2f),
                new Vector3(2.4f, 0.14f, 2f), roof, new Vector3(14f, yaw + 9f, 6f));

            // interior: desk, stool, lamp
            Vector3 desk = pos + rot * new Vector3(1.8f, 0f, -1.6f);
            Box(padRoot, "Hut_Desk", desk + new Vector3(0f, 0.75f, 0f),
                new Vector3(1.8f, 0.1f, 0.8f), M("WoodDark"), new Vector3(0f, yaw, 0f));
            Box(padRoot, "Hut_DeskLeg", desk + new Vector3(0f, 0.37f, 0f),
                new Vector3(1.5f, 0.75f, 0.6f), M("WoodDark"), new Vector3(0f, yaw, 0f));
            var deskLamp = Ball(padRoot, "Hut_DeskLamp", desk + new Vector3(0f, 0.95f, 0f),
                new Vector3(0.2f, 0.2f, 0.2f), M("LightWarm"));
            StripCollider(deskLamp);

            var lampLight = NewPointLight(desk + new Vector3(0f, 1.1f, 0f),
                new Color(1f, 0.8f, 0.5f), 1.2f, 7f, true);
            lampLight.transform.SetParent(padRoot, true);

            // clutter
            for (int i = 0; i < 4; i++)
            {
                Box(padRoot, "Hut_Clutter",
                    pos + rot * new Vector3(rf(-2.4f, 2.4f), rf(0.15f, 0.5f), rf(-1.8f, 1.8f)),
                    new Vector3(rf(0.4f, 1f), rf(0.3f, 0.9f), rf(0.4f, 1f)),
                    M("Crate"), new Vector3(rf(-8f, 8f), rf(0f, 360f), rf(-8f, 8f)));
            }
        }

        static void BuildRaptorNest(Vector3 pos)
        {
            var nestMat = M("Trunk");
            var eggMat = M("Egg");

            // ring of leaf litter
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                var clump = Ball(padRoot, "Nest_Rim",
                    pos + new Vector3(Mathf.Cos(a) * 1.6f, 0.2f, Mathf.Sin(a) * 1.6f),
                    new Vector3(1.1f, 0.5f, 1.1f), nestMat);
                StripCollider(clump);
            }

            // eggs (a couple hatched / broken)
            Vector3[] eggs =
            {
                new Vector3(-0.4f, 0.35f, 0.3f), new Vector3(0.4f, 0.35f, -0.2f),
                new Vector3(0.1f, 0.35f, 0.7f), new Vector3(-0.7f, 0.35f, -0.5f),
                new Vector3(0.8f, 0.35f, 0.6f),
            };
            for (int i = 0; i < eggs.Length; i++)
            {
                var egg = Ball(padRoot, "Egg", pos + eggs[i],
                    new Vector3(0.34f, 0.44f, 0.34f), eggMat,
                    new Vector3(rf(-12f, 12f), rf(0f, 360f), rf(-12f, 12f)));
                StripCollider(egg);
            }
            // broken shell halves
            for (int i = 0; i < 2; i++)
            {
                var shell = Ball(padRoot, "EggShell", pos + new Vector3(rf(-1.6f, 1.6f), 0.14f, rf(-1.6f, 1.6f)),
                    new Vector3(0.36f, 0.2f, 0.36f), eggMat, new Vector3(rf(-25f, 25f), rf(0f, 360f), 0f));
                StripCollider(shell);
            }

            Sign("DO NOT FEED", padRoot, "Sign_Nest",
                new Vector3(54f, 1.4f, 58.6f), 1.8f, 0.7f,
                M("SignYellow"), new Color(0.05f, 0.05f, 0.05f),
                FaceToward(new Vector3(-0.4f, 0f, -0.9f)), 0.16f, 5f);
        }

        static void BuildBonePile(Transform parent, Vector3 pos, float scale, float yaw)
        {
            var bone = M("Bone");
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

            // spine
            for (int i = 0; i < 8; i++)
            {
                Capsule(parent, "Bone_Spine", pos + rot * new Vector3(i * 0.55f * scale - 2f * scale, 0.35f * scale, 0f),
                    0.16f * scale, 0.6f * scale, bone, new Vector3(0f, yaw, 90f));
            }
            // ribs
            for (int i = 0; i < 6; i++)
            {
                float x = i * 0.5f * scale - 1.5f * scale;
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(parent, "Bone_Rib", pos + rot * new Vector3(x, 0.75f * scale, side * 0.5f * scale),
                        new Vector3(0.1f * scale, 1.1f * scale, 0.1f * scale), bone,
                        new Vector3(side * 38f, yaw, 0f));
                }
            }
            // skull
            var skull = Box(parent, "Bone_Skull", pos + rot * new Vector3(-2.6f * scale, 0.5f * scale, 0.1f),
                new Vector3(0.9f * scale, 0.6f * scale, 0.7f * scale), bone,
                new Vector3(rf(-8f, 8f), yaw + 24f, rf(-8f, 8f)));
            Box(parent, "Bone_Jaw", pos + rot * new Vector3(-3.1f * scale, 0.25f * scale, 0.1f),
                new Vector3(0.9f * scale, 0.18f * scale, 0.5f * scale), bone,
                new Vector3(0f, yaw + 24f, rf(-10f, 10f)));

            // scattered bones
            for (int i = 0; i < 7; i++)
            {
                Capsule(parent, "Bone_Scatter",
                    pos + new Vector3(rf(-3f, 3.5f) * scale, 0.08f, rf(-2.5f, 2.5f) * scale),
                    rf(0.1f, 0.16f) * scale, rf(0.5f, 1f) * scale, bone,
                    new Vector3(90f, rf(0f, 360f), 0f));
            }
        }

        /// <summary>Bushes + grass clumps so paddocks feel overgrown.</summary>
        static void BuildInteriorScrub(float x0, float z0, float x1, float z1, int count)
        {
            var leafDark = M("LeafDark");
            var leafMid = M("LeafMid");

            for (int i = 0; i < count; i++)
            {
                float x = rf(x0 + 1.5f, x1 - 1.5f);
                float z = rf(z0 + 1.5f, z1 - 1.5f);
                float s = rf(0.7f, 1.9f);
                var bush = Ball(padRoot, "Scrub", new Vector3(x, s * 0.28f, z),
                    new Vector3(s, s * rf(0.5f, 0.8f), s),
                    chance(0.5f) ? leafDark : leafMid,
                    new Vector3(0f, rf(0f, 360f), 0f));
                StripCollider(bush);
            }
        }
    }
}
