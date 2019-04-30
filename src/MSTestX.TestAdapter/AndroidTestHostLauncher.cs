using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MSTestX.TestAdapter
{
    public class AndroidTestHostLauncher : ITestHostLauncher
    {
        /// <summary>
        /// Interface defining contract for custom test host implementations
        /// </summary>
        public bool IsDebug => false;

        /// <summary>
        /// Launches custom test host using the default test process start info
        /// </summary>
        /// <param name="defaultTestHostStartInfo">Default TestHost Process Info</param>
        /// <returns>Process id of the launched test host</returns>
        public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo)
        {
            return -1;
        }

        /// <summary>
        /// Launches custom test host using the default test process start info
        /// </summary>
        /// <param name="defaultTestHostStartInfo">Default TestHost Process Info</param>
        /// <param name="cancellationToken">The cancellation Token.</param>
        /// <returns>Process id of the launched test host</returns>
        public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo, CancellationToken cancellationToken)
        {
            return -1;
        }
    }
}
