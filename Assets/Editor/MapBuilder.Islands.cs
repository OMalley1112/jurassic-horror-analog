using UnityEngine;
using AnalogHorror;

namespace AnalogHorror.EditorTools
{
    public static partial class MapBuilder
    {
        // ======================================================== ISLAND 2
        // Two islands now. Island one (west) is the movie park — visitor
        // center, paddocks, main gate. Island two (east) across the channel
        // is the NEW park, Jurassic World: main street with shops, the
        // Innovation Center, gyrosphere valley, paddock 09 (breached),
        // the raptor research pen and the monorail. A tour ferry at each
        // dock (IslandFerry) jumps you between them with a tape glitch.
        //
        // Runs LAST in BuildAll — every rf() here only shifts content
        // inside this file, the verified map above is untouched.

        static Transform iwRoot;

        static void BuildIslandWorld()
        {
            iwRoot = NewRoot("_ISLAND 2 JURASSIC WORLD");

            // the player + systems we wire the ferry to
            var playerGo = GameObject.Find("Player");
            var dirGo = GameObject.Find("HorrorDirector");
            var fpc = playerGo != null ? playerGo.GetComponent<FirstPersonController>() : null;
            var director = dirGo != null ? dirGo.GetComponent<HorrorDirector>() : null;
            var overlay = fpc != null && fpc.camHolder != null
                ? fpc.camHolder.GetComponent<VhsOverlay>() : null;

            IslandMaterials();
            IslandWaterAndGround();
            Island1Dock(playerGo, fpc, director, overlay);
            Island2Dock(playerGo, fpc, director, overlay);
            IslandMainStreet();
            IslandInnovationCenter();
            IslandGyrosphereValley();
            IslandIndominusPaddock();
            IslandRaptorPen();
            IslandMonorail();
            IslandDressing();
        }

        // ---------------------------------------------------- materials

        static void IslandMaterials()
        {
            // Mat() just registers/creates — no rng, safe anywhere
            Mat("OceanWater", new Color(0.02f, 0.05f, 0.075f), 0.85f, 0.1f);
            Mat("BoatHull", new Color(0.5f, 0.52f, 0.52f), 0.4f, 0.1f);
            Mat("GlassJW", new Color(0.2f, 0.3f, 0.35f), 0.9f, 0.1f);
            Mat("PaddockWhite", new Color(0.62f, 0.62f, 0.58f), 0.25f);
            Mat("IndoHide", new Color(0.72f, 0.72f, 0.66f), 0.3f);
            Mat("IndoBelly", new Color(0.84f, 0.82f, 0.76f), 0.3f);
            Mat("IndoSpine", new Color(0.42f, 0.42f, 0.4f), 0.3f);
            Mat("ShopTan", new Color(0.42f, 0.36f, 0.28f), 0.25f);
            Mat("ShopBrick", new Color(0.36f, 0.2f, 0.16f), 0.25f);
            Mat("ShopCream", new Color(0.55f, 0.52f, 0.44f), 0.25f);
            Mat("MonorailWhite", new Color(0.68f, 0.69f, 0.7f), 0.5f, 0.15f);
            Mat("GyroWhite", new Color(0.75f, 0.76f, 0.74f), 0.5f);
        }

        // -------------------------------------------------- water / ground

        static void IslandWaterAndGround()
        {
            // the channel + the sea all around island two (visual only —
            // both docks are sealed by walls, nothing can walk on water)
            var ocean = FlatQuad(iwRoot, "Ocean_Channel",
                new Vector3(244f, -0.35f, 0f), new Vector2(216f, 344f), M("OceanWater"));
            StripCollider(ocean);

            // island two itself
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Island2_Ground";
            ground.transform.SetParent(iwRoot, false);
            ground.transform.localPosition = new Vector3(236f, 0f, 0f);
            ground.transform.localScale = new Vector3(14f, 1f, 20f);   // 140 x 200
            ground.GetComponent<MeshRenderer>().sharedMaterial = M("GrassDark");

            // rocky shore lip so the island doesn't float over the water
            var rock = M("Rock");
            Box(iwRoot, "Island2_ShoreW", new Vector3(166f, -0.3f, 0f),
                new Vector3(0.9f, 0.8f, 100.9f), rock);
            Box(iwRoot, "Island2_ShoreE", new Vector3(306f, -0.3f, 0f),
                new Vector3(0.9f, 0.8f, 100.9f), rock);
            Box(iwRoot, "Island2_ShoreN", new Vector3(236f, -0.3f, -100f),
                new Vector3(140.9f, 0.8f, 0.9f), rock);
            Box(iwRoot, "Island2_ShoreS", new Vector3(236f, -0.3f, 100f),
                new Vector3(140.9f, 0.8f, 0.9f), rock);

            // jungle boundary hedge around island two (gap at the ferry dock)
            var hedge = M("Hedge");
            Box(iwRoot, "Island2_BoundaryN", new Vector3(236f, 4f, -97f),
                new Vector3(140f, 8f, 6f), hedge);
            Box(iwRoot, "Island2_BoundaryS", new Vector3(236f, 4f, 97f),
                new Vector3(140f, 8f, 6f), hedge);
            Box(iwRoot, "Island2_BoundaryE", new Vector3(303f, 4f, 0f),
                new Vector3(6f, 8f, 200f), hedge);
            Box(iwRoot, "Island2_BoundaryW_N", new Vector3(169f, 4f, -50.75f),
                new Vector3(6f, 8f, 92.5f), hedge);
            Box(iwRoot, "Island2_BoundaryW_S", new Vector3(169f, 4f, 50.75f),
                new Vector3(6f, 8f, 92.5f), hedge);

            // walkway: ferry dock -> arch -> main street
            StripCollider(FlatQuad(iwRoot, "JW_Walkway",
                new Vector3(172f, 0.03f, 0f), new Vector2(8f, 9f), M("Concrete")));
        }

