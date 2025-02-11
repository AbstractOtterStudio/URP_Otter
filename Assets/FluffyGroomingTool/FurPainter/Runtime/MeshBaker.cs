using FluffyGroomingTool;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshBaker {
    public readonly SkinnedMeshRenderer skinnedMeshRenderer;
    private const int FullMeshBufferStride = sizeof(float) * 16;
    public const int MeshBufferStride = sizeof(float) * 10;
    public ComputeBuffer bakedMesh;

    private ComputeShader computeShader;
    public Mesh sourceMesh;
    private readonly Matrix4x4 indentity = Matrix4x4.identity;
    private readonly int stride;
    internal GraphicsBuffer indexBuffer;
    private readonly ComputeBuffer rayCastHitsBuffer;
    private readonly Transform sourceTransform;

    public MeshBaker(GameObject source, ComputeShader computeShader, bool autoFixIndexFormat = false) {
        sourceTransform = source.transform;
        stride = MeshBufferStride;
        var mf = source.GetComponent<MeshFilter>();
        skinnedMeshRenderer = source.GetComponent<SkinnedMeshRenderer>();
        sourceMesh = mf ? mf.sharedMesh : skinnedMeshRenderer.sharedMesh;
        if (autoFixIndexFormat && sourceMesh != null && sourceMesh.indexFormat != IndexFormat.UInt32) sourceMesh.fixRwAndIndexFormat();
        setupBaker(computeShader);
        getIndexBuffer();
        rayCastHitsBuffer = new ComputeBuffer(10, stride, ComputeBufferType.Append);
        computeShader.SetBuffer(1, "bakedMesh", bakedMesh);
        computeShader.SetBuffer(1, "meshIndexBuffer", indexBuffer);
        computeShader.SetBuffer(1, "rayCastHits", rayCastHitsBuffer);
    }

    public MeshBaker(Mesh mesh, ComputeShader computeShader) {
        stride = MeshBufferStride;
        sourceMesh = mesh;
        if (sourceMesh != null && sourceMesh.indexFormat != IndexFormat.UInt32) {
            sourceMesh.fixRwAndIndexFormat();
        }

        setupBaker(computeShader);
        getIndexBuffer();
        rayCastHitsBuffer = new ComputeBuffer(10, stride, ComputeBufferType.Append);
        computeShader.SetBuffer(1, "bakedMesh", bakedMesh);
        computeShader.SetBuffer(1, "meshIndexBuffer", indexBuffer);
        computeShader.SetBuffer(1, "rayCastHits", rayCastHitsBuffer);
    }


    internal GraphicsBuffer getIndexBuffer() {
        if (indexBuffer == null) {
            indexBuffer = sourceMesh.GetIndexBuffer();
        }

        return indexBuffer;
    }

    /**
     * Used for the hair cards data. This has Colors and UV in addition.
     */
    public MeshBaker(Mesh source, ComputeShader computeShader, int meshStride = FullMeshBufferStride) {
        sourceMesh = source;
        stride = meshStride;
        setupBaker(computeShader);
    }

    private void setupBaker(ComputeShader computeShader) {
        if (sourceMesh == null) return;
        setIndexFormat32(sourceMesh);
        if (sourceMesh.normals == null) {
            Debug.Log("Fluffy: Source mesh was missing normal. Recalculating and adding Normals.");
            sourceMesh.RecalculateNormals();
        }

        if (sourceMesh.uv == null) {
            Debug.Log("Fluffy: Source mesh was missing UVs. Recalculating and adding UVs.");
            sourceMesh.uv = new Vector2[sourceMesh.vertexCount].also((it) => {
                for (int i = 0; i < sourceMesh.vertexCount; i++) it[i] = zero;
            });
        }

        if (sourceMesh.tangents == null) {
            Debug.Log("Fluffy: Source mesh was missing normal. Recalculating and adding Tangents.");
            sourceMesh.RecalculateTangents();
        }

        if (skinnedMeshRenderer) {
            skinnedMeshRenderer.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        } else {
            sourceMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        }

        sourceMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;

        this.computeShader = computeShader;

        bakedMesh = new ComputeBuffer(sourceMesh.vertexCount, stride, ComputeBufferType.Default);

        computeShader.SetBuffer(0, "bakedMesh", bakedMesh);
        bakeMesh(false);
    }

    private static void setIndexFormat32(Mesh sourceMesh) {
        if (sourceMesh.indexFormat != IndexFormat.UInt32) {
            Debug.Log("Fluffy automatically changed the Mesh index format to 32. This may force only one Material on your Mesh. To avoid " +
                      "this from happening. Please change the import settings of your asset to index format 32.");
            var triangles = sourceMesh.triangles;
            sourceMesh.indexFormat = IndexFormat.UInt32;
            sourceMesh.triangles = triangles;
        }
    }

    public bool bakeSkinnedMesh(bool isCreateMeshPass) {
        bool didSucceed = false;
        if (skinnedMeshRenderer || isCreateMeshPass) {
            didSucceed = bakeMesh(isCreateMeshPass);
        }

        return didSucceed;
    }

    private bool bakeMesh(bool isCreateMeshPass) {
        bool didSucceed = false;
        var useSkinnedMeshBuffer = skinnedMeshRenderer && !isCreateMeshPass;

        var sourceMeshData = useSkinnedMeshBuffer ? skinnedMeshRenderer.GetVertexBuffer() : sourceMesh.GetVertexBuffer(0);

        if (sourceMeshData != null) {
            didSucceed = true;
            computeShader.SetBuffer(0, "sourceMeshData", sourceMeshData);
            computeShader.SetInt("vertexBufferStride", sourceMesh.GetVertexBufferStride(0));

            if (skinnedMeshRenderer && !isCreateMeshPass) {
                var transformMatrix = skinnedMeshRenderer.rootBone
                    ? getSourceMatrix() * createTransAndRotationMatrix(skinnedMeshRenderer.rootBone.localToWorldMatrix, Vector3.one)
                    : Matrix4x4.identity;
                computeShader.SetMatrix("transformMatrix", transformMatrix);
                computeShader.SetMatrix("rotationMatrix", Matrix4x4.Rotate(transformMatrix.rotation));
            } else {
                computeShader.DisableKeyword(ShaderID.IS_SKINNED_MESH);
                computeShader.SetMatrix("transformMatrix", indentity);
                computeShader.SetMatrix("rotationMatrix", indentity);
            }

            computeShader.SetInt("verticesCount", sourceMesh.vertexCount);
            computeShader.Dispatch(0, sourceMesh.vertexCount.toCsGroups(), 1, 1);
        }

        sourceMeshData?.Release();
        return didSucceed;
    }

    private Matrix4x4 getSourceMatrix() {
        return sourceTransform.worldToLocalMatrix;
    }

    private static Matrix4x4 createTransAndRotationMatrix(Matrix4x4 inMatrix, Vector3 transformLossyScale) {
        var trans = new Vector3(inMatrix[0, 3], inMatrix[1, 3], inMatrix[2, 3]);
        Quaternion rotation = Quaternion.LookRotation(
            inMatrix.GetColumn(2),
            inMatrix.GetColumn(1)
        );
        return Matrix4x4.TRS(trans, rotation, transformLossyScale);
    }

    public void dispose() {
        bakedMesh?.Dispose();
        indexBuffer?.Dispose();
        rayCastHitsBuffer?.Dispose();
    }

    public bool isSkinnedMesh() {
        return skinnedMeshRenderer != null && skinnedMeshRenderer.rootBone;
    }

    private MeshProperties currentHit;

    public MeshProperties? rayCast(Ray worldRay) {
        MeshProperties[] meshProperties = new MeshProperties[6];
        computeShader.SetMatrix("transformMatrix", sourceTransform.localToWorldMatrix);
        computeShader.SetMatrix("rotationMatrix", Matrix4x4.Rotate(sourceTransform.rotation));
        computeShader.SetVector("origin", worldRay.origin);
        computeShader.SetVector("direction", worldRay.direction);
        rayCastHitsBuffer.SetCounterValue(0);
        rayCastHitsBuffer.SetData(meshProperties);
        computeShader.Dispatch(1, ((int)(indexBuffer.count / 3f)).toCsGroupsEditor(), 1, 1);
        rayCastHitsBuffer.GetData(meshProperties);
        return getClosestHit(worldRay.origin, meshProperties);
    }

    private Vector3 zero = Vector3.zero;

    private MeshProperties? getClosestHit(Vector3 worldRayOrigin, MeshProperties[] meshProperties) {
        MeshProperties returnValue = meshProperties[0];
        if (returnValue.sourceVertex == zero) return null;
        var minDistance = Vector3.Distance(worldRayOrigin, returnValue.sourceVertex);
        foreach (var property in meshProperties) {
            if (property.sourceVertex != zero) {
                var distance = Vector3.Distance(worldRayOrigin, property.sourceVertex);
                if (distance < minDistance) {
                    minDistance = distance;
                    returnValue = property;
                }
            }
        }

        return returnValue;
    }

    public Vector3 getObjectPosition() {
        return isSkinnedMesh() ? skinnedMeshRenderer.rootBone.position : sourceTransform.position;
    }
}

public struct MeshProperties {
    public Vector3 sourceVertex;
    public Vector3 sourceNormal;
    public Vector4 sourceTangent;

    public static MeshProperties zero() {
        return new MeshProperties() {
            sourceVertex = Vector3.zero,
            sourceNormal = Vector3.zero,
            sourceTangent = Vector3.zero
        };
    }
}