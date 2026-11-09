using System;
using System.Collections.Generic;

namespace KrakenCache.Core
{
    /// <summary>Tracks presentation work that must finish before showing a level result.</summary>
    public sealed class GoalAnimationTracker
    {
        private readonly HashSet<Activity> active = new HashSet<Activity>();
        public bool HasPending => active.Count > 0;
        public int PendingCount => active.Count;

        public IDisposable Begin()
        {
            var activity = new Activity(this);
            active.Add(activity);
            return activity;
        }

        // Old completion callbacks cannot remove activities started after a reset.
        public void Reset() => active.Clear();

        private sealed class Activity : IDisposable
        {
            private GoalAnimationTracker owner;
            public Activity(GoalAnimationTracker tracker) => owner = tracker;
            public void Dispose()
            {
                if (owner == null) return;
                owner.active.Remove(this);
                owner = null;
            }
        }
    }
}
