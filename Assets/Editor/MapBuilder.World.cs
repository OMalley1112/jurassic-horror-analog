using UnityEngine;

namespace AnalogHorror.EditorTools
{
    public static partial class MapBuilder
    {
        // world layout notes (X/Z, y-up):
        //   visitor center .......... x[-17,17]   z[-12,12]
        //   stairwell shaft ......... x[-14,-6]   z[-10,-4]  (hole in ground)
        //   plaza ................... x[-12,12]   z[12,22]
        //   main road ............... x[-4.5,4.5] z[13,90]
        //   T-Rex paddock ........... x[-75,-25]  z[8,52]
        //   raptor paddock .......... x[25,65]    z[30,70]
        //   --- expansion (MapBuilder.Expansion) ---
        //   ferry river ............. z[108,118]  (bridge at x[-4,4])
        //   ranger station .......... x[-6,6]     z[121,131]
        //   geothermal plant ........ x[103,135]  z[33,59]
        //   cargo graveyard ......... x[-136,-104] z[14,48]
        //   aviary ruins ............ x[-24,24]   z[-136,-104]
        //   world bounds ............ ±140 (hedge at ±138)

        static void BuildGroundAndRoads()
        {
            var grass = M("GrassDark");
            var road = M("DirtRoad");
            var concrete = M("Concrete");

            // ground split into 4 pieces so the stairwell shaft stays open
            GroundRect("Ground_W", -100f, -14f, -100f, 100f, grass);
            GroundRect("Ground_E", -6f, 100f, -100f, 100f, grass);
            GroundRect("Ground_N", -14f, -6f, -100f, -10f, grass);
            GroundRect("Ground_S", -14f, -6f, -4f, 100f, grass);

            // world expansion ring (constant-only, keeps the RNG stream intact)
            GroundRect("Ground_ExtS", -100f, 100f, 100f, 140f, grass);
            GroundRect("Ground_ExtN", -100f, 100f, -140f, -100f, grass);
            GroundRect("Ground_ExtW", -140f, -100f, -140f, 140f, grass);
            GroundRect("Ground_ExtE", 100f, 140f, -140f, 140f, grass);

            // plaza slab in front of the visitor center
            GroundRect("Plaza", -12f, 12f, 12f, 22f, concrete);

            // roads (slightly raised to avoid z-fighting)
            var mainRoad = FlatQuad(envRoot, "Road_Main", new Vector3(0f, 0.03f, 51.5f),
                new Vector2(9f, 77f), road);
            StripCollider(mainRoad);

            var eastRoad = FlatQuad(envRoot, "Road_EastBranch", new Vector3(15.5f, 0.03f, 48f),
                new Vector2(23f, 10f), road);
            StripCollider(eastRoad);

            var westRoad = FlatQuad(envRoot, "Road_WestBranch", new Vector3(-15.5f, 0.03f, 31f),
                new Vector2(23f, 10f), road);
            StripCollider(westRoad);

            // faded centre line dashes (run on to the river ferry)
            var paint = M("PaintWhite");
            for (float z = 16f; z < 108f; z += 6f)
            {
                var dash = Box(envRoot, "RoadDash", new Vector3(0f, 0.035f, z),
                    new Vector3(0.35f, 0.01f, 2.4f), paint);
                StripCollider(dash);
            }

            // muddy puddles on the road
            var puddle = M("Puddle");
            for (int i = 0; i < 7; i++)
            {
                var p = FlatQuad(envRoot, "RoadPuddle",
                    new Vector3(rf(-3.2f, 3.2f), 0.045f, rf(18f, 85f)),
                    new Vector2(rf(1f, 2.6f), rf(1.4f, 3.4f)), puddle, rf(-25f, 25f));
                StripCollider(p);
            }

            // jungle boundary hedge (keeps the player inside the world — ±138 now)
            var hedge = M("Hedge");
            Box(envRoot, "Boundary_N", new Vector3(0f, 4f, -138f), new Vector3(276f, 8f, 6f), hedge);
            Box(envRoot, "Boundary_S", new Vector3(0f, 4f, 138f), new Vector3(276f, 8f, 6f), hedge);
            Box(envRoot, "Boundary_W", new Vector3(-138f, 4f, 0f), new Vector3(6f, 8f, 276f), hedge);
            Box(envRoot, "Boundary_E", new Vector3(138f, 4f, 0f), new Vector3(6f, 8f, 276f), hedge);
        }

