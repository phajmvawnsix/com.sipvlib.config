#if FIREBASE_REMOTE_CONFIG
using System;
using System.Globalization;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.RemoteConfig;
using SiPVLib.Debugging;
using SiPVLib.Utilities;
using UnityEngine;

namespace SiPVLib.Config.RemoteConfig
{
    /// <summary>
    /// Requires the Firebase RemoteConfig SDK and the FIREBASE_REMOTE_CONFIG scripting define;
    /// see <see cref="Init"/> in the #else branch for the stub used otherwise.
    /// </summary>
    public class FirebaseRemoteConfigProvider : IRemoteConfigProvider
    {
        public async UniTask Init()
        {
            DependencyStatus deps;
            var retryCount = 0;
            do
            {
                deps = await FirebaseApp.CheckAndFixDependenciesAsync();

                if (deps != DependencyStatus.Available)
                {
                    var millisecondsDelay = OtherUtils.GetRetryTime(retryCount);
                    CustomLog.LogError($"Firebase dependencies are not available: {deps}. Retrying in {millisecondsDelay} ms...");
                    await UniTask.Delay(millisecondsDelay);
                    retryCount++;
                }
            } while (deps != DependencyStatus.Available);
        }

        public async UniTask FetchConfigs()
        {
            // Fetch and activate the latest config values.
            // FetchAndActivateAsync returns a bool indicating whether the activated values were fetched from the server.
            var activated = await FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync();
            
            if (activated)
            {
                CustomLog.Log("Remote config values fetched and activated successfully.");
            }
            else
            {
                CustomLog.LogWarning("Remote config values were not fetched from the server. Using cached or default values.");
            }
            
            await UniTask.Yield();
        }

        public string GetJson(string key)
        {
            var val = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
            return val.StringValue ?? string.Empty;
        }

        public T GetValue<T>(string key)
        {
            var json = GetJson(key);

            if (typeof(T) == typeof(string))
            {
                return (T)(object)json;
            }

            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            try
            {
                var type = typeof(T);

                if (type == typeof(int))
                {
                    if (int.TryParse(json, NumberStyles.Any, CultureInfo.InvariantCulture, out var i)) return (T)(object)i;
                    if (float.TryParse(json, NumberStyles.Any, CultureInfo.InvariantCulture, out var fi)) return (T)(object)Convert.ToInt32(fi);
                }

                if (type == typeof(long))
                {
                    if (long.TryParse(json, NumberStyles.Any, CultureInfo.InvariantCulture, out var l)) return (T)(object)l;
                }

                if (type == typeof(float))
                {
                    if (float.TryParse(json, NumberStyles.Any, CultureInfo.InvariantCulture, out var f)) return (T)(object)f;
                }

                if (type == typeof(double))
                {
                    if (double.TryParse(json, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return (T)(object)d;
                }

                if (type == typeof(bool))
                {
                    if (bool.TryParse(json, out var b)) return (T)(object)b;
                    // sometimes bool are represented as 0/1
                    if (int.TryParse(json, out var ib)) return (T)(object)(ib != 0);
                }

                // For complex objects, assume JSON and use Unity's JsonUtility
                return JsonUtility.FromJson<T>(json);
            }
            catch
            {
                return default;
            }
        }
    }
}
#else
using Cysharp.Threading.Tasks;
using SiPVLib.Debugging;

namespace SiPVLib.Config.RemoteConfig
{
    /// <summary>
    /// Stub used when the Firebase RemoteConfig SDK isn't installed (FIREBASE_REMOTE_CONFIG not
    /// defined). Add the Firebase RemoteConfig SDK and define FIREBASE_REMOTE_CONFIG in
    /// Player Settings > Scripting Define Symbols to enable the real implementation.
    /// </summary>
    public class FirebaseRemoteConfigProvider : IRemoteConfigProvider
    {
        public UniTask Init()
        {
            CustomLog.LogWarning("[FirebaseRemoteConfigProvider] Firebase RemoteConfig SDK not installed. Add FIREBASE_REMOTE_CONFIG to Scripting Define Symbols to enable.");
            return UniTask.CompletedTask;
        }

        public UniTask FetchConfigs() => UniTask.CompletedTask;

        public string GetJson(string key) => string.Empty;

        public T GetValue<T>(string key) => default;
    }
}
#endif