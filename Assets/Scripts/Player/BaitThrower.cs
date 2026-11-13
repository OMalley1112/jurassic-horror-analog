using UnityEngine;

namespace AnalogHorror
{
    /// <summary>
    /// Player tool: hurl a bone (Q). It clatters where it lands and every
    /// hunter that hears it goes to investigate the noise — including the
    /// T-Rex. The only way to pull a predator off the thing you need.
    /// </summary>
    public class BaitThrower : MonoBehaviour
    {
        [Header("Wired by builder")]
        public Camera cam;
        public FirstPersonController controller;

        [Header("Kit")]
        public int bones = 5;
        public KeyCode throwKey = KeyCode.Q;
        public float throwForce = 13f;
        public float lureRadius = 45f;
        public float pelletLife = 14f;

        public int BonesLeft => bones;

        void Update()
        {
            if (bones <= 0) return;
            if (!Input.GetKeyDown(throwKey)) return;
            if (controller != null && controller.IsFrozen) return;
            if (HorrorDirector.Instance != null && HorrorDirector.Instance.IsCaught) return;
            if (ObjectiveSystem.Instance != null && ObjectiveSystem.Instance.Escaped) return;
            if (PauseSystem.Instance != null && PauseSystem.Instance.IsPaused) return;

            Throw();
        }

        void Throw()
        {
            if (cam == null) return;
            bones--;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "BaitBone";
            go.transform.position = cam.transform.position + cam.transform.forward * 0.6f;
            go.transform.rotation = Random.rotation;
            go.transform.localScale = new Vector3(0.1f, 0.1f, 0.55f);

            var rend = go.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                var mat = Resources.Load<Material>("Materials/Bone");
                if (mat != null) rend.sharedMaterial = mat;
            }

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            var pel = go.AddComponent<BaitPellet>();
            pel.lureRadius = lureRadius;
            pel.lifeTime = pelletLife;

            rb.AddForce(cam.transform.forward * throwForce + Vector3.up * 3.2f,
                ForceMode.VelocityChange);
        }
    }
}