        // ------------------------------------------------------ ferry docks

        static void Island1Dock(GameObject playerGo, FirstPersonController fpc,
            HorrorDirector director, VhsOverlay overlay)
        {
            // clear jungle off the new ferry road + pier approach
            ClearVegetation(4f, -6f, 158f, 6f);

            // the ferry road: main road -> east -> the pier
            StripCollider(FlatQuad(envRoot, "Road_Ferry",
                new Vector3(69f, 0.03f, 0f), new Vector2(130f, 7f), M("DirtRoad")));

            var conc = M("Concrete");
            var rail = M("MetalDark");
            var deckMat = M("WoodPlank");

            // pier: spans the hedge gap, runs out over the channel
            Box(iwRoot, "FerryPier1_Deck", new Vector3(142f, 0.1f, 0f),
                new Vector3(16f, 0.3f, 9f), deckMat);
            Box(iwRoot, "FerryPier1_WallN", new Vector3(142f, 0.875f, -4.3f),
                new Vector3(16f, 1.25f, 0.4f), conc);
            Box(iwRoot, "FerryPier1_WallS", new Vector3(142f, 0.875f, 4.3f),
                new Vector3(16f, 1.25f, 0.4f), conc);
            Box(iwRoot, "FerryPier1_RailN", new Vector3(142f, 1.65f, -4.3f),
                new Vector3(16f, 0.1f, 0.1f), rail);
            Box(iwRoot, "FerryPier1_RailS", new Vector3(142f, 1.65f, 4.3f),
                new Vector3(16f, 0.1f, 0.1f), rail);
            Box(iwRoot, "FerryPier1_End", new Vector3(149.8f, 0.875f, 0f),
                new Vector3(0.4f, 1.25f, 9f), conc);
            Box(iwRoot, "FerryPier1_EndRail", new Vector3(149.8f, 1.65f, 0f),
                new Vector3(0.1f, 0.1f, 9f), rail);

            // sign over the pier mouth, readable walking east
            Cyl(iwRoot, "FerrySign1_PostN", new Vector3(131f, 1.6f, -3.6f),
                0.14f, 3.2f, M("MetalGray"));
            Cyl(iwRoot, "FerrySign1_PostS", new Vector3(131f, 1.6f, 3.6f),
                0.14f, 3.2f, M("MetalGray"));
            Sign("FERRY \u2014 JURASSIC WORLD", iwRoot, "Sign_Ferry1",
                new Vector3(131f, 3.0f, 0f), 7f, 1.1f, M("SignWhite"),
                new Color(0.07f, 0.07f, 0.07f), FaceToward(Vector3.left), 0.26f);

            BuildFerryBoat(146f, -7.8f, "FerryBoat1");

            var ferry = new GameObject("Ferry_West");
            ferry.transform.SetParent(iwRoot, false);
            ferry.transform.position = new Vector3(147f, 1f, 0f);
            var isl = ferry.AddComponent<IslandFerry>();
            isl.player = playerGo != null ? playerGo.transform : null;
            isl.controller = fpc;
            isl.director = director;
            isl.overlay = overlay;
            isl.targetPos = new Vector3(162f, 0.35f, 0f);
            isl.targetYaw = 90f;                 // arrive facing east, into the new park
            isl.destination = "JURASSIC WORLD";
        }

