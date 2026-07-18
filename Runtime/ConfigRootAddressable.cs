using Cysharp.Threading.Tasks;
using SiPVLib.Debugging;
using SiPVLib.Utilities;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SiPVLib.Config
{
    [CreateAssetMenu(fileName = "ConfigRootAddressable", menuName = "SiPVLib/Config/ConfigRootAddressable")]
    public class ConfigRootAddressable : ConfigRoot
    {
        public override async UniTask<bool> Init()
        {
            // Init Addressable
            var initTryCount = 0;
            while (true)
            {
                var handler = UnityEngine.AddressableAssets.Addressables.InitializeAsync();

                while (handler.Status != AsyncOperationStatus.Failed && handler.Status != AsyncOperationStatus.Succeeded)
                {
                    await UniTask.Delay(100);
                }

                if (handler.Status == AsyncOperationStatus.Succeeded)
                {
                    break;
                }

                // Check for retry
                CustomLog.LogError($"[ConfigRootAddressable] Addressables initialization failed on attempt {initTryCount + 1}. Retrying...");
                await UniTask.Delay(OtherUtils.GetRetryTime(initTryCount));
                initTryCount++;
            }
            
            return await base.Init();
        }
    }
}