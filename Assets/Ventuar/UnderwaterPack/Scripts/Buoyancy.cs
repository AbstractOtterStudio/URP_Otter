using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace LowPolyUnderwaterPack
{
    /// <summary>
    /// Low Poly Underwater Pack script that calculates an object's buoyancy given parameters set in the inspector
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Buoyancy : MonoBehaviour
    {
        #region General Settings

        /**
         *  Fields are handled by the Buoyancy_Editor class
         */

        [Tooltip("Toggle to draw gizmos representing the buoyancy points of the object.")]
        [SerializeField] private bool drawGizmos = true;
        [Tooltip("Size of the gizmos representing the buoyancy points of the object.")]
        [SerializeField] private float gizmoSize = 1;

        [Tooltip("Enable to copy the transform of this object to another object's transform after buoyancy and physics calculations.")]
        [SerializeField] private bool copyToTransform = false;
        [Tooltip("Transform which to copy this object's transform to.")]
        [SerializeField] private Transform copyTransform = null;

        #endregion

        #region Buoyancy Settings

        [Tooltip("Points with which the object's buoyancy will be calculated.")]
        public List<Vector3> buoyancyPoints = new List<Vector3>();

        [Min(0), Tooltip("The depth below the water at which buoyancy points will be considered fully submerged.")]
        [SerializeField] private float depthBeforeSubmerged = 1;
        [Tooltip("How buoyant the object is.")]
        [SerializeField] private float buoyancyAmount = 3;
        [Tooltip("The amount of additional drag the object experiences while submerged.")]
        [SerializeField] private float waterDrag = 1;
        [Tooltip("The amount of additional angular drag the object experiences while submerged.")]
        [SerializeField] private float waterAngularDrag = 0.5f;

        #endregion

        #region Private/Hidden Fields

        // Set by BuoyancyManager
        [HideInInspector] public bool inPlayerRange = false;
        [HideInInspector] public WaterMesh water;

        private Rigidbody rb;
        private BuoyancyMaster bm;
        private Vector3 currentWaterPoint, currentBuoyancyPointPos;
        private Vector3[] waterPoints;
        private bool waterDetected = false;
        private float displacementModifier;

        #endregion

        #region Unity Callbacks

        private void Awake() 
        {
            rb = GetComponent<Rigidbody>();
            bm = FindObjectOfType<BuoyancyMaster>(); 
        }

        private void Start()
        {
            // Determining if object is over/under water or not and getting a reference to the water if it is
            RaycastHit[] overHits = Physics.RaycastAll(transform.position + (Vector3.up * 5), -Vector3.up, float.MaxValue);
            RaycastHit[] underHits = Physics.RaycastAll(transform.position - (Vector3.up * 5), Vector3.up, float.MaxValue);
            RaycastHit[] hits = overHits.Concat(underHits).ToArray();

            // Hits ordered from highest to lowest y position - find the water object detected that is the highest up
            for (int i = hits.Length - 1; i >= 0; i--)
            {
                if (hits[i].transform.TryGetComponent(out WaterMesh waterObj))
                {
                    water = waterObj;
                    waterDetected = true;
                    break;
                }

            }  

            if (!waterDetected)
                Debug.LogWarning("No water object found for " + transform.name + ". Place buoyant objects over a water object with the WaterMesh component to utilize buoyancy.");

            waterPoints = new Vector3[buoyancyPoints.Count];

            // If no buoyancy points exist, add a default one
            if (buoyancyPoints.Count == 0)
            {
                buoyancyPoints.Add(new Vector3(0, 0, 0));
            }
        }

        private void FixedUpdate()
        {
            if (!waterDetected) return;
            
            for (int i = 0; i < buoyancyPoints.Count; i++)
            {
                currentBuoyancyPointPos = transform.TransformPoint(buoyancyPoints[i]);

                if (bm != null && bm.enabled && bm.useAccurateDetection && inPlayerRange)
                {
                    // Accurate water detection if using accurate detection and in range
                    currentWaterPoint = waterPoints[i];
                }
                else
                {
                    // If there is no buoyancy master or it's not enabled, or if not using accurate detection or not in range
                    currentWaterPoint = water.GetWaterPointApprox(currentBuoyancyPointPos);
                }

                ApplyBuoyancy();
            }
            

            // Copy transform to copyTransform object
            if (copyToTransform && copyTransform != null)
            {
                copyTransform.position = transform.position;
                copyTransform.rotation = transform.rotation;
            }
            
        }

        private void OnDrawGizmos()
        {
            bool bmVis = false;
            if (bm != null && bm.useAccurateDetection)
                bmVis = bm.visualizeAccurateDetectionObjs;

            // Draw indicator if in player range
            if (inPlayerRange && bmVis && Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(transform.position, 0.5f);
            }

            if (drawGizmos)
            {
                for (int i = 0; i < buoyancyPoints.Count; i++)
                {
                    // Buoyancy point visualization
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(transform.TransformPoint(buoyancyPoints[i]), gizmoSize);
                }
            }
        }

        #endregion

        /// <summary>
        /// Sets the buoyant object's water points for each of its respective float points.
        /// </summary>
        /// <param name="points">An array of water points with a length eaqual to the amount of float points on the object.</param>
        public void SetWaterPoints(Vector3[] points)
        {
            points.CopyTo(waterPoints, 0);
        }

        public void ApplyBuoyancy() {
            // If under the water surface, apply forces
            if (currentBuoyancyPointPos.y < currentWaterPoint.y)
            {                        
                displacementModifier = Mathf.Clamp01((currentWaterPoint.y - currentBuoyancyPointPos.y) / depthBeforeSubmerged) * buoyancyAmount;

                rb.AddForceAtPosition(Vector3.up * Mathf.Abs(Physics.gravity.y) * displacementModifier, currentBuoyancyPointPos, ForceMode.Acceleration);
                rb.AddForce(displacementModifier * -rb.velocity * waterDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
                rb.AddTorque(displacementModifier * -rb.angularVelocity * waterAngularDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
            }
        }
    }

    /// <summary>
    /// Low Poly Underwater Pack custom editor which creates a custom inspector for Buoyancy to organize properties and improve user experience.
    /// </summary>
#if UNITY_EDITOR
    [CustomEditor(typeof(Buoyancy)), CanEditMultipleObjects, System.Serializable]
    public class Buoyancy_Editor : Editor
    {
        private SerializedProperty drawGizmos, gizmoSize, copyToTransform, copyTransform, buoyancyPoints, depthBeforeSubmerged, buoyancyAmount, waterDrag, waterAngularDrag;

        private bool generalFoldout = true;
        private bool buoyancyFoldout = true;

        private void OnEnable()
        {
            #region Serialized Property Initialization

            drawGizmos = serializedObject.FindProperty("drawGizmos");
            gizmoSize = serializedObject.FindProperty("gizmoSize");
            copyToTransform = serializedObject.FindProperty("copyToTransform");
            copyTransform = serializedObject.FindProperty("copyTransform");

            buoyancyPoints = serializedObject.FindProperty("buoyancyPoints");
            depthBeforeSubmerged = serializedObject.FindProperty("depthBeforeSubmerged");
            buoyancyAmount = serializedObject.FindProperty("buoyancyAmount");
            waterDrag = serializedObject.FindProperty("waterDrag");
            waterAngularDrag = serializedObject.FindProperty("waterAngularDrag");

            #endregion
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Grayed out script property
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script:", MonoScript.FromMonoBehaviour((Buoyancy)target), typeof(Buoyancy), false);
            GUI.enabled = true;

            #region General Settings

            generalFoldout = GUIHelper.Foldout(generalFoldout, "General Settings");

            if (generalFoldout)
            {
                EditorGUI.indentLevel++;

                #region Gizmos

                // Draw custom coral properties toggle
                drawGizmos.boolValue = EditorGUILayout.Toggle(new GUIContent("Draw Gizmos", "Toggles whether gizmos representing the object's buoyancy points will be drawn."), drawGizmos.boolValue);

                EditorGUI.indentLevel++;
                // If custom coral properties has been toggled, draw custom properties
                if (drawGizmos.boolValue)
                    EditorGUILayout.PropertyField(gizmoSize);

                EditorGUI.indentLevel--;

                #endregion

                #region Copy to Transform

                // Draw custom copy to transform toggle
                copyToTransform.boolValue = EditorGUILayout.Toggle(new GUIContent("Copy to Transform", "Toggles whether or not the object will copy its transform to another object after buoyancy calculations."), copyToTransform.boolValue);

                EditorGUI.indentLevel++;
                // If copy to transform has been toggled, draw object field
                if (copyToTransform.boolValue)
                    EditorGUILayout.PropertyField(copyTransform);

                EditorGUI.indentLevel--;

                #endregion

                EditorGUI.indentLevel--;
            }

            #endregion

            #region Buoyancy Settings

            buoyancyFoldout = GUIHelper.Foldout(buoyancyFoldout, "Buoyancy Settings");

            if (buoyancyFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(buoyancyPoints);

                GUILayout.Space(10);

                EditorGUILayout.PropertyField(depthBeforeSubmerged);
                EditorGUILayout.PropertyField(buoyancyAmount);
                EditorGUILayout.PropertyField(waterDrag);
                EditorGUILayout.PropertyField(waterAngularDrag);

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