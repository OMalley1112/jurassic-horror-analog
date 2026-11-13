using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnalogHorror
{
    /// <summary>
    /// The actual game: three power cells are scattered across the island —
    /// recover them (E) and reach the main gate to end the tape.
    ///
    /// Taking a cell howls over the grid, so every raptor in earshot knows
    /// exactly where you are standing. The cells are bait with a bite.
    /// </summary>
    public class ObjectiveSystem : MonoBehaviour
    {
        public static ObjectiveSystem Instance { get; private set; }

        [Header("Wired by builder")]
        public HorrorDirector director;
        public StormController storm;
        public FirstPersonController controller;
        public VhsOverlay overlay;
        public Flashlight flashlight;
        public AmbientAudio ambience;
        public Transform player;
        public List<FuseCell> fuses = new List<FuseCell>();

        [Header("Gate")]
        public Vector3 gatePoint = new Vector3(0f, 1.5f, 21f);
        public float gateRadius = 5.5f;
        public int requiredCells = 3;

        [Header("Tuning")]
        public float pickupRange = 2.8f;
        public float alertRadius = 55f;     // how far the cell-alarm carries

        public int Collected { get; private set; }
        public bool Escaped { get; private set; }
        public float Elapsed => Time.time - startTime;
        public FuseCell NearCell { get; private set; }

        float startTime;
        float nextGateHint;
        float endCardAt = -1f;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            startTime = Time.time;
            StartCoroutine(Briefing());
        }

        /// <summary>The mission briefing, taped over an older recording.</summary>
        IEnumerator Briefing()
        {
            // wait for the title screen — the tape doesn't roll until BEGIN
            while (MainMenu.Instance != null && !MainMenu.Instance.Started)
                yield return null;

            yield return new WaitForSeconds(0.8f);
            Broadcast("OBJECTIVE — RESTORE POWER AT THE MAIN GATE", 4.4f);
            yield return new WaitForSeconds(5.0f);
            Broadcast("3 POWER CELLS:  LOBBY · TUNNEL END · T-REX PADDOCK", 5.0f);
            yield return new WaitForSeconds(5.6f);
            Broadcast("CTRL CROUCH · SHIFT SPRINT · F LAMP · E INTERACT", 4.4f);
        }

        void Broadcast(string text, float duration)
        {
            if (director != null) director.Broadcast(text, duration);
        }

        /// <summary>Main menu dismissed — the tape (and the clock) start here.</summary>
        public void OnRunStarted()
        {
            startTime = Time.time;
            nextGateHint = 0f;
        }

        void Update()
        {
            // the tape is stopped — no keys reach the game
            if (PauseSystem.Instance != null && PauseSystem.Instance.IsPaused) return;

            if (Escaped)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    // during the ending cutscene R skips to the stats card
                    if (CutsceneRunning) cutscene.Skip();
                    // small grace so a double-tap R can't skip the stats card
                    // straight past into an accidental restart
                    else if (Time.time >= endCardAt + 0.8f) Restart();
                }
                return;
            }

            // title screen owns everything until the run begins
            if (MainMenu.Instance != null && !MainMenu.Instance.Started) return;

            if (director != null && director.IsCaught) return;

            UpdatePickup();
            UpdateGate();
        }

        // ------------------------------------------------------------- pickup

        void UpdatePickup()
        {
            NearCell = null;
            float best = pickupRange * pickupRange;

            for (int i = 0; i < fuses.Count; i++)
            {
                var f = fuses[i];
                if (f == null || f.Taken) continue;
                float d = (f.transform.position - player.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    NearCell = f;
                }
            }

            if (NearCell != null && Input.GetKeyDown(KeyCode.E))
                Collect(NearCell);
        }

        void Collect(FuseCell cell)
        {
            cell.Take();
            Collected++;

            if (director != null)
            {
                director.Glitch(0.45f, false);
                Broadcast(Collected >= requiredCells
                    ? "◉ ALL POWER CELLS RECOVERED — GET TO THE MAIN GATE"
                    : "◉ POWER CELL RECOVERED   (" + Collected + "/" + requiredCells + ")",
                    3.8f);
            }

            // THE ALARM: yanking a cell screams over the grid — anything
            // hunting in earshot now knows exactly where you are standing
            if (ambience != null) ambience.PlayScreech();

            for (int i = 0; i < DinoHunter.Pack.Count; i++)
            {
                var h = DinoHunter.Pack[i];
                if (h == null) continue;
                if (Vector3.Distance(h.transform.position, player.position) < alertRadius)
                    h.ReceiveAlert(player.position);
            }

            if (Collected >= requiredCells && ambience != null)
                ambience.PlayRoar();        // something out there knows you're leaving

            // --- escalation: the island gets angrier with every cell ---
            if (Collected == 1) EscalateStorm();
            else if (Collected == 2) BreakoutRex();
            else if (Collected == 3) Stampede();
        }

        // --------------------------------------------------------- escalation

        /// <summary>Cell 1: the weather turns on purpose.</summary>
        void EscalateStorm()
        {
            if (storm != null)
            {
                storm.minGap = Mathf.Max(7f, storm.minGap * 0.62f);
                storm.maxGap = Mathf.Max(18f, storm.maxGap * 0.62f);
            }
            StartCoroutine(LateBroadcast(
                "⚠ WEATHER FRONT INBOUND — GRID NOISE RISING", 5f));
        }

        /// <summary>
        /// Cell 2: the T-Rex abandons its paddock and re-anchors on the west
        /// road branch — between the breach and the gate you must reach.
        /// </summary>
        void BreakoutRex()
        {
            StartCoroutine(RexBreakoutRoutine());
        }

        IEnumerator RexBreakoutRoutine()
        {
            DinoHunter rex = null;
            for (int i = 0; i < DinoHunter.Pack.Count; i++)
            {
                var h = DinoHunter.Pack[i];
                if (h != null && h.kind == DinoHunter.Kind.TRex) { rex = h; break; }
            }
            if (rex == null) yield break;

            yield return new WaitForSeconds(3.5f);

            // wait until it's not mid-hunt, then make it walk out
            float guard = Time.time + 30f;
            while (rex.IsChasing && Time.time < guard) yield return null;

            rex.Redeploy(new Vector3(-14f, 0f, 31f), 180f);
            if (Escaped) yield break;

            if (ambience != null) ambience.PlayRoar();
            if (director != null)
            {
                director.Glitch(0.7f, true);
                director.Broadcast("⚠ PADDOCK BREACH — LARGE SIGNATURE ON THE ROAD", 4.6f);
            }

            // failsafe: if the wilderness won't let it walk there, it is
            // simply *found* at its new post (with a tape glitch for flavor)
            guard = Time.time + 60f;
            while (!rex.AtPost && Time.time < guard)
            {
                if (rex.IsChasing) { yield return null; continue; }
                yield return null;
            }
            if (!rex.AtPost && !rex.IsChasing)
            {
                rex.ResetToHome();
                if (director != null) director.Glitch(0.55f, false);
            }
        }
        /// <summary>Cell 3: everything still alive starts closing in faster.</summary>
        void Stampede()
        {
            for (int i = 0; i < DinoHunter.Pack.Count; i++)
            {
                var h = DinoHunter.Pack[i];
                if (h == null || h.kind != DinoHunter.Kind.Raptor) continue;
                h.runSpeed += 0.35f;         // now 5.35 — still beatable, barely
                h.detectRange *= 1.15f;
                h.loseRange *= 1.1f;
            }
            StartCoroutine(LateBroadcast(
                "⚠ ALL SIGNATURES MOBILE — DO NOT STOP MOVING", 9.5f));
        }

        IEnumerator LateBroadcast(string text, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (Escaped) yield break;
            Broadcast(text, 4.6f);
        }

        // --------------------------------------------------------------- gate

        void UpdateGate()
        {
            Vector3 a = player.position;
            a.y = gatePoint.y;
            if (Vector3.Distance(a, gatePoint) > gateRadius) return;

            if (Collected >= requiredCells)
            {
                // the last blockade: the T-Rex fights you for the gate
                // (unless it was already driven off — then just escape)
                if (boss == null)
                {
                    boss = gameObject.AddComponent<BossFight>();
                    boss.Begin(this);
                }
                if (boss.Active && !boss.Won)
                {
                    boss.RequestFight();
                    return;
                }
                Escape();
                return;
            }

            if (Time.time > nextGateHint)
            {
                nextGateHint = Time.time + 9f;
                Broadcast("GATE CONTROLS DEAD — POWER CELLS  " +
                          Collected + "/" + requiredCells, 3.6f);
            }
        }

        void Escape()
        {
            if (Escaped) return;
            Escaped = true;
            endCardAt = -1f;        // the ending cutscene decides when the card lands

            if (controller != null) controller.SetFrozen(true);
            if (flashlight != null) flashlight.ForceOff();
            if (director != null)
            {
                director.Glitch(0.95f, true);
                director.Shake(0.3f, 1.2f);
                director.SetTension(0f);
            }
            if (ambience != null)
            {
                ambience.PlaySting();
                ambience.PlayRoar();
            }

            for (int i = 0; i < DinoHunter.Pack.Count; i++)
                if (DinoHunter.Pack[i] != null)
                    DinoHunter.Pack[i].Freeze();

            // ---- the movie: directed shots + credits, then the end card ----
            cutscene = gameObject.AddComponent<EndingCutscene>();
            cutscene.Begin(this);
        }

        EndingCutscene cutscene;
        BossFight boss;

        /// <summary>The credits finished — land the END OF TAPE card now.</summary>
        public void NotifyCutsceneDone()
        {
            endCardAt = Time.time;
        }

        public bool CutsceneRunning => cutscene != null && cutscene.Running;

        public bool ShowEndCard => Escaped && endCardAt > 0f && Time.time >= endCardAt;

        void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
