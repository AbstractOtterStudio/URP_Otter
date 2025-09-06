using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public static class CameraShaker
    {
        static ShakerImpl impl;
        public static void Shake(float amplitude, float duration)
        {
            if (!impl)
            {
                GameObject go = new("CameraShaker");
                impl = go.AddComponent<ShakerImpl>();
            }
            impl.Begin(amplitude, duration);
        }
        class ShakerImpl : MonoBehaviour
        {
            float amp, dur; Vector3 originalPos; Camera cam;
            void Awake() { cam = Camera.main; originalPos = cam.transform.position; }
            public void Begin(float a, float d) { amp = a; dur = d; originalPos = cam.transform.position; }
            void LateUpdate()
            {
                if (dur > 0)
                {
                    dur -= Time.deltaTime;
                    cam.transform.position = originalPos + Random.insideUnitSphere * amp;
                }
                else if (cam.transform.position != originalPos) cam.transform.position = originalPos;
            }
        }
    }
}
