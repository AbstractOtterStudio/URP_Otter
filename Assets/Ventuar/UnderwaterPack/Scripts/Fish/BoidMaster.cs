using System;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Burst;

namespace LowPolyUnderwaterPack
{
    /// <summary>
    /// Low Poly Underwater Pack script that handles velocity calculation and some raycasting for boids by using Unity's C# Job System and the Burst compiler.
    /// </summary>
    public class BoidMaster : MonoBehaviour
    {
        [Tooltip("Toggles whether boids use Unity's C# Jobs System or not. Likely much worse performance when toggled off.")]
        public bool runWithJobs = true;
        [Tooltip("Distance at which all boids will be culled.")]
        public float boidDisableDistance = 150;

// Script only used if the burst compiler is installed

        #region Private Fields

        private FlockingBoidAI[] boids, enabledBoids;
        private MeshRenderer[] boidRends = null;
        private LayerMask fishMask;

        #region Jobs Data

        private TransformAccessArray transformAccessArray;

        private NativeArray<Vector3> resultingVelocities;
        private NativeArray<Vector3> boidPositions;
        private NativeArray<Vector3> boidVelocities;
        private NativeArray<Vector3> forwards;
        private NativeArray<Vector2> minMaxBoidGroup;
        private NativeArray<Vector2> detectionBoundsRadius;
        private NativeArray<Vector3> cohAliRepForces;
        private NativeArray<int> hashValues;

        private NativeArray<RaycastHit> raycastResults;
        private NativeArray<RaycastCommand> raycastCommands;

        #endregion

        #endregion

        #region Unity Callbacks

        private void Awake() 
        {
            if (!runWithJobs) return;
            
            boids = FindObjectsOfType<FlockingBoidAI>();
            enabledBoids = new FlockingBoidAI[boids.Length];

            boidRends = new MeshRenderer[boids.Length];
            for (int i = 0; i < boids.Length; i++)
            {
                boidRends[i] = boids[i].gameObject.GetComponentInChildren<MeshRenderer>();
            }
        }

        private void Start()
        {          
            // Only initialize data for jobs if we're choosing to use it
            if (!runWithJobs) return;
            
            transformAccessArray = new TransformAccessArray(boids.Length);

            // Add all boid transforms to the transformAccessArray
            for (int i = 0; i < boids.Length; i++)
            {
                transformAccessArray.Add(boids[i].transform);
            }

            fishMask = ~(1 << LayerMask.NameToLayer("Fish"));
        }

        private void Update()
        {
            if (!runWithJobs) return;
            
            int enabledIndex = 0;
            InitializeJobsData();

            // Loop through all boids and copy information to respective native arrays
            for (int i = 0; i < boids.Length; i++)
            {
                if (!boidRends[i].enabled)
                {
                    boids[i].velocity = Vector3.zero;
                    boids[i].headingForCollision = false;
                    continue;
                }
                
                enabledBoids[enabledIndex] = boids[i];
                boidPositions[enabledIndex] = boids[i].transform.position;
                boidVelocities[enabledIndex] = boids[i].velocity;
                forwards[enabledIndex] = boids[i].transform.forward;
                minMaxBoidGroup[enabledIndex] = boids[i].minMaxBoidGroup;
                detectionBoundsRadius[enabledIndex] = new Vector2(boids[i].detectionRadius, boids[i].boundsRadius);
                cohAliRepForces[enabledIndex] = new Vector3(boids[i].cohesionForce, boids[i].alignmentForce, boids[i].repulsionForce);
                hashValues[enabledIndex] = boids[i].nameHash;

                raycastCommands[enabledIndex] = new RaycastCommand(boidPositions[enabledIndex], boidVelocities[enabledIndex].normalized, detectionBoundsRadius[enabledIndex].x, fishMask);

                enabledIndex++;
            }

            // Schedule and complete batch for raycasting
            JobHandle rayHandle = RaycastCommand.ScheduleBatch(raycastCommands, raycastResults, 64, default);

            // Prepare the velocity update job
            VelocityUpdateJob velocityUpdateJob = new()
            {
                resultingVelocities = resultingVelocities,
                boidPositions = boidPositions,
                boidVelocities = boidVelocities,
                forward = forwards,
                minMaxBoidGroup = minMaxBoidGroup,
                detectionBoundsRadius = detectionBoundsRadius,
                cohAliRepForces = cohAliRepForces,
                hashValues = hashValues,
            };

            // Schedule and complete batch for velocity updating
            JobHandle jobHandle = velocityUpdateJob.Schedule(enabledIndex, 64);
            
            JobHandle.CombineDependencies(rayHandle, jobHandle).Complete();

            // Loop through all boids and apply information back from jobs
            for (int i = 0; i < enabledBoids.Length; i++)
            {
                if (enabledBoids[i] == null) return;    // End of array
                
                bool headingForCollision = raycastResults[i].transform != null;
                
                enabledBoids[i].velocity += resultingVelocities[i];
                enabledBoids[i].headingForCollision = headingForCollision;

                if (headingForCollision)
                    enabledBoids[i].avoidancePoint = raycastResults[i].point;
            }

            DisposeJobsData();
        }