        static void Island2Dock(GameObject playerGo, FirstPersonController fpc,
            HorrorDirector director, VhsOverlay overlay)
        {
            var conc = M("Concrete");
            var rail = M("MetalDark");
            var deckMat = M("WoodPlank");

            // mirror pier on the far shore
            Box(iwRoot, "FerryPier2_Deck", new Vector3(160f, 0.1f, 0f),
                new Vector3(16f, 0.3f, 9f), deckMat);
            Box(iwRoot, "FerryPier2_WallN", new Vector3(160f, 0.875f, -4.3f),
                new Vector3(16f, 1.25f, 0.4f), conc);
            Box(iwRoot, "FerryPier2_WallS", new Vector3(160f, 0.875f, 4.3f),
                new Vector3(16f, 1.25f, 0.4f), conc);
            Box(iwRoot, "FerryPier2_RailN", new Vector3(160f, 1.65f, -4.3f),
                new Vector3(16f, 0.1f, 0.1f), rail);
            Box(iwRoot, "FerryPier2_RailS", new Vector3(160f, 1.65f, 4.3f),
                new Vector3(16f, 0.1f, 0.1f), rail);
            Box(iwRoot, "FerryPier2_End", new Vector3(152.2f, 0.875f, 0f),
                new Vector3(0.4f, 1.25f, 9f), conc);
            Box(iwRoot, "FerryPier2_EndRail", new Vector3(152.2f, 1.65f, 0f),
                new Vector3(0.1f, 0.1f, 9f), rail);

            BuildFerryBoat(158f, 7.8f, "FerryBoat2");

            // arrival arch: the movie gate, but the new park's logo
            Box(iwRoot, "JWGate_PillarN", new Vector3(176f, 4f, -5.5f),
                new Vector3(1.4f, 8f, 1.4f), conc);
            Box(iwRoot, "JWGate_PillarS", new Vector3(176f, 4f, 5.5f),
                new Vector3(1.4f, 8f, 1.4f), conc);
            Box(iwRoot, "JWGate_Beam", new Vector3(176f, 8.4f, 0f),
                new Vector3(1.4f, 1.4f, 12.4f), conc);

            var cream = new Color(0.86f, 0.87f, 0.85f);
            Sign("JURASSIC WORLD", iwRoot, "Sign_JWArch_W",
                new Vector3(175.2f, 8.3f, 0f), 11f, 1.5f, M("SignBlack"),
                cream, FaceToward(Vector3.left), 0.3f);
            Sign("JURASSIC WORLD", iwRoot, "Sign_JWArch_E",
                new Vector3(176.8f, 8.3f, 0f), 11f, 1.5f, M("SignBlack"),
                cream, FaceToward(Vector3.right), 0.3f);

            var ferry = new GameObject("Ferry_East");
            ferry.transform.SetParent(iwRoot, false);
            ferry.transform.position = new Vector3(156f, 1f, 0f);
            var isl = ferry.AddComponent<IslandFerry>();
            isl.player = playerGo != null ? playerGo.transform : null;
            isl.controller = fpc;
            isl.director = director;
            isl.overlay = overlay;
            isl.targetPos = new Vector3(142f, 0.35f, 0f);
            isl.targetYaw = 270f;                // arrive facing west, back at the old park
            isl.destination = "THE OLD PARK";
        }

        /// <summary>Moored tour ferry beside a pier — pure scenery.</summary>
        static void BuildFerryBoat(float cx, float cz, string prefix)
        {
            var hull = M("BoatHull");
            Box(iwRoot, prefix + "_Hull", new Vector3(cx, 0.55f, cz),
                new Vector3(14f, 1.9f, 6f), hull);
            Box(iwRoot, prefix + "_Stripe", new Vector3(cx, 1.2f, cz),
                new Vector3(14.1f, 0.3f, 6.1f), M("SignRed"));
            Box(iwRoot, prefix + "_Cabin", new Vector3(cx - 1f, 2.1f, cz),
                new Vector3(7f, 1.2f, 4.4f), hull);

            // windows on the cabin face that looks at the pier
            float winZ = cz + (cz < 0f ? 2.25f : -2.25f);
            for (int i = -1; i <= 1; i++)
                Box(iwRoot, prefix + "_Win", new Vector3(cx - 1f + i * 2f, 2.2f, winZ),
                    new Vector3(1.5f, 0.5f, 0.12f), M("SignBlack"));

            Box(iwRoot, prefix + "_Wheel", new Vector3(cx + 3.2f, 2.95f, cz),
                new Vector3(2.6f, 1f, 3f), hull);
            Cyl(iwRoot, prefix + "_Mast", new Vector3(cx + 3.2f, 4.1f, cz),
                0.12f, 1.6f, M("MetalGray"));
        }

        // ------------------------------------------------------ main street

        static void IslandMainStreet()
        {
            // promenade from the arch to the Innovation Center
            StripCollider(FlatQuad(iwRoot, "JW_MainStreet",
                new Vector3(211f, 0.03f, 0f), new Vector2(70f, 14f), M("Concrete")));

            var gutter = M("ConcreteDirty");
            Box(iwRoot, "JW_GutterN", new Vector3(211f, 0.05f, 7.4f),
                new Vector3(70f, 0.1f, 0.8f), gutter);
            Box(iwRoot, "JW_GutterS", new Vector3(211f, 0.05f, -7.4f),
                new Vector3(70f, 0.1f, 0.8f), gutter);

            // north row (fronts face the street, -Z)
            JWShop(185f, 12.5f, 11f, 9f, 6.5f, M("ShopTan"), "GIFT SHOP",
                -1f, Vector3.back, M("SignRed"));
            JWShop(198f, 12.5f, 11f, 9f, 7.5f, M("ShopBrick"), "JURASSIC CAFE",
                -1f, Vector3.back, M("SignYellow"));
            JWShop(211f, 12.5f, 11f, 9f, 5.5f, M("ShopCream"), "TOUR PHOTOS",
                -1f, Vector3.back, M("SignRed"));
            JWShop(224f, 12.5f, 11f, 9f, 8f, M("ShopTan"), "CINEMA 3D",
                -1f, Vector3.back, M("SignYellow"));
            JWShop(237f, 12.5f, 11f, 9f, 6f, M("ShopBrick"), "BOOKING OFFICE",
                -1f, Vector3.back, M("SignRed"));

            // south row (fronts face +Z)
            JWShop(191f, -12.5f, 12f, 9f, 6f, M("ShopCream"), "OUTFITTERS",
                1f, Vector3.forward, M("SignYellow"));
            JWShop(205f, -12.5f, 12f, 9f, 7f, M("ShopTan"), "BURGER GRILL",
                1f, Vector3.forward, M("SignRed"));
            JWShop(219f, -12.5f, 12f, 9f, 5.5f, M("ShopBrick"), "FIRST AID",
                1f, Vector3.forward, M("SignYellow"));
            JWShop(233f, -12.5f, 12f, 9f, 7.5f, M("ShopCream"), "MUSEUM SHOP",
                1f, Vector3.forward, M("SignRed"));

            // street lamps — only some carry a real light (perf)
            JWLamp(182f, 6.4f, false);
            JWLamp(196f, 6.4f, true);
            JWLamp(210f, 6.4f, false);
            JWLamp(224f, 6.4f, true);
            JWLamp(238f, 6.4f, false);
            JWLamp(189f, -6.4f, false);
            JWLamp(203f, -6.4f, true);
            JWLamp(217f, -6.4f, false);
            JWLamp(231f, -6.4f, true);
            JWLamp(245f, -6.4f, false);

            // benches
            Bench(216f, 5.6f);
            Bench(198f, -5.6f);
        }

