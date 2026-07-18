using System.Collections.Generic;
using SiPVLib.Config.Configs;
using UnityEngine;

namespace SiPVLib.Config.Compare
{
    public abstract class ConfigCustomComparer : GameConfig
    {
        public abstract bool Compare(string compareValueJson);
    }
    
    public abstract class ConfigCustomComparer<TValue> : ConfigCustomComparer, IComparer<TValue>
    {
        [SerializeField] private TValue _value;
        
        public TValue Value => _value;
        
        public abstract int Compare(TValue x, TValue y);
    }
}