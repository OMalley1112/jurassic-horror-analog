using System.Collections;
using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// A loose power cell: glows, bobs, and lets out a hard click when it is
    /// yanked out of its socket. ObjectiveSystem handles pickup + the alarm;
    /// this is just the object itself.
    /// </summary>
    public class FuseCell : MonoBehaviour
    {
        [Header("Where it was lost (broadcasts / HUD)")]
        public string slotName = "LOBBY";

        public bool Taken { get; private set; }

        AudioSource src;
        Vector3 basePos;
        float seed;

        void Start()
        {
            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 2f;
            src.maxDistance = 26f;

            basePos = transform.position;
            seed = Random.Range(0f, 100f);
        }

        void Update()
        {
            if (Taken) return;

            // dead-tech hover: slow bob + lazy roll
            transform.position = basePos +
                Vector3.up * (Mathf.Sin(Time.time * 1.4f + seed) * 0.07f);
            transform.rotation = Quaternion.Euler(0f, Time.time * 22f + seed * 40f, 0f);
        }

        /// <summary>Yank it out — click, flare, gone.</summary>
        public void Take()
        {
            if (Taken) return;
            Taken = true;

            if (src != null)
                src.PlayOneShot(ProceduralAudio.Click(Random.Range(1, 9999)), 0.9f);

            StartCoroutine(FlareAndDie());
        }

        IEnumerator FlareAndDie()
        {
            var lights = GetComponentsInChildren<Light>();
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                foreach (var l in lights)
                    l.intensity = Mathf.Lerp(l.intensity, 6f, 14f * Time.deltaTime);
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
