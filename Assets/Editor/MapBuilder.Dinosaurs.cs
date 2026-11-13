using UnityEngine;
using AnalogHorror;

namespace AnalogHorror.EditorTools
{
    public static partial class MapBuilder
    {
        // ================================================================ DINOSAURS
        // All dinos are built from primitives in a consistent dark, fog-eaten
        // style. Local convention: the creature faces +Z before yaw is applied.
        //
        // The T-Rex and raptors are rigged for DinoAnimator: a torso pivot
        // (breathing/lean), hip → knee → ankle pivots per leg (walk cycle),
        // a chained tail (travelling wave) and a hinged jaw (lunge gape).

        static void BuildDinosaurs()
        {
            // 1 — the T-Rex, standing in its breached paddock facing the road
            BuildTRexDino(new Vector3(-48f, 0f, 31f), 90f);

            // 2 — raptors: two inside the paddock, one creeping at the
            //     jungle edge where the main road cuts through the trees
            BuildRaptorDino(new Vector3(58f, 0f, 40f), -73f);
            BuildRaptorDino(new Vector3(36f, 0f, 61f), 165f);
            BuildRaptorDino(new Vector3(10.5f, 0f, 66f), -90f);

            // 3 — huge sauropod silhouette deep in the north forest (fog only)
            BuildSauropod(new Vector3(-63f, 0f, -64f), 135f);

            // 4 — pteranodon perched on the visitor center roof edge
            BuildPteranodon(new Vector3(8f, 7.45f, 5.6f), -160f);
        }

        // ---------------------------------------------------------------- T-REX
        // hip height ~3m, head ~5m, total length ~11m

