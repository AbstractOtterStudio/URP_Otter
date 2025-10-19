#if GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Griffin.SplineTool;

namespace Pinwheel.Griffin
{
#if UNITY_EDITOR
    public static class GLayerInitializer
    {
        public static void SetupRaycastLayer()
        {
            int index = GEditorSettings.Instance.layers.raycastLayerIndex;
            string layer = GStylizedTerrain.RAYCAST_LAYER;
            if (GEditorSettings.Instance.layers.SetupLayer(index, layer))
            {
                Debug.Log($"POLARIS: Set layer {index} to {layer}. This layer is reserved for the terrain to work! You can change this in ProjectSettings/ Polaris/ Editor/ Layers");
            }
        }

        public static void SetupSplineLayer()
        {
            int index = GEditorSettings.Instance.layers.splineLayerIndex;
            string layer = GSplineCreator.SPLINE_LAYER;
            if (GEditorSettings.Instance.layers.SetupLayer(index, layer))
            {
                Debug.Log($"POLARIS: Set layer {index} to {layer}. This layer is reserved for Spline tool to work! You can change this in ProjectSettings/ Polaris/ Editor/ Layers");
            }
        }
    }
#endif
}
#endif