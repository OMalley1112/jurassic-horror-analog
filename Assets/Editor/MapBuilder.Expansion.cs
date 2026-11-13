using UnityEngine;
using AnalogHorror;

namespace AnalogHorror.EditorTools
{
    public static partial class MapBuilder
    {
        // ======================================================= WORLD EXPANSION
        // The island grows from ±100 to ±140 with four new districts, each
        // with its own reason to be feared:
        //   SOUTH  — the ferry river and Ranger Station 07 (Boone)
        //   EAST   — the geothermal plant (Vasquez + red strobe)
        //   WEST   — the cargo graveyard: crushed jeeps, open dino crates
        //   NORTH  — the aviary ruins: broken dome, eggs, something watching
        // Runs LAST in BuildAll, so every rf() here only shifts content
        // inside this file — the verified map above is untouched.

        static Transform expRoot;

        static void BuildExpansion()
        {
            expRoot = NewRoot("_EXPANSION");

            ExtendRoads();
            BuildRiverCrossing();
            BuildPowerPlant();
            BuildCargoGraveyard();
            BuildAviaryRuins();
            BuildMidMapDressing();
        }

        /// <summary>
        /// Destroy jungle decoration inside a rect. No RNG consumed — safe
        /// to call anywhere without shifting the generator stream.
        /// </summary>
        static void ClearVegetation(float x0, float z0, float x1, float z1)
        {
            if (jungRoot == null) return;

            var doomed = new System.Collections.Generic.List<Transform>();
            foreach (Transform c in jungRoot)
            {
                Vector3 p = c.position;
                if (p.x >= x0 - 3f && p.x <= x1 + 3f &&
                    p.z >= z0 - 3f && p.z <= z1 + 3f)
                    doomed.Add(c);
            }
            for (int i = 0; i < doomed.Count; i++)
                Object.DestroyImmediate(doomed[i].gameObject);
        }

        // ------------------------------------------------------------- roads

        static void ExtendRoads()
        {
            var road = M("DirtRoad");

            // south: the main road runs on to the ferry crossing
            ClearVegetation(-10f, 86f, 10f, 136f);
            StripCollider(FlatQuad(expRoot, "Road_SouthFerry",
                new Vector3(0f, 0.03f, 99f), new Vector2(9f, 18f), road));

            // east: service road out to the geothermal plant
            ClearVegetation(24f, 40f, 136f, 57f);
            StripCollider(FlatQuad(expRoot, "Road_Plant",
                new Vector3(65.5f, 0.03f, 48f), new Vector2(77f, 9f), road));

            // west: haul road out to the cargo graveyard
            ClearVegetation(-137f, 13f, -24f, 49f);
            StripCollider(FlatQuad(expRoot, "Road_Cargo",
                new Vector3(-65.5f, 0.03f, 31f), new Vector2(77f, 9f), road));

            // north: footpath out to the aviary ruins
            ClearVegetation(-8f, -137f, 8f, -14f);
            StripCollider(FlatQuad(expRoot, "Path_Aviary",
                new Vector3(0f, 0.03f, -60f), new Vector2(8f, 88f), road));
        }

        // ------------------------------------------------------- south: river

