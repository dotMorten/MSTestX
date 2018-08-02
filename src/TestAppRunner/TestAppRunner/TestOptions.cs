using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestAppRunner
{
    public class TestOptions
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
    }
}
