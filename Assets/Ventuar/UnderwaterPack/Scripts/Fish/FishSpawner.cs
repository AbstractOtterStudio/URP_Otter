using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LowPolyUnderwaterPack
{
    /// <summary>
    /// Low Poly Underwater Pack script that spawns a number of fish in a radius around the object.
    /// </summary>
    [ExecuteAlways]
    public class FishSpawner : MonoBehaviour
    {
        [Tooltip("Toggles whether the spawner has a bounding box in which the boids spawned will be confined to. Bounding box comes in the form of a box collider component which will be created on toggle.")]
        [SerializeField] private bool hasBoundingBox = false;

        #region Spawn Settings

        [Tooltip("The fish prefabs to be spawned. Selecting multiple fish types will spawn random amounts of each fish up to the spawn number. Will not spawn unless it has a FlockingBoid or SingleBoid component.")]
        [SerializeField] private GameObject[] fishPrefabs = null;

        [Tooltip("Radius in which the fish will be spawned around the spawner. Visualized by a red wire sphere.")]
        [SerializeField] private float spawnRadius = 20;
        [Tooltip("The number of fish that will be spawned.")]
        [SerializeField] private int spawnNumber = 40;
        [Tooltip("The randomness of the scale of the fish. 0.1 will make the fish scale between 0.9 and 1.1. 0.5 will make the fish scale between 0.5 and 1.5. 0 will make the fish all the same scale.")]
        [Range(0,1), SerializeField] private float scaleRandomness = 0.1f;
        

        #endregion

        #region Private Fields

        private BoidMaster boidMaster;

        private Vector3 boundsSize = Vector3.one * 50;
        private List<GameObject> validPrefabs;
        private GameObject prefab, boid;

        private bool boundsBoolCheck = true;

        #endregion

        #region Unity Callbacks

        private void Awake() 
        {
            if (Application.isPlaying && boidMaster == null)
                boidMaster = FindObjectOfType<BoidMaster>();
        }

        private void OnEnable() 
        {
            if (!Application.isPlaying) return;
            
            validPrefabs = new List<GameObject>();
            boidMaster = FindObjectOfType<BoidMaster>();

            // Only allow prefabs with a FlockingBoid or SingleBoid component to spawn
            for (int i = 0; i < fishPrefabs.Length; i++)
            {
                if (fishPrefabs[i].GetComponent<SingleFishAI>())
                {
                    validPrefabs.Add(fishPrefabs[i]);
                }
                else
                {
                    Debug.LogWarning("Prefab " + fishPrefabs[i].name + "in FishSpawner object " + transform.name + " does not contain a FlockingBoid or SingleBoid component. No objects of that type will be spawned.");
                }
            }

            // Instantiate each boid at a random point in the spawn radius of the spawner
            for (int i = 0; i < spawnNumber; i++)
            {
                // Choose a random fish type from a list of possible spawnable fish
                prefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
                boid = Instantiate(prefab, transform.position + Random.insideUnitSphere * spawnRadius, Random.rotation, transform);

                // Send relevent data to the instantiated boid
                boid.name = prefab.name;
                boid.transform.localScale *= Random.Range(1 - scaleRandomness, 1 + scaleRandomness);

                if (boid.TryGetComponent<FlockingBoidAI>(out var boidAI))
                {
                    // Component is FlockingBoidAI
                    boidAI.noBoidMasterInScene = boidMaster == null;
                    boidAI.bm = boidMaster;
                }

                if (hasBoundingBox)
                    boid.GetComponent<SingleFishAI>().boundsCollider = GetComponent<BoxCollider>();
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Application.isPlaying || boundsBoolCheck == hasBoundingBox)
                return;
            
            // Bounding box checks. If the bool value is toggled, ensure there is a box collider component and the layer is set to "Fish" so only fish from this specific spawner can detect it
            if (hasBoundingBox)
            {
                if (GetComponent<BoxCollider>() == null)
                {
                    BoxCollider col = (BoxCollider)gameObject.AddComponent(typeof(BoxCollider));
                    col.size = boundsSize;
                    col.isTrigger = true;
                }

                gameObject.layer = LayerMask.NameToLayer("Fish");
                boundsBoolCheck = hasBoundingBox;
            }
            else
            {
                if (GetComponent<BoxCollider>() != null)
                {
                    boundsSize = GetComponent<BoxCollider>().size;
                    UnityEditor.Undo.DestroyObjectImmediate(GetComponent<BoxCollider>());
                }

                gameObject.layer = LayerMask.NameToLayer("Default");
                boundsBoolCheck = hasBoundingBox;
            }
        }

        private void OnDrawGizmos()
        {
            // Visualize spawn radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
#endif

        #endregion
    }

    /// <summary>
    /// Low Poly Underwater Pack custom editor which creates a custom inspector for FishSpawner to organize properties and improve user experience.
    /// </summary>
#if UNITY_EDITOR
    [CustomEditor(typeof(FishSpawner), true), CanEditMultipleObjects, System.Serializable]
    public class FishSpawner_Editor : Editor
    {
        private SerializedProperty hasBoundingBox, fishPrefabs, spawnRadius, spawnNumber, scaleRandomness;

        private bool spawnFoldout = true;

        private void OnEnable()
        {
            #region Seriealized Property Initialization

            hasBoundingBox = serializedObject.FindProperty("hasBoundingBox");
            fishPrefabs = serializedObject.FindProperty("fishPrefabs");
            spawnRadius = serializedObject.FindProperty("spawnRadius");
            spawnNumber = serializedObject.FindProperty("spawnNumber");
            scaleRandomness = serializedObject.FindProperty("scaleRandomness");

            #endregion
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Grayed out script property
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script:", MonoScript.FromMonoBehaviour((FishSpawner)target), typeof(FishSpawner), false);
            GUI.enabled = true;

            EditorGUILayout.PropertyField(hasBoundingBox, true);
            
            GUILayout.Space(10);

            #region Spawn Settings

            spawnFoldout = GUIHelper.Foldout(spawnFoldout, "Spawn Settings");

            if (spawnFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(fishPrefabs, true);

                GUILayout.Space(10);

                EditorGUILayout.PropertyField(spawnRadius, true);
                EditorGUILayout.PropertyField(spawnNumber, true);
                EditorGUILayout.PropertyField(scaleRandomness, true);

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