        static void BuildRiverCrossing()
        {
            // the river (walkable ground stays underneath — you wade)
            var river = FlatQuad(expRoot, "River",
                new Vector3(0f, 0.05f, 112f), new Vector2(170f, 10f), M("Puddle"));
            StripCollider(river);

            Box(expRoot, "RiverBank_N", new Vector3(0f, 0.06f, 106.9f),
                new Vector3(170f, 0.12f, 1.6f), M("DirtRoad"));
            Box(expRoot, "RiverBank_S", new Vector3(0f, 0.06f, 117.1f),
                new Vector3(170f, 0.12f, 1.6f), M("DirtRoad"));

            // footbridge — planks keep their colliders, they ARE the deck
            for (float z = 107.6f; z <= 116.4f; z += 0.92f)
            {
                Box(expRoot, "Bridge_Plank",
                    new Vector3(rf(-0.05f, 0.05f), 0.16f, z),
                    new Vector3(rf(6.6f, 7f), 0.14f, 0.74f), M("WoodPlank"),
                    new Vector3(0f, rf(-1.5f, 1.5f), 0f));
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 3.35f;
                for (float z = 108.4f; z <= 115.6f; z += 2.4f)
                    Box(expRoot, "Bridge_Post", new Vector3(x, 0.68f, z),
                        new Vector3(0.14f, 1.15f, 0.14f), M("WoodDark"));
                Box(expRoot, "Bridge_Rail", new Vector3(x, 1.24f, 112f),
                    new Vector3(0.12f, 0.12f, 8.6f), M("WoodDark"));
            }

            // reeds along both banks (decor — no tripping)
            for (int i = 0; i < 40; i++)
            {
                float x = rf(-80f, 80f);
                if (Mathf.Abs(x) < 7f) continue;        // keep the ferry clear
                float z = chance(0.5f) ? rf(105.4f, 107.2f) : rf(116.8f, 118.6f);
                var reed = Box(expRoot, "Reed",
                    new Vector3(x, rf(0.3f, 0.6f), z),
                    new Vector3(0.07f, rf(0.7f, 1.3f), 0.07f), M("LeafMid"),
                    new Vector3(rf(-9f, 9f), rf(0f, 360f), rf(-9f, 9f)));
                StripCollider(reed);
            }

            // mooring bollards at the crossing
            for (int i = -1; i <= 1; i += 2)
                Cyl(expRoot, "Bollard", new Vector3(i * 7f, 0.3f, 117.8f),
                    0.3f, 0.6f, M("MetalDark"));

            // ---- Ranger Station 07 ----
            var st = new GameObject("RangerStation").transform;
            st.SetParent(expRoot, false);

            Box(st, "RS_Floor", new Vector3(0f, 0.1f, 126f),
                new Vector3(11f, 0.2f, 9f), M("ConcreteDirty"));
            // north wall (faces the bridge) with a door gap
            Box(st, "RS_WallN_L", new Vector3(-3.4f, 1.8f, 121.6f),
                new Vector3(4.2f, 3.4f, 0.3f), M("ConcreteDirty"));
            Box(st, "RS_WallN_R", new Vector3(3.4f, 1.8f, 121.6f),
                new Vector3(4.2f, 3.4f, 0.3f), M("ConcreteDirty"));
            Box(st, "RS_Lintel", new Vector3(0f, 3.15f, 121.6f),
                new Vector3(2.6f, 0.7f, 0.3f), M("ConcreteDirty"));
            Box(st, "RS_WallS", new Vector3(0f, 1.8f, 130.4f),
                new Vector3(11f, 3.4f, 0.3f), M("ConcreteDirty"));
            Box(st, "RS_WallW", new Vector3(-5.35f, 1.8f, 126f),
                new Vector3(0.3f, 3.4f, 9f), M("ConcreteDirty"));
            Box(st, "RS_WallE", new Vector3(5.35f, 1.8f, 126f),
                new Vector3(0.3f, 3.4f, 9f), M("ConcreteDirty"));
            Box(st, "RS_Roof", new Vector3(0f, 3.7f, 126f),
                new Vector3(11.8f, 0.35f, 9.8f), M("RoofMetal"));

            // desk with a dead radio in the corner
            Box(st, "RS_Desk", new Vector3(3.6f, 0.55f, 128.6f),
                new Vector3(2.6f, 0.9f, 1f), M("WoodDark"));
            Box(st, "RS_Radio", new Vector3(3.6f, 1.2f, 128.6f),
                new Vector3(0.8f, 0.5f, 0.5f), M("MetalDark"));
            Box(st, "RS_RadioGlow", new Vector3(3.6f, 1.28f, 128.32f),
                new Vector3(0.5f, 0.16f, 0.04f), M("ScreenGlow"));

            // warm lamp over the door — the one light still burning south side
            Box(st, "RS_LampHousing", new Vector3(0f, 3.05f, 122.4f),
                new Vector3(0.6f, 0.2f, 0.6f), M("MetalDark"));
            var lamp = NewPointLight(new Vector3(0f, 2.9f, 122.4f),
                new Color(1f, 0.82f, 0.55f), 1.6f, 13f, false);
            lamp.transform.SetParent(st, true);

            Sign("RANGER STATION 07", st, "RS_Sign",
                new Vector3(0f, 3.15f, 121.3f), 5.2f, 0.7f,
                M("SignYellow"), new Color(0.05f, 0.05f, 0.05f),
                FaceToward(Vector3.forward), 0.2f);

            // yard dressing
            BuildCrateStack(new Vector3(-7.6f, 0f, 123.5f), 3, 18f);
            BuildBarrelCluster(new Vector3(7.8f, 0f, 124.5f), 3, false);
        }

