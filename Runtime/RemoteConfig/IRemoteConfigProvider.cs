using Cysharp.Threading.Tasks;

namespace SiPVLib.Config.RemoteConfig
{
    public interface IRemoteConfigProvider
    {
        public UniTask Init();
        public UniTask FetchConfigs();
        public string GetJson(string key);
        public T GetValue<T>(string key);
    }
}