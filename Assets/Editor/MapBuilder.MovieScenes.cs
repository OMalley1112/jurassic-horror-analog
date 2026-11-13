using UnityEngine;
using AnalogHorror;

namespace AnalogHorror.EditorTools
{
    public static partial class MapBuilder
    {
        // ===================================================== MOVIE SET PIECES
        // The Jurassic-Park-III corner of the island, SE of the ferry road:
        //   — a new east road off the main south run (bigger play space)
        //   — the crashed expedition plane, broken open with its cargo out
        //   — the SPINOSAURUS standing over the wreck (JP III's star)
        //   - a river tour boat thrown up on the bank
        //   — a raptor nest that got there first
        // Runs LAST in BuildAll, so every rf() here only shifts content
        // inside this file — the verified map above is untouched.

        static Transform movRoot;

        static void BuildMovieScenes()
        {
            movRoot = NewRoot("_MOVIE SCENES");

            // the road east + the clearing around the set pieces
            ClearVegetation(2f, 94f, 138f, 104f);
            ClearVegetation(86f, 104f, 140f, 137f);

            StripCollider(FlatQuad(movRoot, "Road_Movie",
                new Vector3(58f, 0.03f, 99f), new Vector2(116f, 9f), M("DirtRoad")));

            BuildPlaneWreck();
            BuildSpinosaurus(new Vector3(115f, 0f, 120f), -148f);
            BuildTourBoat();
            BuildCrashNest();
            MovieDressing();
        }

        // ---------------------------------------------------- the plane wreck