        private void InitializeJobsData()
        {
            // Initialize native arrays for velocity calculation
            resultingVelocities = new NativeArray<Vector3>(boids.Length, Allocator.TempJob);
            boidPositions = new NativeArray<Vector3>(boids.Length, Allocator.TempJob);
            boidVelocities = new NativeArray<Vector3>(boids.Length, Allocator.TempJob);
            forwards = new NativeArray<Vector3>(boids.Length, Allocator.TempJob);
            minMaxBoidGroup = new NativeArray<Vector2>(boids.Length, Allocator.TempJob);
            detectionBoundsRadius = new NativeArray<Vector2>(boids.Length, Allocator.TempJob);
            cohAliRepForces = new NativeArray<Vector3>(boids.Length, Allocator.TempJob);
            hashValues = new NativeArray<int>(boids.Length, Allocator.TempJob);

            // Native arrays for raycasting
            raycastResults = new NativeArray<RaycastHit>(boids.Length, Allocator.TempJob);
            raycastCommands = new NativeArray<RaycastCommand>(boids.Length, Allocator.TempJob);
        }

        private void DisposeJobsData() {
            resultingVelocities.Dispose();
            boidPositions.Dispose();
            boidVelocities.Dispose();
            forwards.Dispose();
            minMaxBoidGroup.Dispose();
            detectionBoundsRadius.Dispose();
            cohAliRepForces.Dispose();
            hashValues.Dispose();

            // Dispose of raycasting native arrays
            raycastResults.Dispose();
            raycastCommands.Dispose();
        }

        private void OnDestroy()
        {
            // Dispose of velocity calcuation native arrays if using jobs
            if (runWithJobs)
            {
                transformAccessArray.Dispose();
                try {
                    DisposeJobsData();
                } catch { /* NativeArrays already disposed */ }
            }
        }

        #endregion