        // ------------------------------------------------------------ jungle

        /// <summary>True if no trees/props should be placed here.</summary>
        static bool InClearZone(float x, float z)
        {
            return InRect(x, z, -21f, -16f, 21f, 16f)      // visitor center
                || InRect(x, z, -13f, 11f, 13f, 23f)        // plaza
                || InRect(x, z, -6.5f, 12f, 6.5f, 92f)      // main road
                || InRect(x, z, 3f, 42f, 28f, 54f)          // east branch
                || InRect(x, z, -28f, 25f, -3f, 37f)        // west branch
                || InRect(x, z, -76f, 7f, -24f, 53f)        // T-Rex paddock
                || InRect(x, z, 24f, 29f, 66f, 71f)         // raptor paddock
                || InRect(x, z, 7.5f, 62.5f, 14f, 69.5f)    // jungle-edge raptor
                || InRect(x, z, -78f, -82f, -48f, -48f);    // sauropod clearing
        }

        static bool InRect(float x, float z, float x0, float z0, float x1, float z1)
        {
            return x >= x0 && x <= x1 && z >= z0 && z <= z1;
        }

        static void BuildJungle()
        {
            var trunk = M("Trunk");
            var leafDark = M("LeafDark");
            var leafMid = M("LeafMid");
            var rock = M("Rock");

            int placed = 0;
            for (int attempt = 0; attempt < 340 && placed < 190; attempt++)
            {
                float x = rf(-94f, 94f);
                float z = rf(-94f, 94f);
                if (InClearZone(x, z)) continue;
                // keep away from the boundary hedge
                if (Mathf.Abs(x) > 91f && Mathf.Abs(z) > 91f) continue;

                BuildTree(new Vector3(x, 0f, z), leafDark, leafMid, trunk);
                placed++;
            }

            // dense tree wall ringing the play area
            for (int i = 0; i < 54; i++)
            {
                float t = i / 54f * 4f;                 // 0..4 around the square
                int side = Mathf.FloorToInt(t);
                float f = (t - side) * 2f - 1f;          // -1..1 along that side
                float edge = rf(88f, 95f);
                Vector3 pos;
                switch (side)
                {
                    case 0: pos = new Vector3(f * 92f, 0f, -edge); break;
                    case 1: pos = new Vector3(edge, 0f, f * 92f); break;
                    case 2: pos = new Vector3(f * 92f, 0f, edge); break;
                    default: pos = new Vector3(-edge, 0f, f * 92f); break;
                }
                if (InClearZone(pos.x, pos.z)) continue;
                BuildTree(pos, leafDark, leafMid, trunk);
            }

            // undergrowth bushes
            for (int attempt = 0; attempt < 300; attempt++)
            {
                float x = rf(-95f, 95f);
                float z = rf(-95f, 95f);
                if (InClearZone(x, z) && chance(0.85f)) continue;

                float w = rf(0.9f, 2.4f);
                var bush = Ball(jungRoot, "Bush", new Vector3(x, w * 0.3f, z),
                    new Vector3(w, w * rf(0.45f, 0.7f), w),
                    chance(0.5f) ? leafDark : leafMid,
                    new Vector3(0f, rf(0f, 360f), 0f));
                StripCollider(bush);
            }

            // fern clusters — dense forest-floor undergrowth along the paths
            for (int c = 0; c < 26; c++)
            {
                float cx = rf(-93f, 93f);
                float cz = rf(-93f, 93f);
                if (InClearZone(cx, cz)) continue;

                int fronds = rint(4, 8);
                float baseYaw = rf(0f, 360f);
                for (int f = 0; f < fronds; f++)
                {
                    float a = baseYaw + f * (360f / fronds) + rf(-16f, 16f);
                    var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                    var frond = Box(jungRoot, "Fern",
                        new Vector3(cx, 0f, cz) + dir * rf(0.35f, 0.7f) + Vector3.up * rf(0.28f, 0.42f),
                        new Vector3(0.34f, 0.05f, rf(0.9f, 1.5f)),
                        chance(0.5f) ? leafDark : leafMid,
                        new Vector3(rf(28f, 48f), a, 0f));
                    StripCollider(frond);   // walkable — no tripping on decoration
                }
            }

            // scattered boulders
            for (int i = 0; i < 34; i++)
            {
                float x = rf(-93f, 93f);
                float z = rf(-93f, 93f);
                if (InClearZone(x, z)) continue;
                float s = rf(0.6f, 2.2f);
                var b = Ball(jungRoot, "Rock", new Vector3(x, s * 0.28f, z),
                    new Vector3(s, s * 0.62f, s * rf(0.8f, 1.3f)), rock,
                    new Vector3(rf(-14f, 14f), rf(0f, 360f), rf(-14f, 14f)));
                // keep the collider on rocks (small obstacle)
            }

            // fallen logs
            for (int i = 0; i < 14; i++)
            {
                float x = rf(-90f, 90f);
                float z = rf(-90f, 90f);
                if (InClearZone(x, z)) continue;
                float len = rf(3f, 6.5f);
                Cyl(jungRoot, "FallenLog", new Vector3(x, 0.32f, z),
                    rf(0.5f, 0.8f), len, trunk,
                    new Vector3(90f, rf(0f, 360f), 0f));
            }
        }

