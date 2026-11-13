using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Underground zone: flips every audio source on the player into a wet
    /// cave reverb while you're below ground, so your own footsteps suddenly
    /// sound like they're happening to someone else. Uses a plain bounds test
    /// so it works with a CharacterController (no trigger/rigidbody needed).
    /// </summary>
    public class TunnelReverb : MonoBehaviour
    {
        [Header("Zone (local to this object)")]
        public Vector3 zoneCenter = Vector3.zero;
        public Vector3 zoneSize = new Vector3(8f, 4f, 40f);

        [Header("Effect")]
        public AudioReverbPreset preset = AudioReverbPreset.Cave;

        AudioSource[] sources;
        AudioReverbFilter[] filters;
        Transform player;
        bool wired;
        bool inside;

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        void Update()
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p == null) return;
                player = p.transform;
            }

            Vector3 local = transform.InverseTransformPoint(player.position);
            bool now = Bounds(local);

            if (now == inside) return;
            inside = now;

            if (now) Wire();
            Apply(now);
        }

        bool Bounds(Vector3 local)
        {
            Vector3 half = zoneSize * 0.5f;
            return Mathf.Abs(local.x - zoneCenter.x) <= half.x
                && Mathf.Abs(local.y - zoneCenter.y) <= half.y
                && Mathf.Abs(local.z - zoneCenter.z) <= half.z;
        }

        void Wire()
        {
            if (wired) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            sources = player.GetComponentsInChildren<AudioSource>(true);
            if (sources == null || sources.Length == 0) return;

            filters = new AudioReverbFilter[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] == null) continue;
                filters[i] = sources[i].gameObject.AddComponent<AudioReverbFilter>();
                filters[i].reverbPreset = preset;
                filters[i].dryLevel = 0f;
                filters[i].enabled = false;
            }
            wired = true;
        }

        void Apply(bool on)
        {
            if (!wired || filters == null) return;
            for (int i = 0; i < filters.Length; i++)
                if (filters[i] != null) filters[i].enabled = on;
        }

        void OnDisable()
        {
            Apply(false);
            inside = false;
        }
    }
}