        // --------------------------------------------------------- east: plant

        static void BuildPowerPlant()
        {
            var p = new GameObject("GeothermalPlant").transform;
            p.SetParent(expRoot, false);

            Box(p, "Plant_Slab", new Vector3(119f, 0.1f, 46f),
                new Vector3(32f, 0.2f, 26f), M("Concrete"));
            Box(p, "Plant_Hall", new Vector3(119f, 4.6f, 40f),
                new Vector3(28f, 9f, 12f), M("ConcreteDark"));

            // lit window strips on the road-facing (west) wall
            for (int i = 0; i < 3; i++)
                Box(p, "Plant_Window",
                    new Vector3(104.97f, 5.6f, 36f + i * 4f),
                    new Vector3(0.06f, 1.2f, 3f),
                    i == 1 ? M("WarningLight") : M("ScreenGlow"));

            // stacks + steam pipe (walkable-under height)
            Cyl(p, "Plant_StackA", new Vector3(110f, 7f, 52f), 4f, 14f, M("MetalRust"));
            Cyl(p, "Plant_StackB", new Vector3(127f, 6.5f, 54f), 3.2f, 13f, M("MetalGray"));
            Cyl(p, "Plant_Pipe", new Vector3(119f, 2.6f, 50f), 1.2f, 24f,
                M("MetalRust"), new Vector3(0f, 0f, 90f));

            // control canopy where the engineer holds his post
            for (int i = 0; i < 4; i++)
            {
                float dx = (i & 1) == 0 ? -4f : 4f;
                float dz = (i & 2) == 0 ? -2.8f : 2.8f;
                Box(p, "Plant_Post", new Vector3(112f + dx, 1.5f, 56.2f + dz),
                    new Vector3(0.16f, 3f, 0.16f), M("MetalGray"));
            }
            Box(p, "Plant_Canopy", new Vector3(112f, 3.1f, 56.2f),
                new Vector3(9f, 0.3f, 7f), M("MetalRust"));
            Box(p, "Plant_Console", new Vector3(112f, 1f, 57.4f),
                new Vector3(3f, 1.2f, 0.9f), M("MetalDark"));
            Box(p, "Plant_ConsoleGlow", new Vector3(112f, 1.65f, 56.93f),
                new Vector3(2.2f, 0.5f, 0.05f), M("ScreenGlow"));
            var glow = NewPointLight(new Vector3(112f, 2.7f, 56.4f),
                new Color(0.55f, 1f, 0.7f), 1.2f, 10f, false);
            glow.transform.SetParent(p, true);

            // red strobe over the hall — visible from the road at night
            BuildBeacon(new Vector3(119f, 9.2f, 40f),
                BeaconRotator.Mode.Strobe, 0f, 0f, p);

            // signage
            Sign("GEOTHERMAL PLANT", p, "Plant_Sign",
                new Vector3(104.85f, 7f, 40f), 5.4f, 0.9f,
                M("SignWhite"), new Color(0.85f, 0.25f, 0.18f),
                FaceToward(Vector3.right), 0.24f);

            Sign("HIGH VOLTAGE", p, "Plant_Sign_Small",
                new Vector3(104.6f, 1.7f, 46.5f), 2.6f, 0.55f,
                M("SignYellow"), new Color(0.05f, 0.05f, 0.05f),
                FaceToward(Vector3.right), 0.18f);
            Box(p, "Plant_SignPost", new Vector3(104.7f, 0.85f, 46.5f),
                new Vector3(0.14f, 1.7f, 0.14f), M("MetalGray"));
        }

        // --------------------------------------------------------- west: cargo