        static void BuildTree(Vector3 pos, Material leafDark, Material leafMid, Material trunk)
        {
            float roll = (float)rng.NextDouble();
            if (roll < 0.42)       // hardwood
            {
                float h = rf(4.5f, 8f);
                float r = rf(0.3f, 0.55f);
                Cyl(jungRoot, "Tree_Trunk", pos + new Vector3(0f, h * 0.5f, 0f),
                    r * 2f, h, trunk, new Vector3(rf(-3f, 3f), rf(0f, 360f), rf(-3f, 3f)));

                int blobs = rint(2, 5);
                for (int b = 0; b < blobs; b++)
                {
                    float s = rf(2.6f, 4.8f);
                    Vector3 offset = new Vector3(rf(-1.6f, 1.6f), h + rf(-0.8f, 1.4f), rf(-1.6f, 1.6f));
                    var canopy = Ball(jungRoot, "Tree_Canopy", pos + offset,
                        new Vector3(s, s * rf(0.6f, 0.85f), s),
                        chance(0.55f) ? leafDark : leafMid,
                        new Vector3(0f, rf(0f, 360f), 0f));
                    StripCollider(canopy);
                }
            }
            else if (roll < 0.72)  // palm
            {
                float h = rf(5f, 8.5f);
                float lean = rf(-8f, 8f);
                Cyl(jungRoot, "Palm_Trunk", pos + new Vector3(0f, h * 0.5f, 0f),
                    rf(0.3f, 0.45f), h, trunk, new Vector3(lean, rf(0f, 360f), rf(-8f, 8f)));

                Vector3 top = pos + new Vector3(lean * 0.06f, h, 0f);
                int leaves = rint(6, 9);
                float yaw = rf(0f, 360f);
                for (int l = 0; l < leaves; l++)
                {
                    float a = yaw + l * (360f / leaves) + rf(-10f, 10f);
                    var frond = Box(jungRoot, "Palm_Frond",
                        top + Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0f, 1.4f),
                        new Vector3(0.5f, 0.09f, rf(2.6f, 3.6f)),
                        chance(0.5f) ? leafDark : leafMid,
                        new Vector3(rf(14f, 30f), a, 0f));
                    StripCollider(frond);
                }
            }
            else                    // dead skeleton tree
            {
                float h = rf(4f, 7f);
                Cyl(jungRoot, "DeadTree_Trunk", pos + new Vector3(0f, h * 0.5f, 0f),
                    rf(0.28f, 0.5f), h, trunk, new Vector3(rf(-4f, 4f), 0f, rf(-4f, 4f)));

                int branches = rint(2, 5);
                for (int b = 0; b < branches; b++)
                {
                    float a = rf(0f, 360f);
                    float len = rf(1.6f, 3.2f);
                    Vector3 start = pos + new Vector3(0f, rf(h * 0.55f, h * 0.95f), 0f);
                    var dir = Quaternion.Euler(rf(-60f, -25f), a, 0f) * Vector3.forward;
                    Box(jungRoot, "DeadTree_Branch", start + dir * (len * 0.5f),
                        new Vector3(0.16f, 0.16f, len), trunk,
                        Quaternion.LookRotation(dir).eulerAngles);
                    StripCollider(Box(jungRoot, "DeadTree_BranchTip", start + dir * len,
                        new Vector3(0.1f, 0.1f, 1.1f), trunk,
                        Quaternion.LookRotation(dir).eulerAngles));
                }
            }
        }
    }
}
