using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Simple first-person controller: mouse look, walk/run, jump,
    /// head bob and procedural footsteps.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 3.0f;
        public float runSpeed = 5.4f;
        public float gravity = -18f;
        public float jumpHeight = 1.05f;

        [Header("Look")]
        public float mouseSensitivity = 2.2f;
        public float pitchLimit = 85f;

        [Header("Head bob")]
        public float bobFrequency = 1.6f;
        public float bobHeight = 0.05f;
        public float bobWidth = 0.035f;

        [Header("Stamina / crouch")]
        public KeyCode crouchKey = KeyCode.LeftControl;
        public float crouchSpeed = 1.7f;
        public float crouchCamDrop = 0.58f;   // fraction of eye height while crouched
        public float staminaMax = 100f;
        public float sprintDrain = 21f;        // per second
        public float staminaRegen = 14f;       // per second, after the delay
        public float regenDelay = 1.1f;
        public float staminaFloor = 26f;       // must recover past this to sprint again

        [Header("Feel")]
        public float runFovBoost = 6f;         // FOV kick while sprinting
        public float fovLerp = 7f;

        [Header("Assigned by builder")]
        public Transform camHolder;
        public PlayerBodyAnimator body;

        CharacterController cc;
        float pitch;
        float yVelocity;
        Vector3 camBasePos;

        float bobPhase;
        float lastBobSin;
        AudioSource footstepSrc;
        AudioClip[] footstepClips;
        Camera cam;
        float baseFov;

        float stamina = 100f;
        float lastSprint = -999f;
        bool exhausted;
        float stanceT;              // 0 = standing, 1 = crouched (smoothed)

        /// <summary>Held down right now.</summary>
        public bool IsCrouched { get; private set; }
        /// <summary>Actually moving at run speed this frame.</summary>
        public bool IsSprinting { get; private set; }
        /// <summary>0..1 — drives the HUD stamina bar.</summary>
        public float Stamina01 => staminaMax > 0f ? stamina / staminaMax : 1f;

        [Header("Camera shake (runtime)")]
        public float shakeAmount;          // current applied amplitude
        float shakeTime;
        float shakeDuration;
        float shakeSeed;
        Vector3 shakeOffset;
        Vector3 shakeEuler;
        bool frozen;          // external: menu, catch, dialogue, cutscenes
        bool pauseFrozen;     // the pause menu's own freeze (separate flag!)

        /// <summary>Freeze look + movement (tape rewind, cutscenes).</summary>
        public void SetFrozen(bool value)
        {
            frozen = value;
            if (value) IsSprinting = false;
        }

        /// <summary>
        /// The pause menu's freeze. Kept separate so unpausing can never
        /// unfreeze a player that a cutscene, the catch sequence or an NPC
        /// dialogue is still holding.
        /// </summary>
        public void SetPauseFrozen(bool value)
        {
            pauseFrozen = value;
            if (value) IsSprinting = false;
        }

        public bool IsFrozen => frozen || pauseFrozen;

        /// <summary>True when something other than the pause menu holds the player.</summary>
        public bool IsHeldFrozen => frozen;

        /// <summary>
        /// Hard-aim the view at a world point (death cam: the last frame
        /// on the tape is whatever ate you). Works while frozen.
        /// </summary>
        public void SnapLookAt(Vector3 worldPoint)
        {
            Vector3 flat = worldPoint - transform.position;
            float planar = new Vector2(flat.x, flat.z).magnitude;
            if (planar < 0.001f) return;

            float yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            float pitchTo = Mathf.Atan2(flat.y, planar) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(-pitchTo, -pitchLimit, pitchLimit);

            if (camHolder != null)
                camHolder.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        /// <summary>Kick the camera. Amplitude in local metres, duration in seconds.</summary>
        public void Shake(float amplitude, float duration)
        {
            if (duration <= 0f || amplitude <= 0f) return;
            shakeAmount = Mathf.Max(shakeAmount, amplitude);
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeTime = Mathf.Max(shakeTime, duration);
            if (shakeSeed == 0f) shakeSeed = Random.Range(0f, 100f);
        }

        public void ResetShake()
        {
            shakeTime = 0f;
            shakeAmount = 0f;
            shakeOffset = Vector3.zero;
            shakeEuler = Vector3.zero;
        }

        void Start()
        {
            cc = GetComponent<CharacterController>();
            stamina = staminaMax;

            if (camHolder == null) camHolder = transform.Find("PlayerCamera");
            if (camHolder != null) camBasePos = camHolder.localPosition;
            cam = camHolder != null ? camHolder.GetComponent<Camera>() : null;
            if (cam != null) baseFov = cam.fieldOfView;

            footstepClips = BakeFootsteps();

            footstepSrc = gameObject.AddComponent<AudioSource>();
            footstepSrc.playOnAwake = false;
            footstepSrc.spatialBlend = 0.65f;
            footstepSrc.dopplerLevel = 0f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            GameSettings.Load();
            ApplyLiveSettings();

            HandleCursor();
            if (!IsFrozen)
            {
                HandleLook();
                HandleMove();
            }
            HandleStance();
            HandleShake();
            HandleBob();
            DriveBody();
        }

        /// <summary>Menu SETTINGS edits GameSettings; push them into the
        /// controller/camera/audio every frame so changes apply live.</summary>
        void ApplyLiveSettings()
        {
            mouseSensitivity = GameSettings.Sensitivity;
            if (cam != null) baseFov = GameSettings.Fov;
            AudioListener.volume = GameSettings.Volume;
        }

        /// <summary>Keeps the visible body in sync with movement — runs even
        /// when frozen so crouch/idle poses stay correct.</summary>
        void DriveBody()
        {
            if (body == null) return;
            Vector3 flat = cc != null ? cc.velocity : Vector3.zero;
            flat.y = 0f;
            body.SetLocomotion(flat.magnitude / runSpeed,
                cc != null && cc.isGrounded, stanceT, IsSprinting);
        }

        /// <summary>Smooth sit/stand for the camera — legs are out of frame.</summary>
        void HandleStance()
        {
            float target = IsCrouched ? 1f : 0f;
            stanceT = Mathf.MoveTowards(stanceT, target, 4.5f * Time.deltaTime);

            // sprint FOV kick (and a slight tighten when crouched)
            if (cam != null)
            {
                float fov = baseFov;
                if (IsSprinting && !IsCrouched) fov += runFovBoost;
                if (IsCrouched) fov -= 4f;
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov,
                    fovLerp * Time.deltaTime);
            }
        }

        Vector3 CamAnchor()
        {
            float standY = camBasePos.y;
            return new Vector3(camBasePos.x,
                Mathf.Lerp(standY, standY * crouchCamDrop, stanceT), camBasePos.z);
        }

        /// <summary>Noise-driven shake, applied after look so it never fights it.</summary>
        void HandleShake()
        {
            if (camHolder == null) return;

            if (shakeTime > 0f)
            {
                shakeTime -= Time.deltaTime;
                if (shakeTime <= 0f) ResetShake();
            }

            float k = shakeDuration > 0f
                ? Mathf.Clamp01(shakeTime / shakeDuration) : 0f;
            float a = shakeAmount * k * k;                 // eased falloff

            if (a > 0.0005f && shakeSeed != 0f)
            {
                float t = Time.time * 34f;
                float nx = Mathf.PerlinNoise(shakeSeed, t) * 2f - 1f;
                float ny = Mathf.PerlinNoise(shakeSeed + 13.7f, t) * 2f - 1f;
                float nz = Mathf.PerlinNoise(shakeSeed + 41.3f, t) * 2f - 1f;

                shakeOffset = new Vector3(nx, ny, 0f) * a;
                shakeEuler = new Vector3(ny * 1.6f, nx * 1.4f, nz * 2.2f) * (a * 62f);
            }
            else
            {
                shakeOffset = Vector3.zero;
                shakeEuler = Vector3.zero;
            }

            camHolder.localRotation =
                Quaternion.Euler(pitch, 0f, 0f) * Quaternion.Euler(shakeEuler);
        }

        void HandleCursor()
        {
            // the main menu owns the cursor until the run begins
            if (MainMenu.Instance != null && !MainMenu.Instance.Started) return;

            // the pause system owns the cursor while the tape is stopped
            if (PauseSystem.Instance != null && PauseSystem.Instance.IsPaused) return;

            if (Input.GetKeyDown(KeyCode.Escape) && PauseSystem.Instance == null)
            {
                // fallback only: no pause system in this scene
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void HandleLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
            float my = Input.GetAxis("Mouse Y") * mouseSensitivity
                     * (GameSettings.InvertY ? -1f : 1f);

            transform.Rotate(Vector3.up * mx);
            pitch = Mathf.Clamp(pitch - my, -pitchLimit, pitchLimit);

            if (camHolder != null)
                camHolder.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void HandleMove()
        {
            bool grounded = cc.isGrounded;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 dir = transform.right * h + transform.forward * v;
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            // --- crouch: silent, slow, hard to see ---
            IsCrouched = Input.GetKey(crouchKey);

            // --- stamina: sprint until the tank cracks, then recover ---
            bool wantsRun = Input.GetKey(KeyCode.LeftShift)
                            && dir.sqrMagnitude > 0.01f
                            && !IsCrouched;

            if (wantsRun && !exhausted)
            {
                IsSprinting = true;
                lastSprint = Time.time;
                stamina -= sprintDrain * Time.deltaTime;
                if (stamina <= 0f)
                {
                    stamina = 0f;
                    exhausted = true;         // gasping — must recover past the floor
                }
            }
            else
            {
                IsSprinting = false;
                if (Time.time - lastSprint > regenDelay)
                    stamina = Mathf.Min(staminaMax, stamina + staminaRegen * Time.deltaTime);
                if (exhausted && stamina >= staminaFloor) exhausted = false;
            }

            float speed = IsSprinting ? runSpeed
                : (IsCrouched ? crouchSpeed : walkSpeed);

            if (grounded)
            {
                if (yVelocity < 0f) yVelocity = -2f;
                if (Input.GetKeyDown(KeyCode.Space) && !IsCrouched)
                    yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            yVelocity += gravity * Time.deltaTime;

            Vector3 velocity = dir * speed;
            velocity.y = yVelocity;
            cc.Move(velocity * Time.deltaTime);
        }

        void HandleBob()
        {
            if (camHolder == null) return;

            Vector3 vel = cc.velocity;
            vel.y = 0f;
            float speed = vel.magnitude;

            if (speed > 0.35f && cc.isGrounded)
            {
                float amp = Mathf.InverseLerp(0.6f, runSpeed, speed);
                bobPhase += speed * Time.deltaTime * bobFrequency;

                Vector3 offset = new Vector3(
                    Mathf.Cos(bobPhase * 0.5f) * bobWidth,
                    Mathf.Sin(bobPhase) * bobHeight,
                    0f) * amp * GameSettings.HeadBob;

                camHolder.localPosition = CamAnchor() + offset + shakeOffset;

                // footstep on each down-beat
                float s = Mathf.Sin(bobPhase);
                if (s < 0f && lastBobSin >= 0f)
                    PlayFootstep(amp);
                lastBobSin = s;
            }
            else
            {
                camHolder.localPosition = Vector3.Lerp(
                    camHolder.localPosition, CamAnchor() + shakeOffset, 10f * Time.deltaTime);
                lastBobSin = 1f;
            }
        }

        static AudioClip[] BakeFootsteps()
        {
            return new[]
            {
                ProceduralAudio.NoiseBurst(0.16f, 26f, 11, 0.55f),
                ProceduralAudio.NoiseBurst(0.15f, 30f, 22, 0.6f),
                ProceduralAudio.NoiseBurst(0.17f, 24f, 33, 0.5f)
            };
        }

        void PlayFootstep(float amp)
        {
            if (footstepSrc == null) return;

            // self-heal: a script recompile during play reloads the domain and
            // drops references to runtime-made AudioClips — re-bake instead of
            // walking in silence until the next restart
            if (footstepClips == null || footstepClips.Length == 0 ||
                footstepClips[0] == null || footstepClips[1] == null ||
                footstepClips[2] == null)
                footstepClips = BakeFootsteps();

            float volume = Mathf.Lerp(0.12f, 0.4f, amp);
            if (IsCrouched) volume *= 0.5f;       // soft heel steps in the ferns
            footstepSrc.pitch = Random.Range(0.9f, 1.15f);
            var clip = footstepClips[Random.Range(0, footstepClips.Length)];
            if (clip != null) footstepSrc.PlayOneShot(clip, volume);
        }
    }
}
