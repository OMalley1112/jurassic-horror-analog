using UnityEngine;
using AnalogHorror;

namespace AnalogHorror.EditorTools
{
    public static partial class MapBuilder
    {
        // =============================================================== NPCS
        // Three survivors stranded on the island. Walk up, press E, and they
        // turn to face you and talk (procedural voice blips). Each one hands
        // over a piece of the picture — and one of them is in the room with
        // the only power cell you can take without a fight.

        static void BuildNpcs()
        {
            var root = NewRoot("_NPCS");
            var controller = Object.FindObjectOfType<FirstPersonController>();

            // 1 — visitor services, hiding in the east lobby
            BuildNpc(root, "MAYA", new Vector3(8.6f, 0f, 1.6f), 297f, controller,
                new[]
                {
                    "Don't come closer \u2014 oh. You're human. Thank god.",
                    "I'm Maya \u2014 visitor services. Nobody came when the alarms started.",
                    "Three power cells keep the gate alive: lobby, tunnels, T-Rex paddock.",
                    "Crouch, kill your lamp, and listen before you move. That's how I'm alive."
                },
                "PlayerKhaki", "hair");

            // 2 — ranger, barricaded in Station 07 across the river
            BuildNpc(root, "RANGER BOONE", new Vector3(0f, 0.21f, 124.4f), 180f, controller,
                new[]
                {
                    "Station's been dark two days. I'm rationing one flare.",
                    "Vasquez is still down at the geothermal plant \u2014 he knows the gate wiring.",
                    "Bones work on anything with a brain. Toss one, then walk away quiet.",
                    "If the ground starts shaking\u2026 don't run straight. Run around."
                },
                "PlayerOlive", "cap");

            // 3 — engineer at the plant console
            BuildNpc(root, "VASQUEZ", new Vector3(112f, 0.21f, 55.4f), 180f, controller,
                new[]
                {
                    "Geothermal \u2014 half dead, but the main gate wiring runs through here.",
                    "I watched the rex leave its paddock on the security feed. It's on the road now.",
                    "Hear that? That's not the storm. Something out there is walking with purpose.",
                    "Get to the gate. Don't wait for me \u2014 my leg's done."
                },
                "PlayerDenim", "helmet");
        }

        static void BuildNpc(Transform parent, string name, Vector3 pos, float yaw,
            FirstPersonController controller, string[] lines, string shirt, string hat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var t = root.transform;

            var denim = M("PlayerDenim");
            var skin = M("PlayerSkin");
            var boot = M("PlayerBoot");
            var dark = M("PlayerDark");

            // legs + boots
            Box(t, "NPC_LegL", new Vector3(-0.12f, 0.45f, 0f),
                new Vector3(0.17f, 0.86f, 0.17f), denim);
            Box(t, "NPC_LegR", new Vector3(0.12f, 0.45f, 0f),
                new Vector3(0.17f, 0.86f, 0.17f), denim);
            Box(t, "NPC_BootL", new Vector3(-0.12f, 0.05f, 0.03f),
                new Vector3(0.19f, 0.1f, 0.26f), boot);
            Box(t, "NPC_BootR", new Vector3(0.12f, 0.05f, 0.03f),
                new Vector3(0.19f, 0.1f, 0.26f), boot);

            // torso + shoulders
            Box(t, "NPC_Torso", new Vector3(0f, 1.16f, 0f),
                new Vector3(0.5f, 0.72f, 0.3f), M(shirt));
            Box(t, "NPC_Shoulders", new Vector3(0f, 1.52f, 0f),
                new Vector3(0.54f, 0.14f, 0.28f), M(shirt));

            // arms + hands
            Box(t, "NPC_ArmL", new Vector3(-0.31f, 1.14f, 0f),
                new Vector3(0.13f, 0.7f, 0.13f), M(shirt));
            Box(t, "NPC_ArmR", new Vector3(0.31f, 1.14f, 0f),
                new Vector3(0.13f, 0.7f, 0.13f), M(shirt));
            Ball(t, "NPC_HandL", new Vector3(-0.31f, 0.76f, 0f),
                new Vector3(0.12f, 0.12f, 0.12f), skin);
            Ball(t, "NPC_HandR", new Vector3(0.31f, 0.76f, 0f),
                new Vector3(0.12f, 0.12f, 0.12f), skin);

            // head: an unscaled pivot so eyes can be placed in world terms
            // and the runtime head-tracking stays predictable
            var headPivot = new GameObject("NPC_Head");
            headPivot.transform.SetParent(t, false);
            headPivot.transform.localPosition = new Vector3(0f, 1.75f, 0f);

            Ball(headPivot.transform, "NPC_Skull", Vector3.zero,
                new Vector3(0.25f, 0.28f, 0.25f), skin);
            Box(headPivot.transform, "NPC_EyeL", new Vector3(-0.06f, 0.03f, 0.115f),
                new Vector3(0.05f, 0.04f, 0.03f), dark);
            Box(headPivot.transform, "NPC_EyeR", new Vector3(0.06f, 0.03f, 0.115f),
                new Vector3(0.05f, 0.04f, 0.03f), dark);

            // headwear by character
            if (hat == "cap")
            {
                Box(t, "NPC_HatTop", new Vector3(0f, 1.9f, 0f),
                    new Vector3(0.28f, 0.09f, 0.28f), M("PlayerHat"));
                Box(t, "NPC_HatBrim", new Vector3(0f, 1.87f, 0.17f),
                    new Vector3(0.26f, 0.04f, 0.16f), M("PlayerHat"));
            }
            else if (hat == "helmet")
            {
                Ball(t, "NPC_Helmet", new Vector3(0f, 1.9f, 0f),
                    new Vector3(0.3f, 0.18f, 0.3f), M("SignYellow"));
                Box(t, "NPC_HelmetBrim", new Vector3(0f, 1.84f, 0f),
                    new Vector3(0.34f, 0.04f, 0.34f), M("SignYellow"));
            }
            else // hair
            {
                Box(t, "NPC_Hair", new Vector3(0f, 1.85f, -0.01f),
                    new Vector3(0.27f, 0.14f, 0.27f), M("PlayerDark"));
                Box(t, "NPC_HairBack", new Vector3(0f, 1.74f, -0.12f),
                    new Vector3(0.24f, 0.24f, 0.08f), M("PlayerDark"));
            }

            // talkable people are not obstacles — you can brush past them
            foreach (var col in root.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(col);

            var npc = root.AddComponent<NpcSurvivor>();
            npc.npcName = name;
            npc.lines = lines;
            npc.controller = controller;
            npc.head = headPivot.transform;
        }
    }
}
