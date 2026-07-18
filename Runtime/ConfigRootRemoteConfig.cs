using Cysharp.Threading.Tasks;
using SiPVLib.Config.RemoteConfig;
using UnityEngine;

namespace SiPVLib.Config
{
    [CreateAssetMenu(fileName = "ConfigRootRemoteConfig", menuName = "SiPVLib/Config/ConfigRootRemoteConfig")]
    public class ConfigRootRemoteConfig : ConfigRoot
    {
        public override async UniTask<bool> Init()
        {
            // Init Remote Config Manager
            await RemoteConfigManager.Instance.Init();
            
            return await base.Init();
        }
    }
}