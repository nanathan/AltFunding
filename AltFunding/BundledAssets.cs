using System.Collections;
using System.IO;
using UnityEngine;

namespace AltFunding
{
    class BundledAssets : MonoBehaviour
    {
        public GameObject mainWindowPrefab;
        public GameObject budgetRowPrefab;

        public GameObject settingsWindowPrefab;
        public GameObject settingsRowPrefab;

        private AssetBundle bundle;

        public bool Loaded { get; private set; }

        void Start()
        {
            StartCoroutine(LoadBundledAssets());
        }

        internal IEnumerator LoadBundledAssets()
        {
            using(WWW www = new WWW("file://" + KSPUtil.ApplicationRootPath + Path.DirectorySeparatorChar + "GameData"
                    + Path.DirectorySeparatorChar + "AltFunding" + Path.DirectorySeparatorChar + "altfunding.ksp"))
            {
                Debug.Log("[AltFunding] Loading bundle assets...");

                yield return www;

                bundle = www.assetBundle;

                Debug.Log("[AltFunding] Loading GameObject assets from AssetBundle");

                mainWindowPrefab = bundle.LoadAsset<GameObject>("AltFundingPanel");
                budgetRowPrefab = mainWindowPrefab.GetChild("ScrollView").GetChild("Viewport").GetChild("Content").GetChild("ContentRow");

                settingsWindowPrefab = bundle.LoadAsset<GameObject>("AltFundingSettingsPanel");
                settingsRowPrefab = settingsWindowPrefab.GetChild("ScrollView").GetChild("Viewport").GetChild("Content").GetChild("SettingsRow");

                Loaded = true;

                Debug.Log("[AltFunding] Finished loading assets from AssetBundle");
            }
        }

        void OnDestroy()
        {
            if(bundle != null)
            {
                bundle.Unload(true);
            }
        }
    }
}