        static void BuildPlaneWreck()
        {
            var p = new GameObject("PlaneWreck").transform;
            p.SetParent(movRoot, false);
            p.position = new Vector3(117f, 0f, 112f);
            p.rotation = Quaternion.Euler(0f, 200f, 0f);

            Material skin = M("PaintWhite");
            Material dark = M("MetalDark");
            Material glass = M("ScreenGlow");
            Material rust = M("MetalRust");

            // rear fuselage section, dropped and tilted into the dirt
            Cyl(p, "Plane_TailSection", new Vector3(0f, 1.5f, -3.6f),
                2.9f, 4.6f, skin, new Vector3(84f, 10f, 0f));
            // torn open end — a black wound where the tube sheared
            Cyl(p, "Plane_TornEnd", new Vector3(0f, 1.75f, -1.35f),
                2.85f, 0.12f, M("ShadowBlack"), new Vector3(84f, 10f, 0f));
            Box(p, "Plane_TailFin", new Vector3(0.9f, 3.9f, -5.6f),
                new Vector3(0.22f, 3.6f, 2.1f), skin, new Vector3(14f, 12f, 0f));
            Box(p, "Plane_TailStripe", new Vector3(0.9f, 3.2f, -5.4f),
                new Vector3(0.26f, 0.5f, 2.0f), M("SignRed"), new Vector3(14f, 12f, 0f));

            // the open middle: floor + side panels, roof gone
            Box(p, "Plane_Floor", new Vector3(0f, 0.55f, 1.4f),
                new Vector3(2.6f, 0.18f, 5.6f), rust, new Vector3(-2f, 0f, 0f));
            Box(p, "Plane_SideL", new Vector3(-1.35f, 1.6f, 1.4f),
                new Vector3(0.16f, 2.3f, 5.6f), skin, new Vector3(-2f, 0f, -4f));
            Box(p, "Plane_SideR", new Vector3(1.35f, 1.6f, 1.4f),
                new Vector3(0.16f, 2.3f, 5.6f), skin, new Vector3(-2f, 0f, 5f));
            // window strips on both panels
            for (int i = 0; i < 4; i++)
            {
                float z = -0.4f + i * 1.25f;
                Box(p, "Plane_Window", new Vector3(-1.44f, 2.1f, z),
                    new Vector3(0.04f, 0.42f, 0.7f), glass, new Vector3(-2f, 0f, -4f));
                Box(p, "Plane_Window", new Vector3(1.44f, 2.1f, z),
                    new Vector3(0.04f, 0.42f, 0.7f), glass, new Vector3(-2f, 0f, 5f));
            }

            // seats still bolted to the floor — most of them empty
            for (int i = 0; i < 4; i++)
            {
                float z = -0.2f + i * 1.3f;
                Box(p, "Plane_SeatBase", new Vector3(-0.55f, 0.85f, z),
                    new Vector3(0.6f, 0.3f, 0.65f), M("PlayerDark"));
                Box(p, "Plane_SeatBack", new Vector3(-0.55f, 1.25f, z - 0.28f),
                    new Vector3(0.6f, 0.7f, 0.16f), M("PlayerDark"),
                    new Vector3(-8f, 0f, 0f));
                Box(p, "Plane_SeatBase", new Vector3(0.55f, 0.85f, z),
                    new Vector3(0.6f, 0.3f, 0.65f), M("PlayerDark"));
                Box(p, "Plane_SeatBack", new Vector3(0.55f, 1.25f, z - 0.28f),
                    new Vector3(0.6f, 0.7f, 0.16f), M("PlayerDark"),
                    new Vector3(-8f, 0f, 0f));
            }

            // nose crumpled into the ground
            Cyl(p, "Plane_Nose", new Vector3(0.3f, 0.85f, 5.4f),
                2.6f, 2.6f, skin, new Vector3(106f, -8f, 0f));
            Box(p, "Plane_CockpitGlass", new Vector3(0.3f, 1.5f, 5.9f),
                new Vector3(1.5f, 0.5f, 0.9f), dark, new Vector3(24f, -8f, 0f));

            // wings: one flat in the scrub, one thrown clear and buried
            Box(p, "Plane_WingL", new Vector3(-4.6f, 0.55f, 1.6f),
                new Vector3(7.6f, 0.3f, 2.7f), skin, new Vector3(0f, 0f, -8f));
            Cyl(p, "Plane_Engine", new Vector3(-6.4f, 1.0f, 1.9f),
                1.6f, 2.4f, dark, new Vector3(90f, 0f, 0f));
            Box(p, "Plane_WingR", new Vector3(4.4f, 1.6f, 3.4f),
                new Vector3(6.8f, 0.3f, 2.6f), skin, new Vector3(16f, -14f, 42f));

            // cargo thrown from the hold
            Box(p, "Plane_SuitcaseA", new Vector3(-3.2f, 0.3f, 5.2f),
                new Vector3(0.9f, 0.6f, 0.5f), M("BarrelRed"),
                new Vector3(0f, 38f, 12f));
            Box(p, "Plane_SuitcaseB", new Vector3(3.6f, 0.28f, -1.4f),
                new Vector3(1.1f, 0.55f, 0.7f), M("BarrelBlue"),
                new Vector3(4f, -24f, 0f));
            Box(p, "Plane_SuitcaseC", new Vector3(-2.4f, 0.32f, -2.6f),
                new Vector3(0.8f, 0.62f, 0.45f), M("PlayerOlive"),
                new Vector3(0f, 65f, -14f));

            // scorch where it plowed in, and what the fire left
            StripCollider(FlatQuad(p, "Plane_Scorch",
                new Vector3(0f, 0.04f, 2.4f), new Vector2(11f, 13f),
                M("ShadowBlack"), 12f));
            StripCollider(FlatQuad(p, "Plane_Scorch2",
                new Vector3(-5.5f, 0.045f, 4.2f), new Vector2(6f, 7f),
                M("ShadowBlack"), -30f));
            BuildBlood(new Vector3(120.5f, 0.05f, 116.5f), 2.4f, rf(0f, 360f));
            BuildBonePile(movRoot, new Vector3(113f, 0f, 108f), 1.3f, rf(0f, 360f));
        }

        // ------------------------------------------------------ the spinosaurus

        static void BuildSpinosaurus(Vector3 pos, float yaw)
        {
            var root = new GameObject("Spinosaurus").transform;
            root.SetParent(movRoot, false);
            root.position = pos;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);

            Material hide = M("DinoHide");
            Material sail = M("DinoSpine");
            Material dark = M("DinoSpine");
            Material eye = M("EyeGlow");

            // ---- body: long, low, crocodile-heavy ----
            Capsule(root, "Spino_Body", new Vector3(0f, 3.1f, 0.3f),
                2.1f, 5.6f, hide, new Vector3(90f, 0f, 0f));
            Ball(root, "Spino_Hips", new Vector3(0f, 3.0f, -2.2f),
                new Vector3(2.4f, 2.2f, 2.6f), hide);
            Ball(root, "Spino_Shoulders", new Vector3(0f, 3.2f, 2.3f),
                new Vector3(2.1f, 2.0f, 2.3f), hide);

