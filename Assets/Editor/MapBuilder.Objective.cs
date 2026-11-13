using UnityEngine;
using AnalogHorror;

namespace AnalogHorror.EditorTools
{
    public static partial class MapBuilder
    {
        // ============================================================ GAME LOOP
        // The actual game: three power cells scattered one per danger zone,
        // a dead main gate, and an escape ending. Wired last so the horror
        // layer and world are fully in place when it looks them up.

        static void BuildGameLoop()
        {
            var dirGo = GameObject.Find("HorrorDirector");
            var playerGo = GameObject.Find("Player");
            if (dirGo == null || playerGo == null)
            {
                Debug.LogWarning("Game loop: Director or Player missing — objectives skipped.");
                return;
            }

            var director = dirGo.GetComponent<HorrorDirector>();
            var controller = playerGo.GetComponent<FirstPersonController>();
            var cam = controller != null ? controller.camHolder : null;
            var overlay = cam != null ? cam.GetComponent<VhsOverlay>() : null;
            var flashlight = cam != null ? cam.GetComponent<Flashlight>() : null;

            var objSys = dirGo.AddComponent<ObjectiveSystem>();
            var hud = dirGo.AddComponent<GameHud>();
            var pause = dirGo.AddComponent<PauseSystem>();

            objSys.director = director;
            objSys.storm = dirGo.GetComponent<StormController>();
            objSys.controller = controller;
            objSys.overlay = overlay;
            objSys.flashlight = flashlight;
            objSys.ambience = director != null ? director.ambience : null;
            objSys.player = playerGo.transform;
            objSys.gatePoint = new Vector3(0f, 1.5f, 21f);   // under the gate arch
            objSys.gateRadius = 5.5f;

            hud.objective = objSys;
            hud.controller = controller;
            hud.director = director;

            pause.controller = controller;

            // bone bait: five bones, thrown with Q — the only way to pull
            // a predator off the thing you need
            var bait = playerGo.AddComponent<BaitThrower>();
            bait.cam = cam != null ? cam.GetComponent<Camera>() : null;
            bait.controller = controller;
            bait.bones = 5;
            bait.throwForce = 13f;
            bait.lureRadius = 45f;
            bait.pelletLife = 14f;
            hud.bait = bait;

            var root = NewRoot("_GAME LOOP");

            // one cell per danger zone: the easy one, the deep one,
            // and the one the T-Rex is standing over
            objSys.fuses.Add(PlaceFuse(root, new Vector3(5.2f, 0.95f, 8.8f),
                "PowerCell_Lobby", "LOBBY"));
            objSys.fuses.Add(PlaceFuse(root, new Vector3(-10f, -3.05f, -47.4f),
                "PowerCell_Tunnel", "TUNNEL END"));
            objSys.fuses.Add(PlaceFuse(root, new Vector3(-33.5f, 0.95f, 37.5f),
                "PowerCell_Paddock", "T-REX PADDOCK"));
        }

        static FuseCell PlaceFuse(Transform parent, Vector3 pos, string name, string slot)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var glow = Emissive("FuseGlow", new Color(0.62f, 0.95f, 0.75f),
                new Color(0.8f, 2.6f, 1.4f), 0.6f);

            var body = Box(go.transform, "Fuse_Case", Vector3.zero,
                new Vector3(0.3f, 0.36f, 0.3f), M("MetalDark"));
            StripCollider(body);

            // glowing core windows on both faces
            var coreA = Box(go.transform, "Fuse_Core", new Vector3(0f, 0f, 0.16f),
                new Vector3(0.18f, 0.24f, 0.02f), glow);
            StripCollider(coreA);
            var coreB = Box(go.transform, "Fuse_Core", new Vector3(0f, 0f, -0.16f),
                new Vector3(0.18f, 0.24f, 0.02f), glow);
            StripCollider(coreB);

            var cap = Cyl(go.transform, "Fuse_Cap", new Vector3(0f, 0.22f, 0f),
                0.14f, 0.1f, M("MetalDark"));
            StripCollider(cap);

            var lightGo = NewPointLight(pos, new Color(0.6f, 1f, 0.75f), 1.5f, 5.5f, false);
            lightGo.transform.SetParent(go.transform, true);

            var cell = go.AddComponent<FuseCell>();
            cell.slotName = slot;
            return cell;
        }

        // ============================================================ MENU
        // The title screen: gates every gameplay system until BEGIN TAPE.

        static void BuildMenu()
        {
            var dirGo = GameObject.Find("HorrorDirector");
            var playerGo = GameObject.Find("Player");
            if (dirGo == null || playerGo == null)
            {
                Debug.LogWarning("Menu: Director or Player missing — title screen skipped.");
                return;
            }

            var controller = playerGo.GetComponent<FirstPersonController>();
            var objective = dirGo.GetComponent<ObjectiveSystem>();

            var root = NewRoot("_MENU");
            var go = new GameObject("MainMenu");
            go.transform.SetParent(root, false);

            var menu = go.AddComponent<MainMenu>();
            menu.controller = controller;
            menu.objective = objective;
        }
    }
}
