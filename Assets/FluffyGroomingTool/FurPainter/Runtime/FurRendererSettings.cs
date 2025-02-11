using System;
using UnityEngine;

namespace FluffyGroomingTool {
    [Serializable]
    public class FurRendererSettings {
        public bool IsCreateMeshPass { get; set; }

        [SerializeField, Range(0f, 1f)]
        public float sourceMeshNormalToStrandNormalPercent;
 
        [Tooltip("Alpha sorting sorts all the triangles based on distance to the camera assigned in the LOD settings. " +
                 "Keep your polygon count down when using this option, since it can be very heavy performance wise." +
                 " Preferably this should only be used with card based rendering with a transparent material.")]
        public bool isAlphaSortingEnabled;

        public bool isFrustumCullingEnabled;
        public bool isOcclusionCullingEnabled;
        public bool enableLod = true;
        [Tooltip("This will enable LOD in edit mode as well. Make sure you disable it whenever you want to groom some changes.")]
        public bool enableLodInEditMode;
        public WindProperties windProperties = new WindProperties();
        public bool IsFrustumCullingEnabled => !IsCreateMeshPass && isFrustumCullingEnabled;
        public bool IsLodEnabled => !IsCreateMeshPass && enableLod && (Application.isPlaying || enableLodInEditMode);
        public VerletSimulationSettings verletSimulationSettings;
        public bool runPhysicsInFixedUpdate;
        public bool perLayerMotionSettings;
        public bool perLayerNormals;
    }

    [Serializable]
    public class FurLodProperties {
        [SerializeField] public float strandsScale = 1f;
        [SerializeField] public int skipStrandsCount = 1;
        [SerializeField] public float startDistance = 0;
    }

    [Serializable]
    public class HeadersExpanded {
        [SerializeField] public bool isMainExpanded = true;
        [SerializeField] public bool isMovementExpanded = false;
        [SerializeField] public bool isNormalExpanded = false;
        [SerializeField] public bool isStrandShapeExpanded = false;
        [SerializeField] public bool isWindExpanded = false;
        [SerializeField] public bool isCullingExpanded = false;
        [SerializeField] public bool isColliderExpanded = false;
        [SerializeField] public bool isLodExpanded = false;
    }

    public struct ColliderStruct {
        public Vector3 position;
        public Vector3 position2;
        public float radius;

        public static int Size() {
            return
                sizeof(float) * 3 +
                sizeof(float) * 3 +
                sizeof(float) * 1;
        }
    }

    [Serializable]
    public class WindProperties {
        [Range(0, 1f), Tooltip("Frequency of the wind gusts. Higher value will result in a fluttery look.")]
        public float gustFrequency = 0.2f;

        [Range(0, 1f), Tooltip("The strength off the wind")]
        public float windStrength = 0.002f;

        [Tooltip("The 360 degree wind direction in world space.")]
        public float windDirectionDegree = 90f;

        [SerializeField, HideInInspector] public Texture2D windTex;

        public Texture2D getWindTexture() {
            if (windTex == null) windTex = Resources.Load<Texture2D>("WindTexProceduralFur");
            return windTex;
        }
    }
}