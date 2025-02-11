using UnityEngine;
using UnityEngine.Rendering;

namespace FluffyGroomingTool {
    public static class DefaultMaterialLoader {
        public static void loadDefaultMaterial(
            out bool isHdrp,
            out bool isUrp,
            ref Material material,
            out Material windMaterial,
            ref Material motionVectorMaterial,
            string materialPostfix,
            Renderer renderer,
            bool isHairRenderer = false
        ) {
            isHdrp = DefaultMaterialLoader.isHdrp();
            isUrp = DefaultMaterialLoader.isUrp();
            windMaterial = null;
#if UNITY_EDITOR
            if (!isHdrp) windMaterial = errorCheckMaterial("Hidden/WindContribution");
#endif
            if (isHdrp) {
                if (material == null) {
#if HAS_PACKAGE_UNITY_HDRP_15_0_7
                    material = isHairRenderer
                        ? Object.Instantiate(Resources.Load<Material>("HDRPDefaultAlembic"))
                        : createMaterial("HDRPDefaultFur" + materialPostfix, BASE_COLOR, BASE_COLOR_MAP, renderer);
#else
                    material = createMaterial("HDRPDefaultFur" + materialPostfix, BASE_COLOR, BASE_COLOR_MAP, renderer);
#endif
                }

                motionVectorMaterial = motionVectorMaterial ? motionVectorMaterial : Resources.Load<Material>("FurMotionVector");
#if UNITY_EDITOR
                windMaterial = errorCheckMaterial("Hidden/HDRPWindContribution");
#endif
            } else if (isUrp) {
                if (material == null) material = createMaterial("URPDefaultFur" + materialPostfix, MAIN_COLOR, MAIN_COLOR_TEXTURE, renderer);
            } else {
                motionVectorMaterial =
                    motionVectorMaterial ? motionVectorMaterial : errorCheckMaterial("Hidden/BuiltInMotionVectorOnly");
                if (material == null) material = createMaterial("BIRPDefaultFur" + materialPostfix, MAIN_COLOR_B, MAIN_COLOR_TEXTURE_B, renderer);
            }
        }

        public static bool isUrp() {
            return GraphicsSettings.currentRenderPipeline && GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("Universal");
        }

        public static bool isHdrp() {
            return GraphicsSettings.currentRenderPipeline &&
                   GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("HighDefinition");
        }

        private static Material errorCheckMaterial(string sharedPath) {
            var shader = Shader.Find(sharedPath);
            if (shader == null) {
                Debug.LogError(
                    "Could not find the Fluffy shaders for your rendering pipeline. Please import the Shaders from unityPackage in FluffyGroomingTool/Shader/");
                return null;
            }

            return new Material(shader);
        }

        private static Material createMaterial(string name, int colorName, int textureName, Renderer renderer) {
            var material = Object.Instantiate(Resources.Load<Material>(name));

            var sourceMaterialTexture = tryToGetSourceTexture(renderer);
            if (sourceMaterialTexture != null) material.SetTexture(textureName, sourceMaterialTexture);

            var sourceMaterialColor = tryToGetSourceColor(renderer);
            if (sourceMaterialColor != null) material.SetColor(colorName, (Color)sourceMaterialColor);
            return material;
        }

        private static readonly int MAIN_TEX = Shader.PropertyToID("_MainTex");
        private static readonly int BASE_COLOR_MAP = Shader.PropertyToID("_BaseColorMap");
        private static readonly int MAIN_COLOR_TEXTURE = Shader.PropertyToID("_MainColorTexture");
        private static readonly int MAIN_COLOR_TEXTURE_B = Shader.PropertyToID("_MainColorTextureB");

        private static readonly int COLOR = Shader.PropertyToID("_Color");
        private static readonly int BASE_COLOR = Shader.PropertyToID("_BaseColor");
        private static readonly int MAIN_COLOR = Shader.PropertyToID("_MainColor");
        private static readonly int MAIN_COLOR_B = Shader.PropertyToID("_MainColorB");

        private static Texture tryToGetSourceTexture(Renderer renderer) {
            if (renderer == null) return null;
            var material = renderer.sharedMaterial;
            if (material != null) {
                if (material.HasTexture(MAIN_TEX)) return material.GetTexture(MAIN_TEX);
                if (material.HasTexture(BASE_COLOR_MAP)) return material.GetTexture(BASE_COLOR_MAP);
                if (material.HasTexture(MAIN_COLOR_TEXTURE)) return material.GetTexture(MAIN_COLOR_TEXTURE);
                if (material.HasTexture(MAIN_COLOR_TEXTURE_B)) return material.GetTexture(MAIN_COLOR_TEXTURE_B);
            }

            return null;
        }

        private static Color? tryToGetSourceColor(Renderer renderer) {
            if (renderer == null) return null;
            var material = renderer.sharedMaterial;
            if (material != null) {
                if (material.HasColor(COLOR)) return material.GetColor(COLOR);
                if (material.HasColor(BASE_COLOR)) return material.GetColor(BASE_COLOR);
                if (material.HasColor(MAIN_COLOR)) return material.GetColor(MAIN_COLOR);
                if (material.HasColor(MAIN_COLOR_B)) return material.GetColor(MAIN_COLOR_B);
            }

            return null;
        }
    }
}