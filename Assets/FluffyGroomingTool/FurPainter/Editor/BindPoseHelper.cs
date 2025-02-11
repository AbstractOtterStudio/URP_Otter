using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    public class BindPoseHelper {
        private Dictionary<Transform, Matrix4x4> bindPoseMap;
        public bool isInBindPose;

        public void updateBindPoseState(FurCreator furCreator) {
            if (furCreator.FurRenderer.CurrentRenderer is SkinnedMeshRenderer renderer) {
                if (bindPoseMap == null) bindPoseMap = createBindPoseMap(getSkinnedMeshRenderers(renderer));
                isInBindPose = checkBindPose(renderer);
            }
            else {
                isInBindPose = true;
            }
        }

        private bool checkBindPose(SkinnedMeshRenderer skinnedMeshRenderer) {
            foreach (var kvp in bindPoseMap) {
                Transform boneTrans = kvp.Key;
                if (boneTrans == null) {
                    bindPoseMap = null;
                    break;
                }

                if (boneTrans != skinnedMeshRenderer.rootBone) {
                    Matrix4x4 bindPose = kvp.Value;

                    var parent = boneTrans.parent;
                    Matrix4x4 localMatrix = bindPoseMap.ContainsKey(parent)
                        ? (bindPose * bindPoseMap[parent].inverse).inverse
                        : bindPose.inverse;

                    var bindPoseScale = new Vector3(
                        localMatrix.GetColumn(0).magnitude,
                        localMatrix.GetColumn(1).magnitude,
                        localMatrix.GetColumn(2).magnitude
                    );
                    if (boneTrans.localPosition != localMatrix.MultiplyPoint(Vector3.zero)
                        || boneTrans.localRotation != Quaternion.LookRotation(localMatrix.GetColumn(2), localMatrix.GetColumn(1))
                        || boneTrans.localScale != bindPoseScale) {
                        return false;
                    }
                }
            }

            return true;
        }

        private const int CancelOption = 1;

        public void restoreBindPose(FurCreator furCreator) {
            var dialogSelection = EditorUtility.DisplayDialogComplex("Restore Bind Pose",
                "This will restore the rotation and scale bind poses. " +
                "Restoring the position bind pose may in some cases give buggy results. " +
                "Would you like to exclude the position property?", "Yes", "Cancel", "No");
            if (dialogSelection != CancelOption) {
                if (furCreator.FurRenderer.CurrentRenderer is SkinnedMeshRenderer renderer) {
                    foreach (var kvp in bindPoseMap) {
                        Transform boneTrans = kvp.Key;
                        if (boneTrans != renderer.rootBone) {
                            Matrix4x4 bindPose = kvp.Value;

                            var parent = boneTrans.parent;
                            Undo.RecordObject(parent, "undo");
                            Matrix4x4 localMatrix = bindPoseMap.ContainsKey(parent)
                                ? (bindPose * bindPoseMap[parent].inverse).inverse
                                : bindPose.inverse;
                            Undo.RegisterCompleteObjectUndo(boneTrans, "Undo: " + boneTrans.name);
                            if (dialogSelection == 0) {
                                boneTrans.localPosition = localMatrix.MultiplyPoint(Vector3.zero);
                            }

                            boneTrans.localRotation = Quaternion.LookRotation(localMatrix.GetColumn(2), localMatrix.GetColumn(1));
                            boneTrans.localScale = new Vector3(
                                localMatrix.GetColumn(0).magnitude,
                                localMatrix.GetColumn(1).magnitude,
                                localMatrix.GetColumn(2).magnitude
                            );
                        }
                    }
                }
            }
        }

        private static Dictionary<Transform, Matrix4x4> createBindPoseMap(SkinnedMeshRenderer[] renderers) {
            Dictionary<Transform, Matrix4x4> bindPoseMap = new Dictionary<Transform, Matrix4x4>();

            foreach (var smr in renderers) {
                for (int i = 0; i < smr.bones.Length; i++) {
                    var smrBone = smr.bones[i];
                    if (smrBone != null && !bindPoseMap.ContainsKey(smrBone)) {
                        bindPoseMap.Add(smrBone, smr.sharedMesh.bindposes[i]);
                    }
                }
            }

            return bindPoseMap;
        }

        private SkinnedMeshRenderer[] getSkinnedMeshRenderers(SkinnedMeshRenderer skinnedMeshRenderer) {
            var parent = skinnedMeshRenderer.transform.parent;
            return parent != null
                ? parent.GetComponentsInChildren<SkinnedMeshRenderer>()
                : skinnedMeshRenderer.transform.GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        public void reset() {
            bindPoseMap.Clear();
            bindPoseMap = null;
        }
    }
}