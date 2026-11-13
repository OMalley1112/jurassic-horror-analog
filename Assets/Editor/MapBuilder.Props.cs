using UnityEngine;
using AnalogHorror;

namespace AnalogHorror.EditorTools
{
    public static partial class MapBuilder
    {
        static void BuildPropsAndDebris()
        {
            BuildGateArch();

            // vehicles
            BuildJeep(new Vector3(7.5f, 0f, 17f), -25f, false);
            BuildJeep(new Vector3(-35f, 0f, 36f), 55f, true);   // crushed by the rex
            BuildJeep(new Vector3(2.5f, 0f, 78f), 100f, false);  // abandoned across the road

            BuildCrateStack(new Vector3(-6.5f, 0f, 17f), 3, 8f);
            BuildCrateStack(new Vector3(8.6f, 0f, 24f), 2, -22f);
            BuildCrateStack(new Vector3(-11.6f, -4f, -15f), 2, 40f);   // tunnel
            BuildBarrelCluster(new Vector3(9.6f, 0f, 14.5f), 3);
            BuildBarrelCluster(new Vector3(-8.4f, -4f, -21f), 2, true);

            // surveillance cameras everywhere (some watching you)
            BuildSecurityCamera(new Vector3(-8.4f, 0f, 13.4f), 145f, true, false);
            BuildSecurityCamera(new Vector3(5.4f, 0f, 40f), -100f, true, false);
            BuildSecurityCamera(new Vector3(24.6f, 0f, 45.5f), 75f, true, true); // hangs broken

            // blood near the raptor gate and in the tunnel
            BuildBlood(new Vector3(27.5f, 0.03f, 49f), 1.4f, 10f);
            BuildBlood(new Vector3(-9.5f, -3.97f, -34f), 1.1f, -40f);
            BuildBlood(new Vector3(-30f, 0.03f, 32f), 2.1f, 65f);

            // litter
            for (int i = 0; i < 26; i++)
            {
                float x = rf(-11f, 11f);
                float z = rf(13f, 60f);
                var paper = Box(propsRoot, "Litter", new Vector3(x, 0.05f, z),
                    new Vector3(rf(0.2f, 0.45f), 0.01f, rf(0.25f, 0.5f)),
                    M("Litter"), new Vector3(rf(-6f, 6f), rf(0f, 360f), rf(-6f, 6f)));
                StripCollider(paper);
            }

            BuildLobbySkeleton();
        }

        // ---------------------------------------------------------------- jeep