        /// <summary>
        /// Uses C# Jobs and the Burst Compiler to handle boids AI for FlockingBoidAI script.
        /// </summary>
        [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
        private struct VelocityUpdateJob : IJobParallelFor
        {
            [WriteOnly, NoAlias] public NativeArray<Vector3> resultingVelocities;

            [ReadOnly, NoAlias] public NativeArray<Vector3> boidPositions;
            [ReadOnly, NoAlias] public NativeArray<Vector3> boidVelocities;
            [ReadOnly, NoAlias] public NativeArray<Vector3> forward;
            [ReadOnly, NoAlias] public NativeArray<Vector2> minMaxBoidGroup;
            [ReadOnly, NoAlias] public NativeArray<Vector2> detectionBoundsRadius;  // detectionRadius and boundsRadius respectively
            [ReadOnly, NoAlias] public NativeArray<Vector3> cohAliRepForces;        // cohesionForce, alignmentForce, and repulsionForce respectively
            [ReadOnly, NoAlias] public NativeArray<int> hashValues;

            Vector3 cohesionAverage, repulsionAverage, alignmentAverage, posAverage;
            Vector3 cohesion, alignment, repulsion;

            public void Execute(int current)
            {
                cohesionAverage = Vector3.zero;
                repulsionAverage = Vector3.zero;
                alignmentAverage = Vector3.zero;
                posAverage = Vector3.zero;

                // Loop through boids until all have been looped through or the max detectable boids has been reached
                int count = 0;
                int avoidCount = 0;
                for (int i = 0; i < boidPositions.Length && count <= minMaxBoidGroup[current].y; i++)
                {
                    // If it is the same boid, continue to next iteration
                    if (boidPositions[current] == boidPositions[i] && boidVelocities[current] == boidPositions[i])
                         continue;
                    
                    Vector3 offset = boidPositions[i] - boidPositions[current];
                    float sqrDst = offset.x * offset.x + offset.y * offset.y + offset.z * offset.z;

                    // If fish is inside detection radius and fish types are the same (via hash value comparison)
                    if (sqrDst < detectionBoundsRadius[current].x * detectionBoundsRadius[current].x && hashValues[current] == hashValues[i])
                    {
                        // Add respective values to forces
                        cohesionAverage += offset;
                        alignmentAverage += boidVelocities[i];
                        posAverage += boidPositions[i];

                        // If the boid is inside bounds radius, add to repulsion force
                        if (sqrDst < detectionBoundsRadius[current].y * detectionBoundsRadius[current].y)
                        {
                            avoidCount++;
                            repulsionAverage += offset;
                        }

                        count++;
                    }
                }

                // Average out all forces
                cohesionAverage /= count;
                alignmentAverage /= count;
                repulsionAverage /= avoidCount;
                posAverage /= count;

                // Calculate force values by lerping from 0 to the average depending on distance/closeness to average or other boids
                cohesion = Vector3.Lerp(Vector3.zero, cohesionAverage.normalized, (posAverage - boidPositions[current]).sqrMagnitude / (detectionBoundsRadius[current].x * detectionBoundsRadius[current].x)) * cohAliRepForces[current].x;
                alignment = Vector3.Lerp(Vector3.zero, alignmentAverage.normalized, Mathf.Abs(1 - Vector3.Dot(boidVelocities[current].normalized, alignmentAverage.normalized)) / 2) * cohAliRepForces[current].y;
                repulsion = Vector3.Lerp(Vector3.zero, repulsionAverage.normalized, 1 - (repulsionAverage.sqrMagnitude / (detectionBoundsRadius[current].y * detectionBoundsRadius[current].y))) * cohAliRepForces[current].z;

                // If not a valid number, return 0 for repulsion
                if (System.Single.IsNaN(repulsion.sqrMagnitude))
                    repulsion = Vector3.zero;

                Vector3 v = cohesion + alignment - repulsion;
                
                // If there are no flocking forces, go forward
                if (v == Vector3.zero)
                    v = forward[current];

                resultingVelocities[current] = v;
            }
        }
    }

    /// <summary>
    /// Low Poly Underwater Pack custom editor which creates a custom inspector for BoidMaster to organize properties and improve user experience.
    /// </summary>
#if UNITY_EDITOR
    [CustomEditor(typeof(BoidMaster), true), CanEditMultipleObjects, System.Serializable]
    public class BoidMaster_Editor : Editor
    {
        private SerializedProperty runWithJobs, boidDisableDistance;

        private bool boidFoldout = true;

        private void OnEnable()
        {
            #region Seriealized Property Initialization

            runWithJobs = serializedObject.FindProperty("runWithJobs");
            boidDisableDistance = serializedObject.FindProperty("boidDisableDistance");

            #endregion
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Grayed out script property
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script:", MonoScript.FromMonoBehaviour((BoidMaster)target), typeof(BoidMaster), false);
            GUI.enabled = true;

            #region Boid Settings

            boidFoldout = GUIHelper.Foldout(boidFoldout, "Boid Settings");

            if (boidFoldout)
            {
                EditorGUI.indentLevel++;

#if BURST_PRESENT
                EditorGUILayout.PropertyField(runWithJobs);
#endif
                EditorGUILayout.PropertyField(boidDisableDistance);

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

