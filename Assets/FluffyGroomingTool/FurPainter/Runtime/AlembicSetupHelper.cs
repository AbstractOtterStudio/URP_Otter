using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.Formats.Alembic.Importer;
#endif
namespace FluffyGroomingTool {
    public class AlembicSetupHelper : MonoBehaviour {
        public GameObject sourceGameObject;
#if UNITY_EDITOR
        public AlembicStreamPlayer alembicFile;
#endif
        public bool dontSkin;
        public Vector3 alembicFileRotation; 
    }
}