        static void BuildCargoGraveyard()
        {
            // containers
            Box(expRoot, "Container_A", new Vector3(-110f, 1.35f, 20f),
                new Vector3(6.2f, 2.6f, 2.5f), M("MetalRust"),
                new Vector3(0f, 14f, 0f));
            Box(expRoot, "Container_B", new Vector3(-118f, 1.35f, 38f),
                new Vector3(6.2f, 2.6f, 2.5f), M("BarrelBlue"),
                new Vector3(0f, -8f, 0f));
            Box(expRoot, "Container_Tipped", new Vector3(-125f, 1.3f, 27f),
                new Vector3(6.2f, 2.6f, 2.5f), M("SignRed"),
                new Vector3(88f, 24f, 0f));

            // dinosaur transport crates — doors blown open, contents gone
            BuildTransportCrate(new Vector3(-112f, 0f, 44f), 200f);
            BuildTransportCrate(new Vector3(-127f, 0f, 16f), 40f);

            BuildJeep(new Vector3(-107.5f, 0f, 30f), 250f, true);
            BuildCrateStack(new Vector3(-113f, 0f, 24f), 4, 12f);
            BuildBarrelCluster(new Vector3(-120f, 0f, 34f), 4, true);

            // a fence panel torn out of the ground and dropped here
            Box(expRoot, "Fence_Fallen", new Vector3(-105f, 0.15f, 42f),
                new Vector3(5f, 0.12f, 3f), M("ChainLink"),
                new Vector3(0f, 30f, 0f));

            BuildBonePile(expRoot, new Vector3(-115f, 0f, 31f), 1.6f, rf(0f, 360f));
            BuildBlood(new Vector3(-108f, 0.05f, 36f), 2.6f, rf(0f, 360f));

            // steady red warning lamp at the road end
            Cyl(expRoot, "CargoLamp_Post", new Vector3(-104.8f, 1.6f, 36f),
                0.14f, 3.2f, M("MetalDark"));
            Ball(expRoot, "CargoLamp_Bulb", new Vector3(-104.8f, 3.3f, 36f),
                new Vector3(0.3f, 0.3f, 0.3f), M("WarningLight"));
            var l = NewPointLight(new Vector3(-104.8f, 3.4f, 36f),
                new Color(1f, 0.22f, 0.14f), 1.3f, 11f, false);
            l.transform.SetParent(expRoot, true);

            Sign("CARGO HOLD 02 \u2014 NO VISITORS", expRoot, "Sign_Cargo",
                new Vector3(-104.6f, 3f, 31f), 5.4f, 0.9f,
                M("SignWhite"), new Color(0.85f, 0.2f, 0.15f),
                FaceToward(Vector3.left), 0.2f);
            Box(expRoot, "Sign_CargoPostL", new Vector3(-104.9f, 1.5f, 29.2f),
                new Vector3(0.14f, 3f, 0.14f), M("MetalGray"));
            Box(expRoot, "Sign_CargoPostR", new Vector3(-104.9f, 1.5f, 32.8f),
                new Vector3(0.14f, 3f, 0.14f), M("MetalGray"));

            // something is standing on the blue container, watching the road
            var overlay = Object.FindObjectOfType<VhsOverlay>();
            var ambience = Object.FindObjectOfType<AmbientAudio>();
            PlaceFigure(new Vector3(-118f, 2.66f, 38f), 90f,
                overlay, ambience, 8f);
        }

