using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using UnityEngine;

namespace FluffyGroomingTool {
    public struct PointOnMesh {
        public Vector3 pos;
        public int triangleIndex1;
        public int triangleIndex2;
        public int triangleIndex3;
        public Vector3 vertex1;
        public Vector3 vertex2;
        public Vector3 vertex3;
    }

    public class PointsOnMeshJob {
        public bool isCompleted;
        private int[] triangles;
        private Vector3[] vertices;

        public List<PointOnMesh> output;
        private List<PointAndIndex>[,,] newPointsOnGrid;
        private List<GroomAndPosition>[,,] existingPointsOnGrid;
        public float distanceBetweenStrands;
        private Vector3 gridResolution;
        private Vector3 gridZeroOffsets;
        private float gridSize;
        public HairStrand[] createdHairStrands;
        public StrandGroom[] createdStrandGrooms;
        public FurCreator furCreator;
        private Vector2[] uvs;
        public Mesh mesh;
        public Action OnCompleteAction { get; set; }
        public StrandGroom[] existingGrooms { get; set; }
        public HairStrand[] existingHairStrands { get; set; }
        public bool isClumpJob = false;
        public float layerIndex;
        private NativeArray<Color32> maskTexture;
        private Vector2 maskTextureSize;

        private int getNumberOfSamples(Vector3 vert1, Vector3 vert2, Vector3 vert3) {
            var dist1 = Vector3.Distance(vert1, vert2);
            var dist2 = Vector3.Distance(vert2, vert3);
            var dist3 = Vector3.Distance(vert3, vert1);
            var biggestDistance1And2 = dist1 < dist2 ? dist2 : dist1;
            var biggestDistance = biggestDistance1And2 < dist3 ? dist3 : biggestDistance1And2;
            var numSamples = biggestDistance / 2f / distanceBetweenStrands;
            return (int)Math.Max(numSamples, 2f);
        }

        private float getClosestDistance(Vector3 vertexCandidate) {
            float closestDistance = float.MaxValue;

            Vector3 candidateGridCoordinate = new Vector3(
                (int)((vertexCandidate.x - gridZeroOffsets.x) * gridResolution.x),
                (int)((vertexCandidate.y - gridZeroOffsets.y) * gridResolution.y),
                (int)((vertexCandidate.z - gridZeroOffsets.z) * gridResolution.z)
            );
            var pointsGridLength = gridSize - 1;
            var maxX = (int)Math.Clamp(candidateGridCoordinate.x + 1, 0, pointsGridLength);
            var maxY = (int)Math.Clamp(candidateGridCoordinate.y + 1, 0, pointsGridLength);
            var maxZ = (int)Math.Clamp(candidateGridCoordinate.z + 1, 0, pointsGridLength);
            for (int x = (int)Math.Max(candidateGridCoordinate.x - 1, 0); x <= maxX; x++) {
                for (int y = (int)Math.Max(candidateGridCoordinate.y - 1, 0); y <= maxY; y++) {
                    for (int z = (int)Math.Max(candidateGridCoordinate.z - 1, 0); z <= maxZ; z++) {
                        var pointsInCell = newPointsOnGrid[x, y, z] =
                            newPointsOnGrid[x, y, z] != null ? newPointsOnGrid[x, y, z] : new List<PointAndIndex>();
                        for (int i = 0; i < pointsInCell.Count; i++) {
                            float curDist = Vector3.Distance(vertexCandidate, pointsInCell[i].position);
                            closestDistance = curDist < closestDistance ? curDist : closestDistance;
                        }
                    }
                }
            }

            return closestDistance;
        }