            // ---- legs ----
            for (int i = 0; i < 2; i++)
            {
                float dx = i == 0 ? -1.05f : 1.05f;
                Capsule(root, "Spino_Thigh", new Vector3(dx, 2.1f, -1.7f),
                    1.0f, 2.5f, hide, new Vector3(14f, 0f, 0f));
                Capsule(root, "Spino_Shin", new Vector3(dx, 1.0f, -1.2f),
                    0.6f, 1.9f, hide, new Vector3(-16f, 0f, 0f));
                Box(root, "Spino_Foot", new Vector3(dx, 0.22f, -0.6f),
                    new Vector3(0.95f, 0.44f, 1.6f), dark);
            }

            // ---- THE SAIL: membrane + a row of spines cresting mid-back ----
            Box(root, "Spino_Sail", new Vector3(0f, 5.3f, -0.15f),
                new Vector3(0.13f, 2.4f, 5.3f), sail);
            for (int i = 0; i < 9; i++)
            {
                float pr = i / 8f;
                float z = Mathf.Lerp(2.5f, -2.9f, pr);
                float h = 1.5f + Mathf.Sin(pr * Mathf.PI) * 1.35f;
                Box(root, "Spino_Spine", new Vector3(0f, 4.15f + h * 0.5f, z),
                    new Vector3(0.17f, h, 0.42f), sail,
                    new Vector3(0f, 0f, Mathf.Lerp(-4f, 4f, pr)));
            }

            // ---- neck forward and up to the croc head ----
            for (int i = 0; i < 3; i++)
            {
                float p = i / 2f;
                Vector3 n = new Vector3(0f,
                    Mathf.Lerp(3.4f, 4.0f, p),
                    Mathf.Lerp(2.9f, 4.1f, p));
                Capsule(root, "Spino_Neck", n,
                    Mathf.Lerp(1.3f, 0.85f, p), 1.5f, hide,
                    new Vector3(Mathf.Lerp(46f, 62f, p), 0f, 0f));
            }

            var head = Pivot(root, "Spino_HeadPivot", new Vector3(0f, 4.15f, 4.6f));
            Box(head, "Spino_Skull", new Vector3(0f, 0.05f, 0.35f),
                new Vector3(0.72f, 0.6f, 1.1f), hide, new Vector3(6f, 0f, 0f));
            Box(head, "Spino_Snout", new Vector3(0f, -0.06f, 1.45f),
                new Vector3(0.46f, 0.4f, 1.5f), hide, new Vector3(6f, 0f, 0f));
            // croc teeth down both sides of the snout
            for (int i = 0; i < 6; i++)
            {
                float z = 0.75f + i * 0.3f;
                Box(head, "Spino_Tooth", new Vector3(-0.22f, -0.24f, z),
                    new Vector3(0.07f, 0.17f, 0.07f), M("Bone"));
                Box(head, "Spino_Tooth", new Vector3(0.22f, -0.24f, z),
                    new Vector3(0.07f, 0.17f, 0.07f), M("Bone"));
            }
            // lower jaw hanging slightly open — it just ate
            Box(head, "Spino_Jaw", new Vector3(0f, -0.34f, 1.1f),
                new Vector3(0.42f, 0.16f, 1.9f), dark, new Vector3(-9f, 0f, 0f));
            Ball(head, "Spino_EyeL", new Vector3(-0.3f, 0.16f, 0.55f),
                new Vector3(0.15f, 0.15f, 0.15f), eye);
            Ball(head, "Spino_EyeR", new Vector3(0.3f, 0.16f, 0.55f),
                new Vector3(0.15f, 0.15f, 0.15f), eye);

            // ---- tail: eight segments dragging back into the treeline ----
            for (int i = 1; i <= 8; i++)
            {
                float z = -3.6f - i * 1.35f;
                float y = Mathf.Max(3.0f - i * 0.33f, 0.75f);
                float sway = Mathf.Sin(i * 0.6f) * 5f;
                float thick = Mathf.Lerp(1.7f, 0.28f, i / 8f);
                Capsule(root, "Spino_Tail", new Vector3(0f, y, z),
                    thick, 1.9f, hide, new Vector3(Mathf.Lerp(90f, 78f, i / 8f), sway, 0f));
            }

            // atmosphere, not an obstacle — strip every collider
            foreach (var col in root.GetComponentsInChildren<Collider>())
                StripCollider(col.gameObject);

            // it breathes, scans the wreck, and answers the storm
            var idle = root.gameObject.AddComponent<DinoIdle>();
            idle.head = head;
            idle.breatheAmount = 0.3f;
            idle.breatheSpeed = 0.55f;
            idle.headAmount = 7f;
            idle.headPitchAmount = 2.4f;
            idle.headSpeed = 0.22f;
            idle.canRoar = true;
            idle.roarDelayMin = 16f;
            idle.roarDelayMax = 48f;
        }