        static void JWShop(float cx, float cz, float w, float d, float h,
            Material facade, string label, float frontSign, Vector3 faceDir,
            Material awning)
        {
            Box(iwRoot, "JWShop_Body", new Vector3(cx, h * 0.5f, cz),
                new Vector3(w, h, d), facade);

            float frontZ = cz + frontSign * (d * 0.5f);
            float glassZ = frontZ + frontSign * 0.06f;

            Box(iwRoot, "JWShop_Glass", new Vector3(cx, h * 0.34f, glassZ),
                new Vector3(w * 0.84f, h * 0.5f, 0.14f), M("GlassJW"));
            Box(iwRoot, "JWShop_Door", new Vector3(cx, h * 0.17f, glassZ + frontSign * 0.04f),
                new Vector3(1.8f, h * 0.34f, 0.12f), M("SignBlack"));
            Box(iwRoot, "JWShop_Upper", new Vector3(cx, h * 0.78f, glassZ),
                new Vector3(w * 0.7f, h * 0.2f, 0.12f), M("GlassJW"));
            Box(iwRoot, "JWShop_Awning", new Vector3(cx, h * 0.6f, frontZ + frontSign * 0.7f),
                new Vector3(w * 0.92f, 0.12f, 1.5f), awning);
            Box(iwRoot, "JWShop_Parapet", new Vector3(cx, h + 0.15f, cz),
                new Vector3(w + 0.3f, 0.3f, d + 0.3f), M("ConcreteDark"));

            Sign(label, iwRoot, "JWShop_Sign",
                new Vector3(cx, h * 0.72f, glassZ + frontSign * 0.03f),
                w * 0.72f, 0.75f, M("SignBlack"), new Color(0.88f, 0.86f, 0.78f),
                FaceToward(faceDir), 0.2f);
        }

        static void JWLamp(float x, float z, bool withLight)
        {
            Cyl(iwRoot, "JWLamp_Post", new Vector3(x, 1.7f, z),
                0.13f, 3.4f, M("MetalGray"));
            var bulb = Ball(iwRoot, "JWLamp_Bulb", new Vector3(x, 3.55f, z),
                new Vector3(0.4f, 0.3f, 0.4f), M("LightWarm"));
            StripCollider(bulb);

            if (withLight)
            {
                var l = NewPointLight(new Vector3(x, 3.5f, z),
                    new Color(1f, 0.9f, 0.68f), 1.1f, 11f, false);
                l.transform.SetParent(iwRoot, true);
            }
        }

        static void Bench(float x, float z)
        {
            Box(iwRoot, "JWBench_Seat", new Vector3(x, 0.42f, z),
                new Vector3(2f, 0.1f, 0.55f), M("WoodDark"));
            Box(iwRoot, "JWBench_Back", new Vector3(x, 0.72f, z + Mathf.Sign(z) * 0.25f),
                new Vector3(2f, 0.5f, 0.1f), M("WoodDark"));
        }

        // ------------------------------------------------- innovation center

