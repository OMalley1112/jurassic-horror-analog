using UnityEngine;
using AnalogHorror;

namespace AnalogHorror.EditorTools
{
    public static partial class MapBuilder
    {
        // ======================================================== RANCH 04
        // The island grows again: past the aviary ruins the footpath runs on
        // into the far north — a fenced breeding ranch with two long barns,
        // a mess hut, a water tower, a floodlit hatching yard, laundry left
        // on the line, and something standing on the porch watching the path.
        // Runs LAST in BuildAll, so every rf() here only shifts content
        // inside this file — the verified map above is untouched.

        static Transform ranchRoot;

        static void BuildRanch()
        {
            ranchRoot = NewRoot("_RANCH 04");

            // new ground north of the aviary + the path running out to it
            ClearVegetation(-52f, -186f, 52f, -144f);
            GroundRect("Ground_ExtN2", -85f, 85f, -185f, -140f, M("GrassDark"));
            StripCollider(FlatQuad(ranchRoot, "Path_Ranch",
                new Vector3(0f, 0.03f, -143f), new Vector2(8f, 82f), M("DirtRoad")));

            RanchPerimeter();
            RanchSign();
            BuildBarn(-26f, -164f, 16f, 8f, true);
            BuildBarn(26f, -166f, 14f, 7f, false);
            BuildMessHut();
            BuildWaterTower();
            RanchYard();
            RanchDressing();
            RanchTrees();
        }

        // ------------------------------------------------------- perimeter

        static void RanchPerimeter()
        {
            // chainlink rectangle with a gap where the path walks in
            RanchFenceRun(new Vector3(-46f, 0f, -152f), new Vector3(-6f, 0f, -152f), true);
            RanchFenceRun(new Vector3(6f, 0f, -152f), new Vector3(46f, 0f, -152f), true);
            RanchFenceRun(new Vector3(46f, 0f, -152f), new Vector3(46f, 0f, -184f), false);
            RanchFenceRun(new Vector3(46f, 0f, -184f), new Vector3(-46f, 0f, -184f), false);
            RanchFenceRun(new Vector3(-46f, 0f, -184f), new Vector3(-46f, 0f, -152f), false);

            // gate cheeks either side of the path opening
            Box(ranchRoot, "RanchGate_CheekL", new Vector3(-6.4f, 1.6f, -152f),
                new Vector3(0.5f, 3.2f, 0.5f), M("ConcreteDirty"));
            Box(ranchRoot, "RanchGate_CheekR", new Vector3(6.4f, 1.6f, -152f),
                new Vector3(0.5f, 3.2f, 0.5f), M("ConcreteDirty"));
        }

        static void RanchFenceRun(Vector3 a, Vector3 b, bool postA)
        {
            Vector3 ab = b - a;
            float len = ab.magnitude;
            if (len < 0.5f) return;

            int segs = Mathf.Max(1, Mathf.RoundToInt(len / 6.5f));
            if (postA) RanchPost(a);

            Vector3 prev = a;
            for (int i = 1; i <= segs; i++)
            {
                Vector3 p = a + ab * (i / (float)segs);
                RanchFenceSpan(prev, p);
                RanchPost(p);
                prev = p;
            }
        }

        static void RanchFenceSpan(Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float len = ab.magnitude;
            if (len < 0.5f) return;
            float yaw = Mathf.Atan2(ab.x, ab.z) * Mathf.Rad2Deg;
            Vector3 mid = (a + b) * 0.5f;
            var rot = new Vector3(0f, yaw, 0f);

            Box(ranchRoot, "RanchFence_Rail", mid + Vector3.up * 1.05f,
                new Vector3(0.1f, 0.1f, len), M("MetalDark"), rot);
            Box(ranchRoot, "RanchFence_Rail", mid + Vector3.up * 2.0f,
                new Vector3(0.1f, 0.1f, len), M("MetalDark"), rot);
            Box(ranchRoot, "RanchFence_Mesh", mid + Vector3.up * 1.5f,
                new Vector3(0.03f, 1.9f, len), M("ChainLink"), rot);
        }

