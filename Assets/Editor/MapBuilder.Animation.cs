using UnityEngine;
using AnalogHorror;

namespace AnalogHorror.EditorTools
{
    public static partial class MapBuilder
    {
        // ================================================================ PROP ANIMATION
        // Runs last, after the world exists: anything that should sway,
        // twitch or breathe gets its runtime component wired here.
        // (Dinosaur joint rigs are wired inside MapBuilder.Dinosaurs.cs,
        //  and the paddock gate attaches its own Swing where it is built.)

        static void BuildPropAnimation()
        {
            // 1 — lobby "WAY OUT" sign, rocking on its two ceiling chains
            SwingGroup(vcRoot, "SignExit_Swing", new Vector3(0f, 6f, 8f),
                new[] { "HangChain_L", "HangChain_R", "Sign_Exit_Board", "Sign_Exit_Text" },
                3.2f, 0.42f, new Vector3(1f, 0f, 0.3f), rf(0f, 6.28f));

            // 2 — "PARK CLOSED" on the gate arch: hang real chains first,
            //     then let the whole assembly sway over the road
            Cyl(propsRoot, "ParkChain_L", new Vector3(-3.4f, 5.7f, 22f),
                0.05f, 1.4f, M("MetalDark"));
            Cyl(propsRoot, "ParkChain_R", new Vector3(3.4f, 5.7f, 22f),
                0.05f, 1.4f, M("MetalDark"));
            SwingGroup(propsRoot, "ParkClosed_Swing", new Vector3(0f, 6.4f, 22f),
                new[] { "ParkChain_L", "ParkChain_R",
                        "Sign_ParkClosed_Board", "Sign_ParkClosed_Text" },
                2.6f, 0.3f, new Vector3(1f, 0f, -0.25f), rf(0f, 6.28f));

            // 3 — roof vines twitch around the edges of the roof holes
            AnimateVines();

            // 4 — the moon shaft breathes instead of sitting at a flat 2.2
            var moon = vcRoot.Find("MoonShaft");
            if (moon != null)
            {
                var pulse = moon.gameObject.AddComponent<LightPulse>();
                pulse.amount = 0.42f;
                pulse.speed = 0.09f;        // one slow breath every ~11 s
                pulse.flutterAmount = 0.05f;
            }
        }

        /// <summary>
        /// Every "Vine" in the visitor center roof gets a pivot at the hole
        /// edge it hangs from, so each sways on its own phase.
        /// </summary>
        static void AnimateVines()
        {
            var vines = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < vcRoot.childCount; i++)
            {
                var c = vcRoot.GetChild(i);
                if (c.name == "Vine") vines.Add(c);
            }

            for (int i = 0; i < vines.Count; i++)
            {
                var v = vines[i];

                // pivot at the top of the box (the roof edge it grips)
                Vector3 top = v.localPosition +
                    vcRoot.InverseTransformDirection(v.transform.up)
                    * (v.localScale.y * 0.5f);

                var pivot = Pivot(vcRoot, "Vine_Swing_" + i, top);
                v.SetParent(pivot, true);

                var sw = pivot.gameObject.AddComponent<Swing>();
                sw.amplitude = rf(1.6f, 4.2f);
                sw.speed = rf(0.14f, 0.3f);
                sw.axis = (i % 2 == 0)
                    ? new Vector3(1f, 0f, rf(-0.5f, 0.5f))
                    : new Vector3(rf(-0.5f, 0.5f), 0f, 1f);
                sw.phase = rf(0f, 6.28f);
            }
        }

        /// <summary>
        /// Puts named direct children of `parent` under a fresh pivot and
        /// gives that pivot a Swing component (children keep their world pose).
        /// </summary>
        static void SwingGroup(Transform parent, string pivotName, Vector3 pivotLocal,
            string[] childNames, float amplitude, float speed, Vector3 axis, float phase)
        {
            var pivot = Pivot(parent, pivotName, pivotLocal);

            foreach (var n in childNames)
            {
                var c = parent.Find(n);
                if (c != null) c.SetParent(pivot, true);
            }

            var sw = pivot.gameObject.AddComponent<Swing>();
            sw.amplitude = amplitude;
            sw.speed = speed;
            sw.axis = axis;
            sw.phase = phase;
        }
    }
}