        static void IslandInnovationCenter()
        {
            var conc = M("Concrete");

            Box(iwRoot, "Innov_Body", new Vector3(261f, 5f, 0f),
                new Vector3(22f, 10f, 26f), conc);
            Box(iwRoot, "Innov_Parapet", new Vector3(261f, 10.15f, 0f),
                new Vector3(22.6f, 0.3f, 26.6f), M("ConcreteDark"));

            // street-side glass with a doorway gap (doors slid open)
            Box(iwRoot, "Innov_GlassN", new Vector3(249.9f, 3.5f, 6.4f),
                new Vector3(0.2f, 7f, 5.2f), M("GlassJW"));
            Box(iwRoot, "Innov_GlassS", new Vector3(249.9f, 3.5f, -6.4f),
                new Vector3(0.2f, 7f, 5.2f), M("GlassJW"));
            Box(iwRoot, "Innov_DoorN", new Vector3(249.7f, 1.6f, 3.9f),
                new Vector3(0.3f, 3.2f, 2.6f), M("MetalGray"));
            Box(iwRoot, "Innov_DoorS", new Vector3(249.7f, 1.6f, -3.9f),
                new Vector3(0.3f, 3.2f, 2.6f), M("MetalGray"));

            // glass dome
            var dome = Ball(iwRoot, "Innov_Dome", new Vector3(261f, 10.4f, 0f),
                new Vector3(17f, 7.6f, 17f), M("GlassJW"));
            StripCollider(dome);

            var cream = new Color(0.88f, 0.85f, 0.72f);
            Sign("INNOVATION CENTER", iwRoot, "Sign_Innov",
                new Vector3(249.4f, 7.6f, 0f), 14f, 1.5f, M("SignBlack"),
                cream, FaceToward(Vector3.left), 0.34f);

            // ---- the lobby's hanging fossil cast (walk in and look up) ----
            Box(iwRoot, "Fossil_Spine", new Vector3(261f, 6f, 0f),
                new Vector3(0.45f, 0.45f, 11f), M("Bone"));
            for (int i = 0; i < 6; i++)
            {
                float z = -4.4f + i * 1.7f;
                Box(iwRoot, "Fossil_RibL", new Vector3(259.7f, 4.9f, z),
                    new Vector3(0.16f, 2.6f, 0.16f), M("Bone"), new Vector3(0f, 0f, 26f));
                Box(iwRoot, "Fossil_RibR", new Vector3(262.3f, 4.9f, z),
                    new Vector3(0.16f, 2.6f, 0.16f), M("Bone"), new Vector3(0f, 0f, -26f));
            }
            Box(iwRoot, "Fossil_Skull", new Vector3(261f, 5.7f, 6.4f),
                new Vector3(1.7f, 1.5f, 2.8f), M("Bone"), new Vector3(6f, 0f, 0f));
            Box(iwRoot, "Fossil_Jaw", new Vector3(261f, 4.85f, 7f),
                new Vector3(1.3f, 0.5f, 2.2f), M("Bone"), new Vector3(14f, 0f, 0f));
            Box(iwRoot, "Fossil_SocketL", new Vector3(260.4f, 6f, 7.6f),
                new Vector3(0.3f, 0.3f, 0.15f), M("SignBlack"));
            Box(iwRoot, "Fossil_SocketR", new Vector3(261.6f, 6f, 7.6f),
                new Vector3(0.3f, 0.3f, 0.15f), M("SignBlack"));
            Cyl(iwRoot, "Fossil_ChainA", new Vector3(261f, 8.2f, -4f),
                0.05f, 4f, M("MetalDark"));
            Cyl(iwRoot, "Fossil_ChainB", new Vector3(261f, 8.2f, 4f),
                0.05f, 4f, M("MetalDark"));

            // plaque on a stand
            Cyl(iwRoot, "Fossil_PlaquePost", new Vector3(256f, 0.55f, 5.5f),
                0.1f, 1.1f, M("MetalGray"));
            Sign("INDOMINUS REX\nSCALE CAST \u2014 INGEN", iwRoot, "Fossil_Plaque",
                new Vector3(256f, 1.3f, 5.5f), 3.4f, 1.1f, M("SignWhite"),
                new Color(0.1f, 0.1f, 0.1f), FaceToward(Vector3.left), 0.15f);

            var l = NewPointLight(new Vector3(261f, 5.5f, 0f),
                new Color(1f, 0.92f, 0.72f), 1.5f, 18f, false);
            l.transform.SetParent(iwRoot, true);
        }

        // ------------------------------------------------- gyrosphere valley

        static void IslandGyrosphereValley()
        {
            // glass fence along the valley edge — panels missing where
            // something went through
            var glass = M("GlassJW");
            for (float x = 250f; x <= 296f; x += 6f)
            {
                Cyl(iwRoot, "GyroFence_Post", new Vector3(x, 1.3f, 38f),
                    0.1f, 2.6f, M("MetalGray"));
                if (x > 262f && x < 274f) continue;           // the breach
                Box(iwRoot, "GyroFence_Panel", new Vector3(x + 2.9f, 1.3f, 38f),
                    new Vector3(5.6f, 2.4f, 0.08f), glass);
            }
            // one panel torn down, lying in the grass
            Box(iwRoot, "GyroFence_Down", new Vector3(268f, 0.15f, 41f),
                new Vector3(5.6f, 0.1f, 2.4f), glass, new Vector3(5f, 18f, 0f));

            Sign("GYROSPHERE VALLEY\nRIDE AT YOUR OWN RISK", iwRoot, "Sign_Gyro",
                new Vector3(250f, 2.7f, 37.2f), 7.5f, 1.7f, M("SignWhite"),
                new Color(0.08f, 0.08f, 0.08f), FaceToward(Vector3.back), 0.2f);
            Cyl(iwRoot, "GyroSign_PostN", new Vector3(247f, 1.4f, 37.2f),
                0.12f, 2.8f, M("MetalGray"));
            Cyl(iwRoot, "GyroSign_PostS", new Vector3(253f, 1.4f, 37.2f),
                0.12f, 2.8f, M("MetalGray"));

            // the spheres themselves
            JWGyro(258f, 50f, 0f);
            JWGyro(266f, 58f, 40f);
            JWGyro(274f, 48f, 120f);
            JWGyro(284f, 62f, 210f);
            // the crashed one, half on its side (solid — you bump into it)
            Ball(iwRoot, "Gyro_Wreck", new Vector3(289f, 1.1f, 52f),
                new Vector3(3f, 3f, 3f), M("GyroWhite"), new Vector3(28f, 50f, 0f));
            for (int i = 0; i < 3; i++)
                Box(iwRoot, "Gyro_Shard", new Vector3(287f + i * 1.1f, 0.1f, 49.5f + i),
                    new Vector3(0.7f, 0.06f, 0.5f), glass, new Vector3(0f, rf(0f, 360f), 0f));

            // a herd member grazing past the broken fence
            // (BuildSauropod parents to jungRoot, not padRoot)
            int n = jungRoot.childCount;
            BuildSauropod(new Vector3(272f, 0f, 82f), rf(0f, 360f));
            jungRoot.GetChild(n).SetParent(iwRoot, true);
        }

