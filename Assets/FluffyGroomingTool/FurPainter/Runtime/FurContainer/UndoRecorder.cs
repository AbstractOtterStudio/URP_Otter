using System;
using System.Collections.Generic;
using System.Linq;
using FluffyGroomingTool;
using UnityEngine;

[Serializable]
public class UndoRecorder {
    private static readonly int MAX_HISTORY_ENTRIES = 10;

    [SerializeField] public int undoIndex;
    private IDictionary<int, List<GroomUndoEntry>> undoEntries = new Dictionary<int, List<GroomUndoEntry>>();

    public void appendUndo(GroomContainer groomContainer) {
        if (groomContainer == null) return;
       
        undoEntries.Remove(undoIndex);
        
        var groomUndoEntries = groomContainer.PainterProperties.isGroomAllLayerAtOnce
            ? groomContainer.layers.Select(createEntry).ToList()
            : new GroomUndoEntry[] {createEntry(groomContainer.getActiveLayer())}.ToList();

        undoEntries.Add(undoIndex, groomUndoEntries);

        undoIndex += 1;
        if (undoIndex > MAX_HISTORY_ENTRIES) {
            undoEntries.Remove(undoIndex - MAX_HISTORY_ENTRIES);
        }
    }

    private static GroomUndoEntry createEntry(GroomLayer activeLayer) {
        return new GroomUndoEntry {
            strandGroom = (StrandGroom[]) activeLayer.strandsGroomOneToOne.Clone(),
            clumpModifiers = activeLayer.clumpModifiers.Select(clumpModifier => clumpModifier.strandsGroom).ToArray(),
            id = activeLayer.id,
            modifiersIds = activeLayer.clumpModifiers.Select(clumpModifier => clumpModifier.id).ToArray()
        };
    }

    public void undoCallback(GroomContainer groomContainer) {
        if (undoEntries.ContainsKey(undoIndex)) {
            var entries = undoEntries[undoIndex];
            foreach (var entry in entries) {
                var layer = groomContainer.getLayerById(entry.id);
                if (layer != null) {
                    layer.strandsGroomOneToOne = entry.strandGroom;
                    groomContainer.needsUpdate = true;

                    for (var index = 0; index < entry.modifiersIds.Length; index++) {
                        var modifiersId = entry.modifiersIds[index];
                        ClumpGroomModifier modifier = layer.getModifierById(modifiersId);
                        modifier.strandsGroom = entry.clumpModifiers[index];
                        modifier.needsUpdate = true;
                    }
                }
            }
        }
    }
}

class GroomUndoEntry {
    public String id;
    public StrandGroom[] strandGroom;
    public StrandGroom[][] clumpModifiers;
    public String[] modifiersIds;
}