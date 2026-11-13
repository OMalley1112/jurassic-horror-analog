using UnityEngine;
using AnalogHorror;

namespace AnalogHorror.EditorTools
{
    public static partial class MapBuilder
    {
        /// <summary>World direction helper: euler so a sign's readable side faces that direction.</summary>
        static Vector3 FaceToward(Vector3 dir)
        {
            return Quaternion.LookRotation(-dir.normalized).eulerAngles;
        }

        // ============================================================ VISITOR CENTER
        // footprint x[-17,17] z[-12,12], height 7, entrance on +Z wall

        static void BuildVisitorCenter()
        {
            var concrete = M("Concrete");
            var dirty = M("ConcreteDirty");
            var woodDark = M("WoodDark");
            var roofMat = M("RoofMetal");
            var metal = M("MetalGray");
            var leafMid = M("LeafMid");

            BuildWalls(concrete);
            BuildRoof(roofMat, woodDark);
            BuildEntranceSign();
            BuildLobbyInterior(dirty, woodDark, metal);
            BuildMoonShaft();
            BuildRoofVines(leafMid);

            // collapsed ceiling panels / rubble on the lobby floor
            for (int i = 0; i < 14; i++)
            {
                var slab = Box(vcRoot, "Rubble",
                    new Vector3(rf(-14f, 14f), rf(0.05f, 0.2f), rf(-9f, 10f)),
                    new Vector3(rf(0.5f, 2f), rf(0.1f, 0.3f), rf(0.5f, 1.6f)),
                    chance(0.5f) ? concrete : woodDark,
                    new Vector3(rf(-12f, 12f), rf(0f, 360f), rf(-12f, 12f)));
            }
        }

        static void BuildWalls(Material concrete)
        {
            // ---- solid front wall with doorway (z = +12) ----
            Box(vcRoot, "Wall_Front_L", new Vector3(-9.75f, 3.5f, 12f),
                new Vector3(14.5f, 7f, 0.5f), concrete);
            Box(vcRoot, "Wall_Front_R", new Vector3(9.75f, 3.5f, 12f),
                new Vector3(14.5f, 7f, 0.5f), concrete);
            Box(vcRoot, "Wall_Front_DoorHeader", new Vector3(0f, 5.75f, 12f),
                new Vector3(5f, 2.5f, 0.5f), concrete);

            // ---- windowed (pier) walls ----
            PierWall(new Vector3(-17f, 0f, -12f), new Vector3(-17f, 0f, 12f), concrete);
            PierWall(new Vector3(17f, 0f, -12f), new Vector3(17f, 0f, 12f), concrete);
            PierWall(new Vector3(-17f, 0f, -12f), new Vector3(17f, 0f, -12f), concrete);
        }

        /// <summary>Wall with sill + header strips and piers between — glassless window openings.</summary>
        static void PierWall(Vector3 a, Vector3 b, Material mat)
        {
            const float height = 7f;
            const float thickness = 0.5f;
            const float sill = 1.1f;
            const float headerStart = 4.3f;

            Vector3 ab = b - a;
            float len = ab.magnitude;
            Vector3 mid = (a + b) * 0.5f;
            Vector3 dir = ab / len;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            Vector3 rot = new Vector3(0f, yaw, 0f);

            Box(vcRoot, "PierWall_Sill", mid + Vector3.up * (sill * 0.5f),
                new Vector3(thickness, sill, len + thickness), mat, rot);

            float headerH = height - headerStart;
            Box(vcRoot, "PierWall_Header", mid + Vector3.up * (headerStart + headerH * 0.5f),
                new Vector3(thickness, headerH, len + thickness), mat, rot);

            int piers = Mathf.Max(2, Mathf.RoundToInt(len / 3.2f));
            for (int i = 0; i < piers; i++)
            {
                float t = (i + 0.5f) / piers;
                Vector3 p = a + ab * t;
                Box(vcRoot, "PierWall_Pier", p + Vector3.up * ((sill + headerStart) * 0.5f),
                    new Vector3(thickness, headerStart - sill, 1.3f), mat, rot);
            }
        }

