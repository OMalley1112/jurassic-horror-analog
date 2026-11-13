using UnityEngine;
using AnalogHorror;

namespace AnalogHorror.EditorTools
{
    public static partial class MapBuilder
    {
        // ------------------------------------------------------------ random

        static float rf(float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        static int rint(int min, int maxExclusive)
        {
            return rng.Next(min, maxExclusive);
        }

        static bool chance(float probability)
        {
            return rng.NextDouble() < probability;
        }

        // ---------------------------------------------------------- primitives

        static GameObject Prim(PrimitiveType type, Transform parent, string name,
            Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = mat;
            return go;
        }

        static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size,
            Material mat, Vector3 euler = default)
        {
            return Prim(PrimitiveType.Cube, parent, name, pos, size, mat, euler);
        }

        static GameObject Cyl(Transform parent, string name, Vector3 pos, float diameter,
            float height, Material mat, Vector3 euler = default)
        {
            // cylinder primitive: 1 diameter, 2 tall
            return Prim(PrimitiveType.Cylinder, parent, name, pos,
                new Vector3(diameter, height * 0.5f, diameter), mat, euler);
        }

        static GameObject Ball(Transform parent, string name, Vector3 pos, Vector3 scale,
            Material mat, Vector3 euler = default)
        {
            return Prim(PrimitiveType.Sphere, parent, name, pos, scale, mat, euler);
        }

        static GameObject Capsule(Transform parent, string name, Vector3 pos, float diameter,
            float height, Material mat, Vector3 euler = default)
        {
            // capsule primitive: 1 diameter, 2 tall (includes caps)
            return Prim(PrimitiveType.Capsule, parent, name, pos,
                new Vector3(diameter, height * 0.5f, diameter), mat, euler);
        }

        /// <summary>Flat quad used for roads, puddles, decals (facing up).</summary>
        static GameObject FlatQuad(Transform parent, string name, Vector3 pos, Vector2 size,
            Material mat, float yEuler = 0f)
        {
            return Prim(PrimitiveType.Quad, parent, name, pos,
                new Vector3(size.x, size.y, 1f), mat, new Vector3(-90f, yEuler, 0f));
        }

        static void StripCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col == null) return;
            if (Application.isPlaying) Object.Destroy(col);
            else Object.DestroyImmediate(col);
        }

        /// <summary>Large ground plane section sized in world units.</summary>
        static void GroundRect(string name, float x0, float x1, float z0, float z1, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane); // 10x10 units
            go.name = name;
            go.transform.SetParent(envRoot, false);
            go.transform.localPosition = new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f);
            go.transform.localScale = new Vector3((x1 - x0) / 10f, 1f, (z1 - z0) / 10f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // ---------------------------------------------------------------- text

        static Font defaultFont;

        static Font DefaultFont()
        {
            if (defaultFont != null) return defaultFont;
            try { defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (defaultFont == null)
            {
                try { defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            }
            if (defaultFont == null)
                defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
            return defaultFont;
        }

        /// <summary>
        /// Some Unity versions generate text facing -Z, others +Z.
        /// Probe the actual generated mesh once so signs always read correctly.
        /// </summary>
        static bool? textFrontIsPositiveZ;

        static bool TextFrontIsPositiveZ()
        {
            if (textFrontIsPositiveZ.HasValue) return textFrontIsPositiveZ.Value;

            var go = new GameObject("__TextProbe");
            var tm = go.AddComponent<TextMesh>();
            tm.font = DefaultFont();
            tm.text = "X";
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && tm.font != null) renderer.sharedMaterial = tm.font.material;

            bool frontPositive = true;
            var mf = go.GetComponent<MeshFilter>();
            var mesh = mf != null ? mf.sharedMesh : null;
            if (mesh != null && mesh.triangles.Length >= 3)
            {
                var verts = mesh.vertices;
                var tris = mesh.triangles;
                Vector3 a = verts[tris[0]];
                Vector3 b = verts[tris[1]];
                Vector3 c = verts[tris[2]];
                Vector3 normal = Vector3.Cross(b - a, c - a);
                frontPositive = normal.z >= 0f;
            }
            else
            {
                Debug.LogWarning("Text probe failed, assuming text faces -Z.");
                frontPositive = false;
            }

            Object.DestroyImmediate(go);
            textFrontIsPositiveZ = frontPositive;
            return frontPositive;
        }

        /// <summary>Standalone 3D text. euler describes the desired "readable from" orientation.</summary>
        static TextMesh Label(string text, Transform parent, string name, Vector3 pos,
            Vector3 euler, float size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            Quaternion q = Quaternion.Euler(euler);
            // rotate so the readable side points where the plain euler would put -Z? No:
            // we want the text plane oriented like `euler` but with readable side facing -Z of it.
            if (TextFrontIsPositiveZ())
                q = q * Quaternion.Euler(0f, 180f, 0f);
            go.transform.localRotation = q;

            var tm = go.AddComponent<TextMesh>();
            tm.font = DefaultFont();
            tm.text = text;
            tm.characterSize = size;
            tm.fontSize = 64;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.color = color;

            var r = go.GetComponent<MeshRenderer>();
            if (r != null && tm.font != null) r.sharedMaterial = tm.font.material;
            return tm;
        }

        /// <summary>
        /// Sign: opaque board with text on its readable face.
        /// `readDir` = direction the viewer looks (world) — sign faces back at them.
        /// </summary>
        static void Sign(string text, Transform parent, string name, Vector3 pos,
            float boardWidth, float boardHeight, Material boardMat, Color textColor,
            Vector3 readDirEulerYXZ, float textSize = 0.16f, float tiltZ = 0f)
        {
            Vector3 euler = new Vector3(readDirEulerYXZ.x, readDirEulerYXZ.y, tiltZ);
            Quaternion rot = Quaternion.Euler(euler);

            // Board behind the text (text sits in front of the board's readable face).
            Box(parent, name + "_Board", pos, new Vector3(boardWidth, boardHeight, 0.1f),
                boardMat, euler);

            // readable side of a rotated text plane is local -Z (probe-compensated in Label)
            Vector3 textOffset = rot * new Vector3(0f, 0f, -0.07f);
            Label(text, parent, name + "_Text", pos + textOffset, euler, textSize, textColor);
        }
    }
}
