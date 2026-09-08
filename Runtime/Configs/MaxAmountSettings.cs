using System;
using Alchemy.Inspector;
using UnityEngine;

namespace SiPVLib.Config.Configs
{
    [Serializable, Flags]
    public enum MaxAmountOption
    {
        None = 0,
        Lifetime = 1,
        Daily = 1 << 1,
        Weekly = 1 << 2,
        Monthly = 1 << 3,
        Yearly = 1 << 4,
        PerEvent = 1 << 5,
    }

    [Serializable]
    public class MaxAmountSettings
    {
        [SerializeField]
        [Tooltip("Select which time-based limits to apply to this max amount setting.")]
        [HelpBox("Configure maximum amount limits based on different time periods. Select which time " +
                 "constraints to apply. Use flags to combine multiple (e.g., Daily | Weekly).")]
        protected MaxAmountOption _maxAmountOption;

        [SerializeField]
        [Tooltip("Maximum amount allowed during the lifetime of the user.")]
        [ShowIf(nameof(HasLifetimeFlag))]
        private long _lifeTime;

        [SerializeField]
        [Tooltip("Maximum amount allowed per day.")]
        [ShowIf(nameof(HasDailyFlag))]
        private long _daily;

        [SerializeField]
        [Tooltip("Maximum amount allowed per week.")]
        [ShowIf(nameof(HasWeeklyFlag))]
        private long _weekly;

        [SerializeField]
        [Tooltip("Maximum amount allowed per month.")]
        [ShowIf(nameof(HasMonthlyFlag))]
        private long _monthly;

        [SerializeField]
        [Tooltip("Maximum amount allowed per year.")]
        [ShowIf(nameof(HasYearlyFlag))]
        private long _yearly;

        [SerializeField]
        [Tooltip("Name of the event to limit the maximum amount per occurrence.")]
        [ShowIf(nameof(HasPerEventFlag))]
        private string _eventName;

        #region Properties

        // Helper methods for ShowIf conditions
        private bool HasLifetimeFlag() => (_maxAmountOption & MaxAmountOption.Lifetime) != MaxAmountOption.None;
        private bool HasDailyFlag() => (_maxAmountOption & MaxAmountOption.Daily) != MaxAmountOption.None;
        private bool HasWeeklyFlag() => (_maxAmountOption & MaxAmountOption.Weekly) != MaxAmountOption.None;
        private bool HasMonthlyFlag() => (_maxAmountOption & MaxAmountOption.Monthly) != MaxAmountOption.None;
        private bool HasYearlyFlag() => (_maxAmountOption & MaxAmountOption.Yearly) != MaxAmountOption.None;
        private bool HasPerEventFlag() => (_maxAmountOption & MaxAmountOption.PerEvent) != MaxAmountOption.None;

        // Expose properties
        public MaxAmountOption MaxAmountOption => _maxAmountOption;
        public long LifeTime => _lifeTime;
        public long Daily => _daily;
        public long Weekly => _weekly;
        public long Monthly => _monthly;
        public long Yearly => _yearly;
        public string EventName => _eventName;
        #endregion
    }
}
