using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestAppRunner
{
    internal class TestSettings : IRunSettings
    {
        public string SettingsXml => null;

        public ISettingsProvider GetSettings(string settingsName)
        {
            return null;
        }
    }
}