        static void RanchPost(Vector3 p)
        {
            Cyl(ranchRoot, "RanchFence_Post", p + Vector3.up * 1.3f,
                0.09f, 2.6f, M("MetalGray"));
        }

        // ----------------------------------------------------------- sign

        static void RanchSign()
        {
            Box(ranchRoot, "Sign_RanchPostL", new Vector3(-3.2f, 1.7f, -149.5f),
                new Vector3(0.16f, 3.4f, 0.16f), M("MetalGray"));
            Box(ranchRoot, "Sign_RanchPostR", new Vector3(3.2f, 1.7f, -149.5f),
                new Vector3(0.16f, 3.4f, 0.16f), M("MetalGray"));

            // readable from the path below, walking in
            Sign("RANCH 04 \u2014 HATCHING SECTION", ranchRoot, "Sign_Ranch",
                new Vector3(0f, 3.5f, -149.5f), 6.4f, 0.9f,
                M("SignWhite"), new Color(0.08f, 0.08f, 0.08f),
                FaceToward(Vector3.forward), 0.2f);
        }

        // ------------------------------------------------------------ barns

        static void BuildBarn(float cx, float cz, float len, float wid, bool lit)
        {
            string prefix = lit ? "BarnA_" : "BarnB_";
            float h = lit ? 4f : 3.6f;
            var wall = M("ConcreteDirty");
            float x0 = cx - len * 0.5f;
            float x1 = cx + len * 0.5f;
            float z0 = cz - wid * 0.5f;
            float z1 = cz + wid * 0.5f;

            Box(ranchRoot, prefix + "Floor", new Vector3(cx, 0.1f, cz),
                new Vector3(len, 0.2f, wid), wall);

            // three solid walls
            Box(ranchRoot, prefix + "WallN", new Vector3(cx, h * 0.5f, z0),
                new Vector3(len, h, 0.3f), wall);
            Box(ranchRoot, prefix + "WallS", new Vector3(cx, h * 0.5f, z1),
                new Vector3(len, h, 0.3f), wall);

            // door wall: gap on the side that faces the compound centre
            float doorX = lit ? x1 : x0;
            float doorMid = cz;
            float half = (wid - 3f) * 0.5f;
            Box(ranchRoot, prefix + "WallD_A", new Vector3(doorX, h * 0.5f, doorMid - half - 0.75f),
                new Vector3(0.3f, h, half), wall);
            Box(ranchRoot, prefix + "WallD_B", new Vector3(doorX, h * 0.5f, doorMid + half + 0.75f),
                new Vector3(0.3f, h, half), wall);
            Box(ranchRoot, prefix + "Lintel", new Vector3(doorX, h - 0.35f, doorMid),
                new Vector3(0.3f, 0.7f, 3f), wall);

            // fourth wall (the one without the door)
            float solidX = lit ? x0 : x1;
            Box(ranchRoot, prefix + "WallSolid", new Vector3(solidX, h * 0.5f, cz),
                new Vector3(0.3f, h, wid), wall);

            // pitched roof, ridge along the long axis
            float halfWid = wid * 0.5f + 0.5f;
            float pitch = 22f;
            float roofY = h + 0.55f;
            Box(ranchRoot, prefix + "RoofN", new Vector3(cx, roofY, cz - halfWid * 0.5f),
                new Vector3(len + 0.8f, 0.22f, halfWid + 0.4f), M("RoofMetal"),
                new Vector3(-pitch, 0f, 0f));
            Box(ranchRoot, prefix + "RoofS", new Vector3(cx, roofY, cz + halfWid * 0.5f),
                new Vector3(len + 0.8f, 0.22f, halfWid + 0.4f), M("RoofMetal"),
                new Vector3(pitch, 0f, 0f));

            // the sliding door, pushed open
            Box(ranchRoot, prefix + "Door", new Vector3(doorX + (lit ? 0.5f : -0.5f),
                    h * 0.45f, doorMid - 2.4f),
                new Vector3(0.18f, h * 0.85f, 2.6f), M("MetalDark"),
                new Vector3(0f, lit ? 16f : -16f, 0f));

            if (lit)
            {
                // warm window strips + the lamp left burning inside
                for (int i = -1; i <= 1; i++)
                    Box(ranchRoot, prefix + "Window",
                        new Vector3(cx + i * 4.6f, 2.6f, z1 + 0.16f),
                        new Vector3(1.7f, 0.95f, 0.06f), M("ScreenGlow"));

                var lamp = NewPointLight(new Vector3(cx, 2.7f, cz),
                    new Color(1f, 0.82f, 0.55f), 1.5f, 13f, false);
                lamp.transform.SetParent(ranchRoot, true);
            }
            else
            {
                // the dark one: a wall panel torn off and thrown outside
                Box(ranchRoot, prefix + "FallenPanel",
                    new Vector3(x1 + 3.4f, 0.3f, cz - 3.6f),
                    new Vector3(3.2f, 0.16f, 4.2f), wall,
                    new Vector3(0f, 27f, 7f));
                BuildBlood(new Vector3(x1 + 1.8f, 0.05f, cz + 2.4f),
                    1.8f, rf(0f, 360f));
            }
        }

