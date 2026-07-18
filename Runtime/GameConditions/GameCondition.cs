using System;
using UnityEngine;

namespace SiPVLib.Config.GameConditions
{
    [Serializable]
    public enum GameConditionType
    {
        None = 0,
        [Tooltip("Indicates whether the specified user data meets the specified condition. Param string: {userDataKey}")]
        UserData = 1,
        [Tooltip("Indicates whether the current time meets the specified condition. Param long (Unix seconds): {targetTime}")]
        LimitedTime = 2,
        [Tooltip("Indicates whether the view is currently active. Param string: {viewId}")]
        ViewActive = 3,
        [Tooltip("Indicates whether the UI is currently active. Param string: {viewId}|{uiId}")]
        UIActive = 4,
    }
    
    [Serializable]
    public partial class GameCondition
    {
        [SerializeField] private GameConditionType _type;
        
        public GameConditionType Type => _type;
    }
}