        static void BuildTRexDino(Vector3 pos, float yaw)
        {
            var root = new GameObject("TRex_Dino");
            root.transform.SetParent(padRoot, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var t = root.transform;

            var hide = M("DinoHide");
            var belly = M("DinoBelly");
            var dark = M("DinoSpine");
            var teeth = M("PaintWhite");
            var eyes = M("EyeGlow");

            // ---- torso parts (everything above the hips) ----
            var body = Capsule(t, "TRex_Body", new Vector3(0f, 3.0f, 0f), 1.8f, 4.6f, hide, new Vector3(90f, 0f, 0f));
            var hipsBall = Ball(t, "TRex_Hips", new Vector3(0f, 3.05f, -0.7f), new Vector3(2.1f, 1.9f, 2.3f), hide);
            var chestBall = Ball(t, "TRex_Chest", new Vector3(0f, 3.3f, 1.6f), new Vector3(1.6f, 1.6f, 1.9f), hide);
            var bellyBox = Box(t, "TRex_Belly", new Vector3(0f, 2.45f, 0.4f), new Vector3(1.3f, 0.7f, 3f), belly);

            // ---- tail: chained pivots so the wave travels tip-ward ----
            var tailSegs = new Transform[7];
            for (int i = 0; i < 7; i++)
            {
                int n = i + 1;
                float z = -1.7f - n * 0.75f;
                float y = 3.0f - n * 0.11f;
                float sway = Mathf.Sin(n * 0.7f) * 5f;
                float thick = Mathf.Lerp(1.05f, 0.16f, n / 7f);
                tailSegs[i] = Capsule(t, "TRex_Tail", new Vector3(0f, y, z), thick, 0.85f, hide,
                    new Vector3(90f, sway, 0f)).transform;
            }
            var tail = BuildTail(t, "TRex", tailSegs, new Vector3(0f, 3.0f, -1.6f));

            // ---- legs: hip → knee → ankle pivots (digitigrade) ----
            var hipJ = new Transform[2];
            var kneeJ = new Transform[2];
            var ankleJ = new Transform[2];

            for (int side = -1, li = 0; side <= 1; side += 2, li++)
            {
                Vector3 hip = new Vector3(side * 1.0f, 3.05f, -0.45f);
                Vector3 knee = new Vector3(side * 1.05f, 1.35f, 0f);
                Vector3 ankle = new Vector3(side * 1.05f, 0.45f, 0.12f);

                hipJ[li] = Pivot(t, "TRex_HipJ" + (li + 1), hip);
                kneeJ[li] = Pivot(hipJ[li], "TRex_KneeJ" + (li + 1), knee - hip);
                ankleJ[li] = Pivot(kneeJ[li], "TRex_AnkleJ" + (li + 1), ankle - knee);

                Capsule(t, "TRex_Thigh", new Vector3(side * 1.0f, 2.15f, -0.45f),
                    0.95f, 2.1f, hide, new Vector3(12f, 0f, side * -9f))
                    .transform.SetParent(hipJ[li], true);
                Ball(t, "TRex_Knee", new Vector3(side * 1.05f, 1.35f, 0f),
                    new Vector3(0.8f, 0.75f, 0.8f), hide)
                    .transform.SetParent(kneeJ[li], true);
                Capsule(t, "TRex_Shin", new Vector3(side * 1.05f, 0.85f, -0.15f),
                    0.5f, 1.5f, hide, new Vector3(-18f, 0f, side * -3f))
                    .transform.SetParent(kneeJ[li], true);
                Box(t, "TRex_Foot", new Vector3(side * 1.05f, 0.16f, 0.45f),
                    new Vector3(0.7f, 0.32f, 1.4f), dark)
                    .transform.SetParent(ankleJ[li], true);
                for (int claw = -1; claw <= 1; claw++)
                {
                    Box(t, "TRex_Toe", new Vector3(side * 1.05f + claw * 0.24f, 0.12f, 1.2f),
                        new Vector3(0.2f, 0.2f, 0.55f), dark)
                        .transform.SetParent(ankleJ[li], true);
                }
            }

            // ---- tiny arms (claws follow their arm) ----
            Transform armL = null, armR = null;
            for (int side = -1; side <= 1; side += 2)
            {
                var arm = Capsule(t, "TRex_Arm", new Vector3(side * 0.8f, 3.25f, 1.95f),
                    0.24f, 0.85f, hide, new Vector3(58f, 0f, side * 16f));
                var claw = Box(t, "TRex_Claw", new Vector3(side * 0.86f, 2.85f, 2.35f),
                    new Vector3(0.1f, 0.1f, 0.3f), dark, new Vector3(35f, 0f, 0f));
                claw.transform.SetParent(arm.transform, true);
                if (side < 0) armL = arm.transform; else armR = arm.transform;
            }

            // ---- neck + head (head on its own pivot so it can scan/strike) ----
            var neck1 = Capsule(t, "TRex_Neck1", new Vector3(0f, 3.8f, 2.3f), 0.75f, 1.3f, hide, new Vector3(52f, 0f, 0f));
            var neck2 = Capsule(t, "TRex_Neck2", new Vector3(0f, 4.3f, 2.85f), 0.62f, 1.0f, hide, new Vector3(74f, 0f, 0f));

            var head = Pivot(t, "TRex_HeadPivot", new Vector3(0f, 4.7f, 3.3f));

            Box(head, "TRex_Skull", new Vector3(0f, 0.12f, 0.5f), new Vector3(0.9f, 0.75f, 1.5f), hide, new Vector3(4f, 0f, 0f));
            Box(head, "TRex_Snout", new Vector3(0f, 0.0f, 1.5f), new Vector3(0.62f, 0.5f, 1.0f), hide, new Vector3(4f, 0f, 0f));
            Box(head, "TRex_BrowL", new Vector3(-0.36f, 0.42f, 0.55f), new Vector3(0.3f, 0.18f, 0.7f), dark);
            Box(head, "TRex_BrowR", new Vector3(0.36f, 0.42f, 0.55f), new Vector3(0.3f, 0.18f, 0.7f), dark);

            // jaw hinges at the back of the skull so the gape reads properly
            var jaw = Pivot(head, "TRex_JawPivot", new Vector3(0f, -0.34f, 0.2f));
            Box(jaw, "TRex_Jaw", new Vector3(0f, -0.16f, 0.75f), new Vector3(0.6f, 0.2f, 1.75f), hide, new Vector3(15f, 0f, 0f));

            // teeth — upper rows on the skull, lower rows on the jaw
            for (int i = 0; i < 7; i++)
            {
                float z = 0.85f + i * 0.2f;
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(head, "TRex_ToothU", new Vector3(side * 0.25f, -0.2f, z),
                        new Vector3(0.07f, 0.2f, 0.07f), teeth, new Vector3(-5f, 0f, 0f));
                    Box(jaw, "TRex_ToothL", new Vector3(side * 0.22f, -0.03f, z - 0.28f),
                        new Vector3(0.06f, 0.16f, 0.06f), teeth, new Vector3(16f, 0f, 0f));
                }
            }

            // glowing eyes — the thing you notice first in the dark
            var eyeL = Ball(head, "TRex_EyeL", new Vector3(-0.4f, 0.3f, 0.55f),
                new Vector3(0.16f, 0.13f, 0.1f), eyes);
            var eyeR = Ball(head, "TRex_EyeR", new Vector3(0.4f, 0.3f, 0.55f),
                new Vector3(0.16f, 0.13f, 0.1f), eyes);
            StripCollider(eyeL);
            StripCollider(eyeR);

            // ---- group everything above the hips onto the torso pivot ----
            var torso = Pivot(t, "TRex_TorsoPivot", new Vector3(0f, 3.0f, -0.45f));
            MoveUnder(torso, body.transform, hipsBall.transform, chestBall.transform,
                bellyBox.transform, tail[0].parent, neck1.transform, neck2.transform,
                head, armL, armR);

            // ---- hunting AI: it hears you through the breach ----
            var hunter = root.AddComponent<DinoHunter>();
            hunter.kind = DinoHunter.Kind.TRex;
            hunter.detectRange = 32f;
            hunter.loseRange = 55f;
            hunter.catchRange = 2.9f;
            hunter.eyeHeight = 4.7f;
            hunter.walkSpeed = 1.7f;
            hunter.runSpeed = 4.5f;      // player sprints at 5.4 — run
            hunter.stareTime = 1.1f;
            hunter.stepInterval = 0.72f;

            // ---- joint-driven animation: walk cycle, breath, tail, lunge ----
            var anim = root.AddComponent<DinoAnimator>();
            anim.hunter = hunter;
            anim.torso = torso;
            anim.hips = hipJ;
            anim.knees = kneeJ;
            anim.ankles = ankleJ;
            anim.tail = tail;
            anim.head = head;
            anim.jaw = jaw;
            anim.armL = armL;
            anim.armR = armR;
            anim.strideFrequency = 0.42f;   // ~2.4m stride at full run
            anim.hipSwing = 26f;
            anim.kneeBend = 42f;
            anim.bodyBob = 0.08f;
        }

