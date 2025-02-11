using System;
using System.Linq;
using FluffyGroomingTool;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif
[PreferBinarySerialization]
public class GroomContainer : ScriptableObject {
    /**
    * The scale of the Transform at the time the FurCreator & FurRenderer was added.
    */
    [SerializeField] public float worldScale = 1f;


    [SerializeField] public GroomLayer[] layers;
    [SerializeField] public int activeLayerIndex;
    [SerializeField] private PainterProperties _painterProperties;

    public PainterProperties PainterProperties {
        get {
            if (_painterProperties == null) _painterProperties = CreateInstance<PainterProperties>();
            return _painterProperties;
        }
        set => _painterProperties = value;
    }

    public bool needsUpdate;
    public bool isUsingCardPreset;

    public void update() {
        if (needsUpdate && getActiveLayer().strandsGroomOneToOne.Length > 0) {
            recreateGroomBuffer();
            needsUpdate = false;
        }

        foreach (var layer in layers) {
            foreach (var modifier in layer.clumpModifiers) {
                modifier.update();
            }
        }
    }

    private void recreateGroomBuffer() {
        foreach (var layer in layers) {
            layer.recreateGroomBuffer();
        }
    }

    public GroomContainer() {
        if (layers != null) return;
        layers = new GroomLayer[1];
        layers[0] = new GroomLayer();
    }

    public void invalidate() {
        foreach (var layer in layers) {
            foreach (var modifier in layer.clumpModifiers) {
                modifier.needsUpdate = true;
            }
        }

        needsUpdate = true;
    }

    public GroomLayer getActiveLayer() {
        activeLayerIndex = Mathf.Clamp(activeLayerIndex, 0, layers.Length - 1);
        return layers[activeLayerIndex];
    }

    public void addNewLayer() {
        var list = layers.ToList();
        var newLayer = new GroomLayer() {
            randomRotation = getActiveLayer().randomRotation,
            randomHeight = getActiveLayer().randomHeight
        };
        list.Add(newLayer);
        layers = list.ToArray();
        if (isUsingCardPreset) newLayer.setCardPreset();


        activeLayerIndex = layers.Length - 1;
    }

    public void copyValuesFromComputeBufferToNativeObject() {
        foreach (var t in layers) {
            t.copyValuesFromComputeBufferToNativeObject();
        }

        setDirty();
    }

    public void setDirty() {
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(PainterProperties);
#endif
    }

    public void removeLayer(int index) {
        layers[index].disposeBuffers();
        var list = layers.ToList();
        list.RemoveAt(index);
        layers = list.ToArray();
        activeLayerIndex = Math.Min(activeLayerIndex, activeLayerIndex - 1);
    }

    public void disposeBuffers() {
        foreach (var layer in layers) {
            layer.disposeBuffers();
        }
    }

    public bool isFirstLayerStrandBufferInizialized() {
        return layers[0].strandsGroomBuffer != null;
    }

    public ComputeBuffer getLayerGroomBuffer(int index) {
        return layers[index].strandsGroomBuffer;
    }

    public GroomLayer getLayerById(string entryID) {
        return layers.FirstOrDefault(layer => entryID.Equals(layer.id));
    }

    public void duplicateLayer(int index) {
#if UNITY_EDITOR
        EditorUtility.DisplayProgressBar("Adding Layer", "Hang In There..", 0.2f);
        EditorUtility.ClearProgressBar();
#endif
        var list = layers.ToList();
        var newLayer = (GroomLayer) layers[index].Clone();
        newLayer.strandsGroomBuffer = null;
        list.Add(newLayer);
        layers = list.ToArray();
        if (isUsingCardPreset) newLayer.setCardPreset();
        activeLayerIndex = layers.Length - 1;
    }

    public bool hasHiddenLayers() {
        foreach (var layer in layers) {
            if (layer.isHidden) {
                return true;
            }
        }

        return false;
    }

    public void clearApplyTextureState() {
        foreach (var layer in layers) {
            layer.isInApplyTextureMode = false;
        }
    }
}

public enum PaintType {
    DIRECTION_ROOT = 0,
    HEIGHT,
    RAISE,
    MASK,
    ADD_FUR,
    DELETE_FUR,
    WIND_MAX_DISTANCE,
    CLUMPING_MASK,
    DIRECTION_BEND,
    DIRECTION_ORIENTATION,
    TWIST,
    RESET,
    ATTRACT,
    SMOOTH,
    WIDTH,
    COLOR_OVERRIDE
}