        static void BuildTransportCrate(Vector3 pos, float yaw)
        {
            var root = new GameObject("DinoCrate");
            root.transform.SetParent(expRoot, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var t = root.transform;

            var metal = M("MetalGray");
            var rust = M("MetalRust");

            Box(t, "Crate_Floor", new Vector3(0f, 0.15f, 0f),
                new Vector3(3.4f, 0.3f, 2.4f), metal);
            for (int i = 0; i < 4; i++)
            {
                float dx = (i & 1) == 0 ? -1.6f : 1.6f;
                float dz = (i & 2) == 0 ? -1.1f : 1.1f;
                Box(t, "Crate_Post", new Vector3(dx, 1.4f, dz),
                    new Vector3(0.16f, 2.5f, 0.16f), rust);
            }
            Box(t, "Crate_Back", new Vector3(0f, 1.4f, -1.15f),
                new Vector3(3.4f, 2.5f, 0.12f), metal);
            Box(t, "Crate_SideL", new Vector3(-1.65f, 1.4f, 0f),
                new Vector3(0.12f, 2.5f, 2.4f), metal);
            Box(t, "Crate_SideR", new Vector3(1.65f, 1.4f, 0f),
                new Vector3(0.12f, 2.5f, 2.4f), metal);
            Box(t, "Crate_Roof", new Vector3(0f, 2.7f, 0f),
                new Vector3(3.5f, 0.16f, 2.5f), metal);
            // door blown open
            Box(t, "Crate_Door", new Vector3(1.1f, 1.4f, 1.6f),
                new Vector3(1.9f, 2.4f, 0.12f), rust, new Vector3(0f, 55f, 0f));
            // dead status light
            Box(t, "Crate_Status", new Vector3(-1f, 2.2f, -1.08f),
                new Vector3(0.3f, 0.16f, 0.05f), M("WarningLight"));
        }

        // -------------------------------------------------------- north: aviary

        static void BuildAviaryRuins()
        {
            const float cx = 0f;
            const float cz = -118f;
            const float rad = 14.5f;
            int panels = 14;

            for (int i = 0; i < panels; i++)
            {
                if (i == 3 || i == 4) continue;     // entry gap on the path side

                float a = i / (float)panels * 360f;
                Vector3 pos = new Vector3(
                    cx + Mathf.Cos(a * Mathf.Deg2Rad) * rad, 0f,
                    cz + Mathf.Sin(a * Mathf.Deg2Rad) * rad);

                if (i % 3 == 0)
                {
                    // collapsed panel, lying in the dirt
                    Box(expRoot, "Aviary_PanelDown",
                        pos + new Vector3(0f, 0.12f, 0f),
                        new Vector3(6f, 0.12f, 3f), M("ChainLink"),
                        new Vector3(0f, a + 90f, 0f));
                }
                else
                {
                    Box(expRoot, "Aviary_Panel", pos + Vector3.up * 2.1f,
                        new Vector3(6f, 4.2f, 0.12f), M("ChainLink"),
                        new Vector3(rf(-4f, 4f), a + 90f, rf(-3f, 3f)));
                    Box(expRoot, "Aviary_Post", pos + Vector3.up * 2.2f,
                        new Vector3(0.16f, 4.4f, 0.16f), M("MetalGray"));
                }
            }

            // collapsed dome beams
            for (int i = 0; i < 5; i++)
                Cyl(expRoot, "Aviary_Beam",
                    new Vector3(rf(-8f, 8f), rf(0.5f, 2.2f), cz + rf(-8f, 8f)),
                    0.24f, rf(6f, 11f), M("MetalGray"),
                    new Vector3(rf(50f, 80f), rf(0f, 360f), 0f));

            // nest with eggs on the feeding plinth
            Box(expRoot, "Aviary_Plinth", new Vector3(2.5f, 0.3f, -122f),
                new Vector3(3.6f, 0.6f, 3.6f), M("ConcreteDark"));
            for (int i = 0; i < 4; i++)
                Ball(expRoot, "Aviary_Egg",
                    new Vector3(2.5f + rf(-0.7f, 0.7f), 0.75f, -122f + rf(-0.7f, 0.7f)),
                    new Vector3(0.28f, 0.34f, 0.28f), M("Egg"));

            // feathers, blood, bones — whatever hatched didn't stay a bird
            for (int i = 0; i < 14; i++)
                Box(expRoot, "Aviary_Feather",
                    new Vector3(rf(-10f, 10f), 0.05f, rf(-128f, -110f)),
                    new Vector3(rf(0.15f, 0.4f), 0.03f, rf(0.05f, 0.1f)),
                    M("Litter"), new Vector3(0f, rf(0f, 360f), 0f));

            BuildBonePile(expRoot, new Vector3(-6f, 0f, -124f), 1.2f, rf(0f, 360f));
            BuildBlood(new Vector3(-2f, 0.05f, -114f), 2.4f, rf(0f, 360f));

            // entrance sign + strobe on a pole
            Sign("AVIARY \u2014 SECTOR C", expRoot, "Sign_Aviary",
                new Vector3(0f, 3.4f, -106.4f), 5f, 0.8f,
                M("SignWhite"), new Color(0.85f, 0.2f, 0.15f),
                FaceToward(Vector3.back), 0.22f);
            Box(expRoot, "Sign_AviaryPostL", new Vector3(-2.4f, 1.7f, -106.4f),
                new Vector3(0.14f, 3.4f, 0.14f), M("MetalGray"));
            Box(expRoot, "Sign_AviaryPostR", new Vector3(2.4f, 1.7f, -106.4f),
                new Vector3(0.14f, 3.4f, 0.14f), M("MetalGray"));
            Cyl(expRoot, "Aviary_BeaconPole", new Vector3(5.4f, 2.3f, -105.8f),
                0.18f, 4.6f, M("MetalGray"));
            BuildBeacon(new Vector3(5.4f, 4.7f, -105.8f),
                BeaconRotator.Mode.Strobe, 0f, 0f, expRoot);

            // it's been standing in the dark since the power died
            var overlay = Object.FindObjectOfType<VhsOverlay>();
            var ambience = Object.FindObjectOfType<AmbientAudio>();
            PlaceFigure(new Vector3(1.5f, 0f, -127f), 0f,
                overlay, ambience, 7.5f);
        }

        // -------------------------------------------------- mid-map dressing

        static void BuildMidMapDressing()
        {
            // wrecked tour jeeps — one on the main road, one on the plant road
            BuildJeep(new Vector3(-2.4f, 0f, 57f), 163f, true);
            BuildJeep(new Vector3(62f, 0f, 50.5f), 74f, true);

            // staff barricade across the main road: they tried to close this
            for (int i = 0; i < 4; i++)
            {
                float x = -3.75f + i * 2.5f;
                Box(expRoot, "Barricade_Board", new Vector3(x, 0.55f, 74f),
                    new Vector3(2.3f, 0.5f, 0.12f),
                    i % 2 == 0 ? M("SignYellow") : M("SignBlack"),
                    new Vector3(0f, rf(-5f, 5f), 0f));
            }
            Box(expRoot, "Barricade_LegL", new Vector3(-4.5f, 0.4f, 74f),
                new Vector3(0.16f, 0.8f, 0.5f), M("WoodDark"));
            Box(expRoot, "Barricade_LegR", new Vector3(4.5f, 0.4f, 74f),
                new Vector3(0.16f, 0.8f, 0.5f), M("WoodDark"));
            Sign("ROAD CLOSED", expRoot, "Sign_RoadClosed",
                new Vector3(0f, 1.7f, 74f), 3.6f, 0.55f,
                M("SignYellow"), new Color(0.06f, 0.06f, 0.06f),
                FaceToward(Vector3.forward), 0.2f);
            Box(expRoot, "Sign_RoadPostL", new Vector3(-1.7f, 1f, 74f),
                new Vector3(0.14f, 2f, 0.14f), M("WoodDark"));
            Box(expRoot, "Sign_RoadPostR", new Vector3(1.7f, 1f, 74f),
                new Vector3(0.14f, 2f, 0.14f), M("WoodDark"));

            for (int i = 0; i < 5; i++)
                Cyl(expRoot, "TrafficCone",
                    new Vector3(rf(-5f, 5f), 0.24f, rf(72.2f, 76f)),
                    0.3f, 0.48f, M("SignYellow"));

            // sandbag checkpoint on the west branch
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 6 - row; i++)
                    Box(expRoot, "Sandbag",
                        new Vector3(-20f + i * 0.95f + row * 0.45f,
                            0.17f + row * 0.29f, 33.6f),
                        new Vector3(0.9f, 0.28f, 0.5f), M("ConcreteDirty"),
                        new Vector3(0f, rf(-7f, 7f), 0f));

            // distance marker to the gate
            Sign("MAIN GATE 0.4 KM", expRoot, "Sign_Km",
                new Vector3(5.6f, 1.5f, 30f), 3.4f, 0.5f,
                M("SignWhite"), new Color(0.08f, 0.08f, 0.08f),
                FaceToward(Vector3.forward), 0.17f);
            Box(expRoot, "Sign_KmPost", new Vector3(5.6f, 0.75f, 30f),
                new Vector3(0.12f, 1.5f, 0.12f), M("MetalGray"));

            // carnage the original cameras never cut away from
            BuildBlood(new Vector3(4.4f, 0.05f, 44f), 2.4f, rf(0f, 360f));
            BuildBlood(new Vector3(-3.6f, 0.05f, 80f), 1.8f, rf(0f, 360f));
            BuildBonePile(expRoot, new Vector3(-4.6f, 0f, 68f), 1.1f, rf(0f, 360f));
        }
    }
}
