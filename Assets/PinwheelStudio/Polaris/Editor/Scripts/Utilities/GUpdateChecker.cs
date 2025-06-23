#if GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEditor;
using System;
using Unity.EditorCoroutines.Editor;

namespace Pinwheel.Griffin
{
    public class GUpdateChecker
    {
        [System.Serializable]
        private class VersionResponse
        {
            public int major;
            public int minor;
            public int patch;
        }

        private static readonly string PREF_PREFIX = "polaris3-check-update-";

        internal static bool CheckedToday()
        {
            string dateString = DateTime.Now.ToString("yyyy-MM-dd");
            return EditorPrefs.HasKey(PREF_PREFIX + dateString);
        }

        internal static void CheckForUpdate()
        {
            string dateString = DateTime.Now.ToString("yyyy-MM-dd");
            EditorPrefs.SetBool(PREF_PREFIX + dateString, true);
            EditorCoroutineUtility.StartCoroutineOwnerless(ICheckForUpdate());
        }

        private static IEnumerator ICheckForUpdate()
        {
            string url = "https://api.pinwheelstud.io/polaris/3000/version-info";
            UnityWebRequest r = UnityWebRequest.Get(url);
            yield return r.SendWebRequest();
            if (r.result == UnityWebRequest.Result.Success)
            {
                VersionResponse response = new VersionResponse();
                EditorJsonUtility.FromJsonOverwrite(r.downloadHandler.text, response);

                if (response.major > GVersionInfo.Major ||
                    response.minor > GVersionInfo.Minor ||
                    response.patch > GVersionInfo.Patch)
                {
                    Debug.Log($"POLARIS: New version {response.major}.{response.minor}.{response.patch} is available, please update the asset.");
                }
            }
        }
    }
}
#endif
