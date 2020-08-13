using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

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

        private string settingsXml;
        private bool settingsXmlIsDirty = true;

        /// <summary>
        /// Gets or sets the MSTestSettings XML
        /// </summary>
        public string SettingsXml
        {

            get
            {
                if (settingsXmlIsDirty)
                {
                    settingsXml = AppendParameters(settingsXml);
                    settingsXmlIsDirty = false;
                }
                return settingsXml;
            }
            set
            {
                settingsXml = value;
                settingsXmlIsDirty = true;
            }
        }

        private string AppendParameters(string settingsXml)
        {
            if (string.IsNullOrWhiteSpace(settingsXml))
                settingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?><RunSettings />";
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(settingsXml);
                var runsettings = xmlDoc.SelectSingleNode("RunSettings");
                if (runsettings == null)
                {
                    runsettings = xmlDoc.CreateElement("RunSettings");
                    xmlDoc.AppendChild(runsettings);
                }
                var testRunParameters = runsettings.SelectSingleNode("TestRunParameters");
                if (testRunParameters == null)
                {
                    testRunParameters = xmlDoc.CreateElement("TestRunParameters");
                    runsettings.AppendChild(testRunParameters);
                }
                AddParameter(testRunParameters, "DeploymentDirectory", DeploymentDirectory);
                AddParameter(testRunParameters, "ResultsDirectory", ResultsDirectory);
                AddParameter(testRunParameters, "TestDeploymentDir", DeploymentDirectory);
                AddParameter(testRunParameters, "TestDir", TestRunDirectory);
                AddParameter(testRunParameters, "TestLogsDir", TestRunResultsDirectory);
                AddParameter(testRunParameters, "TestResultsDirectory", TestResultsDirectory);
                AddParameter(testRunParameters, "TestRunDirectory", TestRunDirectory);
                AddParameter(testRunParameters, "TestRunResultsDirectory", TestRunResultsDirectory);
                
                using (var tw = new StringWriter())
                {
                    xmlDoc.Save(tw);
                    var xml = tw.ToString();
                    return xml;
                }
            }
            catch
            {
                return settingsXml;
            }

        }
        private static void AddParameter(XmlNode parametersNode, string name, string value)
        {
            foreach (var node in parametersNode.SelectNodes("Parameter").OfType<XmlElement>())
                if (node.HasAttribute("name") && node.Attributes["name"].Value == name || node.HasAttribute("Name") && node.Attributes["Name"].Value == name)
                    return;
            var p = parametersNode.OwnerDocument.CreateElement("Parameter");
            var atr = parametersNode.OwnerDocument.CreateAttribute("name");
            atr.Value = name;
            p.Attributes.Append(atr);

            atr = parametersNode.OwnerDocument.CreateAttribute("value");
            atr.Value = value;
            p.Attributes.Append(atr);

            parametersNode.AppendChild(p);
        }

        /// <summary>
        /// If set, will be using a socket connection to discover, launch and monitor tests.
        /// </summary>
        public ushort TestAdapterPort { get; set; } = 38300;

        ISettingsProvider IRunSettings.GetSettings(string settingsName)
        {
            return null;
        }

        #region Directories

        /// <summary>
        /// Gets base directory for the test run, under which deployed files and result files are stored.
        /// </summary>
        public string TestRunDirectory
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create));
            }
        }

        /// <summary>
        /// Gets directory for files deployed for the test run. Typically a subdirectory of TestRunDirectory.
        /// </summary>
        public string DeploymentDirectory
        {
            get
            {
#if __ANDROID__
                return Android.App.Application.Context.FilesDir.AbsolutePath;
#elif __IOS__
                return Foundation.NSBundle.MainBundle.BundleUrl.Path;
#else
                return "";
#endif
            }
        }

        /// <summary>
        /// Gets base directory for results from the test run. Typically a subdirectory of <see cref="TestRunDirectory"/>.
        /// </summary>
        public string ResultsDirectory => Path.Combine(TestRunDirectory, "TestResults");

        /// <summary>
        /// Gets directory for test run result files. Typically a subdirectory of <see cref="ResultsDirectory"/>.
        /// </summary>
        public string TestRunResultsDirectory => Path.Combine(TestRunDirectory, "TestResults");


        /// <summary>
        /// Gets directory for test result files.
        /// </summary>
        public string TestResultsDirectory => Path.Combine(TestRunDirectory, "TestResults");


        #endregion
    }
}
