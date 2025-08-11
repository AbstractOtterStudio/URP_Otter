using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class UnderWaterPreviewHelper : MonoBehaviour
{
    public Camera previewCamera { set; private get; }
    public bool isUnderwater { set; private get; }

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnPreRenderCam;
        RenderPipelineManager.endCameraRendering += OnPostRenderCam;
    }
    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnPreRenderCam;
        RenderPipelineManager.endCameraRendering -= OnPostRenderCam;
    }

    void OnPreRenderCam(ScriptableRenderContext context, Camera cam)
    {
        if (cam != previewCamera)
        {
            return;
        }

        if (isUnderwater)
        {
            EffectManager.SetUnderwaterEffectImmediate(true);
        }
    }

    void OnPostRenderCam(ScriptableRenderContext context, Camera cam)
    {
        if (cam != previewCamera)
        {
            return;
        }

        if (isUnderwater)
        {
            EffectManager.SetUnderwaterEffectImmediate(false);
        }
    }
}

public class GameCameraPreviewWindow : EditorWindow
{
    Camera _tempCam;
    RenderTexture _rt;
    UnderWaterPreviewHelper _underWaterPreviewHelper;

    Vector2 _aspectRatio = new Vector2(16, 10);
    float _orthoSizeScale = 1;
    bool _toggleUnderwaterPreview = false;

    [MenuItem("Window/游戏视角预览工具")]
    public static void ShowWindow()
    {
        var win = GetWindow<GameCameraPreviewWindow>("游戏视角预览工具");
        win.minSize = new Vector2(200, 120);
        win.Focus();
    }

    void OnEnable()
    {
        CreateTempCameraIfNeeded();
        EditorApplication.update += Repaint;
    }

    void OnDisable()
    {
        EditorApplication.update -= Repaint;
        Cleanup();
    }

    void CreateTempCameraIfNeeded()
    {
        if (_tempCam != null) return;

        var go = new GameObject("~OrthoPreview_TempCamera");
        go.hideFlags = HideFlags.HideAndDontSave;
        _tempCam = go.AddComponent<Camera>();
        _tempCam.enabled = false; // we render manually
        _underWaterPreviewHelper = _tempCam.gameObject.AddComponent<UnderWaterPreviewHelper>();
        _underWaterPreviewHelper.previewCamera = _tempCam;
    }

    void Cleanup()
    {
        if (_rt != null)
        {
            _rt.Release();
            DestroyImmediate(_rt);
            _rt = null;
        }
        if (_tempCam != null)
        {
            DestroyImmediate(_tempCam.gameObject);
            _tempCam = null;
        }
    }

    void EnsureRT(int w, int h)
    {
        var desiredAspect = _aspectRatio.y > 0 ? _aspectRatio.x / _aspectRatio.y : 0.0f;
        if (desiredAspect > 0.5f)
        {
            _tempCam.aspect = desiredAspect;
        }
        else
        {
            _tempCam.aspect = 16.0f / 9.0f;
        }

        w = Mathf.Max(1, w);
        h = Mathf.Max(1, h);

        if (_rt != null && (_rt.width != w || _rt.height != h))
        {
            _rt.Release();
            DestroyImmediate(_rt);
            _rt = null;
        }
        if (_rt == null)
        {
            _rt = new RenderTexture(w, h, 24, RenderTextureFormat.DefaultHDR)
            {
                name = "~OrthoPreview_RT",
                hideFlags = HideFlags.HideAndDontSave
            };
            _rt.Create();
        }
    }

    void CopySettingsFromSource(Camera src)
    {
        // Copy most settings in one go, then override projection
        if (src != null)
        {
            _tempCam.CopyFrom(src);
            // Ensure we don't accidentally render post FX to the SceneView RT
            _tempCam.targetTexture = null;
        }

        _tempCam.orthographic = true; // force ortho
    }

    void SyncFromSceneView()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null || sv.camera == null) return;

        // we use scene camera as a rough guide for the preview camera which follows the source camera's pitch
        // Step 1: match position
        _tempCam.transform.position = sv.camera.transform.position;

        // Step 2: extract yaw from guide camera
        Vector3 euler = _tempCam.transform.eulerAngles; // keep pitch & roll
        float guideYaw = sv.camera.transform.eulerAngles.y; // only yaw

        // Step 3: apply yaw, keep pitch & roll
        _tempCam.transform.rotation = Quaternion.Euler(euler.x, guideYaw, euler.z);

        _tempCam.orthographicSize = _tempCam.orthographicSize * _orthoSizeScale;
    }

    void OnGUI()
    {
        CreateTempCameraIfNeeded();

        var sv = SceneView.lastActiveSceneView;
        if (sv == null || sv.camera == null)
        {
            EditorGUILayout.HelpBox("Open a Scene view to drive the preview.", MessageType.Info);
            return;
        }

        using (new EditorGUILayout.VerticalScope())
        {
            _orthoSizeScale = EditorGUILayout.Slider("Ortho Size Scale", _orthoSizeScale, 0.5f, 2.0f);
            var prevToggleUnderwaterPreview = _toggleUnderwaterPreview;
            _toggleUnderwaterPreview = EditorGUILayout.Toggle("Underwater Preview", _toggleUnderwaterPreview);
            if (prevToggleUnderwaterPreview != _toggleUnderwaterPreview)
            {
                _underWaterPreviewHelper.isUnderwater = _toggleUnderwaterPreview;
            }
            _aspectRatio = EditorGUILayout.Vector2Field("Aspect Ratio", _aspectRatio);
        }

        EditorGUILayout.Space(3);

        float width = position.width;
        float aspect = _aspectRatio.y > 0 ? _aspectRatio.x / _aspectRatio.y : 0.0f;
        if (aspect < 0.5f)
        {
            aspect = 16.0f / 9.0f;
        }

        float height = width / aspect;

        var rect = GUILayoutUtility.GetRect(width, height);
        if (Event.current.type != EventType.Repaint)
        {
            // Draw a frame so it doesn’t vanish while resizing/laying out
            EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.25f));
            return;
        }

        // Prepare RT
        int w = Mathf.CeilToInt(rect.width);
        int h = Mathf.CeilToInt(rect.height);
        EnsureRT(w, h);

        if (Event.current.type == EventType.Repaint)
        {
            // Configure camera
            var src = Camera.main;
            CopySettingsFromSource(src);
            SyncFromSceneView();

            // Render
            _tempCam.targetTexture = _rt;
            var prevSceneCamRT = RenderTexture.active;
            RenderTexture.active = _rt;
            GL.Clear(true, true, src.backgroundColor);
            _tempCam.Render();
            RenderTexture.active = prevSceneCamRT;
            _tempCam.targetTexture = null;

            // Blit to window
            GUI.DrawTexture(rect, _rt, ScaleMode.ScaleToFit, false);


            // Little overlay
            var overlay = new Rect(rect.x + 8, rect.y + 8, rect.width - 16, 22);
            var label = $"Source: {src.name}  |  Ortho Size: {_tempCam.orthographicSize}";
            EditorGUI.DropShadowLabel(overlay, label);
        }
    }
}
