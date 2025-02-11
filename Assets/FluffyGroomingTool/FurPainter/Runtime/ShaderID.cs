using UnityEngine;

namespace FluffyGroomingTool {
    public class ShaderID {
        public static readonly int LOCAL_TO_WORLD_MATRIX = Shader.PropertyToID("localToWorldMatrix"); 
        public static readonly int OBJECT_ROTATION_MATRIX = Shader.PropertyToID("objectRotationMatrix");
        public static readonly int DELTA_TIME = Shader.PropertyToID("deltaTime");
 
    
        public static readonly int SOURCE_MESH = Shader.PropertyToID("sourceMesh"); 
        public static readonly int WIND_GUST = Shader.PropertyToID("WindGust");
        public static readonly int TIME = Shader.PropertyToID("time");
        public static readonly int WIND_STRENGTH = Shader.PropertyToID("WindStrength");
        public static readonly int EXTRA_SCALE = Shader.PropertyToID("extraScale");
        public static readonly int WIND_DIRECTION = Shader.PropertyToID("WindDirection");
        public static readonly int WIND_DISTORTION_MAP = Shader.PropertyToID("WindDistortionMap");
        public static readonly int COLLIDERS_COUNT = Shader.PropertyToID("CollidersCount");
        public static readonly int COLLIDER_BUFFER = Shader.PropertyToID("ColliderBuffer");
        public static readonly int CLUMP_POINTS_POSITION = Shader.PropertyToID("clumpPointsPosition");
        public static readonly int CLUMP_ATTRACTION_CURVE = Shader.PropertyToID("clumpAttractionCurve");
        public static readonly int CLUMP_Y_COORDINATES = Shader.PropertyToID("clumpYCoordinates");
        public static readonly int LAYER_VERTEX_START_INDEX = Shader.PropertyToID("layerVertexStartIndex");
        public static readonly int LAYER_VERTICES_COUNT = Shader.PropertyToID("layerVerticesCount"); 
        public static readonly int FUR_MESH_BUFFER_STRIDE = Shader.PropertyToID("furMeshBufferStride");
        public static readonly int FUR_MESH_BUFFER = Shader.PropertyToID("furMeshBuffer");
        public static readonly int OBJECT_GLOBAL_SCALE = Shader.PropertyToID("objectGlobalScale");
  
        public static readonly int SOURCE_MESH_NORMAL_TO_STRAND_NORMAL_PERCENT = Shader.PropertyToID("sourceMeshNormalToStrandNormalPercent");
        public static readonly string HAS_CLUMPS = "HAS_CLUMPS";
        public static readonly string IS_SKINNED_MESH = "IS_SKINNED_MESH";
        public static readonly string HAS_COLLIDERS = "HAS_COLLIDERS";
        public static readonly string UPDATE_FUR_MESH_KERNEL = "UpdateFurMeshKernel";
        public static readonly string UPDATE_CLUMPS_POSITION_KERNEL = "UpdateClumpsPositionKernel";
        public static readonly string FUR_RENDERER_CS_NAME = "ProceduralFurSetup";
        public static readonly string MESH_BAKER_CS_NAME = "MeshBaker";
        public static readonly string MESH_BAKER_FULL_CS_NAME = "MeshBakerFull";

        public static readonly int MESH_INDEX_BUFFER = Shader.PropertyToID("meshIndexBuffer");
        public static readonly int TRIANGLES = Shader.PropertyToID("triangles");
        public static readonly int VISIBLE_TRIANGLES = Shader.PropertyToID("visibleTriangles");
        public static readonly int VISIBLE_TRIANGLES_READ = Shader.PropertyToID("visibleTrianglesRead");
        public static readonly int IS_FLIPPED_Y = Shader.PropertyToID("isFlippedY");

        public static readonly int VERTEX_BUFFER_STRIDE = Shader.PropertyToID("vertexBufferStride"); 
        public static readonly int UNITY_MATRIX_MVP = Shader.PropertyToID("_UNITY_MATRIX_MVP");
        public static readonly int WORLD_SPACE_CAMERA_POSITION = Shader.PropertyToID("worldSpaceCameraPosition");
        public static readonly int IS_FRUSTUM_CULLING_ENABLED = Shader.PropertyToID("isFrustumCullingEnabled");
        public static readonly int CAMERA_FAR_CLIP_PLANE = Shader.PropertyToID("cameraFarClipPlane");
        public static readonly int LOD_SKIP_STRANDS_COUNT = Shader.PropertyToID("lodSkipStrandsCount");
        public static readonly int LOD_MAX_SKIP_STRANDS_COUNT = Shader.PropertyToID("maxSkipStrandsCount");
        public static readonly int KEYS = Shader.PropertyToID("Keys");
        public static readonly int NUM_VISIBLE_TRIANGLES_COUNT = Shader.PropertyToID("numberOfVisibleTriangles");
        public static readonly int TRIANGLES_COUNT = Shader.PropertyToID("trianglesCount");
    }
}