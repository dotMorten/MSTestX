using Microsoft.Build.Framework;

using Task = Microsoft.Build.Utilities.Task;

namespace MSTestX.BuildTasks
{
    public sealed class RunTestsTask : Task
    {
        private MSTestX.Remote.RunSettings settings = new Remote.RunSettings();

        public RunTestsTask() : base()
        {

        }

        public string? DeviceName { get => settings.DeviceName; set => settings.DeviceName = value; }
        public string? IosAppPath { get => settings.IosAppPath; set => settings.IosAppPath = value; }
        public string? ApkPath { get => settings.ApkPath; set => settings.ApkPath = value; }
        public string? ApkId { get => settings.ApkId; set => settings.ApkId = value; }
        public string? ActivityName { get => settings.ActivityName; set => settings.ActivityName = value; }
        public string? Pin { get => settings.Pin; set => settings.Pin = value; }
        public string? SettingsXml { get => settings.SettingsXml; set => settings.SettingsXml = value; }
        public bool WaitForRemote { get => settings.WaitForRemote; set => settings.WaitForRemote = value; }
        public int WaitForRemoteTimeout { get => settings.WaitForRemoteTimeout; set => settings.WaitForRemoteTimeout = value; }
        public string? RemoteIp { get => settings.RemoteIp; set => settings.RemoteIp = value; }
        public string? OutputFilename { get => settings.OutputFilename; set => settings.OutputFilename = value; }


        /// <inheritdoc />
        public override bool Execute()
        {
            var run = Remote.Remote.RunTests(settings, new MSBuildLogger(this));
            try
            {
               var code = run.Result;
                return code == 0;
            }
            catch
            {
                return false;
            }
        }

    }
}
