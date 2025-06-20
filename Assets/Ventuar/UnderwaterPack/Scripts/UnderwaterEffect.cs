using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace LowPolyUnderwaterPack
{
    /// <summary>
    /// Low Poly Underwater Pack script that applies camera, lighting, and scene effects when the camera is underwater.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class UnderwaterEffect : MonoBehaviour
    {
        [Tooltip("Position offset for detecting whether the camera is above or under the water.")]
        [SerializeField] private Vector3 waterDetectionOffset = Vector3.zero;

        #region Effect Settings

        [Tooltip("Post processing profile for when the camera is underwater.")]
        [SerializeField] private VolumeProfile underwaterProfile = null;
        [Tooltip("Post processing profile for when the camera is above water.")]
        [SerializeField] private VolumeProfile surfaceProfile = null;
        [Tooltip("The color of the underwater fog.")]
        [SerializeField] private Color fogColor = Color.blue;
        [Tooltip("Toggle whether to change the tint color of the skybox to the color of the fog when underwater to better blend into the environment.")]
        [SerializeField] private bool modifySkyboxTint = false;

        [Tooltip("Water refraction renderer feature, used to create a refraction image effect.")]
        public ScriptableRendererFeature refractionRendererFeature;

        #endregion

        #region Hidden Fields

        [HideInInspector] public bool isUnderwater = false;
        [HideInInspector] public Vector3[] waterVerts;

        #endregion

        #region Private Fields

        private Material skybox;
        private Color skyColor;

        private WaterMesh water;
        private Volume vol;
        private Camera cam;

        private string tintPropName;
        private Vector3 waterMeshPoint = Vector3.zero;
        private RaycastHit[] overHits, underHits;
        private int overHitsLength, underHitsLength;
        private Vector3 offsetPos;

        #region Boids Test Scene Variables
        
        private GameObject boidsRoom; 
        private bool inTestScene;

        #endregion

        #endregion

        #region Unity Callbacks

        private void Awake() 
        {
            vol = GetComponent<Volume>();
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            // Ensure camera generates a depth texture
            cam.depthTextureMode = DepthTextureMode.Depth;

            overHits = new RaycastHit[32];
            underHits = new RaycastHit[32];

            if (modifySkyboxTint) {
                skybox = RenderSettings.skybox;

                // Skybox tint is usually "Sky Tint", but can sometimes just be "Tint"
                tintPropName =  RenderSettings.skybox.HasProperty("_SkyTint") ? "_SkyTint" : 
                                RenderSettings.skybox.HasProperty("_Tint") ? "_Tint" : 
                                null;

                skyColor = skybox.GetColor(tintPropName);
            }
            
            // Only find the boids test room if in the boids test scene
            if (SceneManager.GetActiveScene().name == "Boids Test") {
                inTestScene = true;
                boidsRoom = GameObject.Find("Boid Test Room");
            }
        }

        private void OnValidate()
        {
            if (cam == null)
                cam = GetComponent<Camera>();

            // Change underwater fog and background color in real time
            RenderSettings.fogColor = fogColor;
            cam.backgroundColor = fogColor;
            
            if (refractionRendererFeature != null)
                refractionRendererFeature.SetActive(false);
        }

        private void LateUpdate()
        {
            // Special logic if we are in the boids test room
            if (inTestScene)
            {
                // If in the boids test scene but no boids room exists, do nothing and return
                if (boidsRoom == null)
                    return;

                // Camera is underwater if it is inside the walls of the test room
                bool insideBoidsRoom = boidsRoom.GetComponent<MeshCollider>().bounds.Contains(transform.position);
                ApplyUnderwaterEffects(insideBoidsRoom);

                return;
            }

            // Assign the current water reference to "water" variable. Will assign null if no water above or under the camera. 
            water = FindWater(); 

            // Water detection position can be offsetted for a optimally smooth transition between above and below water
            offsetPos = transform.position + waterDetectionOffset;
            if (water != null)
                waterMeshPoint = water.GetWaterPoint(offsetPos);

            // If the point on the water is above our position, we are underwater and should be applying underwater effects
            ApplyUnderwaterEffects((water != null && waterMeshPoint != Vector3.zero) ? 
                waterMeshPoint.y > offsetPos.y : 
                false);
        }

        private void OnDrawGizmos() {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(waterMeshPoint, 0.25f);
        }

        private void OnApplicationQuit()
        {
            // Reset underwater effects when quitting or going back into edit mode
            ApplyUnderwaterEffects(false);
        }

        #endregion

        private void ApplyUnderwaterEffects(bool underwater)
        {
            // Underwater effects
            isUnderwater = underwater;
            RenderSettings.fog = underwater;
            RenderSettings.skybox.SetColor(tintPropName, (!underwater && modifySkyboxTint) ? skyColor : fogColor);
            vol.profile = underwater ? underwaterProfile : surfaceProfile;

            if (refractionRendererFeature != null)
                refractionRendererFeature.SetActive(underwater);
        }

        private WaterMesh FindWater() {
            // Determining if object is over/under water or not and getting a reference to the water if it is
            overHitsLength = Physics.RaycastNonAlloc(transform.position + (Vector3.up * 10), -Vector3.up, overHits, float.MaxValue);
            underHitsLength = Physics.RaycastNonAlloc(transform.position - (Vector3.up * 10), Vector3.up, underHits, float.MaxValue);

            WaterMesh currentWater = null;

            int maxLength = Mathf.Max(overHitsLength, underHitsLength);
            for (int i = 0; i < maxLength; i++)
            {
                // Check if over water
                if (i < overHitsLength && overHits[i].transform.TryGetComponent(out currentWater))
                {
                    return currentWater;
                }

                // Check if under water
                if (i < underHitsLength && underHits[i].transform.TryGetComponent(out currentWater))
                {
                    return currentWater;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Low Poly Underwater Pack custom editor which creates a custom inspector for UnderwaterEffect to organize properties and improve user experience.
    /// </summary>
#if UNITY_EDITOR
    [CustomEditor(typeof(UnderwaterEffect), true), CanEditMultipleObjects, System.Serializable]
    public class UnderwaterEffect_Editor : Editor
    {
        private SerializedProperty waterDetectionOffset, surfaceProfile, underwaterProfile, fogColor, modifySkyboxTint, refractionRendererFeature;

        private bool effectFoldout = true;

        private void OnEnable()
        {
            #region Seriealized Property Initialization

            waterDetectionOffset = serializedObject.FindProperty("waterDetectionOffset");
            
            surfaceProfile = serializedObject.FindProperty("surfaceProfile");
            underwaterProfile = serializedObject.FindProperty("underwaterProfile");
            fogColor = serializedObject.FindProperty("fogColor");
            modifySkyboxTint = serializedObject.FindProperty("modifySkyboxTint");

            refractionRendererFeature = serializedObject.FindProperty("refractionRendererFeature");

            #endregion
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Grayed out script property
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script:", MonoScript.FromMonoBehaviour((UnderwaterEffect)target), typeof(UnderwaterEffect), false);
            GUI.enabled = true;

            EditorGUILayout.PropertyField(waterDetectionOffset);

            EditorGUILayout.Space(10);

            #region Effect Settings

            effectFoldout = GUIHelper.Foldout(effectFoldout, "Effect Settings");

            if (effectFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(surfaceProfile);
                EditorGUILayout.PropertyField(underwaterProfile);
                EditorGUILayout.PropertyField(fogColor);
                EditorGUILayout.PropertyField(modifySkyboxTint);

                EditorGUILayout.Space(10);

                EditorGUILayout.PropertyField(refractionRendererFeature);

                EditorGUI.indentLevel--;
            }

            #endregion

            serializedObject.ApplyModifiedProperties();
            
            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }
    }
#endif
}