        static void BuildRoof(Material roofMat, Material beamMat)
        {
            // beams across the full depth
            for (float z = -10f; z <= 10f; z += 4f)
            {
                Box(vcRoot, "Roof_Beam", new Vector3(0f, 6.8f, z),
                    new Vector3(34.4f, 0.35f, 0.3f), beamMat);
            }

            // roof strips — two missing = holes open to the night sky
            // (hole over z -5..2 lets the moon shaft hit the skeleton)
            var strips = new[]
            {
                new { z0 = -12f, z1 = -5f,  present = true  },
                new { z0 = -5f,  z1 = 2f,   present = false },
                new { z0 = 2f,   z1 = 6f,   present = true  },
                new { z0 = 6f,   z1 = 9f,   present = false },
                new { z0 = 9f,   z1 = 12f,  present = true  },
            };

            foreach (var s in strips)
            {
                if (!s.present) continue;
                float depth = s.z1 - s.z0;
                Box(vcRoot, "Roof_Panel", new Vector3(0f, 7.15f, (s.z0 + s.z1) * 0.5f),
                    new Vector3(34.4f, 0.3f, depth), roofMat);
            }

            // a couple of fallen roof panels inside
            Box(vcRoot, "Roof_Fallen1", new Vector3(-6f, 1.9f, 4f),
                new Vector3(6f, 0.25f, 4f), roofMat, new Vector3(18f, 24f, 11f));
            Box(vcRoot, "Roof_Fallen2", new Vector3(8.5f, 1.3f, -1f),
                new Vector3(5f, 0.25f, 3.4f), roofMat, new Vector3(-13f, -40f, 9f));
        }

        static void BuildEntranceSign()
        {
            // over the doorway, facing the approach road (+Z)
            Sign("VISITOR\nCENTER", vcRoot, "Sign_VisitorCenter",
                new Vector3(0f, 5.7f, 12.4f), 4.6f, 1.7f,
                M("SignBlack"), new Color(0.95f, 0.85f, 0.6f),
                FaceToward(Vector3.forward), 0.24f, -1.6f);

            // hanging directional sign inside the lobby (tilted, half-broken)
            var hangPos = new Vector3(0f, 4.6f, 8f);
            Cyl(vcRoot, "HangChain_L", hangPos + new Vector3(-1.6f, 0.7f, 0f), 0.05f, 1.4f, M("MetalDark"));
            Cyl(vcRoot, "HangChain_R", hangPos + new Vector3(1.6f, 0.7f, 0f), 0.05f, 1.4f, M("MetalDark"));
            Sign("WAY OUT", vcRoot, "Sign_Exit",
                hangPos, 3.4f, 0.8f, M("SignYellow"), new Color(0.05f, 0.05f, 0.05f),
                FaceToward(Vector3.back), 0.2f, 7f);
        }

