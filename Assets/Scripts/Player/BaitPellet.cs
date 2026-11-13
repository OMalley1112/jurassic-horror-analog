using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// The thrown bone itself: clatters on impact, taps around for a while,
    /// and keeps calling hunters inside its lure radius to the noise —
    /// not to the player. Durable enough to plan around, short-lived
    /// enough that it never trivializes the hunt.
    /// </summary>
    public class BaitPellet : MonoBehaviour
    {
        public float lureRadius = 45f;
        public float lifeTime = 14f;

        AudioSource src;
        bool landed;
        float dieAt;
        float nextTap;

        void Start()
        {
            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 3f;
            src.maxDistance = 55f;
            src.dopplerLevel = 0f;

            dieAt = Time.time + lifeTime;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (landed) return;
            landed = true;

            // hard clatter on contact
            src.PlayOneShot(
                ProceduralAudio.NoiseBurst(0.35f, 20f, Random.Range(1, 9999), 0.75f), 0.85f);
            Lure();
        }

        void Update()
        {
            if (Time.time > dieAt)
            {
                Destroy(gameObject);
                return;
            }

            if (!landed || Time.time < nextTap) return;

            // small settling taps + a fresh call so wanderers keep coming
            nextTap = Time.time + Random.Range(1.2f, 2.2f);
            src.PlayOneShot(
                ProceduralAudio.NoiseBurst(0.1f, 34f, Random.Range(1, 9999), 0.6f), 0.45f);
            if (Random.value < 0.5f) Lure();
        }

        void Lure()
        {
            for (int i = 0; i < DinoHunter.Pack.Count; i++)
            {
                var h = DinoHunter.Pack[i];
                if (h == null) continue;
                if (Vector3.Distance(h.transform.position, transform.position) <= lureRadius)
                    h.ReceiveAlert(transform.position);
            }
        }
    }
}