        static void BuildJeep(Vector3 pos, float yaw, bool wrecked)
        {
            var jeep = new GameObject(wrecked ? "Jeep_Wrecked" : "Jeep");
            jeep.transform.SetParent(propsRoot, false);
            jeep.transform.position = pos;
            jeep.transform.rotation = Quaternion.Euler(
                wrecked ? -7f : 0f, yaw, wrecked ? 11f : 0f);

            var t = jeep.transform;
            var green = M("JeepGreen");
            var tire = M("Tire");
            var chrome = M("Chrome");
            var dark = M("PlayerDark");

            Box(t, "Jeep_Tub", new Vector3(0f, 1f, -0.5f), new Vector3(2.1f, 0.6f, 3f), green);
            Box(t, "Jeep_Hood", new Vector3(0f, 1.05f, 1.6f), new Vector3(2f, 0.45f, 1.5f), green);
            Box(t, "Jeep_Grille", new Vector3(0f, 1f, 2.4f), new Vector3(1.9f, 0.5f, 0.1f), chrome);
            Box(t, "Jeep_FenderL", new Vector3(-1.1f, 1.35f, 1.5f), new Vector3(0.3f, 0.15f, 1.6f), green);
            Box(t, "Jeep_FenderR", new Vector3(1.1f, 1.35f, 1.5f), new Vector3(0.3f, 0.15f, 1.6f), green);

            if (!wrecked)
            {
                // windshield frame + top bar
                Box(t, "Jeep_Windshield", new Vector3(0f, 1.85f, 0.55f),
                    new Vector3(1.9f, 0.75f, 0.08f), green, new Vector3(-14f, 0f, 0f));
            }
            else
            {
                // crushed-in roof panel
                Box(t, "Jeep_CavedRoof", new Vector3(0.2f, 1.55f, -0.4f),
                    new Vector3(1.8f, 0.12f, 1.6f), green, new Vector3(24f, 9f, -17f));
            }

            // roll bar
            Box(t, "Jeep_RollBarTop", new Vector3(0f, 2.05f, -1.1f),
                new Vector3(2f, 0.1f, 0.1f), chrome);
            Box(t, "Jeep_RollBarL", new Vector3(-0.95f, 1.6f, -1.1f),
                new Vector3(0.1f, 0.9f, 0.1f), chrome);
            Box(t, "Jeep_RollBarR", new Vector3(0.95f, 1.6f, -1.1f),
                new Vector3(0.1f, 0.9f, 0.1f), chrome);

            // seats + steering wheel
            Box(t, "Jeep_SeatL", new Vector3(-0.5f, 1.4f, -0.5f), new Vector3(0.7f, 0.5f, 0.7f), dark);
            Box(t, "Jeep_SeatR", new Vector3(0.5f, 1.4f, -0.5f), new Vector3(0.7f, 0.5f, 0.7f), dark);
            Cyl(t, "Jeep_Steering", new Vector3(0.5f, 1.7f, 0.15f), 0.42f, 0.06f, dark,
                new Vector3(72f, 0f, 0f));

            // wheels (wreck is missing the front-left)
            PlaceWheel(t, new Vector3(-1.15f, 0.45f, 1.5f), tire, chrome, wrecked);
            PlaceWheel(t, new Vector3(1.15f, 0.45f, 1.5f), tire, chrome, false);
            PlaceWheel(t, new Vector3(-1.15f, 0.45f, -1.6f), tire, chrome, false);
            PlaceWheel(t, new Vector3(1.15f, 0.45f, -1.6f), tire, chrome, false);
            if (wrecked)
            {
                // the missing wheel, lying in the dirt
                Cyl(propsRoot, "Jeep_LostWheel", pos + new Vector3(1.6f, 0.18f, 2.2f),
                    0.9f, 0.35f, tire, new Vector3(90f, yaw + 40f, 0f));
            }

            // headlights + bumper
            var hl = Ball(t, "Jeep_HeadlightL", new Vector3(-0.7f, 1.15f, 2.46f),
                new Vector3(0.3f, 0.3f, 0.3f), M("HeadlightGlow"));
            StripCollider(hl);
            var hr = Ball(t, "Jeep_HeadlightR", new Vector3(0.7f, 1.15f, 2.46f),
                new Vector3(0.3f, 0.3f, 0.3f), M("HeadlightGlow"));
            StripCollider(hr);
            Box(t, "Jeep_Bumper", new Vector3(0f, 0.78f, 2.52f), new Vector3(2f, 0.16f, 0.16f), chrome);

            // spare tire on the back
            Cyl(t, "Jeep_Spare", new Vector3(0f, 1.45f, -2.16f), 0.9f, 0.3f, tire,
                new Vector3(90f, 0f, 0f));
        }

        static void PlaceWheel(Transform jeep, Vector3 p, Material tire, Material chrome, bool missing)
        {
            if (missing) return;
            Cyl(jeep, "Jeep_Wheel", p, 0.9f, 0.35f, tire, new Vector3(0f, 0f, 90f));
            Cyl(jeep, "Jeep_Hub", p, 0.36f, 0.37f, chrome, new Vector3(0f, 0f, 90f));
        }

        // ---------------------------------------------------------- crates etc.

