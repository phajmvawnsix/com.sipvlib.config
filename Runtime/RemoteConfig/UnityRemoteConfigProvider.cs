using System;
using Cysharp.Threading.Tasks;
using SiPVLib.Debugging;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;

namespace SiPVLib.Config.RemoteConfig
{
    /// <summary>
    /// Implements IRemoteConfigProvider using Unity's Remote Config service.
    /// Connects to Unity Gaming Services to fetch and provide remote configuration.
    /// </summary>
    public class UnityRemoteConfigProvider : IRemoteConfigProvider
    {
        // Define a simple empty struct for when no custom attributes are needed
        private struct EmptyAttributes { }

        /// <summary>
        /// Initializes the Unity Remote Config service.
        /// Authenticates the user anonymously if not already signed in.
        /// </summary>
        /// <returns>A UniTask representing the initialization operation.</returns>
        public async UniTask Init()
        {
            // initialize handlers for unity game services
            await UnityServices.InitializeAsync();

            // options can be passed in the initializer, e.g if you want to set AnalyticsUserId or an EnvironmentName use the lines from below:
            // var options = new InitializationOptions()
            // .SetEnvironmentName("testing")
            // .SetAnalyticsUserId("test-user-id-12345");
            // await UnityServices.InitializeAsync(options);

            // remote config requires authentication for managing environment information
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        /// <summary>
        /// Fetches the latest configuration settings from the remote server.
        /// </summary>
        /// <returns>A UniTask representing the fetch operation.</returns>
        public async UniTask FetchConfigs()
        {
            // Fetch configuration with empty user and app attributes.
            // Pass custom structs if you need to use segmentation rules based on attributes.
            await RemoteConfigService.Instance.FetchConfigsAsync(new EmptyAttributes(), new EmptyAttributes());
        }

        /// <summary>
        /// Retrieves the raw JSON string value for a given key.
        /// </summary>
        /// <param name="key">The identifier for the configuration setting.</param>
        /// <returns>The JSON string associated with the key.</returns>
        public string GetJson(string key)
        {
            // Use the appConfig object to retrieve the JSON string
            return RemoteConfigService.Instance.appConfig.GetJson(key);
        }

        /// <summary>
        /// Retrieves a strongly typed value for a given key.
        /// Supports primitive types (int, float, bool, string, long) and JSON-serializable objects.
        /// </summary>
        /// <typeparam name="T">The type to cast the value to.</typeparam>
        /// <param name="key">The identifier for the configuration setting.</param>
        /// <returns>The value of type T.</returns>
        public T GetValue<T>(string key)
        {
            // Handle primitive types directly using Remote Config's API
            if (typeof(T) == typeof(bool))
                return (T)(object)RemoteConfigService.Instance.appConfig.GetBool(key);

            if (typeof(T) == typeof(int))
                return (T)(object)RemoteConfigService.Instance.appConfig.GetInt(key);

            if (typeof(T) == typeof(float))
                return (T)(object)RemoteConfigService.Instance.appConfig.GetFloat(key);

            if (typeof(T) == typeof(string))
                return (T)(object)RemoteConfigService.Instance.appConfig.GetString(key);

            if (typeof(T) == typeof(long))
                return (T)(object)RemoteConfigService.Instance.appConfig.GetLong(key);

            // For complex types, attempt to deserialize from JSON
            var json = RemoteConfigService.Instance.appConfig.GetJson(key);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonUtility.FromJson<T>(json);
                }
                catch (Exception e)
                {
                    CustomLog.LogError($"[UnityRemoteConfigProvider] Failed to parse JSON for key '{key}': {e.Message}");
                }
            }

            // Return default value if parsing fails or key is missing
            return default;
        }
    }
}