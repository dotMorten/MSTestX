using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MSTestX.TestAdapter
{
    [ExtensionUri(AndroidTestHostUri)]
    [FriendlyName(AndroidTestHostFriendlyName)]
    public class TestRuntimeProvider : ITestRuntimeProvider
    {
        private const string AndroidTestHostUri = "HostProvider://AndroidTestHost";
        private const string AndroidTestHostFriendlyName = "AndroidTestHost";
        public TestRuntimeProvider()
        {

        }
        public bool Shared => true;

        public event EventHandler<HostProviderEventArgs> HostLaunched;

        public event EventHandler<HostProviderEventArgs> HostExited;

        public bool CanExecuteCurrentRunConfiguration(string runsettingsXml)
        {
            return true;
        }

        public Task CleanTestHostAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        public TestHostConnectionInfo GetTestHostConnectionInfo()
        {
            return new TestHostConnectionInfo() { Role = ConnectionRole.Host, Endpoint = "127.0.0.1:38300", Transport = Transport.Sockets };
        }

        public TestProcessStartInfo GetTestHostProcessStartInfo(IEnumerable<string> sources, IDictionary<string, string> environmentVariables, TestRunnerConnectionInfo connectionInfo)
        {
            return new TestProcessStartInfo() { };
        }

        public IEnumerable<string> GetTestPlatformExtensions(IEnumerable<string> sources, IEnumerable<string> extensions)
        {
            return extensions;
        }

        public IEnumerable<string> GetTestSources(IEnumerable<string> sources)
        {
            return sources;
        }

        public void Initialize(IMessageLogger logger, string runsettingsXml)
        {
            
        }

        public Task<bool> LaunchTestHostAsync(TestProcessStartInfo testHostStartInfo, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void SetCustomLauncher(ITestHostLauncher customLauncher)
        {
            throw new NotImplementedException();
        }
    }
}
