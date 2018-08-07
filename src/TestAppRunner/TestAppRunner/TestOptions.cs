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

        /// <summary>
        /// Serializes progress so a crashed test run can be continued.
        /// </summary>
        /// <remarks>Test run can be continued by setting <see cref="ContinueCrashedTestrun"/> to <c>true</c>.</remarks>
        public bool StoreProgressForRelaunch { get; set; }

        /// <summary>
        /// If <see cref="ContinueCrashedTestrun"/> was set to <c>true</c> and a test run wasn't completed due to a crash, this'll continue where it left off, skipping
        /// the last test that was active when it crashed
        /// </summary>
        public bool ContinueCrashedTestrun { get; set; }
    }
}