        // --------------------------------------------------------------- RAPTOR
        // crouched stalking pose, ~1.2m at the hip, ~4m nose-to-tail

        static void BuildRaptorDino(Vector3 pos, float yaw)
        {
            var root = new GameObject("Raptor_Dino");
            root.transform.SetParent(padRoot, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var t = root.transform;

            var hide = M("DinoHide");
            var belly = M("DinoBelly");
            var dark = M("DinoSpine");
            var teeth = M("PaintWhite");
            var eyes = M("EyeGlow");

            // ---- torso parts ----
            var body = Capsule(t, "Rap_Body", new Vector3(0f, 1.25f, 0f), 0.8f, 2.3f, hide, new Vector3(90f, 0f, 0f));
            var chestBall = Ball(t, "Rap_Chest", new Vector3(0f, 1.32f, 0.95f), new Vector3(0.85f, 0.8f, 0.95f), hide);
            var bellyBox = Box(t, "Rap_Belly", new Vector3(0f, 0.95f, 0.2f), new Vector3(0.55f, 0.35f, 1.7f), belly);

            // ---- stiff counterweight tail, chained pivots ----
            var tailSegs = new Transform[6];
            for (int i = 0; i < 6; i++)
            {
                int n = i + 1;
                float z = -1.1f - n * 0.5f;
                float y = 1.3f - n * 0.03f;
                float sway = Mathf.Sin(n * 0.9f) * 3f;
                float thick = Mathf.Lerp(0.42f, 0.07f, n / 6f);
                tailSegs[i] = Capsule(t, "Rap_Tail", new Vector3(0f, y, z), thick, 0.58f, hide,
                    new Vector3(90f, sway, 0f)).transform;
            }
            var tail = BuildTail(t, "Rap", tailSegs, new Vector3(0f, 1.3f, -1.0f));

            // ---- legs (crouched, ready to spring): hip → knee → ankle ----
            var hipJ = new Transform[2];
            var kneeJ = new Transform[2];
            var ankleJ = new Transform[2];

            for (int side = -1, li = 0; side <= 1; side += 2, li++)
            {
                Vector3 hip = new Vector3(side * 0.44f, 1.5f, -0.35f);
                Vector3 knee = new Vector3(side * 0.46f, 0.92f, -0.23f);
                Vector3 ankle = new Vector3(side * 0.46f, 0.2f, 0.3f);

                hipJ[li] = Pivot(t, "Rap_HipJ" + (li + 1), hip);
                kneeJ[li] = Pivot(hipJ[li], "Rap_KneeJ" + (li + 1), knee - hip);
                ankleJ[li] = Pivot(kneeJ[li], "Rap_AnkleJ" + (li + 1), ankle - knee);

                Capsule(t, "Rap_Thigh", new Vector3(side * 0.44f, 1.02f, -0.35f),
                    0.48f, 1.1f, hide, new Vector3(28f, 0f, side * -8f))
                    .transform.SetParent(hipJ[li], true);
                Capsule(t, "Rap_Shin", new Vector3(side * 0.46f, 0.5f, 0.05f),
                    0.28f, 1.0f, hide, new Vector3(-34f, 0f, 0f))
                    .transform.SetParent(kneeJ[li], true);
                Box(t, "Rap_Foot", new Vector3(side * 0.46f, 0.08f, 0.35f),
                    new Vector3(0.26f, 0.16f, 0.6f), dark)
                    .transform.SetParent(ankleJ[li], true);
                // sickle claw
                Box(t, "Rap_Sickle", new Vector3(side * 0.46f, 0.16f, 0.62f),
                    new Vector3(0.07f, 0.1f, 0.28f), teeth, new Vector3(-25f, 0f, 0f))
                    .transform.SetParent(ankleJ[li], true);
            }

            // ---- arms with grasping claws (hands follow their arm) ----
            Transform armL = null, armR = null;
            for (int side = -1; side <= 1; side += 2)
            {
                var arm = Capsule(t, "Rap_Arm", new Vector3(side * 0.42f, 1.18f, 0.85f),
                    0.17f, 0.62f, hide, new Vector3(62f, 0f, side * -18f));
                var hand = Box(t, "Rap_Hand", new Vector3(side * 0.5f, 0.88f, 1.12f),
                    new Vector3(0.1f, 0.1f, 0.26f), dark, new Vector3(30f, 0f, 0f));
                hand.transform.SetParent(arm.transform, true);
                if (side < 0) armL = arm.transform; else armR = arm.transform;
            }

            // ---- neck + head ----
            var neck = Capsule(t, "Rap_Neck", new Vector3(0f, 1.55f, 1.3f), 0.42f, 0.8f, hide, new Vector3(56f, 0f, 0f));

            var head = Pivot(t, "Rap_HeadPivot", new Vector3(0f, 1.8f, 1.62f));

            Box(head, "Rap_Skull", new Vector3(0f, 0.05f, 0.28f), new Vector3(0.44f, 0.4f, 0.75f), hide);
            Box(head, "Rap_Snout", new Vector3(0f, -0.03f, 0.78f), new Vector3(0.28f, 0.26f, 0.55f), hide);

            // jaw hinges at the back of the skull
            var jaw = Pivot(head, "Rap_JawPivot", new Vector3(0f, -0.2f, 0.12f));
            Box(jaw, "Rap_Jaw", new Vector3(0f, -0.06f, 0.38f), new Vector3(0.26f, 0.11f, 0.8f), hide, new Vector3(13f, 0f, 0f));

            for (int i = 0; i < 5; i++)
            {
                float z = 0.55f + i * 0.13f;
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(head, "Rap_Tooth", new Vector3(side * 0.11f, -0.13f, z),
                        new Vector3(0.04f, 0.11f, 0.04f), teeth);
                }
            }

            var eyeL = Ball(head, "Rap_EyeL", new Vector3(-0.19f, 0.13f, 0.3f),
                new Vector3(0.09f, 0.08f, 0.06f), eyes);
            var eyeR = Ball(head, "Rap_EyeR", new Vector3(0.19f, 0.13f, 0.3f),
                new Vector3(0.09f, 0.08f, 0.06f), eyes);
            StripCollider(eyeL);
            StripCollider(eyeR);

            // ---- group everything above the hips onto the torso pivot ----
            var torso = Pivot(t, "Rap_TorsoPivot", new Vector3(0f, 1.25f, -0.3f));
            MoveUnder(torso, body.transform, chestBall.transform, bellyBox.transform,
                tail[0].parent, neck.transform, head, armL, armR);

            // ---- hunting AI: fast, patient, hard to lose ----
            var hunter = root.AddComponent<DinoHunter>();
            hunter.kind = DinoHunter.Kind.Raptor;
            hunter.detectRange = 25f;
            hunter.loseRange = 44f;
            hunter.catchRange = 1.9f;
            hunter.eyeHeight = 1.9f;
            hunter.walkSpeed = 2.3f;
            hunter.runSpeed = 5.0f;         // just under your sprint
            hunter.stareTime = 1.5f;
            hunter.stepInterval = 0.5f;

            // ---- joint-driven animation: quicker and twitchier than the rex ----
            var anim = root.AddComponent<DinoAnimator>();
            anim.hunter = hunter;
            anim.torso = torso;
            anim.hips = hipJ;
            anim.knees = kneeJ;
            anim.ankles = ankleJ;
            anim.tail = tail;
            anim.head = head;
            anim.jaw = jaw;
            anim.armL = armL;
            anim.armR = armR;
            anim.strideFrequency = 0.6f;    // ~1.7m stride
            anim.hipSwing = 30f;
            anim.kneeBend = 55f;
            anim.bodyBob = 0.05f;
            anim.headScan = 7f;             // twitchier scans
            anim.headScanSpeed = 0.55f;
        }

