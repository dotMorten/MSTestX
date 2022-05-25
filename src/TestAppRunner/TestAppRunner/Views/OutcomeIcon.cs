namespace TestAppRunner.Views
{
    /// <summary>
    /// Generates the proper icon to show for a test result
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class OutcomeIcon : Label
    {
        /// <summary>
        /// Identifies the <see cref="Result"/> Bindable property.
        /// </summary>
        public static readonly BindableProperty ResultProperty =
            BindableProperty.Create(nameof(Result), typeof(TestResult), typeof(OutcomeIcon), null, BindingMode.OneWay, null, OnResultPropertyChanged);

        /// <summary>
        /// Gets or sets the test result
        /// </summary>
        public TestResult Result
        {
            get { return (TestResult)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        private static void OnResultPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var icon = bindable as OutcomeIcon;
            icon.UpdateIcon();
        }

        private void UpdateIcon()
        {
            if(Result == null)
            {
                Text = "";
            }
            else { 
                switch (Result.Outcome)
                {
                    case TestOutcome.NotFound:
                        Text = "❔";
                        TextColor = Colors.Orange;
                        break;
                    case TestOutcome.Failed:
                        if (Result.ErrorStackTrace == null && Result.ErrorMessage != null &&  Result.ErrorMessage.Contains("timeout"))
                            Text = "⏱";
                        else
                            Text = "⛔"; //⛔⨯"
                        TextColor = Colors.Red;
                        break;
                    case TestOutcome.Passed:
                        Text = "✔";
                        TextColor = Colors.Green;
                        break;
                    case TestOutcome.Skipped:
                        Text = "⚠"; 
                        TextColor = Colors.Gray;
                        break;
                    case TestOutcome.None:
                    default:
                        Text = "";
                        break;
                }
            }
        }
    }
}