        // ------------------------------------------------------- the tour boat

        static void BuildTourBoat()
        {
            var b = new GameObject("TourBoat_Wreck").transform;
            b.SetParent(movRoot, false);
            b.position = new Vector3(96f, 0f, 125f);
            b.rotation = Quaternion.Euler(0f, 96f, 6f);   // thrown up on its side

            Material hull = M("PaintWhite");
            Material trim = M("SignRed");
            Material deck = M("WoodPlank");
            Material dark = M("MetalDark");

            // hull + deck
            Box(b, "Boat_Hull", new Vector3(0f, 1.0f, 0f),
                new Vector3(3.2f, 2.0f, 9.6f), hull);
            Box(b, "Boat_HullStripe", new Vector3(0f, 1.7f, 0f),
                new Vector3(3.26f, 0.34f, 9.62f), trim);
            Box(b, "Boat_Deck", new Vector3(0f, 2.06f, 0f),
                new Vector3(2.9f, 0.14f, 9.2f), deck);
            // bow wedge
            Box(b, "Boat_Bow", new Vector3(0f, 1.4f, 5.3f),
                new Vector3(2.2f, 1.4f, 1.6f), hull, new Vector3(0f, 45f, 0f));

            // cabin + roof + dark windows
            Box(b, "Boat_Cabin", new Vector3(0f, 2.9f, -1.6f),
                new Vector3(2.5f, 1.7f, 3.6f), hull);
            Box(b, "Boat_CabinWindowF", new Vector3(0f, 3.2f, 0.22f),
                new Vector3(1.9f, 0.7f, 0.06f), M("ScreenGlow"));
            Box(b, "Boat_CabinWindowL", new Vector3(-1.27f, 3.2f, -1.6f),
                new Vector3(0.06f, 0.7f, 2.6f), M("ScreenGlow"));
            Box(b, "Boat_CabinWindowR", new Vector3(1.27f, 3.2f, -1.6f),
                new Vector3(0.06f, 0.7f, 2.6f), M("ScreenGlow"));
            Box(b, "Boat_Roof", new Vector3(0f, 3.8f, -1.6f),
                new Vector3(2.8f, 0.16f, 4.0f), M("RoofMetal"));

            // mast, railings, a life ring that didn't help anyone
            Cyl(b, "Boat_Mast", new Vector3(0f, 4.9f, 2.2f), 0.16f, 3.4f, dark);
            for (int i = -1; i <= 1; i += 2)
                Box(b, "Boat_Rail", new Vector3(i * 1.45f, 2.5f, 2.4f),
                    new Vector3(0.1f, 0.1f, 4.2f), dark);
            Cyl(b, "Boat_LifeRing", new Vector3(1.68f, 2.7f, -0.4f),
                0.7f, 0.16f, M("SignWhite"), new Vector3(0f, 0f, 90f));

            // deck clutter washed against the rail
            Box(b, "Boat_Crate", new Vector3(-0.8f, 2.5f, 3.4f),
                new Vector3(0.9f, 0.8f, 0.9f), M("WoodDark"), new Vector3(0f, 18f, 0f));
            Cyl(b, "Boat_Barrel", new Vector3(0.9f, 2.5f, 3.8f),
                0.7f, 0.9f, M("BarrelBlue"));
        }

        // ------------------------------------------------- the raptor nest