        // ------------------------------------------------- SAUROPOD (SILHOUETTE)
        // enormous, near-black, meant to be glimpsed through fog and trees.
        // colliders stripped — pure atmosphere.

        static void BuildSauropod(Vector3 pos, float yaw)
        {
            var root = new GameObject("Sauropod_Silhouette");
            root.transform.SetParent(jungRoot, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var t = root.transform;

            var dark = M("DinoSpine");
            var hide = M("DinoHide");

            // ---- body ----
            Capsule(t, "Saur_Body", new Vector3(0f, 5.4f, 0f), 3.4f, 7.5f, dark, new Vector3(90f, 0f, 0f));
            Ball(t, "Saur_Hips", new Vector3(0f, 5.5f, -2.4f), new Vector3(4.2f, 3.8f, 4.4f), dark);

            // ---- four pillar legs ----
            for (int i = 0; i < 4; i++)
            {
                float dx = (i & 1) == 0 ? -1.5f : 1.5f;
                float dz = (i & 2) == 0 ? -1.9f : 2.1f;
                Cyl(t, "Saur_Leg", new Vector3(dx, 2.5f, dz), 1.3f, 5f, dark,
                    new Vector3(dz * 1.2f, 0f, -dx * 1.6f));
                Box(t, "Saur_Foot", new Vector3(dx, 0.3f, dz + 0.3f),
                    new Vector3(1.9f, 0.6f, 2.3f), dark);
            }

            // ---- neck arcing up into the canopy ----
            Vector3 neckBase = new Vector3(0f, 6.2f, 3.2f);
            for (int i = 0; i < 6; i++)
            {
                float p = i / 5f;
                Vector3 n = neckBase + new Vector3(0f,
                    p * 5.5f + Mathf.Sin(p * Mathf.PI) * 0.8f,
                    p * 2.6f);
                float thick = Mathf.Lerp(1.7f, 0.55f, p);
                Capsule(t, "Saur_Neck", n, thick, 1.7f, dark,
                    new Vector3(Mathf.Lerp(35f, 78f, p), 0f, 0f));
            }

            // small head with a blunt muzzle, high above the trees
            var head = Pivot(t, "Saur_HeadPivot", new Vector3(0f, 12.2f, 6.1f));
            Box(head, "Saur_Head", new Vector3(0f, 0f, 0.4f), new Vector3(1.1f, 1.0f, 1.9f), dark, new Vector3(12f, 0f, 0f));
            Box(head, "Saur_Muzzle", new Vector3(0f, -0.2f, 1.3f), new Vector3(0.9f, 0.6f, 1.0f), dark, new Vector3(12f, 0f, 0f));
            var nostril = Ball(head, "Saur_Nostril", new Vector3(0.3f, 0.2f, 1.0f),
                new Vector3(0.2f, 0.2f, 0.2f), M("ShadowBlack"));
            StripCollider(nostril);

            // ---- tail dragging back into the dark ----
            for (int i = 1; i <= 7; i++)
            {
                float z = -4.2f - i * 1.5f;
                float y = 5.2f - i * 0.55f;
                float sway = Mathf.Sin(i * 0.55f) * 6f;
                float thick = Mathf.Lerp(2.2f, 0.35f, i / 7f);
                Capsule(t, "Saur_Tail", new Vector3(0f, Mathf.Max(y, 0.9f), z), thick, 1.9f, dark,
                    new Vector3(Mathf.Lerp(90f, 74f, i / 7f), sway, 0f));
            }

            // strip every collider — it is atmosphere, not an obstacle
            foreach (var col in root.GetComponentsInChildren<Collider>())
                StripCollider(col.gameObject);

            var idle = root.AddComponent<DinoIdle>();
            idle.head = head;
            idle.breatheAmount = 0.2f;
            idle.breatheSpeed = 0.35f;
            idle.headAmount = 5f;
            idle.headSpeed = 0.12f;
            idle.canRoar = false;
        }

        // ----------------------------------------------------------- PTERANODON
        // perched on the roof edge, wings folded — strips colliders (roof décor)

        static void BuildPteranodon(Vector3 pos, float yaw)
        {
            var root = new GameObject("Pteranodon_Roof");
            root.transform.SetParent(vcRoot, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var t = root.transform;

            var hide = M("DinoHide");
            var dark = M("DinoSpine");
            var eyes = M("EyeGlow");

            // body, crouched
            Capsule(t, "Pter_Body", new Vector3(0f, 0.42f, 0f), 0.55f, 1.2f, hide, new Vector3(72f, 0f, 0f));
            Ball(t, "Pter_Hips", new Vector3(0f, 0.45f, -0.45f), new Vector3(0.6f, 0.55f, 0.65f), hide);

            // folded wings: long bone segments swept back along the body
            Transform wingL = null, wingR = null;
            for (int side = -1; side <= 1; side += 2)
            {
                var arm = Box(t, "Pter_WingArm", new Vector3(side * 0.55f, 0.55f, -0.1f),
                    new Vector3(1.3f, 0.1f, 0.22f), dark, new Vector3(0f, side * -14f, side * 8f));
                var finger = Box(t, "Pter_WingFinger", new Vector3(side * 0.75f, 0.38f, -0.85f),
                    new Vector3(0.9f, 0.07f, 1.6f), hide, new Vector3(-6f, side * 10f, side * 4f));
                finger.transform.SetParent(arm.transform, true);
                if (side < 0) wingL = arm.transform; else wingR = arm.transform;
            }

            // long crested head
            var head = Pivot(t, "Pter_HeadPivot", new Vector3(0f, 0.72f, 0.5f));
            Box(head, "Pter_Skull", new Vector3(0f, 0f, 0.25f), new Vector3(0.24f, 0.26f, 0.6f), hide);
            Box(head, "Pter_Beak", new Vector3(0f, -0.04f, 0.85f), new Vector3(0.11f, 0.12f, 1.0f), dark, new Vector3(3f, 0f, 0f));
            // backward crest
            Box(head, "Pter_Crest", new Vector3(0f, 0.2f, -0.15f), new Vector3(0.06f, 0.32f, 0.7f), dark, new Vector3(-28f, 0f, 0f));

            var eyeL = Ball(head, "Pter_EyeL", new Vector3(-0.11f, 0.07f, 0.3f),
                new Vector3(0.05f, 0.05f, 0.04f), eyes);
            var eyeR = Ball(head, "Pter_EyeR", new Vector3(0.11f, 0.07f, 0.3f),
                new Vector3(0.05f, 0.05f, 0.04f), eyes);
            StripCollider(eyeL);
            StripCollider(eyeR);

            // spindly legs gripping the roof
            for (int side = -1; side <= 1; side += 2)
            {
                Cyl(t, "Pter_Leg", new Vector3(side * 0.22f, 0.14f, -0.25f), 0.09f, 0.35f, dark);
            }

            foreach (var col in root.GetComponentsInChildren<Collider>())
                StripCollider(col.gameObject);

            var idle = root.AddComponent<DinoIdle>();
            idle.head = head;
            idle.wingL = wingL;
            idle.wingR = wingR;
            idle.breatheAmount = 0.6f;
            idle.breatheSpeed = 1.3f;
            idle.headAmount = 9f;
            idle.headSpeed = 0.7f;
            idle.canRoar = false;
        }

        // --------------------------------------------------------------- helpers

        /// <summary>Empty pivot for independently animated parts.</summary>
        static Transform Pivot(Transform parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            return go.transform;
        }

        /// <summary>
        /// Reparents parts under a pivot while keeping their world pose
        /// (the builder works in root-local numbers, so this just re-bases).
        /// </summary>
        static void MoveUnder(Transform pivot, params Transform[] parts)
        {
            foreach (var p in parts)
                if (p != null) p.SetParent(pivot, true);
        }

        /// <summary>
        /// Threads empty pivots through tail segment joints so rotations
        /// accumulate down the chain — a wave at the base travels to the tip.
        /// Pivot 0 sits at the first joint, its parent is the chain root.
        /// </summary>
        static Transform[] BuildTail(Transform parent, string prefix,
            Transform[] segs, Vector3 tailBase)
        {
            var pivots = new Transform[segs.Length];

            // chain root: the tail's attachment point (reparented with the torso)
            var rootPv = Pivot(parent, prefix + "_TailRoot", tailBase);

            Transform prevPivot = rootPv;
            Vector3 prevPos = tailBase;      // world-space (parent-local) pos of prevPivot
            Vector3 prevAnchor = tailBase;   // previous segment center

            for (int i = 0; i < segs.Length; i++)
            {
                Vector3 center = segs[i].localPosition;      // still under parent
                Vector3 joint = (prevAnchor + center) * 0.5f;

                // pivots have identity rotation, so local = joint − parent pos
                var pv = Pivot(prevPivot, prefix + "_TailJ" + (i + 1), joint - prevPos);
                segs[i].SetParent(pv, true);                 // keep world pose
                pivots[i] = pv;

                prevPivot = pv;
                prevPos = joint;
                prevAnchor = center;
            }
            return pivots;
        }
    }
}