        static void BuildLobbyInterior(Material floorMat, Material woodDark, Material metal)
        {
            // ---- reception desk (L shape) with dead CCTV monitors ----
            Vector3 deskPos = new Vector3(6.5f, 0f, 7f);
            Box(vcRoot, "Desk_Front", deskPos + new Vector3(0f, 0.55f, 0.6f),
                new Vector3(5f, 1.1f, 0.2f), woodDark);
            Box(vcRoot, "Desk_Return", deskPos + new Vector3(-2.4f, 0.55f, -0.4f),
                new Vector3(0.2f, 1.1f, 2.2f), woodDark);
            Box(vcRoot, "Desk_Top", deskPos + new Vector3(0f, 1.14f, 0.3f),
                new Vector3(5.2f, 0.1f, 1.1f), floorMat);

            Box(vcRoot, "Monitor_1", deskPos + new Vector3(-1.2f, 1.5f, 0.2f),
                new Vector3(0.7f, 0.5f, 0.12f), M("MetalDark"), new Vector3(0f, -18f, 0f));
            var screen = Box(vcRoot, "Monitor_1_Screen", deskPos + new Vector3(-1.2f, 1.5f, 0.12f),
                new Vector3(0.6f, 0.4f, 0.03f), M("ScreenGlow"), new Vector3(0f, -18f, 0f));
            StripCollider(screen);

            Box(vcRoot, "Monitor_2", deskPos + new Vector3(1.4f, 1.45f, 0.3f),
                new Vector3(0.7f, 0.5f, 0.12f), M("MetalDark"), new Vector3(9f, 14f, 0f));

            // small flickering green glow over the desk
            var glow = NewPointLight(deskPos + new Vector3(-1.2f, 1.6f, 0f),
                new Color(0.4f, 1f, 0.5f), 0.55f, 5f, true);
            glow.transform.SetParent(vcRoot, true);

            // ---- benches ----
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = new Vector3(-9f + i * 3.2f, 0f, 8.5f);
                float yaw = rf(-30f, 30f);
                Box(vcRoot, "Bench_Seat", p + new Vector3(0f, 0.45f, 0f),
                    new Vector3(2.6f, 0.1f, 0.6f), woodDark, new Vector3(0f, yaw, 0f));
                Box(vcRoot, "Bench_LegL", p + Quaternion.Euler(0f, yaw, 0f) * new Vector3(-1f, 0.22f, 0f),
                    new Vector3(0.12f, 0.45f, 0.5f), metal, new Vector3(0f, yaw, 0f));
                Box(vcRoot, "Bench_LegR", p + Quaternion.Euler(0f, yaw, 0f) * new Vector3(1f, 0.22f, 0f),
                    new Vector3(0.12f, 0.45f, 0.5f), metal, new Vector3(0f, yaw, 0f));
            }

            // ---- ceiling lamps ----
            for (int i = 0; i < 2; i++)
            {
                Vector3 p = new Vector3(i == 0 ? -7f : 7f, 6.6f, 2f);
                Box(vcRoot, "LampHousing", p, new Vector3(0.9f, 0.2f, 0.9f), M("MetalDark"));
                var bulb = Box(vcRoot, "LampBulb", p + new Vector3(0f, -0.14f, 0f),
                    new Vector3(0.7f, 0.08f, 0.7f), M("LightWarm"));
                StripCollider(bulb);

                var lightGo = NewPointLight(p + new Vector3(0f, -0.3f, 0f),
                    new Color(1f, 0.85f, 0.6f), i == 0 ? 1.6f : 1.1f, 14f, i == 0);
                lightGo.transform.SetParent(vcRoot, true);
            }

            // ---- mezzanine + railing + stair ----
            Box(vcRoot, "Mezzanine", new Vector3(0f, 3.65f, -9f),
                new Vector3(34f, 0.3f, 6f), floorMat);
            BuildMezzanineRailing(metal);
            BuildLobbyStair(floorMat);

            // ---- wall posters ----
            Sign("INGEN", vcRoot, "Poster_Ingen",
                new Vector3(-16.6f, 3.6f, 0f), 3f, 2f,
                M("SignRed"), new Color(0.9f, 0.88f, 0.8f),
                FaceToward(Vector3.right), 0.34f);
            Sign("SAFETY\nFIRST", vcRoot, "Poster_Safety",
                new Vector3(16.6f, 3.6f, -4f), 2.4f, 2.4f,
                M("SignYellow"), new Color(0.05f, 0.05f, 0.05f),
                FaceToward(Vector3.left), 0.26f, 2.5f);

            // ---- stairwell guard rails around the open shaft ----
            BuildShaftRailings(metal);
        }