        public int getClosestClumpGuideIndex(Vector3 vertexCandidate, int neighborCells = 1) {
            var closestDistance = float.MaxValue;
            var closestClumpIndex = -1;
            var candidateGridCoordinate = new Vector3(
                (int)((vertexCandidate.x - gridZeroOffsets.x) * gridResolution.x),
                (int)((vertexCandidate.y - gridZeroOffsets.y) * gridResolution.y),
                (int)((vertexCandidate.z - gridZeroOffsets.z) * gridResolution.z)
            );
            var pointsGridLength = gridSize - 1;
            var maxX = (int)Math.Clamp(candidateGridCoordinate.x + neighborCells, 0, pointsGridLength);
            var maxY = (int)Math.Clamp(candidateGridCoordinate.y + neighborCells, 0, pointsGridLength);
            var maxZ = (int)Math.Clamp(candidateGridCoordinate.z + neighborCells, 0, pointsGridLength);
            for (int x = (int)Math.Max(candidateGridCoordinate.x - neighborCells, 0); x <= maxX; x++) {
                for (int y = (int)Math.Max(candidateGridCoordinate.y - neighborCells, 0); y <= maxY; y++) {
                    for (int z = (int)Math.Max(candidateGridCoordinate.z - neighborCells, 0); z <= maxZ; z++) {
                        var pointsInCell = newPointsOnGrid[x, y, z] != null ? newPointsOnGrid[x, y, z] : new List<PointAndIndex>();
                        for (int i = 0; i < pointsInCell.Count; i++) {
                            var pointAndIndex = pointsInCell[i];
                            float curDist = Vector3.Distance(vertexCandidate, pointAndIndex.position);
                            if (curDist < closestDistance) {
                                closestDistance = curDist;
                                closestClumpIndex = pointAndIndex.index;
                            }
                        }
                    }
                }
            }

            if (closestClumpIndex == -1 && neighborCells == 4) {
                return getClosestClumpGuideIndex(vertexCandidate, newPointsOnGrid.Length);
            }

            if (closestClumpIndex == -1) {
                return getClosestClumpGuideIndex(vertexCandidate, 4);
            }

            return closestClumpIndex;
        }

        private StrandGroom getClosesExistingGroom(PointOnMesh pointOnMesh, int neighborCells = 1) {
            float closestDistance = float.MaxValue;
            StrandGroom closestGroom = null;
            Vector3 candidateGridCoordinate = new Vector3(
                (int)((pointOnMesh.pos.x - gridZeroOffsets.x) * gridResolution.x),
                (int)((pointOnMesh.pos.y - gridZeroOffsets.y) * gridResolution.y),
                (int)((pointOnMesh.pos.z - gridZeroOffsets.z) * gridResolution.z)
            );
            var pointsGridLength = gridSize - 1;
            var maxX = (int)Math.Min(candidateGridCoordinate.x + neighborCells, pointsGridLength);
            var maxY = (int)Math.Min(candidateGridCoordinate.y + neighborCells, pointsGridLength);
            var maxZ = (int)Math.Min(candidateGridCoordinate.z + neighborCells, pointsGridLength);
            for (int x = (int)Math.Max(candidateGridCoordinate.x - neighborCells, 0); x <= maxX; x++) {
                for (int y = (int)Math.Max(candidateGridCoordinate.y - neighborCells, 0); y <= maxY; y++) {
                    for (int z = (int)Math.Max(candidateGridCoordinate.z - neighborCells, 0); z <= maxZ; z++) {
                        var pointsInCell = existingPointsOnGrid[x, y, z] != null ? existingPointsOnGrid[x, y, z] : new List<GroomAndPosition>();
                        for (int i = 0; i < pointsInCell.Count; i++) {
                            var pointAndIndex = pointsInCell[i];
                            float curDist = Vector3.Distance(pointOnMesh.pos, pointAndIndex.position);
                            if (curDist < closestDistance) {
                                closestDistance = curDist;
                                closestGroom = pointAndIndex.groom;
                            }
                        }
                    }
                }
            }

            if (closestGroom == null && neighborCells == 4) {
                return getClosesExistingGroom(pointOnMesh, 10);
            }

            if (closestGroom == null && neighborCells == 1) {
                return getClosesExistingGroom(pointOnMesh, 4);
            }

            if (neighborCells > 4 && closestGroom == null) {
                closestGroom = existingGrooms[0];
            }

            return closestGroom;
        }

        private void addPoint(PointOnMesh pos) {
            var index = output.Count;
            output.Add(pos);
            var gridResolutionX = (int)Math.Clamp((pos.pos.x - gridZeroOffsets.x) * gridResolution.x, 0, gridSize - 1);
            var gridResolutionY = (int)Math.Clamp((pos.pos.y - gridZeroOffsets.y) * gridResolution.y, 0, gridSize - 1);
            var gridResolutionZ = (int)Math.Clamp((pos.pos.z - gridZeroOffsets.z) * gridResolution.z, 0, gridSize - 1);
            newPointsOnGrid[gridResolutionX, gridResolutionY, gridResolutionZ].Add(new PointAndIndex {
                index = index, position = pos.pos
            });
        }