        static void BuildCrashNest()
        {
            var n = new GameObject("MovieNest").transform;
            n.SetParent(movRoot, false);
            n.position = new Vector3(128f, 0f, 105f);

            // scraped-out bowl of dirt and sticks
            Ball(n, "Nest_Mound", new Vector3(0f, 0.25f, 0f),
                new Vector3(4.4f, 0.7f, 4.4f), M("DirtRoad"));
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * 360f;
                Box(n, "Nest_Stick",
                    new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 1.9f,
                                0.62f,
                                Mathf.Sin(a * Mathf.Deg2Rad) * 1.9f),
                    new Vector3(0.1f, 0.1f, rf(1.0f, 1.7f)), M("WoodDark"),
                    new Vector3(0f, a + rf(-16f, 16f), rf(-6f, 6f)));
            }

            // eggs — one already split open
            for (int i = 0; i < 5; i++)
                Ball(n, "Nest_Egg",
                    new Vector3(rf(-0.9f, 0.9f), 0.85f, rf(-0.9f, 0.9f)),
                    new Vector3(0.36f, 0.44f, 0.36f), M("Egg"),
                    new Vector3(rf(-14f, 14f), rf(0f, 360f), rf(-14f, 14f)));
            Box(n, "Nest_EggShell", new Vector3(1.4f, 0.72f, 0.8f),
                new Vector3(0.4f, 0.24f, 0.34f), M("Egg"),
                new Vector3(18f, 42f, 0f));

            // hatchling tracks dragged away toward the treeline
            for (int i = 0; i < 7; i++)
                Box(n, "Nest_Track",
                    new Vector3(2.2f + i * 0.75f, 0.045f, -1.4f - i * 0.5f),
                    new Vector3(0.34f, 0.02f, 0.5f), M("ShadowBlack"),
                    new Vector3(0f, -32f + (i % 2) * 14f, 0f));

            BuildBlood(new Vector3(125.5f, 0.05f, 108.2f), 2.0f, rf(0f, 360f));
        }

        // ------------------------------------------------------- set dressing

        static void MovieDressing()
        {
            // signage off the new road
            Sign("SECTOR E \u2014 AIRCRAFT RECOVERY", movRoot, "Sign_Crash",
                new Vector3(104f, 3f, 105f), 5.8f, 0.95f,
                M("SignWhite"), new Color(0.85f, 0.22f, 0.16f),
                FaceToward(Vector3.forward), 0.22f);
            Box(movRoot, "Sign_CrashPostL", new Vector3(101.4f, 1.5f, 105f),
                new Vector3(0.14f, 3f, 0.14f), M("MetalGray"));
            Box(movRoot, "Sign_CrashPostR", new Vector3(106.6f, 1.5f, 105f),
                new Vector3(0.14f, 3f, 0.14f), M("MetalGray"));

            Sign("DANGER \u2014 LARGE CARNIVORE", movRoot, "Sign_Carnivore",
                new Vector3(88f, 2.1f, 105.2f), 4.4f, 0.7f,
                M("SignYellow"), new Color(0.06f, 0.06f, 0.06f),
                FaceToward(Vector3.forward), 0.19f);
            Box(movRoot, "Sign_CarnivorePost", new Vector3(88f, 1.05f, 105.2f),
                new Vector3(0.14f, 2.1f, 0.14f), M("MetalGray"));

            // steady floodlight over the wreck — the recovery crew never came
            var flood = NewPointLight(new Vector3(114f, 7.5f, 108f),
                new Color(1f, 0.86f, 0.6f), 1.7f, 26f, false);
            flood.transform.SetParent(movRoot, true);
            Cyl(movRoot, "CrashLamp_Post", new Vector3(114f, 3.75f, 108f),
                0.2f, 7.5f, M("MetalGray"));
            Box(movRoot, "CrashLamp_Head", new Vector3(114f, 7.4f, 108.6f),
                new Vector3(0.9f, 0.35f, 0.7f), M("MetalDark"),
                new Vector3(24f, 0f, 0f));

            // red marker lamp on the nest side
            var marker = NewPointLight(new Vector3(124.5f, 4.2f, 101.5f),
                new Color(1f, 0.24f, 0.16f), 1.2f, 14f, false);
            marker.transform.SetParent(movRoot, true);
            Cyl(movRoot, "CrashMarker_Post", new Vector3(124.5f, 2.1f, 101.5f),
                0.14f, 4.2f, M("MetalDark"));
            Ball(movRoot, "CrashMarker_Bulb", new Vector3(124.5f, 4.3f, 101.5f),
                new Vector3(0.3f, 0.3f, 0.3f), M("WarningLight"));

            // scattered recovery gear along the road
            for (int i = 0; i < 4; i++)
                Cyl(movRoot, "CrashCone",
                    new Vector3(rf(96f, 118f), 0.24f, rf(95.5f, 102.5f)),
                    0.3f, 0.48f, M("SignYellow"));
            BuildCrateStack(new Vector3(111f, 0f, 104.5f), 3, rf(0f, 360f));
            BuildJeep(new Vector3(122.5f, 0f, 97.5f), 214f, true);

            // carnage the tour never made it back from
            BuildBlood(new Vector3(99f, 0.05f, 118f), 1.9f, rf(0f, 360f));
            BuildBonePile(movRoot, new Vector3(92f, 0f, 130f), 1.2f, rf(0f, 360f));

            // something is watching the road from the wreck's shadow
            var overlay = Object.FindObjectOfType<VhsOverlay>();
            var ambience = Object.FindObjectOfType<AmbientAudio>();
            PlaceFigure(new Vector3(121.5f, 0f, 109f), -125f,
                overlay, ambience, 9f);
        }
    }
}
