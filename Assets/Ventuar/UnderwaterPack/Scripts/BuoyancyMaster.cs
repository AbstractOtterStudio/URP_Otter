using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace LowPolyUnderwaterPack
{
    /// <summary>
    /// Low Poly Underwater Pack script that handles the computation of accurate water detection for child objects with the Buoyancy component.
    /// </summary>
    public class BuoyancyMaster : MonoBehaviour
    {
        [Tooltip("Enables use of accurate water detection in a specific range of the player. Can be heavy on performance at times.")]
        public bool useAccurateDetection = true;

        [Tooltip("Distance at which buoyant props will switch from approximate to exact water detection.")]
        public float accurateBuoyancyDist = 100;
        [Tooltip("Toggle to visualize objects which are using accurate water detection. Gizmos will only appear during runtime.")]
        public bool visualizeAccurateDetectionObjs = true;

        #region Private Fields

        private Buoyancy[] buoyantObjs;
        private List<WaterMesh> waters = new List<WaterMesh>();
        private Dictionary<WaterMesh, List<Vector3>> validFloatPointsDict = new Dictionary<WaterMesh, List<Vector3>>();
        private Dictionary<WaterMesh, Vector3[]> waterTargetPointsDict = new Dictionary<WaterMesh, Vector3[]>();
        // private List<Vector3>[] validFloatPoints;
        private Dictionary<WaterMesh, int> waterPointInterationOffset = new Dictionary<WaterMesh, int>();
        private Vector3[] currentWaterPoints;

        private Transform player;

        private bool validFloatPointsInRangeExist = false;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            buoyantObjs = FindObjectsOfType<Buoyancy>();
            player = GameObject.FindGameObjectWithTag("MainCamera").transform;
        }

        private void Start() 
        {
            if (!useAccurateDetection)
                return;

            // Get all waters in-use by buoyant objects if using accurate detection
            for (int i = 0; i < buoyantObjs.Length; i++)
            {
                WaterMesh currWater = buoyantObjs[i].water;

                if (!waters.Contains(currWater))
                {
                    waters.Add(currWater);
                }
            }

            for (int i = 0; i < waters.Count; i++)
            {
                validFloatPointsDict.Add(waters[i], new List<Vector3>());
                waterTargetPointsDict.Add(waters[i], new Vector3[0]);
                waterPointInterationOffset.Add(waters[i], 0);
            }
        }

        private void Update()
        {
            bool validPointsFound = FindValidFloatPoints();
            if (!validPointsFound)
                return;

            ApplyWaterPoints();
        }

        #endregion

        /// <summary>
        /// Finds all valid float points for each buoyant object in the scene and turns them into corresponding points on the water mesh using WaterMesh's GetWaterPoints.
        /// Results are stored in the waterTargetPointsDict dictionary.
        /// </summary>
        /// <returns>True if successful, false if no valid float points were found</returns>
        private bool FindValidFloatPoints() {
            // If not using accurate detection, there is no water object, and there are no buoyant objects in the scene, don't do anything
            if (!useAccurateDetection || waters.Count == 0 || buoyantObjs.Length == 0) 
                return false;

            // Clear each valid float point list
            foreach (List<Vector3> points in validFloatPointsDict.Values)
            {
                points.Clear();
            }

            // Assign each in-range float point to validFloatPoints respective to their water object
            for (int i = 0; i < buoyantObjs.Length; i++)
            {
                Buoyancy buoyantObj = buoyantObjs[i];
                WaterMesh waterObj = buoyantObj.water;
                // float distSqr = (buoyantObj.transform.position - player.position).sqrMagnitude;
                // bool inRange = distSqr < accurateBuoyancyDist * accurateBuoyancyDist && buoyantObj.water != null;

                float dist = Vector3.Distance(buoyantObj.transform.position, player.position);
                bool inRange = dist < accurateBuoyancyDist && buoyantObj.water != null;

                buoyantObjs[i].inPlayerRange = inRange;

                // Add floating points of this object if it's inside the player range threshold
                if (inRange && waterObj != null)
                {
                    if (validFloatPointsDict.TryGetValue(waterObj, out List<Vector3> validFloatPoints))
                    {
                        for (int j = 0; j < buoyantObj.buoyancyPoints.Count; j++)
                        {
                            validFloatPoints.Add(buoyantObj.transform.TransformPoint(buoyantObj.buoyancyPoints[j]));
                        }
                    }
                }
            }           
            
            // Get Vector3 arrays for all calculated water points relative to their respective water objects
            // First check if there are any float points that actually are in range
            validFloatPointsInRangeExist = false;
            for (int i = 0; i < waters.Count; i++)
            {
                WaterMesh water = waters[i];
                List<Vector3> points = validFloatPointsDict[water];

                // Call GetWaterPoints() to retrieve accurate water points for points in the validFloatPoints array
                if (points.Count > 0) {
                    waterTargetPointsDict[water] = water.GetWaterPoints(points.ToArray());
                    validFloatPointsInRangeExist = true;
                }
            }

            return validFloatPointsInRangeExist;

        }

        /// <summary>
        /// Applies the calculated water points to each buoyant object in the scene.
        /// </summary>
        private void ApplyWaterPoints() {
            // Array of the number of water points that have been dealt corresponding to the water object they are under. Each index corresponds to a water object in "waters"
            // Clearing the array before working with it any further
            foreach (WaterMesh water in waterPointInterationOffset.Keys.ToList())
            {
                waterPointInterationOffset[water] = 0;
            }

            // Loop through each buoyant object and set the respective water point for each of their floating points
            for (int i = 0; i < buoyantObjs.Length; i++)
            {
                // If the object isn't in the player range, continue
                if (!buoyantObjs[i].inPlayerRange)
                    continue;

                // The list of water points to assign back to each individual buoyant object
                // Clearing it before working with it any further
                currentWaterPoints = new Vector3[buoyantObjs[i].buoyancyPoints.Count];

                // Add the correct points to assign back to each buoyant object 
                if (waterTargetPointsDict.TryGetValue(buoyantObjs[i].water, out Vector3[] waterPoints))
                {
                    WaterMesh water = buoyantObjs[i].water;
                    for (int j = 0; j < buoyantObjs[i].buoyancyPoints.Count; j++)
                    {
                        currentWaterPoints[j] = waterPoints[waterPointInterationOffset[water] + j];
                    }

                    // Update the count offset value
                    waterPointInterationOffset[water] += buoyantObjs[i].buoyancyPoints.Count;
                }

                // Set the water points for the buoyant object
                buoyantObjs[i].SetWaterPoints(currentWaterPoints);
            }
        }
    }

    /// <summary>
    /// Low Poly Underwater Pack custom editor which creates a custom inspector for BuoyancyMaster to organize properties and improve user experience.
    /// </summary>
