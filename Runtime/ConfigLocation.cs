using System;

namespace SiPVLib.Config
{
	[Serializable]
    public enum ConfigLocation
    {
        Local = 0,
        Resources = 1,
        Addressable = 2,
        RemoteConfig = 3,
    }
}