        public class PointAndIndex {
            public int index;
            public Vector3 position;
        }

        public class GroomAndPosition {
            public StrandGroom groom;
            public Vector3 position;
        }

        private Thread thread;

        public void start() {
            setupValues();
            thread = new Thread(execute);
            thread.Start();
        }

        private void setupValues() {
            gridSize = getGridSize(distanceBetweenStrands);
            gridResolution = getGridResolution();
            var size = (int)gridSize;
            newPointsOnGrid = new List<PointAndIndex>[size, size, size];
            existingPointsOnGrid = new List<GroomAndPosition>[size, size, size];
            vertices = mesh.vertices;
            triangles = mesh.triangles;
            output = new List<PointOnMesh>();
            gridZeroOffsets = mesh.bounds.min;
            uvs = mesh.uv;
            var tex = furCreator.getActiveLayer().maskTexture;
            if (tex != null) {
                maskTexture = fixTextureFormat(tex);
                maskTextureSize = new Vector2(tex.width, tex.height);
            }
        }

        private NativeArray<Color32> fixTextureFormat(Texture2D tex) {
            var texture = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            texture.SetPixels(tex.GetPixels());
            return texture.GetRawTextureData<Color32>();
        }

        private void execute() {
            addExistingPointsToGrid();
            float numberOfIndices = triangles.Length;
            float numberOfTriangles = numberOfIndices / 3;
            for (int x = 0; x < numberOfTriangles; x++) {
                int triID = x * 3;
                if (triID < numberOfTriangles * 3) {
                    PointOnMesh pointOnMesh = new PointOnMesh();
                    pointOnMesh.triangleIndex1 = triangles[triID + 0];
                    pointOnMesh.triangleIndex2 = triangles[triID + 1];
                    pointOnMesh.triangleIndex3 = triangles[triID + 2];
                    var triMaskedAmount = triangleMaskedAmount(pointOnMesh);
                    // if (triMaskedAmount == 0) continue; //Entire triangle is masked out.

                    pointOnMesh.vertex1 = vertices[pointOnMesh.triangleIndex1];
                    pointOnMesh.vertex2 = vertices[pointOnMesh.triangleIndex2];
                    pointOnMesh.vertex3 = vertices[pointOnMesh.triangleIndex3];
                    Vector3 ab = pointOnMesh.vertex2 - pointOnMesh.vertex1;
                    Vector3 ac = pointOnMesh.vertex3 - pointOnMesh.vertex1;
                    int samplesPerTriangle = getNumberOfSamples(pointOnMesh.vertex1, pointOnMesh.vertex2, pointOnMesh.vertex3);
                    for (int i = 0; i < samplesPerTriangle; i++) {
                        Vector2 randomSeed = new Vector2(
                            new Vector2(x + i + layerIndex, layerIndex + triID + i * 2).rand(),
                            new Vector2(triID + i + layerIndex / 2, pointOnMesh.triangleIndex2 * i * 2 + layerIndex / 2).rand()
                        );

                        randomSeed = randomSeed.x + randomSeed.y > 1.0 ? Vector2.one - randomSeed : randomSeed;
                        pointOnMesh.pos = pointOnMesh.vertex1 + randomSeed.x * ab + randomSeed.y * ac;
                        if (triMaskedAmount < 255 * 3 && isPointMasked(pointOnMesh)) continue;

                        var closestDistance = getClosestDistance(pointOnMesh.pos);
                        if (closestDistance > distanceBetweenStrands) {
                            addPoint(pointOnMesh);
                        }
                    }
                }
            }

            var tempHairStrands = new List<HairStrand>();
            var tempStrandGroom = new List<StrandGroom>();

            foreach (var t in output) {
                tempHairStrands.Add(FurCreator.createPointAtPosition(t, uvs));
                if (isClumpJob) {
                    tempStrandGroom.Add(hasExistingGroom() ? (StrandGroom)getClosesExistingGroom(t).Clone() : StrandGroom.zero());
                } else {
                    tempStrandGroom.Add(hasExistingGroom() ? getClosesExistingGroom(t) : StrandGroom.zero());
                }
            }

            createdHairStrands = tempHairStrands.ToArray();
            createdStrandGrooms = tempStrandGroom.ToArray();
            OnCompleteAction?.Invoke();
            isCompleted = true;
            if (maskTexture.IsCreated) maskTexture.Dispose();
        }