        static void BuildCrateStack(Vector3 pos, int count, float yaw)
        {
            var mat = M("Crate");
            float y = 0f;
            for (int i = 0; i < count; i++)
            {
                float s = rf(0.9f, 1.25f);
                float h = s * 0.85f;
                float jitterX = rf(-0.15f, 0.15f);
                float jitterZ = rf(-0.15f, 0.15f);
                float turn = yaw + rf(-16f, 16f);
                Box(propsRoot, "Crate", new Vector3(pos.x + jitterX, y + h * 0.5f, pos.z + jitterZ),
                    new Vector3(s, h, s * rf(0.85f, 1.1f)), mat, new Vector3(0f, turn, 0f));
                y += h;
            }
        }

        static void BuildBarrelCluster(Vector3 pos, int count, bool tipped = false)
        {
            for (int i = 0; i < count; i++)
            {
                var mat = chance(0.5f) ? M("BarrelBlue") : M("BarrelRed");
                Vector3 p = pos + new Vector3(rf(-0.9f, 0.9f), 0f, rf(-0.9f, 0f + 0.9f));
                if (tipped || (i == count - 1 && chance(0.4f)))
                {
                    Cyl(propsRoot, "Barrel_Tipped", p + new Vector3(0f, 0.45f, 0f),
                        0.62f, 1.2f, mat, new Vector3(90f, rf(0f, 360f), 0f));
                }
                else
                {
                    Cyl(propsRoot, "Barrel", p + new Vector3(0f, 0.6f, 0f),
                        0.62f, 1.2f, mat, new Vector3(0f, rf(0f, 360f), 0f));
                    // rims
                    Cyl(propsRoot, "Barrel_Rim", p + new Vector3(0f, 0.9f, 0f),
                        0.66f, 0.07f, M("MetalGray"));
                    Cyl(propsRoot, "Barrel_Rim", p + new Vector3(0f, 0.3f, 0f),
                        0.66f, 0.07f, M("MetalGray"));
                }
            }
        }

        // ----------------------------------------------------- security camera

        static void BuildSecurityCamera(Vector3 basePos, float yaw, bool tiltDown, bool hanging)
        {
            var metal = M("MetalGray");
            float h = hanging ? 2.1f : 3.2f;

            Cyl(propsRoot, "Cam_Pole", basePos + Vector3.up * (h * 0.5f), 0.14f, h, metal);

            if (hanging)
            {
                // snapped off — dangles from its cable
                Vector3 top = basePos + Vector3.up * h;
                Box(propsRoot, "Cam_Cable", top + new Vector3(0.15f, -0.5f, 0f),
                    new Vector3(0.03f, 1f, 0.03f), M("PlayerDark"), new Vector3(0f, 0f, 18f));
                var body = Box(propsRoot, "Cam_Body",
                    top + new Vector3(0.3f, -1.05f, 0f),
                    new Vector3(0.5f, 0.28f, 0.24f), metal, new Vector3(34f, yaw + 20f, 62f));
                Ball(propsRoot, "Cam_Lens",
                    top + new Vector3(0.44f, -1.2f, 0.06f),
                    new Vector3(0.14f, 0.14f, 0.14f), M("ShadowBlack"));
                return;
            }

            // arm + housing angled down at the path
            Vector3 pivot = basePos + Vector3.up * h;
            Quaternion yawQ = Quaternion.Euler(0f, yaw, 0f);
            Vector3 outDir = yawQ * Vector3.forward;

            Box(propsRoot, "Cam_Arm", pivot + outDir * 0.4f + Vector3.up * 0.1f,
                new Vector3(0.08f, 0.08f, 0.8f), metal, new Vector3(0f, yaw, 0f));

            Vector3 headPos = pivot + outDir * 0.8f;
            float pitch = tiltDown ? 38f : 10f;
            Box(propsRoot, "Cam_Body", headPos + new Vector3(0f, -0.15f, 0f),
                new Vector3(0.24f, 0.28f, 0.5f), metal, new Vector3(pitch, yaw, 0f));
            Vector3 lensDir = Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
            var lens = Ball(propsRoot, "Cam_Lens", headPos + lensDir * 0.28f + new Vector3(0f, -0.15f, 0f),
                new Vector3(0.14f, 0.14f, 0.14f), M("ShadowBlack"));
            StripCollider(lens);
            var rec = Ball(propsRoot, "Cam_RecLED", headPos + new Vector3(0f, 0.02f, -0.18f),
                new Vector3(0.05f, 0.05f, 0.05f), M("WarningLight"));
            StripCollider(rec);
        }

