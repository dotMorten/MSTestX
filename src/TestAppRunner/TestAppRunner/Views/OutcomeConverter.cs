using System.Globalization;

namespace TestAppRunner.Views
{
    /// <summary>
    /// Converts the outcome value to a color or readable name
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class OutcomeConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = value as TestResult;
            if(v != null)
            {
                if(v.Outcome == TestOutcome.Failed)
                {
                    if(v.ErrorStackTrace == null && v.ErrorMessage?.Contains("timeout") == true)
                    {
                        if (targetType == typeof(Color))
                            return LookupColor("timeoutColor", Colors.OrangeRed);
                        return "Timed out";
                    }
                    if (targetType == typeof(Color))
                        return LookupColor("failedColor", Colors.Red);
                }
                if(v.Outcome == TestOutcome.Passed)
                {
                    if (targetType == typeof(Color))
                        return LookupColor("successColor", Colors.Green);
                    return "Passed";
                }
                if (v.Outcome == TestOutcome.Skipped)
                {
                    if (targetType == typeof(Color))
                        return LookupColor("skippedColor", Colors.Orange);
                    return "Skipped";
                }
                return v.Outcome.ToString();
            }
            if (v == null)
            {
                if (targetType == typeof(Color))
                    return LookupColor("notExecutedColor", Colors.Gray);
                return "Not Executed";
            }
            return value;
        }

        internal static Color LookupColor(string key, Color fallback)
        {
            try
            {
                if (Application.Current.Resources.TryGetValue(key, out var newColor))
                    return (Color)newColor;
            }
            catch
            {
            }
            return fallback;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts the outcome value to a color
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class Outcome2Converter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var outcome = (TestOutcome)value;
            if (outcome == TestOutcome.Failed)
            {
                return OutcomeConverter.LookupColor("failedColor", Colors.Red);
            }
            if (outcome == TestOutcome.Passed)
            {
                return OutcomeConverter.LookupColor("successColor", Colors.Green);
            }
            if (outcome == TestOutcome.Skipped)
            {
                return OutcomeConverter.LookupColor("skippedColor", Colors.Orange);
            }
            return OutcomeConverter.LookupColor("notExecutedColor", Colors.Gray);
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
