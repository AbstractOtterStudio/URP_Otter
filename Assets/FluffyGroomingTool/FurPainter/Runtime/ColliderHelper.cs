using System.Linq;
using UnityEngine;

namespace FluffyGroomingTool {
    public static class ColliderHelper {
        public static void setupCollidersBuffer(
            ref ComputeBuffer colliderBuffer,
            ref ColliderStruct[] collidersStruct,
            SphereCollider[] sphereColliders,
            CapsuleCollider[] capsuleColliders
        ) {
            colliderBuffer?.Dispose();
            colliderBuffer = null;
            var collidersCount = getCollidersCount(sphereColliders, capsuleColliders);
            if (collidersCount > 0) {
                colliderBuffer = new ComputeBuffer(collidersCount, ColliderStruct.Size());
                collidersStruct = new ColliderStruct[collidersCount];
                for (int i = 0; i < collidersCount; i++) {
                    collidersStruct[i] = new ColliderStruct();
                }

                colliderBuffer.SetData(collidersStruct);
            }
        }

        public static void setupColliderProperties(
            ref ComputeBuffer colliderBuffer,
            ref ColliderStruct[] collidersStruct,
            SphereCollider[] sphereColliders,
            CapsuleCollider[] capsuleColliders,
            ComputeShader compute, 
            int kernel
        ) {
            if (!collidersAssigned(sphereColliders, capsuleColliders)) return;
            var collidersCount = getCollidersCount(sphereColliders, capsuleColliders);
            if (collidersCount > 0) {
                if (colliderBuffer?.count != collidersCount) setupCollidersBuffer(ref colliderBuffer, ref collidersStruct, sphereColliders, capsuleColliders);

                var sphereCollidersLength = sphereColliders.Length;
                setupSphereColliders(collidersStruct, sphereColliders, sphereCollidersLength);
                setupCapsuleColliders(collidersStruct, capsuleColliders, sphereCollidersLength);
                colliderBuffer.SetData(collidersStruct);
                setSphereAndCapsuleColliders(compute, kernel, collidersCount, colliderBuffer);
            }
            else {
                compute.DisableKeyword(ShaderID.HAS_COLLIDERS);
            }
        }

        private static void setupSphereColliders(ColliderStruct[] collidersStruct, SphereCollider[] sphereColliders, int sphereCollidersLength) {
            for (var i = 0; i < sphereCollidersLength; i++) {
                if (sphereColliders[i] != null) {
                    collidersStruct[i].position = sphereColliders[i].transform.position + sphereColliders[i].center;
                    collidersStruct[i].radius = sphereColliders[i].radius * sphereColliders[i].transform.lossyScale.x;
                    collidersStruct[i].position2 = SPHERE_COLLIDER_ID; //For now this means that its a sphereCollider.
                }
            }
        }

        private static void setupCapsuleColliders(ColliderStruct[] collidersStruct, CapsuleCollider[] capsuleColliders, int sphereCollidersLength) {
            for (var i = 0; i < capsuleColliders.Length; i++) {
                if (capsuleColliders[i] != null) {
                    ColliderStruct colliderStruct = collidersStruct[sphereCollidersLength + i];
                    capsuleColliders[i]
                        .toWorldSpaceCapsule(out colliderStruct.position, out colliderStruct.position2, out colliderStruct.radius);
                    collidersStruct[sphereCollidersLength + i] = colliderStruct;
                }
            }
        }

        private static void setSphereAndCapsuleColliders(ComputeShader compute, int kernel, int collidersCount, ComputeBuffer colliderBuffer) {
            compute.SetBuffer(kernel, ShaderID.COLLIDER_BUFFER, colliderBuffer);
            compute.SetInt(ShaderID.COLLIDERS_COUNT, collidersCount);
            compute.EnableKeyword(ShaderID.HAS_COLLIDERS);
        }

        private static readonly Vector3 SPHERE_COLLIDER_ID = Vector3.left * 1000;

        public static bool collidersAssigned(SphereCollider[] sphereColliders, CapsuleCollider[] capsuleColliders) {
            return sphereColliders != null && capsuleColliders != null &&
                   sphereColliders.All(t => t != null) && capsuleColliders.All(t => t != null);
        }

        private static int getCollidersCount(SphereCollider[] sphereColliders, CapsuleCollider[] capsuleColliders) {
            return sphereColliders == null ? 0 : sphereColliders.Length + capsuleColliders.Length;
        }
    }
}