        // -------------------------------------------------------- mess hut

        static void BuildMessHut()
        {
            const float cx = 0f, cz = -178f;
            var wall = M("ConcreteDirty");

            Box(ranchRoot, "Mess_Floor", new Vector3(cx, 0.1f, cz),
                new Vector3(9f, 0.2f, 6f), wall);
            Box(ranchRoot, "Mess_WallN", new Vector3(cx, 1.7f, cz - 3f),
                new Vector3(9f, 3.4f, 0.3f), wall);
            Box(ranchRoot, "Mess_WallW", new Vector3(cx - 4.35f, 1.7f, cz),
                new Vector3(0.3f, 3.4f, 6f), wall);
            Box(ranchRoot, "Mess_WallE", new Vector3(cx + 4.35f, 1.7f, cz),
                new Vector3(0.3f, 3.4f, 6f), wall);

            // doorway facing the path
            Box(ranchRoot, "Mess_WallS_L", new Vector3(cx - 2.7f, 1.7f, cz + 3f),
                new Vector3(3.6f, 3.4f, 0.3f), wall);
            Box(ranchRoot, "Mess_WallS_R", new Vector3(cx + 2.7f, 1.7f, cz + 3f),
                new Vector3(3.6f, 3.4f, 0.3f), wall);
            Box(ranchRoot, "Mess_Lintel", new Vector3(cx, 3.05f, cz + 3f),
                new Vector3(1.8f, 0.7f, 0.3f), wall);

            Box(ranchRoot, "Mess_Roof", new Vector3(cx, 3.6f, cz),
                new Vector3(9.6f, 0.3f, 6.6f), M("RoofMetal"));

            // long table and a bench inside
            Box(ranchRoot, "Mess_Table", new Vector3(cx + 1.4f, 0.55f, cz - 1.2f),
                new Vector3(2.6f, 0.9f, 1f), M("WoodDark"));
            Box(ranchRoot, "Mess_Bench", new Vector3(cx + 1.4f, 0.4f, cz - 0.2f),
                new Vector3(2.6f, 0.16f, 0.5f), M("WoodDark"));

            // porch bulb still burning over the door
            Ball(ranchRoot, "Mess_PorchBulb", new Vector3(cx, 2.95f, cz + 3.5f),
                new Vector3(0.3f, 0.3f, 0.3f), M("HeadlightGlow"));
            var porch = NewPointLight(new Vector3(cx, 3.1f, cz + 3.9f),
                new Color(1f, 0.8f, 0.5f), 1.25f, 11f, false);
            porch.transform.SetParent(ranchRoot, true);
        }

        // ----------------------------------------------------- water tower

