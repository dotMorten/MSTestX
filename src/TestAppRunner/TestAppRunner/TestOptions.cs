using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Text;

namespace MSTestX
{
    public class TestOptions : IRunSettings
    {
        /// <summary>
        /// Start the test run when the test app launches
        /// </summary>
        public bool AutoRun { get; set; }
        
        /// <summary>
        /// Shuts the app down when the test run completes (only applies when <see cref="AutoRun"/> is enabled)
        /// </summary>
        public bool TerminateAfterExecution { get; set; }

        /// <summary>
        /// Path to a location to store a TRX Report when the test run completes
        /// </summary>
        public string TrxOutputPath { get; set; }
        
        /// <summary>
        /// Path to a log file to write to as the test run progresses
        /// </summary>
        public string ProgressLogPath { get; set; }

        /// <summary>
        /// Custom test recorder for recording test progress
        /// </summary>
        public ITestExecutionRecorder TestRecorder { get; set; }

        /// <summary>
        /// Gets or sets the MSTestSettings XML
        /// </summary>
        public string SettingsXml { get; set; }

        /// <summary>
        /// If set, will be using a socket connection to discover, launch and monitor tests.
        /// </summary>
        public ushort TestAdapterPort { get; set; } = 38300;

        ISettingsProvider IRunSettings.GetSettings(string settingsName)
        {
            return null;
        }
    }
}
