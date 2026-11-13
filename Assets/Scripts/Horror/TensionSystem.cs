using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Builds dread over time: a heartbeat that accelerates as dinosaurs get
    /// closer / start chasing, plus the pulse itself driving a subtle VHS
    /// wobble and camera micro-shake at high tension.
    /// </summary>
    public class TensionSystem : MonoBehaviour
    {
        [Header("Wired by builder")]
        public HorrorDirector director;
        public Transform player;

        [Header("Tension sources")]
        public float idleDrift = 0.06f;          // ambient creep when nothing is near
        public float proximityWeight = 0.75f;    // nearest dino closeness
        public float chaseWeight = 0.45f;        // extra spike while being hunted
        public float stareWeight = 0.30f;        // something watching you
        public float decayPerSecond = 0.055f;

        [Header("Heartbeat")]
        public float beatSlow = 1.15f;           // seconds between beats (calm)
        public float beatFast = 0.42f;           // seconds between beats (panic)
        public float beatVolumeMin = 0.14f;
        public float beatVolumeMax = 0.72f;

        AudioSource beatSrc;
        AudioClip beatClip;
        float nextBeat;
        float target;
        float noiseSeed;

        DinoHunter[] hunters = new DinoHunter[0];
        float nextScan;

        void Start()
        {
            beatSrc = gameObject.AddComponent<AudioSource>();
            beatSrc.playOnAwake = false;
            beatSrc.spatialBlend = 0f;
            beatSrc.loop = false;
            beatClip = ProceduralAudio.Heartbeat(4);
            noiseSeed = Random.Range(0f, 100f);
            nextBeat = Time.time + 2f;
        }

        void Update()
        {
            if (director == null) return;

            target = ComputeTension();

            // rise fast (panic), fall slow (aftermath)
            float rate = target > director.tension ? 1.9f : decayPerSecond;
            director.SetTension(Mathf.MoveTowards(director.tension, target,
                rate * Time.deltaTime));

            float t = director.displayedTension;
            DriveHeartbeat(t);
            DriveTapeAndShake(t);
        }

        float ComputeTension()
        {
            float t = idleDrift;
            if (player == null) return t;

            DinoHunter nearest = null;
            float nearestDist = float.MaxValue;
            bool anyChasing = false;
            bool beingStaredAt = false;

            // scene scan is expensive — refresh the hunter list on an interval
            if (Time.time >= nextScan)
            {
                hunters = FindObjectsOfType<DinoHunter>();
                nextScan = Time.time + 0.4f;
            }

            var all = hunters;
            for (int i = 0; i < all.Length; i++)
            {
                var h = all[i];
                if (h == null || !h.gameObject.activeInHierarchy) continue;

                float d = Vector3.Distance(player.position, h.transform.position);
                if (d < nearestDist) { nearestDist = d; nearest = h; }
                if (h.IsChasing) anyChasing = true;
                if (h.IsStaring) beingStaredAt = true;
            }

            if (nearest != null && nearest.detectRange > 0f)
            {
                float closeness = 1f - Mathf.Clamp01(nearestDist / (nearest.detectRange * 1.6f));
                t += closeness * proximityWeight;
            }

            if (anyChasing) t += chaseWeight;
            if (beingStaredAt) t += stareWeight;

            return Mathf.Clamp01(t);
        }

        void DriveHeartbeat(float t)
        {
            if (beatSrc == null || beatClip == null) return;

            // heartbeat only matters once something is wrong
            float urgency = Mathf.Clamp01((t - 0.10f) / 0.75f);
            if (urgency <= 0.01f) return;

            if (Time.time >= nextBeat)
            {
                float interval = Mathf.Lerp(beatSlow, beatFast, urgency);
                beatSrc.pitch = Random.Range(0.94f, 1.07f) * Mathf.Lerp(0.92f, 1.14f, urgency);
                beatSrc.PlayOneShot(beatClip,
                    Mathf.Lerp(beatVolumeMin, beatVolumeMax, urgency));
                nextBeat = Time.time + interval;

                // heavy beats nudge the camera — you *feel* it
                if (urgency > 0.55f && HorrorDirector.Instance != null)
                    HorrorDirector.Instance.Shake(0.012f * urgency, 0.16f);
            }
        }

        void DriveTapeAndShake(float t)
        {
            if (director.overlay == null) return;

            // constant low-level tracking wobble while afraid
            float wob = Mathf.Clamp01((t - 0.25f) / 0.75f) * 0.16f;
            if (wob > 0f)
            {
                float n = Mathf.PerlinNoise(Time.time * 1.7f, noiseSeed);
                director.overlay.glitch = Mathf.Max(director.overlay.glitch,
                    wob * n * n);
            }
        }
    }
}