        static void JWGyro(float x, float z, float yaw)
        {
            // solid sphere — you bounce off it like a real gyrosphere
            Ball(iwRoot, "Gyro_Body", new Vector3(x, 1.5f, z),
                new Vector3(3f, 3f, 3f), M("GyroWhite"),
                new Vector3(0f, yaw, 0f));
            // the dark observation window, both sides
            var win = M("SignBlack");
            var rot = new Vector3(0f, yaw, 90f);
            var off = Quaternion.Euler(0f, yaw, 0f) * new Vector3(1.45f, 0.1f, 0f);
            var w1 = Cyl(iwRoot, "Gyro_Win", new Vector3(x + off.x, 1.6f, z + off.z),
                1.5f, 0.16f, win, rot);
            StripCollider(w1);
            var w2 = Cyl(iwRoot, "Gyro_Win", new Vector3(x - off.x, 1.6f, z - off.z),
                1.5f, 0.16f, win, rot);
            StripCollider(w2);
        }

        // -------------------------------------------------- paddock 09

        static void IslandIndominusPaddock()
        {
            var wall = M("PaddockWhite");

            // tall perimeter — south side is open where she went through
            Box(iwRoot, "Pad09_N", new Vector3(254f, 4.5f, -88f),
                new Vector3(48f, 9f, 1.2f), wall);
            Box(iwRoot, "Pad09_W", new Vector3(230f, 4.5f, -69f),
                new Vector3(1.2f, 9f, 38f), wall);
            Box(iwRoot, "Pad09_E", new Vector3(278f, 4.5f, -69f),
                new Vector3(1.2f, 9f, 38f), wall);
            Box(iwRoot, "Pad09_S_West", new Vector3(236f, 4.5f, -50f),
                new Vector3(12f, 9f, 1.2f), wall);
            Box(iwRoot, "Pad09_S_East", new Vector3(270f, 4.5f, -50f),
                new Vector3(16f, 9f, 1.2f), wall);

            // the fallen slab, tossed into the grass
            Box(iwRoot, "Pad09_Fallen", new Vector3(250f, 0.6f, -45f),
                new Vector3(14f, 1.2f, 3f), wall, new Vector3(84f, 18f, 0f));

            // claw gouges beside the breach
            for (int i = 0; i < 3; i++)
                Box(iwRoot, "Pad09_Claw", new Vector3(237.4f + i, 5f, -49.3f),
                    new Vector3(0.3f, 4f, 0.08f), M("BloodDark"),
                    new Vector3(0f, 0f, 8f + i * 5f));

            // dirt floor inside
            StripCollider(FlatQuad(iwRoot, "Pad09_Floor",
                new Vector3(254f, 0.04f, -69f), new Vector2(46f, 36f), M("DirtRoad")));

            Sign("PADDOCK 09\nINDOMINUS REX", iwRoot, "Sign_Pad09",
                new Vector3(236f, 6.6f, -49.2f), 8f, 2.2f, M("SignYellow"),
                new Color(0.15f, 0.03f, 0.02f), FaceToward(Vector3.forward), 0.3f);

            BuildWarningLamp(new Vector3(231.6f, 0f, -52f));
            BuildWarningLamp(new Vector3(276f, 0f, -52f));

            // the path from main street to the breach (threaded between
            // the street end and the Innovation Center wall)
            StripCollider(FlatQuad(iwRoot, "Pad09_Approach",
                new Vector3(248f, 0.03f, -22f), new Vector2(3f, 56f), M("DirtRoad")));

            // HER — white, already out of the paddock
            int n = padRoot.childCount;
            BuildTRexDino(new Vector3(246f, 0f, -72f), 40f);
            var indo = padRoot.GetChild(n);
            TintDino(indo.gameObject, M("IndoHide"), M("IndoBelly"), M("IndoSpine"));
            indo.SetParent(iwRoot, true);

            // and something already loose on the lawn between street and pads
            int m = padRoot.childCount;
            BuildRaptorDino(new Vector3(220f, 0f, -30f), rf(0f, 360f));
            padRoot.GetChild(m).SetParent(iwRoot, true);

            BuildBlood(new Vector3(253f, 0.06f, -44f), 2.2f, rf(0f, 360f));
            BuildBlood(new Vector3(247f, 0.06f, -41f), 1.4f, rf(0f, 360f));
        }

