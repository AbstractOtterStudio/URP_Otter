using System.Collections;
using System.Linq;
using UnityEngine;

namespace FluffyGroomingTool {
    public class AddStrandsCoroutine {
        public FurCreator furCreator;

        private bool isCancelled;

        private readonly IEnumerator coroutine;
        private PointsOnMeshJob pointsOnMeshJob;

        public AddStrandsCoroutine() {
            coroutine = addPreviewStrandsCoroutine();
        }

        public AddStrandsCoroutine start() {
            furCreator.StartCoroutine(coroutine);
            return this;
        }

        public void cancel() {
            furCreator.StopCoroutine(coroutine);
            pointsOnMeshJob?.cancel();
            isCancelled = true;
            currentClumpCoroutine?.cancel();
        }

        private AddClumpsCoroutine currentClumpCoroutine;

        private IEnumerator addPreviewStrandsCoroutine() {
            yield return new WaitForSeconds(0.4f);
            furCreator.IsFurStrandsProgressVisible = true;
            yield return null;
            var furRenderer = furCreator.FurRenderer;
            furRenderer.furContainer.copyValuesFromComputeBufferToNativeObject();
            furCreator.groomContainer.copyValuesFromComputeBufferToNativeObject();
            yield return new WaitForSeconds(0.5f); //Give the async copyFromNativeFunctions some time to finish. 
            var activeLayerIndex = furCreator.groomContainer.activeLayerIndex;
            pointsOnMeshJob = new PointsOnMeshJob {
                distanceBetweenStrands = getInterpolatedStrandsDistance(),
                furCreator = furCreator,
                mesh = furCreator.FurRenderer.meshBaker.sourceMesh,
                existingGrooms = furCreator.getActiveLayer().getExistingGroomsAndPermanentlyDeleted(),
                existingHairStrands =
                    getLayerExistingHairStrands(furRenderer, activeLayerIndex, furCreator.getActiveLayer().permanentlyDeletedStrands),
                layerIndex = activeLayerIndex
            };
            pointsOnMeshJob.start();
            while (!pointsOnMeshJob.isCompleted) {
                yield return new WaitForSeconds(0.1f);
            }

            if (!isCancelled) {
                furRenderer.furContainer = furRenderer.furContainer
                    ? furRenderer.furContainer
                    : ScriptableObject.CreateInstance<FurContainer>();
                if (activeLayerIndex > furRenderer.furContainer.layerStrandsList.Length - 1) {
                    var list = furRenderer.furContainer.layerStrandsList.ToList();
                    var hairStrandLayer = new HairStrandLayer {layerHairStrands = pointsOnMeshJob.createdHairStrands};
                    list.Add(hairStrandLayer);
                    furRenderer.furContainer.layerStrandsList = list.ToArray();
                    hairStrandLayer.setupCardShape(furCreator.groomContainer);
                }
                else {
                    furRenderer.furContainer.layerStrandsList[activeLayerIndex].layerHairStrands = pointsOnMeshJob.createdHairStrands;
                }

                furCreator.groomContainer.getActiveLayer().strandsGroomOneToOne = pointsOnMeshJob.createdStrandGrooms;
                furRenderer.furContainer.copyClumpValuesFromComputeBufferToNativeObject();

                var groomClumpsModifierLayers = furCreator.groomContainer.getActiveLayer().clumpModifiers;
                if (groomClumpsModifierLayers.Length > 0) {
                    yield return new WaitForSeconds(0.5f); //We need a small delay until copyClumpValuesFromComputeBufferToNativeObject has finished
                    var currentModifiers = furRenderer.furContainer.layerStrandsList[activeLayerIndex].clumpsModifiers.ToList();

                    if (groomClumpsModifierLayers.Length > currentModifiers.Count) {
                        currentModifiers.Add(new ClumpModifierLayer());
                        furRenderer.furContainer.layerStrandsList[activeLayerIndex].clumpsModifiers = currentModifiers.ToArray();
                    }

                    var modifiers = groomClumpsModifierLayers;
                    for (var index = modifiers.Length - 1; index >= 0; index--) {
                        if (isCancelled) break;
                        var isParentClump = modifiers.Length > 1 && index < modifiers.Length - 1;
                        currentClumpCoroutine = new AddClumpsCoroutine {
                            furCreator = furCreator,
                            layerIndex = activeLayerIndex,
                            clumpIndex = index,
                            distanceBetweenClumps = groomClumpsModifierLayers[index].clumpsSpacing / furCreator.groomContainer.worldScale,
                            isParentClump = isParentClump
                        }.start();
                        yield return new WaitForSeconds(0.1f);
                        while (!currentClumpCoroutine.IsCompleted) {
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }

                furCreator.groomContainer.clearApplyTextureState();
                furCreator.IsFurStrandsProgressVisible = false;
                if (!furCreator.IsFirstLoad) {
                    yield return new WaitForSeconds(0.2f);
                }

                furCreator.IsFirstLoad = false;
                furCreator.groomContainer.invalidate();
                furCreator.groomContainer.getActiveLayer().clearPermanentlyDeletedBackup();
                furRenderer.furContainer.recreateAll.Invoke();
                furCreator.needsUpdate = true;
                Debug.Log("Completed calculating strands/cards. Final count: " + pointsOnMeshJob.output.Count);
                yield return new WaitForSeconds(1f);
                furCreator.copyBuffersToNative();
            }
        }

        private float getInterpolatedStrandsDistance() {
            var distance = furCreator.groomContainer.getActiveLayer().distanceBetweenStrands;
            const float threshold = 0.005f;
            const float multiplier = 4f;
            if (distance > threshold) {
                distance = threshold + (distance - threshold) * multiplier;
            }

            return distance / furCreator.groomContainer.worldScale;
        }

        public static HairStrand[]
            getLayerExistingHairStrands(FurRenderer furRenderer, int activeLayerIndex, HairStrand[] permanentlyDeletedStrands) {
            var existingStrands = new HairStrand[0];
            if (activeLayerIndex <= furRenderer.furContainer.layerStrandsList.Length - 1) {
                existingStrands = furRenderer.furContainer.layerStrandsList[activeLayerIndex].layerHairStrands;
            }

            if (permanentlyDeletedStrands != null && permanentlyDeletedStrands.Length > 0) {
                var hairStrands = existingStrands.ToList();
                hairStrands.AddRange(permanentlyDeletedStrands);
                existingStrands = hairStrands.ToArray();
            }

            return existingStrands;
        }
    }
}