        static void BuildMezzanineRailing(Material metal)
        {
            float y0 = 3.8f;
            // front edge at z = -6, broken gap where the stair arrives (x 12.5..15.5)
            for (float x = -16.5f; x <= 16.5f; x += 2f)
            {
                if (x > 12f && x < 15.8f) continue;    // gap at stair top
                Box(vcRoot, "Mezz_Post", new Vector3(x, y0 + 0.5f, -6f),
                    new Vector3(0.08f, 1f, 0.08f), metal);
            }
            // rails split around the gap
            Box(vcRoot, "Mezz_Rail_A", new Vector3(-2f, y0 + 1f, -6f),
                new Vector3(29f, 0.08f, 0.08f), metal);
            Box(vcRoot, "Mezz_Rail_B", new Vector3(16.2f, y0 + 1f, -6f),
                new Vector3(1.2f, 0.08f, 0.08f), metal);
            // a bent section lying loose
            Box(vcRoot, "Mezz_Rail_Fallen", new Vector3(10f, 3.9f, -5.4f),
                new Vector3(3.4f, 0.08f, 0.08f), metal, new Vector3(0f, 24f, 38f));
        }

        static void BuildLobbyStair(Material mat)
        {
            // 10 solid steps rising from lobby floor (z ≈ -0.5) to mezzanine (z = -6)
            for (int i = 0; i < 10; i++)
            {
                float topY = 0.35f * (i + 1);
                float z = -0.5f - 0.55f * i - 0.275f;
                Box(vcRoot, "Stair_Step", new Vector3(14f, topY * 0.5f, z),
                    new Vector3(2.4f, topY, 0.55f), mat);
            }
        }

        static void BuildShaftRailings(Material metal)
        {
            // shaft opening x[-14,-6], z[-10,-4]; stair enters from the z=-4 side
            void RailSegment(Vector3 from, Vector3 to, float gapStartT, float gapEndT)
            {
                Vector3 ab = to - from;
                float len = ab.magnitude;
                int posts = Mathf.Max(2, Mathf.RoundToInt(len / 1.6f));
                for (int i = 0; i <= posts; i++)
                {
                    float t = i / (float)posts;
                    if (t > gapStartT && t < gapEndT) continue;
                    Vector3 p = from + ab * t;
                    Box(vcRoot, "ShaftRail_Post", p + Vector3.up * 0.5f,
                        new Vector3(0.08f, 1f, 0.08f), metal);
                }
            }

            // west edge x=-14 (z -10..-4)
            RailSegment(new Vector3(-14f, 0f, -10f), new Vector3(-14f, 0f, -4f), 9f, 10f);
            // east edge x=-6
            RailSegment(new Vector3(-6f, 0f, -10f), new Vector3(-6f, 0f, -4f), 9f, 10f);
            // north edge z=-10
            RailSegment(new Vector3(-14f, 0f, -10f), new Vector3(-6f, 0f, -10f), 9f, 10f);
            // south edge z=-4: posts only at the two corners, open where the stairs start
            RailSegment(new Vector3(-14f, 0f, -4f), new Vector3(-6f, 0f, -4f), 0.125f, 0.875f);

            // top rails (skip the stair entry on the south edge)
            Box(vcRoot, "ShaftRail_Top_W", new Vector3(-14f, 1f, -7f),
                new Vector3(0.08f, 0.08f, 6f), metal);
            Box(vcRoot, "ShaftRail_Top_E", new Vector3(-6f, 1f, -7f),
                new Vector3(0.08f, 0.08f, 6f), metal);
            Box(vcRoot, "ShaftRail_Top_N", new Vector3(-10f, 1f, -10f),
                new Vector3(8f, 0.08f, 0.08f), metal);
            Box(vcRoot, "ShaftRail_Top_S1", new Vector3(-13.5f, 1f, -4f),
                new Vector3(1f, 0.08f, 0.08f), metal);
            Box(vcRoot, "ShaftRail_Top_S2", new Vector3(-6.5f, 1f, -4f),
                new Vector3(1f, 0.08f, 0.08f), metal);

            // warning sign by the stair entry
            Sign("MAINTENANCE\nAUTHORIZED ONLY", vcRoot, "Sign_Shaft",
                new Vector3(-5.4f, 1.5f, -3.2f), 2.6f, 1.4f,
                M("SignYellow"), new Color(0.05f, 0.05f, 0.05f),
                FaceToward(new Vector3(0.5f, 0f, 0.86f)), 0.16f, -3f);
        }

