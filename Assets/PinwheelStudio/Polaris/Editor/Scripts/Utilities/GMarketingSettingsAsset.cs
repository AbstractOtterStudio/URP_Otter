#if GRIFFIN
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Object = UnityEngine.Object;
using UnityEngine.Networking;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

namespace Pinwheel.Griffin
{
    [System.Serializable]
    [ExecuteInEditMode]
    //[CreateAssetMenu(menuName = "Polaris Internal/Marketing Settings Asset")]
    public class GMarketingSettingsAsset : ScriptableObject
    {
        [System.Serializable]
        public class NewsEntry
        {
            public string title;
            public string description;
            public string link;
        }

        [System.Serializable]
        public class ListNewsResponse
        {
            public NewsEntry[] entries;
        }

        private NewsEntry[] m_news;

        [System.Serializable]
        public class AssetEntry : IDisposable
        {
            public string imageUrl;
            public string name;
            public string description;
            public string link;
            public string assemblyName;
            public string promotionText;

            public Texture2D texture { get; private set; }
            public bool isInstalled { get; private set; }

            public void Init()
            {
                Dispose();
                EditorCoroutineUtility.StartCoroutine(IDownloadImage(), this);
                isInstalled = string.IsNullOrEmpty(assemblyName) ? false : GEditorCommon.HasAssembly(assemblyName);
            }

            private IEnumerator IDownloadImage()
            {
                if (string.IsNullOrEmpty(imageUrl))
                    yield break;
                UnityWebRequest r = UnityWebRequestTexture.GetTexture(imageUrl);
                yield return r.SendWebRequest();
                if (r.result == UnityWebRequest.Result.Success)
                {
                    texture = (r.downloadHandler as DownloadHandlerTexture).texture;
                }
                r.Dispose();
            }

            public void Dispose()
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        [System.Serializable]
        public class ListModulesResponse
        {
            public AssetEntry[] entries;
        }

        private AssetEntry[] m_featuredAssets;

        private static GMarketingSettingsAsset instance;
        public static GMarketingSettingsAsset Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<GMarketingSettingsAsset>("MarketingSettingsAsset");
                    if (instance == null)
                    {
                        instance = ScriptableObject.CreateInstance<GMarketingSettingsAsset>();
                    }
                }
                return instance;
            }
        }

        private void OnEnable()
        {
            Init();
        }

        private void OnDisable()
        {
            CleanUp();
        }

        public void Init()
        {
            EditorCoroutineUtility.StartCoroutine(IListNews(), this);
            EditorCoroutineUtility.StartCoroutine(IListFeaturedAssets(), this);
        }

        private IEnumerator IListNews()
        {
            string url = "https://api.pinwheelstud.io/news";
            UnityWebRequest r = UnityWebRequest.Get(url);
            yield return r.SendWebRequest();
            ListNewsResponse response = new ListNewsResponse();
            EditorJsonUtility.FromJsonOverwrite(r.downloadHandler.text, response);
            m_news = response.entries;
        }

        private IEnumerator IListFeaturedAssets()
        {
            string url = "https://api.pinwheelstud.io/polaris/3000/other-products";
            UnityWebRequest r = UnityWebRequest.Get(url);
            yield return r.SendWebRequest();
            if (r.result == UnityWebRequest.Result.Success)
            {
                ListModulesResponse response = new ListModulesResponse();
                EditorJsonUtility.FromJsonOverwrite(r.downloadHandler.text, response);
                m_featuredAssets = response.entries;
                foreach (AssetEntry e in m_featuredAssets)
                {
                    e.Init();
                }
            }
        }

        public void CleanUp()
        {
            if (m_featuredAssets != null)
            {
                foreach (AssetEntry e in m_featuredAssets)
                {
                    e.Dispose();
                }
                m_featuredAssets = null;
            }
        }

        internal AssetEntry[] GetFeaturedAssets()
        {
            if (m_featuredAssets != null)
            {
                return m_featuredAssets;
            }
            else
            {
                return new AssetEntry[0];
            }
        }

        internal NewsEntry[] GetNews()
        {
            if (m_news != null)
            {
                return m_news;
            }
            else
            {
                return new NewsEntry[0];
            }
        }

        internal List<NewsEntry> GetSpecialNews()
        {
            List<NewsEntry> news = new List<NewsEntry>(GetNews());
            news.RemoveAll(n => !n.description.Contains("#p3"));
            return news;
        }
    }
}
#endif