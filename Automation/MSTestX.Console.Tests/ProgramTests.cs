using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace MSTestX.Console.Tests;

[TestClass]
public class ProgramTests
{
    [TestMethod]
    public void GetLaunchMode_UsesRemoteAdapter_WhenRemoteIpIsProvided()
    {
        var arguments = new Dictionary<string, string?>
        {
            ["remoteIp"] = "127.0.0.1:38300"
        };

        Assert.AreEqual(LaunchMode.RemoteAdapter, Program.GetLaunchMode(arguments));
    }

    [TestMethod]
    public void GetLaunchMode_UsesRemoteAdapter_WhenWaitForRemoteIsProvided()
    {
        var arguments = new Dictionary<string, string?>
        {
            ["waitForRemote"] = null
        };

        Assert.AreEqual(LaunchMode.RemoteAdapter, Program.GetLaunchMode(arguments));
    }

    [TestMethod]
    public void GetLaunchMode_PrefersRemoteAdapter_OverAppPath()
    {
        var arguments = new Dictionary<string, string?>
        {
            ["remoteIp"] = "127.0.0.1:38300",
            ["apppath"] = "/tmp/Test.app"
        };

        Assert.AreEqual(LaunchMode.RemoteAdapter, Program.GetLaunchMode(arguments));
    }

    [TestMethod]
    public void GetLaunchMode_UsesAppleApp_WhenAppPathIsProvidedWithoutRemoteArguments()
    {
        var arguments = new Dictionary<string, string?>
        {
            ["apppath"] = "/tmp/Test.app"
        };

        Assert.AreEqual(LaunchMode.AppleApp, Program.GetLaunchMode(arguments));
    }

    [TestMethod]
    public void GetLaunchMode_DefaultsToAndroidAdb_WhenNoRemoteOrAppleArgumentsAreProvided()
    {
        var arguments = new Dictionary<string, string?>();

        Assert.AreEqual(LaunchMode.AndroidAdb, Program.GetLaunchMode(arguments));
    }
}