        static void BuildWaterTower()
        {
            const float cx = -42f, cz = -177f;
            var legMat = M("MetalGray");

            // four splayed legs
            for (int i = 0; i < 4; i++)
            {
                float dx = (i & 1) == 0 ? -2.2f : 2.2f;
                float dz = (i & 2) == 0 ? -2.2f : 2.2f;
                Box(ranchRoot, "Tower_Leg", new Vector3(cx + dx, 3.5f, cz + dz),
                    new Vector3(0.3f, 7f, 0.3f), legMat,
                    new Vector3(0f, 0f, dx < 0f ? -4f : 4f));
            }

            Cyl(ranchRoot, "Tower_Tank", new Vector3(cx, 8.6f, cz),
                3f, 3.6f, M("MetalRust"));
            Cyl(ranchRoot, "Tower_Cap", new Vector3(cx, 10.5f, cz),
                3.1f, 0.3f, M("MetalDark"));

            // ladder up the east side
            Box(ranchRoot, "Tower_LadderL", new Vector3(cx + 3.4f, 3.5f, cz - 0.6f),
                new Vector3(0.07f, 7f, 0.07f), legMat);
            Box(ranchRoot, "Tower_LadderR", new Vector3(cx + 3.4f, 3.5f, cz + 0.6f),
                new Vector3(0.07f, 7f, 0.07f), legMat);
            for (int i = 0; i < 8; i++)
                Box(ranchRoot, "Tower_Rung", new Vector3(cx + 3.4f, 0.7f + i * 0.9f, cz),
                    new Vector3(0.06f, 0.06f, 1.2f), legMat);

            // red warning lamp on the crown — visible from the aviary path
            Ball(ranchRoot, "Tower_Beacon", new Vector3(cx, 10.9f, cz),
                new Vector3(0.36f, 0.36f, 0.36f), M("WarningLight"));
            var beacon = NewPointLight(new Vector3(cx, 11.2f, cz),
                new Color(1f, 0.24f, 0.16f), 0.9f, 18f, false);
            beacon.transform.SetParent(ranchRoot, true);
        }

        // -------------------------------------------------- hatching yard

        static void RanchYard()
        {
            // floodlight over the yard
            Cyl(ranchRoot, "RanchMast", new Vector3(5f, 4.5f, -154f),
                0.16f, 9f, M("MetalGray"));
            Box(ranchRoot, "RanchMast_Head", new Vector3(5f, 9.1f, -154f),
                new Vector3(1f, 0.35f, 0.7f), M("MetalDark"), new Vector3(28f, 0f, 0f));
            var flood = NewPointLight(new Vector3(5f, 8.7f, -153.4f),
                new Color(0.82f, 0.9f, 1f), 2f, 30f, false);
            flood.transform.SetParent(ranchRoot, true);

            // path lamps
            PathLamp(-5.5f, -144f);
            PathLamp(5.5f, -162f);

            // the clutch: crates, eggs under a red heat lamp, one already split
            BuildCrateStack(new Vector3(-14f, 0f, -156f), 4, rf(0f, 360f));

            Cyl(ranchRoot, "Heat_Post", new Vector3(-14f, 1.6f, -155.4f),
                0.1f, 3.2f, M("MetalDark"));
            Cyl(ranchRoot, "Heat_Shade", new Vector3(-14f, 3.3f, -155.4f),
                0.55f, 0.4f, M("SignRed"));
            var heat = NewPointLight(new Vector3(-14f, 3f, -155.4f),
                new Color(1f, 0.55f, 0.35f), 1.4f, 9f, false);
            heat.transform.SetParent(ranchRoot, true);

            for (int i = 0; i < 4; i++)
                Ball(ranchRoot, "Clutch_Egg",
                    new Vector3(-13.2f + i * 0.8f + rf(-0.15f, 0.15f),
                        0.55f, -154.5f + rf(-0.3f, 0.3f)),
                    new Vector3(0.75f, 0.95f, 0.75f), M("Egg"),
                    new Vector3(rf(-10f, 10f), rf(0f, 360f), rf(-10f, 10f)));
            Box(ranchRoot, "Clutch_SplitShell", new Vector3(-10.4f, 0.36f, -154.1f),
                new Vector3(0.7f, 0.4f, 0.6f), M("Egg"),
                new Vector3(16f, 42f, 0f));

            // feed barrels by the mess hut
            for (int i = 0; i < 3; i++)
                Cyl(ranchRoot, "Ranch_Barrel",
                    new Vector3(rf(5f, 9.5f), 0.45f, rf(-177f, -172f)),
                    0.55f, 0.9f, i % 2 == 0 ? M("BarrelBlue") : M("BarrelRed"));

            // laundry left on the line — it still moves in the storm
            Box(ranchRoot, "Laundry_PostL", new Vector3(-8f, 1.7f, -171f),
                new Vector3(0.12f, 3.4f, 0.12f), M("MetalDark"));
            Box(ranchRoot, "Laundry_PostR", new Vector3(8f, 1.7f, -171f),
                new Vector3(0.12f, 3.4f, 0.12f), M("MetalDark"));
            Box(ranchRoot, "Laundry_Line", new Vector3(0f, 3.3f, -171f),
                new Vector3(16.2f, 0.04f, 0.04f), M("MetalDark"));
            string[] clothMats = { "SignWhite", "PlayerKhaki", "PlayerOlive",
                                   "PlayerDenim", "SignRed" };
            for (int i = 0; i < clothMats.Length; i++)
                Box(ranchRoot, "Laundry_Cloth",
                    new Vector3(-6f + i * 3f, 2.65f, -171f),
                    new Vector3(1f, 1.2f, 0.06f), M(clothMats[i]),
                    new Vector3(0f, rf(-8f, 8f), rf(-4f, 4f)));
        }