#if UNITY_EDITOR
    [CustomEditor(typeof(BuoyancyMaster), true), CanEditMultipleObjects, System.Serializable]
    public class BuoyancyMaster_Editor : Editor
    {
        SerializedProperty useAccurateDetection, accurateBuoyancyDist, visualizeAccurateDetectionObjs;

        private bool buoyancyFoldout = true;

        private void OnEnable()
        {
            #region Seriealized Property Initialization

            useAccurateDetection = serializedObject.FindProperty("useAccurateDetection");
            accurateBuoyancyDist = serializedObject.FindProperty("accurateBuoyancyDist");
            visualizeAccurateDetectionObjs = serializedObject.FindProperty("visualizeAccurateDetectionObjs");

            #endregion
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Grayed out script property
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script:", MonoScript.FromMonoBehaviour((BuoyancyMaster)target), typeof(BuoyancyMaster), false);
            GUI.enabled = true;

            #region Buoyancy Settings

            buoyancyFoldout = GUIHelper.Foldout(buoyancyFoldout, "Buoyancy Settings");

            if (buoyancyFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(useAccurateDetection);

                if (useAccurateDetection.boolValue)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(accurateBuoyancyDist);
                    EditorGUILayout.PropertyField(visualizeAccurateDetectionObjs);
                
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            #endregion

            // Draw the rest of the inspector excluding everything specifically drawn here
            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }
    }
#endif
}