        static void BuildMoonShaft()
        {
            // spotlight shining through the roof hole onto the skeleton platform
            var go = new GameObject("MoonShaft");
            go.transform.SetParent(vcRoot, false);
            go.transform.position = new Vector3(0f, 12f, -1.5f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(0.68f, 0.78f, 0.95f);
            light.intensity = 2.2f;
            light.range = 22f;
            light.spotAngle = 34f;
            light.innerSpotAngle = 16f;
            light.shadows = LightShadows.Soft;
        }

        static void BuildRoofVines(Material leaf)
        {
            for (int i = 0; i < 12; i++)
            {
                // hang from the edges of the two roof holes
                float z = chance(0.5f) ? rf(-5.4f, -4.6f) : rf(5.6f, 6.4f);
                float x = rf(-15f, 15f);
                float len = rf(1.4f, 4.2f);
                Box(vcRoot, "Vine", new Vector3(x, 7f - len * 0.5f, z),
                    new Vector3(0.12f, len, 0.12f), leaf,
                    new Vector3(rf(-6f, 6f), rf(0f, 360f), rf(-6f, 6f)));
            }
        }

        // ================================================================ GATE ARCH
        // concrete arch over the road at z = 22

        static void BuildGateArch()
        {
            var concrete = M("ConcreteDirty");
            var bulbMat = M("LightWarm");

            Box(propsRoot, "Arch_PillarL", new Vector3(-6.2f, 3.4f, 22f),
                new Vector3(1.4f, 6.8f, 1.6f), concrete);
            Box(propsRoot, "Arch_PillarR", new Vector3(6.2f, 3.4f, 22f),
                new Vector3(1.4f, 6.8f, 1.6f), concrete);
            Box(propsRoot, "Arch_Beam", new Vector3(0f, 7.2f, 22f),
                new Vector3(14.6f, 1.8f, 1.8f), concrete);

            // sign both ways
            Sign("JURASSIC PARK", propsRoot, "Sign_Arch_South",
                new Vector3(0f, 7.2f, 23.05f), 11.5f, 1.4f,
                M("SignRed"), new Color(0.92f, 0.86f, 0.75f),
                FaceToward(Vector3.forward), 0.42f);
            Sign("JURASSIC PARK", propsRoot, "Sign_Arch_North",
                new Vector3(0f, 7.2f, 20.95f), 11.5f, 1.4f,
                M("SignRed"), new Color(0.92f, 0.86f, 0.75f),
                FaceToward(Vector3.back), 0.42f);

            // bulbs along the underside of the beam (one flickers)
            for (float x = -5.5f; x <= 5.5f; x += 1.8f)
            {
                var b = Ball(propsRoot, "Arch_Bulb", new Vector3(x, 6.2f, 22f),
                    new Vector3(0.28f, 0.28f, 0.28f), bulbMat);
                StripCollider(b);
            }

            var lightGo = NewPointLight(new Vector3(0f, 6f, 22f),
                new Color(1f, 0.85f, 0.55f), 2.2f, 16f, true);
            lightGo.transform.SetParent(propsRoot, true);

            // hanging "PARK CLOSED" chain sign between the pillars
            Sign("PARK CLOSED", propsRoot, "Sign_ParkClosed",
                new Vector3(0f, 4.4f, 22f), 8f, 1.3f,
                M("SignWhite"), new Color(0.4f, 0.05f, 0.05f),
                FaceToward(Vector3.forward), 0.4f, -4f);
        }
    }
}