        static void PathLamp(float x, float z)
        {
            Cyl(ranchRoot, "PathLamp_Post", new Vector3(x, 1.6f, z),
                0.1f, 3.2f, M("MetalDark"));
            Ball(ranchRoot, "PathLamp_Bulb", new Vector3(x, 3.3f, z),
                new Vector3(0.34f, 0.34f, 0.34f), M("HeadlightGlow"));
            var l = NewPointLight(new Vector3(x, 3.4f, z),
                new Color(1f, 0.82f, 0.55f), 1.1f, 15f, false);
            l.transform.SetParent(ranchRoot, true);
        }

        // -------------------------------------------------------- dressing

        static void RanchDressing()
        {
            // the truck that never left
            BuildJeep(new Vector3(38f, 0f, -156f), 128f, true);

            BuildCrateStack(new Vector3(12f, 0f, -172f), 3, rf(0f, 360f));
            BuildBlood(new Vector3(16f, 0.05f, -160f), 2.1f, rf(0f, 360f));
            BuildBonePile(ranchRoot, new Vector3(-32f, 0f, -157f),
                1.2f, rf(0f, 360f));

            // it has been standing on the porch the whole time
            var overlay = Object.FindObjectOfType<VhsOverlay>();
            var ambience = Object.FindObjectOfType<AmbientAudio>();
            PlaceFigure(new Vector3(-8f, 0f, -174f), 0f,
                overlay, ambience, 8.5f);
        }

        static void RanchTrees()
        {
            // scrub pines flanking the compound — named differently from the
            // jungle trunks so the verified Tree_Trunk count stays put
            for (int i = 0; i < 16; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * rf(16f, 80f);
                float z = rf(-182f, -143f);
                float h = rf(3.5f, 6.5f);

                Cyl(ranchRoot, "RanchTree_Trunk", new Vector3(x, h * 0.5f, z),
                    rf(0.22f, 0.34f), h, M("WoodDark"));
                Ball(ranchRoot, "RanchTree_Canopy",
                    new Vector3(x, h + 1.1f, z),
                    new Vector3(rf(2.6f, 4f), rf(2.2f, 3.2f), rf(2.6f, 4f)),
                    M(chance(0.5f) ? "LeafDark" : "LeafMid"));

                if (chance(0.3f))
                    Ball(ranchRoot, "RanchRock",
                        new Vector3(x + rf(-3.5f, 3.5f), 0.28f, z + rf(-3.5f, 3.5f)),
                        new Vector3(rf(0.6f, 1.3f), rf(0.4f, 0.7f), rf(0.6f, 1.3f)),
                        M("Rock"), new Vector3(rf(-12f, 12f), rf(0f, 360f), rf(-12f, 12f)));
            }
        }
    }
}
