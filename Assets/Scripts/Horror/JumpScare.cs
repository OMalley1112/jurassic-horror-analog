using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Death jumpscare: a procedurally built dinosaur head lunges at the
    /// lens, jaws snap shut on a white flash, and the tape is torn to
    /// static. Spawned by HorrorDirector the instant a hunter reaches the
    /// player — this is what "dying" looks like on the tape.
    /// </summary>
    public class JumpScare : MonoBehaviour
    {
        static JumpScare active;

        FirstPersonController ctl;
        VhsOverlay overlay;
        Transform root;
        Transform jaw;
        AudioSource src;

        float t;
        bool snapped;
        float flash;
        float baseScale;

        /// <summary>Fire it. Safe to call twice — the old head is replaced.</summary>
        public static void Play(FirstPersonController controller,
            DinoHunter.Kind kind, VhsOverlay vhs)
        {
            if (controller == null || controller.camHolder == null) return;
            if (active != null) Destroy(active.gameObject);

            var go = new GameObject("JUMPSCARE");
            var js = go.AddComponent<JumpScare>();
            js.Build(controller, kind, vhs);
        }

        void Build(FirstPersonController controller, DinoHunter.Kind kind, VhsOverlay vhs)
        {
            ctl = controller;
            overlay = vhs;
            active = this;

            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;

            bool rex = kind == DinoHunter.Kind.TRex;
            baseScale = rex ? 1.45f : 1f;

            root = new GameObject(rex ? "Scare_RexHead" : "Scare_RaptorHead").transform;
            root.SetParent(ctl.camHolder, false);
            // yaw 180 — the snout points back down the view, AT the lens
            root.localPosition = new Vector3(0.12f, -0.07f, 1.9f);
            root.localRotation = Quaternion.Euler(3f, 180f, 0f);
            root.localScale = Vector3.one * (baseScale * 0.5f);

            Material hide = GameMaterials.Get("DinoHide");
            Material dark = GameMaterials.Get("DinoSpine");
            Material bone = GameMaterials.Get("Bone");
            Material eye = GameMaterials.Get("EyeGlow");

            // ---- skull, snout, brow (head space: +Z = toward the camera) ----
            Part(PrimitiveType.Cube, root, "Skull",
                new Vector3(0f, 0.05f, 0f), new Vector3(0.52f, 0.4f, 0.8f), hide, Vector3.zero);
            Part(PrimitiveType.Cube, root, "BrowL",
                new Vector3(-0.17f, 0.17f, 0.2f), new Vector3(0.2f, 0.1f, 0.36f), dark, new Vector3(8f, 0f, 0f));
            Part(PrimitiveType.Cube, root, "BrowR",
                new Vector3(0.17f, 0.17f, 0.2f), new Vector3(0.2f, 0.1f, 0.36f), dark, new Vector3(8f, 0f, 0f));
            Part(PrimitiveType.Cube, root, "Snout",
                new Vector3(0f, -0.01f, 0.62f), new Vector3(rex ? 0.4f : 0.3f, 0.28f, 0.62f), hide, Vector3.zero);
            Part(PrimitiveType.Cube, root, "Nostril",
                new Vector3(0f, 0.07f, 0.9f), new Vector3(0.1f, 0.06f, 0.06f), dark, Vector3.zero);

            // eyes — they catch the flashlight
            Part(PrimitiveType.Sphere, root, "EyeL",
                new Vector3(-0.18f, 0.1f, 0.26f), new Vector3(0.12f, 0.12f, 0.12f), eye, Vector3.zero);
            Part(PrimitiveType.Sphere, root, "EyeR",
                new Vector3(0.18f, 0.1f, 0.26f), new Vector3(0.12f, 0.12f, 0.12f), eye, Vector3.zero);

            // upper teeth hang from the skull
            for (int i = 0; i < 5; i++)
            {
                float z = 0.42f + i * 0.13f;
                float w = rex ? 0.075f : 0.055f;
                Part(PrimitiveType.Cube, root, "ToothU",
                    new Vector3(-0.15f, -0.16f, z), new Vector3(w, 0.16f, w), bone, Vector3.zero);
                Part(PrimitiveType.Cube, root, "ToothU",
                    new Vector3(0.15f, -0.16f, z), new Vector3(w, 0.16f, w), bone, Vector3.zero);
            }

            // ---- jaw on its own pivot: gape → snap → clench chatter ----
            var jawGo = new GameObject("JawPivot");
            jawGo.transform.SetParent(root, false);
            jawGo.transform.localPosition = new Vector3(0f, -0.16f, 0.06f);
            jaw = jawGo.transform;

            Part(PrimitiveType.Cube, jaw, "Jaw",
                new Vector3(0f, -0.05f, 0.34f), new Vector3(rex ? 0.4f : 0.3f, 0.11f, 0.72f), dark, Vector3.zero);
            Part(PrimitiveType.Cube, jaw, "Chin",
                new Vector3(0f, -0.12f, 0.1f), new Vector3(0.28f, 0.1f, 0.3f), hide, Vector3.zero);
            for (int i = 0; i < 4; i++)
            {
                float z = 0.44f + i * 0.15f;
                float w = rex ? 0.07f : 0.05f;
                Part(PrimitiveType.Cube, jaw, "ToothL",
                    new Vector3(-0.13f, 0.05f, z), new Vector3(w, 0.14f, w), bone, Vector3.zero);
                Part(PrimitiveType.Cube, jaw, "ToothL",
                    new Vector3(0.13f, 0.05f, z), new Vector3(w, 0.14f, w), bone, Vector3.zero);
            }
        }

        void Update()
        {
            t += Time.deltaTime;
            if (root == null) return;

            // lunge: far → nose-to-nose, growing as it closes
            float k = Mathf.Clamp01(t / 0.44f);
            float e = 1f - (1f - k) * (1f - k);              // ease-out
            Vector3 lp = root.localPosition;
            lp.z = Mathf.Lerp(1.9f, 0.46f, e);
            root.localPosition = lp;
            root.localScale = Vector3.one * (baseScale * Mathf.Lerp(0.5f, 1.3f, e));
            root.localRotation = Quaternion.Euler(
                3f + Mathf.Sin(t * 41f) * 1.6f,
                180f + Mathf.Sin(t * 33f) * 2.6f,
                Mathf.Sin(t * 27f) * 3f);

            // jaw: hold the gape, snap shut, then chatter
            float a;
            if (t < 0.26f) a = -44f;
            else if (t < 0.34f) a = Mathf.Lerp(-44f, 8f, (t - 0.26f) / 0.08f);
            else a = 8f + Mathf.Sin(t * 46f) * 5f;
            if (jaw != null) jaw.localRotation = Quaternion.Euler(a, 0f, 0f);

            // the snap: flash, shake, glitch, roar
            if (!snapped && t >= 0.30f)
            {
                snapped = true;
                flash = 1f;
                if (ctl != null) ctl.Shake(0.6f, 0.45f);
                if (overlay != null) overlay.TriggerGlitch(1f);
                if (src != null)
                {
                    src.pitch = Random.Range(0.9f, 1.1f);
                    src.PlayOneShot(ProceduralAudio.Roar(1.7f, Random.Range(1, 9999)), 0.95f);
                }
                var amb = AmbientAudio.Instance;
                if (amb != null) amb.PlayScreech();
            }

            if (flash > 0f) flash -= Time.deltaTime * 6.5f;

            if (t > 1.15f)
            {
                active = null;
                Destroy(gameObject);
            }
        }

        void OnGUI()
        {
            if (flash <= 0f) return;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(flash));
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height),
                Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // -----------------------------------------------------------------

        static Transform Part(PrimitiveType type, Transform parent, string name,
            Vector3 pos, Vector3 scale, Material mat, Vector3 euler)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;

            var r = go.GetComponent<MeshRenderer>();
            if (r != null) r.sharedMaterial = mat;

            var c = go.GetComponent<Collider>();      // never block the view
            if (c != null) Destroy(c);
            return go.transform;
        }
    }
}
