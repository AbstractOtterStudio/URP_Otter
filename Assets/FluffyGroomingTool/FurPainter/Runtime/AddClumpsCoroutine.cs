using System;
using System.Collections;
using UnityEngine;

namespace FluffyGroomingTool {
    public class AddClumpsCoroutine {
        public FurCreator furCreator;
        public int layerIndex;
        public int clumpIndex;
        public float distanceBetweenClumps;
        public bool isParentClump;

        private bool isCancelled;

        private IEnumerator coroutine;
        private PointsOnMeshJob pointsOnMeshJob;
        private Vector3[] sourceMeshVertices;

        public AddClumpsCoroutine() {
            coroutine = addClumpStrandsCoroutine();
        }

        public bool IsCompleted => pointsOnMeshJob.isCompleted || isCancelled;

        public AddClumpsCoroutine start() {
            sourceMeshVertices = furCreator.FurRenderer.meshBaker.sourceMesh.vertices;
            furCreator.StartCoroutine(coroutine);
            return this;
        }

        public void cancel() {
            furCreator.StopCoroutine(coroutine);
            pointsOnMeshJob?.cancel();
            isCancelled = true;
        }

        private IEnumerator addClumpStrandsCoroutine() {
            var existingHairStrands = furCreator.FurRenderer.furContainer.layerStrandsList[layerIndex].clumpsModifiers[clumpIndex].clumps;
            existingHairStrands = existingHairStrands.Length == 0
                ?  AddStrandsCoroutine.getLayerExistingHairStrands(furCreator.FurRenderer, layerIndex, furCreator.getActiveLayer().permanentlyDeletedStrands)
                : existingHairStrands;
            var existingGroom = furCreator.groomContainer.getActiveLayer().clumpModifiers[clumpIndex].strandsGroom;
            existingGroom = existingGroom == null || existingGroom.Length == 0
                ? furCreator.groomContainer.getActiveLayer().getExistingGroomsAndPermanentlyDeleted()
                : existingGroom;

            pointsOnMeshJob = new PointsOnMeshJob {
                distanceBetweenStrands = distanceBetweenClumps,
                furCreator = furCreator,
                mesh = furCreator.FurRenderer.meshBaker.sourceMesh,
                existingHairStrands = existingHairStrands,
                existingGrooms = existingGroom,
                isClumpJob = true
            };

            pointsOnMeshJob.start();
            pointsOnMeshJob.OnCompleteAction = connectClumpsToStrandsOrChildClumps();
            while (!pointsOnMeshJob.isCompleted) {
                yield return new WaitForSeconds(0.1f);
            }
        }

        private Action connectClumpsToStrandsOrChildClumps() {
            return () =>
            {
                Debug.Log("Add clump guides Complete: " + pointsOnMeshJob.output.Count);
                var furContainerLayerStrands = furCreator.FurRenderer.furContainer.layerStrandsList[layerIndex];
                var layerHairStrands = isParentClump
                    ? furContainerLayerStrands.clumpsModifiers[clumpIndex + 1].clumps
                    : furContainerLayerStrands.layerHairStrands;
                var vertices = sourceMeshVertices;
                foreach (var layerHairStrand in layerHairStrands) {
                    var meshPosition = Barycentric.interpolateV3(
                        vertices[(int) layerHairStrand.triangles.x],
                        vertices[(int) layerHairStrand.triangles.y],
                        vertices[(int) layerHairStrand.triangles.z],
                        layerHairStrand.barycentricCoordinates
                    );
                    var closestIndex = pointsOnMeshJob.getClosestClumpGuideIndex(meshPosition);
                    layerHairStrand.clumpIndices = closestIndex;
                }

                furContainerLayerStrands.clumpsModifiers[clumpIndex].clumps = pointsOnMeshJob.createdHairStrands;
                furCreator.groomContainer.layers[layerIndex].clumpModifiers[clumpIndex].strandsGroom = pointsOnMeshJob.createdStrandGrooms;
                furCreator.groomContainer.layers[layerIndex].clumpModifiers[clumpIndex].needsUpdate = true;
            };
        }
    }
}