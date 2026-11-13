using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Builds the player's visible body (survivor/ranger with jacket, hat,
    /// backpack and a held flashlight) out of primitives. The rig has real
    /// knee and elbow pivots so PlayerBodyAnimator can fold the legs for a
    /// crouch and pump the arms while running. Works both in the editor
    /// (called by the map builder) and at runtime.
    /// </summary>
    public static class PlayerModelBuilder
    {
        /// <summary>Creates the model under the player and wires up the animator.</summary>
        public static PlayerBodyAnimator Build(Transform player)
        {
            Transform existing = player.Find("PlayerModel");
            if (existing != null)
            {
                var existingAnimator = player.GetComponent<PlayerBodyAnimator>();
                if (existingAnimator != null && existingAnimator.leftKneePivot != null)
                    return existingAnimator;
                // legacy model (no knees/elbows): rebuild from scratch
                if (Application.isPlaying) Object.Destroy(existing.gameObject);
                else Object.DestroyImmediate(existing.gameObject);
            }

            var model = new GameObject("PlayerModel");
            model.transform.SetParent(player, false);
            model.transform.localPosition = Vector3.zero;
            existing = model.transform;

            Material khaki = GameMaterials.Get("PlayerKhaki");
            Material denim = GameMaterials.Get("PlayerDenim");
            Material skin = GameMaterials.Get("PlayerSkin");
            Material boot = GameMaterials.Get("PlayerBoot");
            Material hat = GameMaterials.Get("PlayerHat");
            Material dark = GameMaterials.Get("PlayerDark");
            Material olive = GameMaterials.Get("PlayerOlive");
            Material metal = GameMaterials.Get("MetalDark");
            Material lens = GameMaterials.Get("LightWarm");

            // ---- hips + legs (thigh -> knee pivot -> shin -> boot) -----------
            Transform hips = Pivot(existing, "Hips", new Vector3(0f, 0.92f, 0f));
            Transform legL = Pivot(hips, "LegL_Pivot", new Vector3(-0.14f, 0f, 0f));
            Transform legR = Pivot(hips, "LegR_Pivot", new Vector3(0.14f, 0f, 0f));

            BuildLeg(legL, "L", denim, boot, dark);
            BuildLeg(legR, "R", denim, boot, dark);

            // ---- torso -------------------------------------------------------
            Transform torso = Pivot(existing, "Torso", new Vector3(0f, 0.95f, 0f));

            Box(torso, "Body", new Vector3(0f, 0.28f, 0f), new Vector3(0.44f, 0.62f, 0.25f), khaki, Vector3.zero);
            Box(torso, "Belt", new Vector3(0f, -0.02f, 0f), new Vector3(0.46f, 0.1f, 0.27f), dark, Vector3.zero);
            Box(torso, "Buckle", new Vector3(0f, -0.02f, 0.14f), new Vector3(0.09f, 0.08f, 0.03f), metal, Vector3.zero);
            Box(torso, "Collar", new Vector3(0f, 0.6f, 0f), new Vector3(0.2f, 0.08f, 0.18f), khaki, Vector3.zero);

            // field jacket: shoulder strap + chest pouches + radio
            Box(torso, "StrapL", new Vector3(-0.1f, 0.3f, 0.13f), new Vector3(0.07f, 0.56f, 0.03f), dark, new Vector3(0f, 0f, -8f));
            Box(torso, "StrapR", new Vector3(0.1f, 0.3f, 0.13f), new Vector3(0.07f, 0.56f, 0.03f), dark, new Vector3(0f, 0f, 8f));
            Box(torso, "PouchL", new Vector3(-0.13f, 0.08f, 0.14f), new Vector3(0.14f, 0.13f, 0.06f), olive, Vector3.zero);
            Box(torso, "PouchR", new Vector3(0.13f, 0.08f, 0.14f), new Vector3(0.14f, 0.13f, 0.06f), olive, Vector3.zero);
            Box(torso, "Radio", new Vector3(0.19f, 0.42f, 0.1f), new Vector3(0.09f, 0.16f, 0.07f), dark, Vector3.zero);
            Cylinder(torso, "RadioAntenna", new Vector3(0.19f, 0.55f, 0.1f), 0.02f, 0.16f, metal, Vector3.zero);

            // backpack (behind the player)
            Box(torso, "Backpack", new Vector3(0f, 0.3f, -0.19f), new Vector3(0.34f, 0.42f, 0.16f), olive, Vector3.zero);
            Cylinder(torso, "Bedroll", new Vector3(0f, 0.54f, -0.19f), 0.16f, 0.34f, dark, new Vector3(0f, 0f, 90f));
            Box(torso, "BedStrap1", new Vector3(0f, 0.47f, -0.28f), new Vector3(0.34f, 0.04f, 0.03f), dark, Vector3.zero);
            Box(torso, "BedStrap2", new Vector3(0f, 0.61f, -0.28f), new Vector3(0.34f, 0.04f, 0.03f), dark, Vector3.zero);

            // ---- head + ranger hat ------------------------------------------
            Box(torso, "Neck", new Vector3(0f, 0.63f, 0f), new Vector3(0.14f, 0.1f, 0.14f), skin, Vector3.zero);
            Transform head = Pivot(torso, "HeadPivot", new Vector3(0f, 0.75f, 0f));
            Box(head, "Head", Vector3.zero, new Vector3(0.24f, 0.26f, 0.24f), skin, Vector3.zero);
            Box(head, "Hair", new Vector3(0f, 0.12f, -0.02f), new Vector3(0.25f, 0.07f, 0.24f), dark, Vector3.zero);
            Box(head, "Nose", new Vector3(0f, -0.01f, 0.13f), new Vector3(0.05f, 0.06f, 0.04f), skin, Vector3.zero);
            Box(head, "Brow", new Vector3(0f, 0.05f, 0.12f), new Vector3(0.2f, 0.03f, 0.03f), dark, Vector3.zero);
            Box(head, "EarL", new Vector3(-0.13f, 0f, 0f), new Vector3(0.03f, 0.07f, 0.05f), skin, Vector3.zero);
            Box(head, "EarR", new Vector3(0.13f, 0f, 0f), new Vector3(0.03f, 0.07f, 0.05f), skin, Vector3.zero);
            Cylinder(head, "HatBrim", new Vector3(0f, 0.15f, 0f), 0.44f, 0.045f, hat, Vector3.zero);
            Cylinder(head, "HatCrown", new Vector3(0f, 0.205f, 0f), 0.25f, 0.11f, hat, Vector3.zero);
            Box(head, "HatBand", new Vector3(0f, 0.165f, 0f), new Vector3(0.26f, 0.035f, 0.26f), dark, Vector3.zero);

            // ---- arms (parented to torso; shoulder -> upper arm -> elbow) ----
            Transform armL = Pivot(torso, "ArmL_Pivot", new Vector3(-0.27f, 0.5f, 0f));
            Transform armR = Pivot(torso, "ArmR_Pivot", new Vector3(0.27f, 0.5f, 0f));

            BuildArm(armL, "L", khaki, skin, -1f);
            BuildArm(armR, "R", khaki, skin, 1f);

            // prop flashlight gripped in front of the chest
            Cylinder(torso, "HeldFlashlight", new Vector3(0f, 0.32f, 0.32f), 0.07f, 0.3f, metal, new Vector3(90f, 0f, 0f));
            Cylinder(torso, "HeldFlashlightLens", new Vector3(0f, 0.32f, 0.47f), 0.095f, 0.05f, lens, new Vector3(90f, 0f, 0f));

            // ---- wire up animator -------------------------------------------
            var animator = player.GetComponent<PlayerBodyAnimator>();
            if (animator == null) animator = player.gameObject.AddComponent<PlayerBodyAnimator>();
            animator.hips = hips;
            animator.torso = torso;
            animator.leftLegPivot = legL;
            animator.rightLegPivot = legR;
            animator.leftKneePivot = legL.Find("KneeL_Pivot");
            animator.rightKneePivot = legR.Find("KneeR_Pivot");
            animator.leftArmPivot = armL;
            animator.rightArmPivot = armR;
            animator.leftElbowPivot = armL.Find("ElbowL_Pivot");
            animator.rightElbowPivot = armR.Find("ElbowR_Pivot");
            return animator;
        }

        // ---------------------------------------------------------------------

        /// <summary>Thigh hangs from the leg pivot; the knee pivot at the
        /// thigh's end carries the shin and boot.</summary>
        static void BuildLeg(Transform legPivot, string side, Material denim, Material boot, Material pad)
        {
            Box(legPivot, "Thigh" + side, new Vector3(0f, -0.21f, 0f),
                new Vector3(0.18f, 0.42f, 0.2f), denim, Vector3.zero);
            // knee pad on the front of the thigh end
            Box(legPivot, "KneePad" + side, new Vector3(0f, -0.4f, 0.1f),
                new Vector3(0.16f, 0.12f, 0.05f), pad, Vector3.zero);

            Transform knee = Pivot(legPivot, "Knee" + side + "_Pivot", new Vector3(0f, -0.42f, 0f));
            Box(knee, "Shin" + side, new Vector3(0f, -0.19f, 0f),
                new Vector3(0.16f, 0.38f, 0.17f), denim, Vector3.zero);
            // boot: sole + toe, hanging from the knee
            Box(knee, "Boot" + side, new Vector3(0f, -0.4f, 0.03f),
                new Vector3(0.19f, 0.14f, 0.3f), boot, Vector3.zero);
            Box(knee, "BootSole" + side, new Vector3(0f, -0.48f, 0.03f),
                new Vector3(0.2f, 0.04f, 0.32f), pad, Vector3.zero);
        }

        /// <summary>Upper arm hangs from the shoulder; the elbow pivot carries
        /// the forearm and hand (a slight base bend reads as "carrying the
        /// light" before the animator takes over).</summary>
        static void BuildArm(Transform armPivot, string side, Material sleeve, Material skin, float sx)
        {
            Box(armPivot, "Shoulder" + side, new Vector3(0f, -0.03f, 0f),
                new Vector3(0.16f, 0.16f, 0.16f), sleeve, Vector3.zero);
            Box(armPivot, "UpperArm" + side, new Vector3(0f, -0.15f, 0f),
                new Vector3(0.13f, 0.3f, 0.14f), sleeve, Vector3.zero);
            Box(armPivot, "ElbowPad" + side, new Vector3(0f, -0.28f, 0.06f),
                new Vector3(0.12f, 0.09f, 0.05f), sleeve, Vector3.zero);

            Transform elbow = Pivot(armPivot, "Elbow" + side + "_Pivot", new Vector3(0f, -0.3f, 0f));
            Box(elbow, "Forearm" + side, new Vector3(0f, -0.12f, 0.03f),
                new Vector3(0.115f, 0.24f, 0.12f), sleeve, Vector3.zero);
            Box(elbow, "Hand" + side, new Vector3(0f, -0.28f, 0.03f),
                new Vector3(0.11f, 0.12f, 0.14f), skin, Vector3.zero);
        }

        // ---------------------------------------------------------------------

        static Transform Pivot(Transform parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            return go.transform;
        }

        static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat, Vector3 euler)
        {
            return Prim(PrimitiveType.Cube, parent, name, localPos, size, mat, euler);
        }

        static GameObject Cylinder(Transform parent, string name, Vector3 localPos, float diameter, float height, Material mat, Vector3 euler)
        {
            // Cylinder primitive is 1 wide and 2 tall by default.
            return Prim(PrimitiveType.Cylinder, parent, name, localPos,
                new Vector3(diameter, height * 0.5f, diameter), mat, euler);
        }

        static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat, Vector3 euler)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = localScale;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = mat;

            // The body must never block the player's own movement.
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }
            return go;
        }
    }
}
