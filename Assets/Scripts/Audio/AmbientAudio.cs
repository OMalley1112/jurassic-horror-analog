using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Procedural soundscape: looping low drone, random distant roars,
    /// and scare stings triggered by CreepyFigure.
    /// </summary>
    public class AmbientAudio : MonoBehaviour
    {
        public static AmbientAudio Instance { get; private set; }

        public float firstRoarDelayMin = 18f;
        public float firstRoarDelayMax = 45f;
        public float roarIntervalMin = 28f;
        public float roarIntervalMax = 75f;

        AudioSource loopSrc;
        AudioSource windSrc;
        AudioSource oneShotSrc;
        AudioClip roarClip;
        AudioClip stingClip;
        float nextRoar;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            loopSrc = gameObject.AddComponent<AudioSource>();
            loopSrc.playOnAwake = false;
            loopSrc.loop = true;
            loopSrc.spatialBlend = 0f;
            loopSrc.volume = 0.22f;
            loopSrc.clip = ProceduralAudio.LoopDrone(24f, 1993, 44f);
            loopSrc.Play();

            // jungle wind through the canopy — click-free seamless loop
            windSrc = gameObject.AddComponent<AudioSource>();
            windSrc.playOnAwake = false;
            windSrc.loop = true;
            windSrc.spatialBlend = 0f;
            windSrc.volume = 0.13f;
            windSrc.clip = ProceduralAudio.WindLoop(30f, 777);
            windSrc.Play();

            oneShotSrc = gameObject.AddComponent<AudioSource>();
            oneShotSrc.playOnAwake = false;
            oneShotSrc.spatialBlend = 0f;

            roarClip = ProceduralAudio.Roar(3.2f, 5);
            stingClip = ProceduralAudio.Sting(99);

            nextRoar = Time.time + Random.Range(firstRoarDelayMin, firstRoarDelayMax);
        }

        void Update()
        {
            if (roarClip != null && Time.time > nextRoar)
            {
                PlayRoar();
                nextRoar = Time.time + Random.Range(roarIntervalMin, roarIntervalMax);
            }
        }

        public void PlayRoar()
        {
            if (oneShotSrc == null || roarClip == null) return;
            oneShotSrc.pitch = Random.Range(0.82f, 1.08f);
            oneShotSrc.PlayOneShot(roarClip, Random.Range(0.45f, 0.8f));
        }

        public void PlaySting()
        {
            if (oneShotSrc == null || stingClip == null) return;
            oneShotSrc.pitch = Random.Range(0.95f, 1.1f);
            oneShotSrc.PlayOneShot(stingClip, 0.75f);
        }

        /// <summary>Harsh raptor shriek (non-spatial, tape-wide).</summary>
        public void PlayScreech()
        {
            if (oneShotSrc == null) return;
            oneShotSrc.pitch = Random.Range(0.92f, 1.12f);
            oneShotSrc.PlayOneShot(
                ProceduralAudio.Screech(Random.Range(1, 9999)), 0.7f);
        }

        /// <summary>Long muffled thunder roll.</summary>
        public void PlayThunder()
        {
            if (oneShotSrc == null) return;
            oneShotSrc.pitch = Random.Range(0.85f, 1.08f);
            oneShotSrc.PlayOneShot(
                ProceduralAudio.Thunder(Random.Range(1, 9999)), 0.8f);
        }

        /// <summary>Sibilant whisper over the drone.</summary>
        public void PlayWhisper()
        {
            if (oneShotSrc == null) return;
            oneShotSrc.pitch = Random.Range(0.9f, 1.15f);
            oneShotSrc.PlayOneShot(
                ProceduralAudio.Whisper(Random.Range(1, 9999)), 0.6f);
        }

        /// <summary>Heavy distant stomp / impact.</summary>
        public void PlayStomp()
        {
            if (oneShotSrc == null) return;
            oneShotSrc.pitch = Random.Range(0.8f, 1.05f);
            oneShotSrc.PlayOneShot(
                ProceduralAudio.Stomp(Random.Range(1, 9999)), 0.8f);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
