using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace FluffyGroomingTool {
    public static class Extensions {
        public static void setKeywordEnabled(this ComputeShader compute, string keyWord, bool enable) {
            if (enable) {
                compute.EnableKeyword(keyWord);
            }
            else {
                compute.DisableKeyword(keyWord);
            }
        }
        public static  List<List<T>> split<T>(this  IList<T> source, int length)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / length)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
        static float fract(float floatNumber) {
            return floatNumber - Mathf.Floor(floatNumber);
        }

        public static bool getControlButton(this Event ev) {
            return Application.platform == RuntimePlatform.OSXEditor ? ev.command : ev.control;
        }

        private static readonly Vector2 staticVec = new Vector2(12.9898f, 78.233f);
        private static readonly float staticFloat = 43758.5453f;

        public static float rand(this Vector2 co) {
            return fract(Mathf.Sin(Vector2.Dot(co, staticVec)) * staticFloat);
        }

        private static Vector3 AbsVec3(Vector3 v) {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        public static string toRealString(this Vector2 v) {
            return "x: " + v.x + ", y: " + v.y;
        }

        public static string toRealString(this Vector3 v) {
            return "x: " + v.x + ", y: " + v.y + ", z: " + v.z;
        }

        public static NativeArray<T> ToPersistentArray<T>(this AsyncGPUReadbackRequest request) where T : struct {
            var nativeList = request.GetData<T>();
            var persistentArray = new NativeArray<T>(nativeList.Length, Allocator.Persistent);
            nativeList.CopyTo(persistentArray);
            return persistentArray;
        }

        public static string toRealString(this Vector4 v) {
            return "x: " + v.x + ", y: " + v.y + ", z: " + v.z + ", w: " + v.w;
        }

        public static float getCurveSum(this AnimationCurve curve) {
            float sum = 0f;
            foreach (var key in curve.keys) {
                sum += key.time;
                sum += key.value;
                sum += key.inTangent;
                sum += key.inWeight;
                sum += key.outTangent;
                sum += key.outWeight;
                sum += (int) key.weightedMode;
            }

            return sum;
        }

        public static void toWorldSpaceCapsule(this CapsuleCollider capsule, out Vector3 point0, out Vector3 point1, out float radius) {
            var center = capsule.transform.TransformPoint(capsule.center);
            radius = 0f;
            float height = 0f;
            Vector3 lossyScale = AbsVec3(capsule.transform.lossyScale);
            Vector3 dir = Vector3.zero;

            switch (capsule.direction) {
                case 0: // x
                    radius = Mathf.Max(lossyScale.y, lossyScale.z) * capsule.radius;
                    height = lossyScale.x * capsule.height;
                    dir = capsule.transform.TransformDirection(Vector3.right);
                    break;
                case 1: // y
                    radius = Mathf.Max(lossyScale.x, lossyScale.z) * capsule.radius;
                    height = lossyScale.y * capsule.height;
                    dir = capsule.transform.TransformDirection(Vector3.up);
                    break;
                case 2: // z
                    radius = Mathf.Max(lossyScale.x, lossyScale.y) * capsule.radius;
                    height = lossyScale.z * capsule.height;
                    dir = capsule.transform.TransformDirection(Vector3.forward);
                    break;
            }

            if (height < radius * 2f) {
                dir = Vector3.zero;
            }

            point0 = center + dir * (height * 0.5f - radius);
            point1 = center - dir * (height * 0.5f - radius);
        }
        
        public static float getValueFurthestFromOne(this Vector3 vec) {
            var max = Mathf.Max(Mathf.Max(vec.x, vec.y), vec.z);
            var min = Mathf.Min(Mathf.Min(vec.x, vec.y), vec.z);
            var minDistanceToOne = 1 - min;
            var maxDistanceToOne = max - 1;
            return minDistanceToOne > maxDistanceToOne ? min : max;
        }

        public static Camera getCamera([CanBeNull] this Camera lodCamera) {
            var main = Camera.main;
#if UNITY_EDITOR
            if (!Application.isPlaying && SceneView.lastActiveSceneView != null && EditorWindow.focusedWindow == SceneView.lastActiveSceneView) {
                return SceneView.lastActiveSceneView.camera;
            }
#endif
            Camera cam = lodCamera ? lodCamera : main ? main : Camera.current;
            return cam;
        }
        public static R let<T, R>(this T self, Func<T, R> block) 
        {
            return block(self);
        }
        public static T also<T>(this T self, Action<T> block) {
            block(self);
            return self;
        }
        public static void fixRwAndIndexFormat(this Mesh mesh) {
#if UNITY_EDITOR
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long localId);
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path) && !path.Contains("Library/unity")) {
                if (AssetImporter.GetAtPath(path) is ModelImporter modelImporter) {
                    modelImporter.indexFormat = ModelImporterIndexFormat.UInt32;
                    modelImporter.isReadable = true;
                    modelImporter.meshCompression = ModelImporterMeshCompression.Off;
                    modelImporter.meshOptimizationFlags = 0;
                }

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ImportRecursive);
            }
            else if (mesh.isReadable && mesh.subMeshCount <= 1) {
                var meshTriangles = mesh.triangles;
                mesh.indexFormat = IndexFormat.UInt32;
                mesh.triangles = meshTriangles;
            }
            else {
                Debug.LogError("Fluffy could not change the settings from code. Please do so manually by selecting the asset in the Project view.");
            }
#endif
        }
    }
}