        private bool isPointMasked(PointOnMesh pointOnMesh) {
            var uv = pointOnMesh.creatBaryCentricMeshCoordinates().interpolatedUv(uvs);
            var index = (int)(
                (int)Math.Min(maskTextureSize.x - 1, Math.Floor(maskTextureSize.x * uv.x)) +
                maskTextureSize.x * Math.Min(maskTextureSize.y - 1, Math.Floor(maskTextureSize.y * uv.y))
            );
            return maskTexture[index].r < 128;
        }

        private float triangleMaskedAmount(PointOnMesh pointOnMesh) {
            if (!maskTexture.IsCreated || maskTexture.Length == 0) return 256 * 3;
            var uv1 = uvs[pointOnMesh.triangleIndex1];
            var uv2 = uvs[pointOnMesh.triangleIndex2];
            var uv3 = uvs[pointOnMesh.triangleIndex3];
            var uv1X = (int)(
                (int)Math.Min(maskTextureSize.x - 1, Math.Floor(maskTextureSize.x * uv1.x)) +
                maskTextureSize.x * Math.Min(maskTextureSize.y - 1, Math.Floor(maskTextureSize.y * uv1.y))
            );
            var uv2X = (int)(
                (int)Math.Min(maskTextureSize.x - 1, Math.Floor(maskTextureSize.x * uv2.x)) +
                maskTextureSize.x * Math.Min(maskTextureSize.y - 1, Math.Floor(maskTextureSize.y * uv2.y))
            );
            var uv3X = (int)(
                (int)Math.Min(maskTextureSize.x - 1, Math.Floor(maskTextureSize.x * uv3.x)) +
                maskTextureSize.x * Math.Min(maskTextureSize.y - 1, Math.Floor(maskTextureSize.y * uv3.y))
            );

            return maskTexture[uv1X].r + maskTexture[uv2X].r + maskTexture[uv3X].r;
        }

        private void addExistingPointsToGrid() {
            if (hasExistingGroom()) {
                for (int i = 0; i < existingGrooms.Length; i++) {
                    StrandGroom groom = existingGrooms[i];
                    HairStrand existingHairStrand = existingHairStrands[i];

                    Vector3 position = Barycentric.interpolateV3(
                        vertices[(int)existingHairStrand.triangles.x],
                        vertices[(int)existingHairStrand.triangles.y],
                        vertices[(int)existingHairStrand.triangles.z],
                        existingHairStrand.barycentricCoordinates
                    );

                    var x = (int)Math.Clamp((position.x - gridZeroOffsets.x) * gridResolution.x, 0, gridSize - 1);
                    var y = (int)Math.Clamp((position.y - gridZeroOffsets.y) * gridResolution.y, 0, gridSize - 1);
                    var z = (int)Math.Clamp((position.z - gridZeroOffsets.z) * gridResolution.z, 0, gridSize - 1);
                    try {
                        existingPointsOnGrid[x, y, z] =
                            existingPointsOnGrid[x, y, z] != null ? existingPointsOnGrid[x, y, z] : new List<GroomAndPosition>();
                        existingPointsOnGrid[x, y, z].Add(new GroomAndPosition { position = position, groom = groom });
                    } catch {
                        //Do nothing. This can happen if the mesh gets corrupted or contains Nan values.
                    }
                }
            }
        }

        private bool hasExistingGroom() {
            return existingGrooms != null && existingGrooms.Length > 0 && existingGrooms.Length == existingHairStrands.Length;
        }

        public void cancel() {
            thread?.Abort();
        }

        private float getGridSize(float furCreatorDistanceBetweenStrands) {
            var biggestSizeXY = mesh.bounds.size.x > mesh.bounds.size.y ? mesh.bounds.size.x : mesh.bounds.size.y;
            var biggestSize = biggestSizeXY > mesh.bounds.size.z ? biggestSizeXY : mesh.bounds.size.z;
            var gSize = Mathf.Clamp(biggestSize / furCreatorDistanceBetweenStrands, 5, 100);

            //Round to nearest ten. 
            if (gSize % 10 == 0) return gSize;
            return (10 - gSize % 10) + gSize;
        }

        private Vector3 getGridResolution() {
            return new Vector3(
                gridSize / mesh.bounds.size.x,
                gridSize / mesh.bounds.size.y,
                gridSize / mesh.bounds.size.z
            );
        }
    }
}