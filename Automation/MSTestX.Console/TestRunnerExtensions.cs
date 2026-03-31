using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using System;
using System.Linq;

namespace MSTestX.Console
{
    public static class TestRunnerExtensions
    {
        public static string GetDisplayName(this TestCaseStartEventArgs args)
        {
            return string.IsNullOrEmpty(args.TestCaseName) ? args.TestElement.DisplayName : args.TestCaseName;
        }

        public static Guid GetParentExecId(this TestCaseStartEventArgs args)
        {
            var parentExecIdProperty = args.TestElement.Properties.FirstOrDefault(t => t.Id == "ParentExecId");
            return parentExecIdProperty is null ? Guid.Empty : args.TestElement.GetPropertyValue<Guid>(parentExecIdProperty, Guid.Empty);
        }

        public static string GetDisplayName(this Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection.TestResultEventArgs args)
        {
            return string.IsNullOrEmpty(args.TestResult.DisplayName) ? args.TestElement.DisplayName : args.TestResult.DisplayName;
        }

        public static Guid GetParentExecId(this Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection.TestResultEventArgs args)
        {
            var parentExecIdProperty = args.TestResult.Properties.FirstOrDefault(t => t.Id == "ParentExecId");
            return parentExecIdProperty is null ? Guid.Empty : args.TestResult.GetPropertyValue<Guid>(parentExecIdProperty, Guid.Empty);
        }

        public static string FormatDuration(this TimeSpan duration)
        {
            if (duration.TotalMilliseconds < 1)
                return "[< 1ms]";
            if (duration.TotalSeconds < 1)
                return $"[{duration.Milliseconds}ms]";
            if (duration.TotalMinutes < 1)
                return $"[{duration.Seconds}s {duration.Milliseconds:0}ms]";
            if (duration.TotalHours < 1)
                return $"[{duration.Minutes}m {duration.Seconds}s {duration.Milliseconds:0}ms]";
            if (duration.TotalDays < 1)
                return $"[{duration.Hours}h {duration.Minutes}m {duration.Seconds}s {duration.Milliseconds:0}ms]";

            return $"[{Math.Floor(duration.TotalDays)}d {duration.Hours}h {duration.Minutes}m {duration.Seconds}s {duration.Milliseconds:0}ms]";
        }
    }
}
