using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestAppRunner
{
    public class TestSettings
    {
        /// <summary>
        /// Start the test run when the test app launches
        /// </summary>
        public bool AutoStart { get; set; }
        
        /// <summary>
        /// Shuts the app down when the test run completes (only applies when <see cref="AutoStart"/> is enabled)
        /// </summary>
        public bool ShutdownOnCompletion { get; set; }

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
    }
}
