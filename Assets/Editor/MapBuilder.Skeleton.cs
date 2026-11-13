using UnityEngine;

namespace AnalogHorror.EditorTools
{
    public static partial class MapBuilder
    {
        /// <summary>
        /// Museum T-Rex skeleton on a platform under the roof hole —
        /// the lobby centrepiece, skull facing the entrance (+Z).
        /// </summary>
        static void BuildLobbySkeleton()
        {
            // platform (moon shaft hits it)
            Box(vcRoot, "SkeletonPlatform", new Vector3(0f, 0.2f, -1.5f),
                new Vector3(11f, 0.4f, 6f), M("ConcreteDirty"));
            Box(vcRoot, "SkeletonPlatform_Trim", new Vector3(0f, 0.42f, -1.5f),
                new Vector3(11.2f, 0.08f, 6.2f), M("ConcreteDark"));

            var root = new GameObject("T-RexSkeleton");
            root.transform.SetParent(vcRoot, false);
            root.transform.position = new Vector3(0f, 0.44f, -1.5f);
            var t = root.transform;

            var bone = M("Bone");
            var dark = M("ShadowBlack");
            var white = M("PaintWhite");

            // ---- spine: runs from tail (-z) to shoulders (+z) ----
            for (int i = 0; i <= 11; i++)
            {
                float z = 2.2f - i * 0.5f;
                float y = 1.5f + Mathf.Sin(i / 11f * Mathf.PI) * 0.18f;
                Capsule(t, "Spine", new Vector3(0f, y, z), 0.2f, 0.55f, bone,
                    new Vector3(90f, 0f, 0f));
            }

            // ---- ribs ----
            for (int i = 0; i < 6; i++)
            {
                float z = 1.4f - i * 0.55f;
                float y = 1.55f + Mathf.Sin(i / 6f * Mathf.PI) * 0.12f;
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(t, "Rib", new Vector3(side * 0.34f, y - 0.55f, z),
                        new Vector3(0.09f, 1.35f, 0.09f), bone,
                        new Vector3(0f, 0f, side * -26f));
                    // lower rib tip curving inward
                    Box(t, "RibTip", new Vector3(side * 0.6f, y - 1.15f, z),
                        new Vector3(0.08f, 0.55f, 0.08f), bone,
                        new Vector3(0f, 0f, side * -58f));
                }
            }

            // ---- hips ----
            Box(t, "Hip", new Vector3(0f, 1.45f, -1.6f),
                new Vector3(0.95f, 0.5f, 0.7f), bone);
            Box(t, "HipCrestL", new Vector3(-0.45f, 1.8f, -1.7f),
                new Vector3(0.14f, 0.55f, 0.9f), bone, new Vector3(0f, 0f, 22f));
            Box(t, "HipCrestR", new Vector3(0.45f, 1.8f, -1.7f),
                new Vector3(0.14f, 0.55f, 0.9f), bone, new Vector3(0f, 0f, -22f));

            // ---- legs ----
            for (int side = -1; side <= 1; side += 2)
            {
                Capsule(t, "Femur", new Vector3(side * 0.55f, 0.92f, -1.45f),
                    0.3f, 1.15f, bone, new Vector3(16f, 0f, side * -12f));
                Capsule(t, "Shin", new Vector3(side * 0.6f, 0.42f, -1.05f),
                    0.22f, 0.95f, bone, new Vector3(-14f, 0f, side * -5f));
                Box(t, "Foot", new Vector3(side * 0.62f, 0.08f, -0.7f),
                    new Vector3(0.34f, 0.16f, 0.75f), bone);
                for (int claw = -1; claw <= 1; claw++)
                {
                    Box(t, "Toe", new Vector3(side * 0.62f + claw * 0.13f, 0.07f, -0.28f),
                        new Vector3(0.09f, 0.1f, 0.3f), bone);
                }
                // tiny arms
                Capsule(t, "Arm", new Vector3(side * 0.42f, 1.42f, 1.7f),
                    0.11f, 0.5f, bone, new Vector3(52f, 0f, side * 18f));
            }

            // ---- tail ----
            for (int i = 1; i <= 8; i++)
            {
                float z = -1.9f - i * 0.5f;
                float y = 1.35f - i * 0.09f;
                float sway = Mathf.Sin(i * 0.6f) * 0.16f;
                float thickness = Mathf.Lerp(0.26f, 0.08f, i / 8f);
                Capsule(t, "Tail", new Vector3(sway, y, z), thickness, 0.55f, bone,
                    new Vector3(90f, 0f, i * 2f));
            }

            // ---- neck + skull ----
            Capsule(t, "Neck1", new Vector3(0f, 1.7f, 2.55f), 0.22f, 0.7f, bone,
                new Vector3(48f, 0f, 0f));
            Capsule(t, "Neck2", new Vector3(0f, 1.95f, 2.85f), 0.2f, 0.6f, bone,
                new Vector3(66f, 0f, 0f));

            Box(t, "Skull", new Vector3(0f, 2.05f, 3.35f),
                new Vector3(0.72f, 0.6f, 1.1f), bone, new Vector3(6f, 0f, 0f));
            Box(t, "Snout", new Vector3(0f, 1.95f, 4.15f),
                new Vector3(0.5f, 0.42f, 0.75f), bone, new Vector3(6f, 0f, 0f));
            // open jaw
            Box(t, "Jaw", new Vector3(0f, 1.6f, 3.75f),
                new Vector3(0.46f, 0.16f, 1.4f), bone, new Vector3(-13f, 0f, 0f));

            // teeth (upper + lower)
            for (int i = 0; i < 7; i++)
            {
                float z = 3.75f + i * 0.16f;
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(t, "Tooth_Upper", new Vector3(side * 0.21f, 1.72f, z),
                        new Vector3(0.055f, 0.14f, 0.055f), white, new Vector3(-6f, 0f, 0f));
                    Box(t, "Tooth_Lower", new Vector3(side * 0.19f, 1.7f, z - 0.06f),
                        new Vector3(0.05f, 0.12f, 0.05f), white, new Vector3(10f, 0f, 0f));
                }
            }

            // eye sockets
            var socketL = Ball(t, "EyeSocketL", new Vector3(-0.3f, 2.2f, 3.5f),
                new Vector3(0.2f, 0.18f, 0.16f), dark);
            var socketR = Ball(t, "EyeSocketR", new Vector3(0.3f, 2.2f, 3.5f),
                new Vector3(0.2f, 0.18f, 0.16f), dark);
            StripCollider(socketL);
            StripCollider(socketR);

            // ---- museum support rods ----
            Cyl(t, "SupportRod1", new Vector3(0f, 0.75f, 1.2f), 0.07f, 1.5f, M("MetalGray"));
            Cyl(t, "SupportRod2", new Vector3(0f, 0.7f, -0.6f), 0.07f, 1.4f, M("MetalGray"));
            Cyl(t, "SupportRod3", new Vector3(0f, 0.65f, -2.4f), 0.07f, 1.3f, M("MetalGray"));

            // plaque
            Sign("TYRANNOSAURUS REX\nSKELETON CAST  1992", vcRoot, "Sign_SkeletonPlaque",
                new Vector3(3.4f, 1f, 1.75f), 3f, 1f,
                M("SignBlack"), new Color(0.85f, 0.8f, 0.65f),
                FaceToward(Vector3.forward), 0.15f, -4f);
        }
    }
}