        static void BuildBlood(Vector3 pos, float size, float yaw)
        {
            var b = Ball(propsRoot, "BloodStain", pos,
                new Vector3(size, 0.05f, size * 0.8f), M("BloodDark"),
                new Vector3(0f, yaw, 0f));
            StripCollider(b);
        }

        // ------------------------------------------------------- creepy figures

        static void BuildCreepyFigures()
        {
            var overlay = Object.FindObjectOfType<VhsOverlay>();
            var ambience = Object.FindObjectOfType<AmbientAudio>();

            // 1 — standing at the end of the tunnel, watching the stairs
            PlaceFigure(new Vector3(-10f, -4f, -46.5f), 0f, overlay, ambience, 6.5f);
            // 2 — far down the main road, blocking the way out
            PlaceFigure(new Vector3(0.6f, 0f, 84f), 180f, overlay, ambience, 8f);
            // 3 — inside the raptor paddock, just behind the fence
            PlaceFigure(new Vector3(50f, 0f, 66f), 180f, overlay, ambience, 7f);
            // 4 — deep in the T-Rex paddock, facing the breach
            PlaceFigure(new Vector3(-55f, 0f, 36f), 90f, overlay, ambience, 7.5f);
        }

        static void PlaceFigure(Vector3 pos, float yaw, VhsOverlay overlay,
            AmbientAudio ambience, float vanishDistance)
        {
            var root = new GameObject("CreepyFigure");
            root.transform.SetParent(propsRoot, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var black = M("ShadowBlack");
            var eyes = M("EyeGlow");
            var t = root.transform;

            Box(t, "Fig_LegL", new Vector3(-0.12f, 0.45f, 0f), new Vector3(0.16f, 0.9f, 0.16f), black);
            Box(t, "Fig_LegR", new Vector3(0.12f, 0.45f, 0f), new Vector3(0.16f, 0.9f, 0.16f), black);
            Box(t, "Fig_Coat", new Vector3(0f, 1.32f, 0f), new Vector3(0.56f, 1.05f, 0.34f), black);
            Box(t, "Fig_Shoulders", new Vector3(0f, 1.8f, 0f), new Vector3(0.62f, 0.2f, 0.3f), black);
            Box(t, "Fig_ArmL", new Vector3(-0.34f, 1.3f, 0f), new Vector3(0.13f, 0.95f, 0.13f), black);
            Box(t, "Fig_ArmR", new Vector3(0.34f, 1.3f, 0f), new Vector3(0.13f, 0.95f, 0.13f), black);
            var head = Ball(t, "Fig_Head", new Vector3(0f, 2.02f, 0f),
                new Vector3(0.27f, 0.3f, 0.27f), black);
            var eyeL = Ball(t, "Fig_EyeL", new Vector3(-0.07f, 2.05f, 0.12f),
                new Vector3(0.055f, 0.04f, 0.03f), eyes);
            var eyeR = Ball(t, "Fig_EyeR", new Vector3(0.07f, 2.05f, 0.12f),
                new Vector3(0.055f, 0.04f, 0.03f), eyes);

            // never a physical obstacle — it only exists to be seen
            foreach (var col in root.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(col);

            var comp = root.AddComponent<CreepyFigure>();
            comp.overlay = overlay;
            comp.ambience = ambience;
            comp.vanishDistance = vanishDistance;
        }
    }
}
