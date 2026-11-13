using System.Collections.Generic;
using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// A dinosaur that is not scenery: it notices you (proximity, flashlight
    /// sweep, or you staring at it too long), closes in with weighty footsteps,
    /// and if it reaches you the tape is eaten and rewound.
    /// </summary>
    public class DinoHunter : MonoBehaviour
    {
        public enum Kind { TRex, Raptor }

        [Header("Identity")]
        public Kind kind = Kind.Raptor;

        [Header("Wired by builder")]
        public Transform player;
        public Camera playerCamera;

        [Header("Senses")]
        public float detectRange = 26f;
        public float loseRange = 46f;
        public float stareTime = 1.4f;         // seconds of eye contact to provoke
        public float catchRange = 2.1f;
        public float eyeHeight = 3.4f;         // raycast origin height (rex 4.6)
        public float beamRange = 46f;          // how far YOUR flashlight beam reaches

        [Header("Motion")]
        public float walkSpeed = 1.5f;
        public float runSpeed = 4.7f;          // player runs at 5.4 — you *can* escape
        public float turnSpeed = 85f;          // deg/sec
        public float homeRadius = 7f;

        [Header("Audio")]
        public float stepInterval = 0.62f;
        public float roarCooldownMin = 16f;
        public float roarCooldownMax = 44f;

        public bool IsChasing { get; private set; }
        public bool IsStaring { get; private set; }
        public bool IsInvestigating => investigating;

        /// <summary>Current ground speed — drives the walk animation.</summary>
        public float CurrentSpeed { get; private set; }

        /// <summary>Every living hunter, for pack alerts and the alarm.</summary>
        public static readonly List<DinoHunter> Pack = new List<DinoHunter>();

        Transform playerCamT;
        Vector3 home;
        float homeYaw;
        DinoAnimator anim;
        Flashlight flashlight;
        FirstPersonController ctl;
        AudioSource voice;
        AudioSource steps;
        AudioClip[] stompClips;
        AudioClip[] roarClips;
        AudioClip[] screechClips;

        // bake recipes — one source of truth for eager baking in Start()
        // and for on-the-spot repairs in Pick()
        static readonly System.Func<int, AudioClip> bakeStomp =
            i => ProceduralAudio.Stomp(100 + i * 7);
        static readonly System.Func<int, AudioClip> bakeRoar =
            i => ProceduralAudio.Roar(2.8f + i * 0.35f, 300 + i * 11);
        static readonly System.Func<int, AudioClip> bakeScreech =
            i => ProceduralAudio.Screech(400 + i * 13);

        /// <summary>
        /// Random baked clip, self-healing: a script recompile during play
        /// reloads the domain and drops references to runtime-made AudioClips,
        /// which used to leave null holes here (silent roars + PlayOneShot
        /// warnings). Any hole is re-baked on the spot instead.
        /// </summary>
        static AudioClip Pick(ref AudioClip[] clips, int count, System.Func<int, AudioClip> bake)
        {
            if (clips == null || clips.Length != count)
            {
                clips = new AudioClip[count];
                for (int i = 0; i < count; i++) clips[i] = bake(i);
            }
            int idx = Random.Range(0, count);
            if (clips[idx] == null) clips[idx] = bake(idx);
            return clips[idx];
        }
        float loseSightTimer;
        float stareTimer;
        float nextStep;
        float nextRoar;
        Vector3 lastKnown;
        bool returning;
        bool frozen;

        // pack-investigation state (a packmate screeched your position)
        bool investigating;
        Vector3 investigateTarget;
        float investigateUntil;

        void Start()
        {
            home = transform.position;
            homeYaw = transform.eulerAngles.y;
            anim = GetComponent<DinoAnimator>();

            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
            if (player != null && playerCamera == null)
            {
                var cam = player.GetComponentInChildren<Camera>();
                if (cam != null) playerCamera = cam;
            }
            if (playerCamera != null) playerCamT = playerCamera.transform;
            if (playerCamera != null) flashlight = playerCamera.GetComponent<Flashlight>();
            if (player != null) ctl = player.GetComponent<FirstPersonController>();

            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 1f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.minDistance = 4f;
            voice.maxDistance = kind == Kind.TRex ? 120f : 70f;

            steps = gameObject.AddComponent<AudioSource>();
            steps.playOnAwake = false;
            steps.spatialBlend = 1f;
            steps.rolloffMode = AudioRolloffMode.Linear;
            steps.minDistance = 2f;
            steps.maxDistance = 45f;

            // pre-bake the footstep thuds — never allocate clips mid-chase
            stompClips = new AudioClip[5];
            for (int i = 0; i < stompClips.Length; i++)
                stompClips[i] = bakeStomp(i);

            roarClips = new AudioClip[3];
            for (int i = 0; i < roarClips.Length; i++)
                roarClips[i] = bakeRoar(i);

            screechClips = new AudioClip[3];
            for (int i = 0; i < screechClips.Length; i++)
                screechClips[i] = bakeScreech(i);

            nextRoar = Time.time + Random.Range(roarCooldownMin, roarCooldownMax);

            if (HorrorDirector.Instance != null)
                HorrorDirector.Instance.Register(this);
        }

        void Update()
        {
            if (frozen || player == null) return;
            if (MainMenu.Instance != null && !MainMenu.Instance.Started) return;
            if (HorrorDirector.Instance != null && HorrorDirector.Instance.IsCaught)
            {
                // the tape is rewinding — nobody moves
                SetHunting(false);
                return;
            }

            Vector3 toPlayer = player.position - transform.position;
            float dist = toPlayer.magnitude;

            UpdateStare(dist);
            UpdateRoar(dist);

            // detection is stance-sensitive while it's searching, but a chase
            // already in progress runs at full senses — crouching mid-chase
            // does not erase you from the world
            float effDetect = IsChasing ? detectRange : EffectiveDetectRange();
            bool canSee = dist < effDetect && HasLineOfSight();
            bool beam = !IsChasing && BeamReveals(dist);   // your own lamp swept across it
            if (beam) canSee = true;

            if (!IsChasing && canSee && (beam || dist < effDetect * 0.72f))
                BeginChase();

            if (IsChasing)
            {
                loseSightTimer = canSee ? 0f : loseSightTimer + Time.deltaTime;

                if (dist <= catchRange)
                {
                    // it reached you — lunge, then the tape eats itself
                    if (anim != null) anim.Lunge();
                    if (HorrorDirector.Instance != null)
                        HorrorDirector.Instance.CatchPlayer(this);
                    SetHunting(false);
                    return;
                }

                if (dist > loseRange || loseSightTimer > 4.5f)
                {
                    EndChase();
                }
                else
                {
                    ChaseStep(canSee ? player.position : lastKnown, dist);
                }
            }
            else if (investigating)
            {
                InvestigateStep();
            }
            else if (returning)
            {
                ReturnHome();
            }
            else
            {
                IdleTurn();
            }
        }

        // ------------------------------------------------------------- senses

        /// <summary>
        /// How far it can notice you right now: crouching halves it,
        /// switching the flashlight on nearly doubles it. The beam is a
        /// signal flare in the dark.
        /// </summary>
        float EffectiveDetectRange()
        {
            float r = detectRange;
            if (ctl != null && ctl.IsCrouched) r *= 0.5f;
            if (flashlight != null && flashlight.IsOn) r *= 1.35f;
            return r;
        }

        /// <summary>The player is pointing the flashlight straight at it.</summary>
        bool BeamReveals(float dist)
        {
            if (flashlight == null || !flashlight.IsOn || playerCamT == null) return false;
            if (dist > beamRange) return false;

            Vector3 toMe = transform.position + Vector3.up * (eyeHeight * 0.6f)
                           - playerCamT.position;
            if (Vector3.Dot(playerCamT.forward, toMe.normalized) < 0.93f) return false;
            return HasLineOfSight();
        }

        bool HasLineOfSight()
        {
            if (playerCamT == null) return false;

            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 target = playerCamT.position;
            Vector3 delta = target - origin;
            float dist = delta.magnitude;
            if (dist < 0.01f) return true;

            if (Physics.Raycast(origin, delta / dist, out RaycastHit hit, dist + 0.5f))
            {
                if (hit.transform == player || hit.transform.IsChildOf(player))
                    return true;

                // the flashlight beam counts as being seen
                return false;
            }
            return true;
        }

        void UpdateStare(float dist)
        {
            IsStaring = false;
            if (dist > detectRange * 1.4f || playerCamT == null) return;

            Vector3 toMe = transform.position + Vector3.up * (eyeHeight * 0.5f)
                           - playerCamT.position;
            float dot = Vector3.Dot(playerCamT.forward, toMe.normalized);
            if (dot < 0.965f) { stareTimer = 0f; return; }

            stareTimer += Time.deltaTime;
            if (stareTimer >= stareTime)
            {
                IsStaring = true;
                if (!IsChasing) BeginChase(true);
            }
        }

        void BeginChase(bool provoked = false)
        {
            if (IsChasing) return;
            IsChasing = true;
            returning = false;
            investigating = false;
            lastKnown = player.position;
            loseSightTimer = 0f;

            if (anim == null) transform.localRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

            PlayAttackSound();

            // raptors hunt as a pack: the one that finds you tells the others
            if (kind == Kind.Raptor) NotifyPack();

            if (HorrorDirector.Instance != null)
            {
                HorrorDirector.Instance.Glitch(provoked ? 0.55f : 0.35f, false);
                HorrorDirector.Instance.Shake(kind == Kind.TRex ? 0.08f : 0.045f, 0.5f);
                HorrorDirector.Instance.Broadcast(
                    kind == Kind.TRex
                        ? "⚠ MOTION TRACKER — LARGE SIGNATURE APPROACHING"
                        : "⚠ MOTION TRACKER — MULTIPLE SIGNATURES", 2.8f);
            }
        }

        void NotifyPack()
        {
            for (int i = 0; i < Pack.Count; i++)
            {
                var h = Pack[i];
                if (h == null || h == this || h.kind != Kind.Raptor) continue;
                h.ReceiveAlert(player.position);
            }
        }

        /// <summary>
        /// A noise (packmate screech, thrown bone) drew it here: break off
        /// whatever you're doing and sweep toward it — swinging wide to one
        /// flank so the pack closes in an arc instead of a line. The T-Rex
        /// answers bait too; it just doesn't flank.
        /// </summary>
        public void ReceiveAlert(Vector3 position)
        {
            if (frozen) return;
            if (IsChasing || investigating) return;   // committed to what it's doing

            Vector3 flat = position - transform.position;
            flat.y = 0f;
            if (flat.magnitude > 75f) return;

            Vector3 target = position;
            if (flat.sqrMagnitude > 0.04f)
            {
                Vector3 perp = Vector3.Cross(Vector3.up, flat.normalized);
                float side = (Mathf.Abs(GetInstanceID()) & 1) == 0 ? 1f : -1f;
                target += perp * side * Mathf.Min(9f, flat.magnitude * 0.45f);
            }

            investigateTarget = target;
            investigateUntil = Time.time + 15f;
            investigating = true;
            returning = false;

            // answer the call — a short bark from the dark
            if (voice != null && Random.value < 0.55f)
            {
                voice.pitch = 1.18f;
                voice.volume = 0.5f;
                var bark = Pick(ref screechClips, 3, bakeScreech);
                if (bark != null) voice.PlayOneShot(bark);
            }
        }

        void EndChase()
        {
            SetHunting(false);
            returning = true;
        }

        void SetHunting(bool hunting)
        {
            if (IsChasing == hunting) return;
            IsChasing = hunting;
            // the DinoAnimator handles blending back to idle itself
        }

        // -------------------------------------------------------------- motion

        void ChaseStep(Vector3 target, float dist)
        {
            Vector3 flat = target - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0004f) return;

            FaceDirection(flat, turnSpeed * 1.6f);

            float speed = runSpeed;
            speed *= Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(dist / 9f)); // wind up, then sprint

            Vector3 step = transform.forward * (speed * Time.deltaTime);
            AvoidObstacles(ref step);
            transform.position += step;
            transform.position = new Vector3(transform.position.x, home.y,
                transform.position.z);

            PlayFootsteps(speed);
            lastKnown = target;
            CurrentSpeed = speed;
        }

        void ReturnHome()
        {
            Vector3 flat = home - transform.position;
            flat.y = 0f;
            if (flat.magnitude < homeRadius * 0.35f)
            {
                returning = false;
                SetHunting(false);
                CurrentSpeed = 0f;
                return;
            }

            FaceDirection(flat, turnSpeed);
            Vector3 step = transform.forward * (walkSpeed * Time.deltaTime);
            AvoidObstacles(ref step);
            transform.position += step;
            transform.position = new Vector3(transform.position.x, home.y,
                transform.position.z);

            PlayFootsteps(walkSpeed);
            CurrentSpeed = walkSpeed;

            if (flat.magnitude < 0.6f) returning = false;
        }

        void InvestigateStep()
        {
            Vector3 flat = investigateTarget - transform.position;
            flat.y = 0f;

            if (flat.magnitude < 1.6f || Time.time > investigateUntil)
            {
                investigating = false;
                returning = true;              // nothing here — drift home
                CurrentSpeed = 0f;
                return;
            }

            FaceDirection(flat, turnSpeed);

            float speed = walkSpeed * 1.35f;   // trot, not sprint
            Vector3 step = transform.forward * (speed * Time.deltaTime);
            AvoidObstacles(ref step);
            transform.position += step;
            transform.position = new Vector3(transform.position.x, home.y,
                transform.position.z);

            PlayFootsteps(speed);
            CurrentSpeed = speed;
        }

        void IdleTurn()
        {
            // a slow paranoid sweep of its own territory
            float yaw = homeYaw + Mathf.Sin(Time.time * 0.13f + home.x) * 34f;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            CurrentSpeed = 0f;
        }

        void FaceDirection(Vector3 flat, float maxDegreesPerSecond)
        {
            if (flat.sqrMagnitude < 0.0001f) return;
            Quaternion want = Quaternion.LookRotation(flat.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want,
                maxDegreesPerSecond * Time.deltaTime);
        }

        void AvoidObstacles(ref Vector3 step)
        {
            if (step.sqrMagnitude < 0.000001f) return;
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            float len = step.magnitude * 14f + 1.2f;

            if (Physics.Raycast(origin, step.normalized, out RaycastHit hit, len))
            {
                if (hit.transform == player || hit.transform.IsChildOf(player)) return;

                // slide along the blocker instead of stopping dead
                Vector3 slide = Vector3.ProjectOnPlane(step, hit.normal);
                if (slide.sqrMagnitude > 0.0000001f) step = slide * 0.7f;
                else step = Quaternion.Euler(0f, 45f, 0f) * step * 0.5f;
            }
        }

        void PlayFootsteps(float speed)
        {
            if (steps == null || Time.time < nextStep) return;
            nextStep = Time.time + Mathf.Lerp(stepInterval * 1.7f, stepInterval,
                Mathf.Clamp01(speed / runSpeed));

            steps.pitch = Random.Range(0.82f, 1.06f);
            steps.volume = kind == Kind.TRex ? 0.85f : 0.5f;
            var thud = Pick(ref stompClips, 5, bakeStomp);
            if (thud != null) steps.PlayOneShot(thud);

            if (HorrorDirector.Instance != null)
                HorrorDirector.Instance.Shake(kind == Kind.TRex ? 0.03f : 0.014f, 0.22f);
        }

        void UpdateRoar(float dist)
        {
            if (Time.time < nextRoar) return;
            nextRoar = Time.time + Random.Range(roarCooldownMin, roarCooldownMax);

            if (dist < 70f) PlayAttackSound();
        }

        // --------------------------------------------------------------- audio

        public void PlayAttackSound()
        {
            if (voice == null) return;
            voice.pitch = Random.Range(0.9f, 1.1f);

            if (kind == Kind.TRex)
            {
                voice.maxDistance = 130f;
                voice.volume = 0.95f;
                var roar = Pick(ref roarClips, 3, bakeRoar);
                if (roar != null) voice.PlayOneShot(roar);
            }
            else
            {
                voice.maxDistance = 75f;
                voice.volume = 0.8f;
                var screech = Pick(ref screechClips, 3, bakeScreech);
                if (screech != null) voice.PlayOneShot(screech);
            }
        }

        // ------------------------------------------------------------- control

        /// <summary>Send it back to where it was spawned (after a tape rewind).</summary>
        public void ResetToHome()
        {
            SetHunting(false);
            returning = false;
            investigating = false;
            stareTimer = 0f;
            loseSightTimer = 0f;
            CurrentSpeed = 0f;
            frozen = true;
            transform.position = home;
            transform.rotation = Quaternion.Euler(0f, homeYaw, 0f);
            frozen = false;
        }

        /// <summary>Game over — the tape ended. Stop hunting forever.</summary>
        public void Freeze()
        {
            frozen = true;
            SetHunting(false);
            investigating = false;
            returning = false;
            CurrentSpeed = 0f;
        }

        /// <summary>
        /// Boss-fight control: break the freeze, drop everything else and
        /// come straight for the player (used by the gate showdown).
        /// </summary>
        public void ForceHunt()
        {
            frozen = false;
            returning = false;
            investigating = false;
            stareTimer = 0f;
            loseSightTimer = 0f;
            if (player != null) lastKnown = player.position;
            if (!IsChasing) BeginChase(true);
        }

        /// <summary>
        /// Boss-fight lost: it turns and strolls off to a new post — the
        /// walk-away uses the normal return-home gait so it strides, not glides.
        /// </summary>
        public void Retreat(Vector3 newHome, float newYaw)
        {
            frozen = false;
            SetHunting(false);
            investigating = false;
            stareTimer = 0f;
            loseSightTimer = 0f;
            CurrentSpeed = 0f;
            home = newHome;
            homeYaw = newYaw;
            returning = true;
        }

        /// <summary>
        /// Break out / re-anchor: it abandons its post and walks (or, worst
        /// case, is found) at a new home. Used by the escalation pass when
        /// the T-Rex leaves the paddock.
        /// </summary>
        public void Redeploy(Vector3 newHome, float newYaw)
        {
            home = newHome;
            homeYaw = newYaw;
            if (IsChasing) return;          // mid-hunt it relocates afterwards
            investigating = false;
            returning = true;               // walk there at a stroll
        }

        /// <summary>True once it's back within its patrol radius of `home`.</summary>
        public bool AtPost =>
            !IsChasing && !returning && !investigating &&
            Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(home.x, 0f, home.z)) < homeRadius * 0.6f;

        void OnEnable()
        {
            if (!Pack.Contains(this)) Pack.Add(this);
            if (HorrorDirector.Instance != null)
                HorrorDirector.Instance.Register(this);
        }

        void OnDisable()
        {
            Pack.Remove(this);
        }
    }
}
