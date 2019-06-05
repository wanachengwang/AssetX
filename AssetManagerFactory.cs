using UnityEngine;
using System.Collections;

namespace AssetX
{
    public class AssetManager
    {
        class AssetManagerFactory
        {
            static public IAssetManager CreateInstance(System.Object mode = null)
            {
#if UNITY_EDITOR
                return new NativeAssetManager();
#else
                return new AssetBundleManager();
#endif
            }
        }

        static IAssetManager instance;
        public static IAssetManager Instance
        {
            get
            {
                if (instance == null)
                    instance = AssetManagerFactory.CreateInstance();
                return instance;
            }
        }
    }
}