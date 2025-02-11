using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluffyGroomingTool {
    [Serializable]
    public class GroomLayer : ICloneable {
        [SerializeField] [Range(0f, 0.5f)] public float distanceBetweenStrands = 0.0007f;
        [SerializeField] [Range(0f, 1f)] public float minHeight;
        [SerializeField] [Range(0f, 4f)] public float maxHeight = 2f;
        [SerializeField] [Range(0f, 1f)] public float minWidth;
        [SerializeField] [Range(0f, 4f)] public float maxWidth = 0.021f;
        [SerializeField] [Range(0, 1f)] public float randomRotation = 1f;
        [SerializeField] public float randomHeight = 1f;

        [SerializeField, HideInInspector] public ClumpGroomModifier[] clumpModifiers = new ClumpGroomModifier[0];
        [SerializeField, HideInInspector] public StrandGroom[] strandsGroomOneToOne;
        [SerializeField, HideInInspector] public StrandGroom[] permanentlyDeletedGrooms;
        [SerializeField, HideInInspector] public HairStrand[] permanentlyDeletedStrands;
        [SerializeField, HideInInspector] public bool isHidden;
        [SerializeField, HideInInspector] public Texture2D maskTexture;
        public bool isInApplyTextureMode { get; set; }
        public ComputeBuffer strandsGroomBuffer;
        public string id = Guid.NewGuid().ToString();

        public GroomLayer() {
            if (strandsGroomOneToOne == null) strandsGroomOneToOne = new StrandGroom[0];
        }

        public object Clone() {
            return new GroomLayer {
                distanceBetweenStrands = distanceBetweenStrands,
                minHeight = minHeight,
                maxHeight = maxHeight,
                minWidth = minWidth,
                maxWidth = maxWidth,
                randomRotation = randomRotation,
                randomHeight = randomHeight,
                clumpModifiers = clumpModifiers.Select(e => (ClumpGroomModifier) e.Clone()).ToArray(),
                strandsGroomOneToOne = strandsGroomOneToOne,
                permanentlyDeletedGrooms = permanentlyDeletedGrooms,
                permanentlyDeletedStrands = permanentlyDeletedStrands,
                isHidden = isHidden,
                maskTexture = maskTexture
            };
        }

        public void addClumpModifier() {
            var clumpSpacing = 0.05f;

            if (clumpModifiers.Length > 0) {
                clumpSpacing = clumpModifiers[clumpModifiers.Length - 1].clumpsSpacing / 2f;
            }

            var list = clumpModifiers.ToList();
            list.Add(new ClumpGroomModifier() {clumpsSpacing = clumpSpacing});
            clumpModifiers = list.ToArray();
        }

        public void copyValuesFromComputeBufferToNativeObject() {
            foreach (var modifier in clumpModifiers) {
                modifier.copyValuesFromComputeBufferToNativeObject();
            }

            if (strandsGroomBuffer != null) AsyncGPUReadback.Request(strandsGroomBuffer, OnCompleteReadback);
        }

        void OnCompleteReadback(AsyncGPUReadbackRequest request) {
            if (request.hasError) {
                Debug.Log("GPU readback error detected for Strands.");
                return;
            }

            var persistentArray = request.ToPersistentArray<StrandGroomStruct>();
            new Thread(() => {
                strandsGroomOneToOne = persistentArray.Select(StrandGroom.convertFromStruct).ToArray();
                persistentArray.Dispose();
            }).Start();
        }

        public void removeClumpModifier(int index) {
            clumpModifiers[index].clumpGroomBuffer.Dispose();
            var list = clumpModifiers.ToList();
            list.RemoveAt(index);
            clumpModifiers = list.ToArray();
        }

        public void recreateGroomBuffer() {
            strandsGroomBuffer?.Dispose();
            strandsGroomBuffer = new ComputeBuffer(strandsGroomOneToOne.Length, StrandGroomStruct.size());
            strandsGroomBuffer.SetData(strandsGroomOneToOne.ToList().Select(StrandGroomStruct.convertToStruct).ToArray());
        }

        public void disposeBuffers() {
            strandsGroomBuffer?.Dispose();
            strandsGroomBuffer = null;
            foreach (var modifier in clumpModifiers) {
                modifier.clumpGroomBuffer?.Dispose();
                modifier.clumpGroomBuffer = null;
            }
        }

        public ClumpGroomModifier getModifierById(string modifiersId) {
            return clumpModifiers.FirstOrDefault(modifier => modifiersId.Equals(modifier.id));
        }

        public void setCardPreset() {
            distanceBetweenStrands = 0.007f;
            maxWidth = 0.19f;
            randomRotation = 0f;
        }

        public StrandGroom[] getExistingGroomsAndPermanentlyDeleted() {
            var allGrooms = new List<StrandGroom>();
            allGrooms.AddRange(strandsGroomOneToOne);
            if (permanentlyDeletedGrooms != null && permanentlyDeletedGrooms.Length > 0) {
                allGrooms.AddRange(permanentlyDeletedGrooms);
            }

            return allGrooms.ToArray();
        }

        public void clearPermanentlyDeletedBackup() {
            permanentlyDeletedGrooms = null;
            permanentlyDeletedStrands = null;
        }

        public List<StrandGroom> getPermanentlyDeletedGrooms() {
            return permanentlyDeletedGrooms != null ? permanentlyDeletedGrooms.ToList() : new List<StrandGroom>();
        }

        public List<HairStrand> getPermanentlyDeletedStrands() {
            return permanentlyDeletedStrands != null ? permanentlyDeletedStrands.ToList() : new List<HairStrand>();
        }

        private float _tempDistanceBetweenStrands = -1;

        public float TempDistanceBetweenStrands {
            get {
                if (_tempDistanceBetweenStrands == -1f) _tempDistanceBetweenStrands = distanceBetweenStrands;
                return _tempDistanceBetweenStrands;
            }
            set => _tempDistanceBetweenStrands = value;
        }

        public static readonly float FLOAT_COMPARISON_TOLERANCE = 0.000000001f;

        public bool isDistanceBetweenStrandsUndoPerformed() {
            return Math.Abs(TempDistanceBetweenStrands - distanceBetweenStrands) > FLOAT_COMPARISON_TOLERANCE;
        }

        public bool hasClumpLayer() {
            return clumpModifiers.Length > 0;
        }
    }
}