        /// <summary>Swap a builder-made dinosaur to the Indominus palette.</summary>
        static void TintDino(GameObject root, Material hide, Material belly, Material spine)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var m = r.sharedMaterial;
                if (m == null) continue;
                if (m.name == "DinoHide") r.sharedMaterial = hide;
                else if (m.name == "DinoBelly") r.sharedMaterial = belly;
                else if (m.name == "DinoSpine") r.sharedMaterial = spine;
            }
        }

        // -------------------------------------------------- raptor research

        static void IslandRaptorPen()
        {
            // chainlink pen with a breach on the south side
            JWFenceRun(new Vector3(190f, 0f, 44f), new Vector3(222f, 0f, 44f));
            JWFenceRun(new Vector3(222f, 0f, 44f), new Vector3(222f, 0f, 66f));
            JWFenceRun(new Vector3(222f, 0f, 66f), new Vector3(206f, 0f, 66f));
            JWFenceRun(new Vector3(200f, 0f, 66f), new Vector3(190f, 0f, 66f));
            JWFenceRun(new Vector3(190f, 0f, 66f), new Vector3(190f, 0f, 44f));

            StripCollider(FlatQuad(iwRoot, "RaptorPen_Floor",
                new Vector3(206f, 0.04f, 55f), new Vector2(30f, 20f), M("DirtRoad")));

            // observation catwalk along the south fence + access ramp
            Box(iwRoot, "Catwalk_Deck", new Vector3(206f, 3.2f, 67.2f),
                new Vector3(32f, 0.15f, 1.6f), M("MetalGray"));
            Box(iwRoot, "Catwalk_Rail", new Vector3(206f, 4.1f, 68f),
                new Vector3(32f, 0.1f, 0.1f), M("MetalDark"));
            for (float x = 191f; x <= 221f; x += 5f)
                Cyl(iwRoot, "Catwalk_Post", new Vector3(x, 3.7f, 68f),
                    0.07f, 1.6f, M("MetalDark"));
            for (float x = 192f; x <= 220f; x += 7f)
                Cyl(iwRoot, "Catwalk_Leg", new Vector3(x, 1.6f, 67.2f),
                    0.16f, 3.2f, M("MetalGray"));
            Box(iwRoot, "Catwalk_Ramp", new Vector3(226f, 1.6f, 67.2f),
                new Vector3(9f, 0.15f, 1.6f), M("MetalGray"), new Vector3(0f, 0f, -22f));

            Sign("RAPTOR RESEARCH\nSUBJECTS 08 \u00b7 09 \u00b7 10", iwRoot, "Sign_RaptorPen",
                new Vector3(206f, 4.7f, 66.4f), 7f, 1.4f, M("SignWhite"),
                new Color(0.08f, 0.08f, 0.08f), FaceToward(Vector3.forward), 0.2f);

            // enrichment: a tire on a chain
            Cyl(iwRoot, "Pen_Chain", new Vector3(206f, 3.4f, 50f),
                0.04f, 2.4f, M("MetalDark"));
            var toy = Ball(iwRoot, "Pen_Toy", new Vector3(206f, 2.1f, 50f),
                new Vector3(0.5f, 0.5f, 0.5f), M("Tire"));
            StripCollider(toy);

            var l = NewPointLight(new Vector3(206f, 4.5f, 66f),
                new Color(0.75f, 0.85f, 1f), 1.2f, 14f, true);
            l.transform.SetParent(iwRoot, true);

            // the residents
            int a = padRoot.childCount;
            BuildRaptorDino(new Vector3(198f, 0f, 52f), rf(0f, 360f));
            padRoot.GetChild(a).SetParent(iwRoot, true);
            int b = padRoot.childCount;
            BuildRaptorDino(new Vector3(214f, 0f, 60f), rf(0f, 360f));
            padRoot.GetChild(b).SetParent(iwRoot, true);
        }

        static void JWFenceRun(Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float len = ab.magnitude;
            if (len < 0.5f) return;

            int segs = Mathf.Max(1, Mathf.RoundToInt(len / 5.5f));
            float yaw = Mathf.Atan2(ab.x, ab.z) * Mathf.Rad2Deg;
            Vector3 mid = (a + b) * 0.5f;
            var rot = new Vector3(0f, yaw, 0f);

            for (int i = 0; i <= segs; i++)
            {
                Vector3 p = a + ab * (i / (float)segs);
                Cyl(iwRoot, "JWPen_Post", p + Vector3.up * 1.4f,
                    0.09f, 2.8f, M("MetalGray"));
            }

            Box(iwRoot, "JWPen_Rail", mid + Vector3.up * 1.1f,
                new Vector3(0.09f, 0.09f, len), M("MetalDark"), rot);
            Box(iwRoot, "JWPen_Rail", mid + Vector3.up * 2.4f,
                new Vector3(0.09f, 0.09f, len), M("MetalDark"), rot);
            Box(iwRoot, "JWPen_Mesh", mid + Vector3.up * 1.7f,
                new Vector3(0.03f, 2.6f, len), M("ChainLink"), rot);
        }

        // ---------------------------------------------------------- monorail

        static void IslandMonorail()
        {
            var conc = M("Concrete");

            // elevated beam on pillars, running the back of main street
            for (float x = 180f; x <= 236f; x += 14f)
            {
                Cyl(iwRoot, "Monorail_Pillar", new Vector3(x, 3f, -16f),
                    1.1f, 6f, conc);
                Box(iwRoot, "Monorail_Cap", new Vector3(x, 6.1f, -16f),
                    new Vector3(2.4f, 0.6f, 3f), conc);
            }
            Box(iwRoot, "Monorail_Beam", new Vector3(208f, 6.7f, -16f),
                new Vector3(64f, 1f, 2.2f), conc);
            Box(iwRoot, "Monorail_RailA", new Vector3(208f, 7.25f, -16.7f),
                new Vector3(64f, 0.1f, 0.1f), M("MetalDark"));
            Box(iwRoot, "Monorail_RailB", new Vector3(208f, 7.25f, -15.3f),
                new Vector3(64f, 0.1f, 0.1f), M("MetalDark"));

            // the train, still parked at the platform
            var white = M("MonorailWhite");
            Box(iwRoot, "Monorail_Car", new Vector3(212f, 8.7f, -16f),
                new Vector3(14f, 2.6f, 2.4f), white);
            Box(iwRoot, "Monorail_Bogie", new Vector3(212f, 7.5f, -16f),
                new Vector3(10f, 0.8f, 1.6f), M("MetalDark"));
            Box(iwRoot, "Monorail_WinA", new Vector3(212f, 9f, -14.8f),
                new Vector3(12f, 0.9f, 0.12f), M("GlassJW"));
            Box(iwRoot, "Monorail_WinB", new Vector3(212f, 9f, -17.2f),
                new Vector3(12f, 0.9f, 0.12f), M("GlassJW"));
            Box(iwRoot, "Monorail_Nose", new Vector3(219.2f, 8.7f, -16f),
                new Vector3(0.14f, 0.9f, 1.8f), M("GlassJW"));

            // station platform + roof at the east end
            Box(iwRoot, "Station_Deck", new Vector3(244f, 6.6f, -16f),
                new Vector3(14f, 0.3f, 4f), conc);
            Box(iwRoot, "Station_Roof", new Vector3(244f, 9.6f, -16f),
                new Vector3(14f, 0.3f, 4.4f), white);
            for (float x = 238f; x <= 250f; x += 4f)
            {
                Cyl(iwRoot, "Station_PostN", new Vector3(x, 8.1f, -17.8f),
                    0.16f, 3.2f, M("MetalGray"));
                Cyl(iwRoot, "Station_PostS", new Vector3(x, 8.1f, -14.2f),
                    0.16f, 3.2f, M("MetalGray"));
            }
            // access ramp down to the lawn (32° — walks up fine)
            Box(iwRoot, "Station_Ramp", new Vector3(255f, 3.3f, -16f),
                new Vector3(12f, 0.15f, 2.4f), conc, new Vector3(0f, 0f, -32f));

            Sign("MONORAIL \u2014 STATION", iwRoot, "Sign_Station",
                new Vector3(244f, 8.4f, -13.7f), 8f, 1f, M("SignBlack"),
                new Color(0.88f, 0.86f, 0.78f), FaceToward(Vector3.forward), 0.22f);

            var l = NewPointLight(new Vector3(244f, 8f, -16f),
                new Color(1f, 0.9f, 0.68f), 1.3f, 15f, false);
            l.transform.SetParent(iwRoot, true);

            // a support that came down — where? nobody says.
            Box(iwRoot, "Monorail_FallenPillar", new Vector3(173f, 0.55f, -27f),
                new Vector3(1.1f, 6f, 1.1f), conc, new Vector3(0f, 25f, 90f));
            for (int i = 0; i < 3; i++)
                Box(iwRoot, "Monorail_Rubble",
                    new Vector3(171f + rf(0f, 5f), rf(0.15f, 0.4f), -25f + rf(0f, 4f)),
                    new Vector3(rf(0.5f, 1.1f), rf(0.3f, 0.6f), rf(0.5f, 1.1f)),
                    M("Rock"), new Vector3(rf(0f, 30f), rf(0f, 360f), rf(0f, 30f)));
        }

        // ---------------------------------------------------------- dressing

        static void IslandDressing()
        {
            // jungle reclaiming the island edges (kept off the street)
            var leafDark = M("LeafDark");
            var leafMid = M("LeafMid");
            var trunk = M("Trunk");

            for (float x = 174f; x <= 224f; x += 10f)
                BuildTree(new Vector3(x + rf(-2f, 2f), 0f, -92f + rf(-1.5f, 1.5f)),
                    leafDark, leafMid, trunk);
            for (float z = -88f; z <= 88f; z += 13f)
                BuildTree(new Vector3(298.5f + rf(-1.5f, 1.5f), 0f, z + rf(-2f, 2f)),
                    leafDark, leafMid, trunk);
            for (float x = 176f; x <= 246f; x += 13f)
                BuildTree(new Vector3(x + rf(-2f, 2f), 0f, 91f + rf(-1.5f, 1.5f)),
                    leafDark, leafMid, trunk);
            for (float z = -84f; z <= 84f; z += 14f)
            {
                if (Mathf.Abs(z) < 16f) continue;              // keep the dock mouth clear
                BuildTree(new Vector3(175f + rf(-1.5f, 1.5f), 0f, z + rf(-2f, 2f)),
                    leafDark, leafMid, trunk);
            }

            // street litter (muted little scraps)
            for (int i = 0; i < 10; i++)
            {
                var scrap = FlatQuad(iwRoot, "JW_Litter",
                    new Vector3(rf(179f, 244f), 0.05f, rf(-6f, 6f)),
                    new Vector2(rf(0.4f, 0.9f), rf(0.4f, 0.9f)), M("Litter"),
                    rf(0f, 360f));
                StripCollider(scrap);
            }

            // evidence
            BuildBlood(new Vector3(215f, 0.06f, -22f), 1.8f, rf(0f, 360f));
            BuildBlood(new Vector3(269f, 0.06f, 44f), 1.4f, rf(0f, 360f));

            // the tour vehicle that didn't make it
            BuildJeep(new Vector3(186f, 0f, 26f), rf(0f, 360f), true);

            // freight left by the monorail
            BuildCrateStack(new Vector3(248f, 0f, 22f), 3, rf(0f, 360f));
            BuildBarrelCluster(new Vector3(254f, 0f, -24f), 3, true);
        }
    }
}
