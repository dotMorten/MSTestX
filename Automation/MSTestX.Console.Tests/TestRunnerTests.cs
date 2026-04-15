using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MSTestX.Console.Tests;

[TestClass]
public class TestRunnerTests
{
    [TestMethod]
    public void ShouldWriteTestResult_ReturnsTrue_ForTopLevelResult()
    {
        Assert.IsTrue(TestRunner.ShouldWriteTestResult(Guid.Empty, isOutputRedirected: false));
        Assert.IsTrue(TestRunner.ShouldWriteTestResult(Guid.Empty, isOutputRedirected: true));
    }

    [TestMethod]
    public void ShouldWriteRunningTest_ReturnsTrue_ForTopLevelResult_WhenOutputIsInteractive()
    {
        Assert.IsTrue(TestRunner.ShouldWriteRunningTest(Guid.Empty, isOutputRedirected: false));
    }

    [TestMethod]
    public void ShouldWriteRunningTest_ReturnsFalse_ForTopLevelResult_WhenOutputIsRedirected()
    {
        Assert.IsFalse(TestRunner.ShouldWriteRunningTest(Guid.Empty, isOutputRedirected: true));
    }

    [TestMethod]
    public void ShouldWriteTestResult_ReturnsTrue_ForChildResult_WhenOutputIsInteractive()
    {
        Assert.IsTrue(TestRunner.ShouldWriteTestResult(Guid.NewGuid(), isOutputRedirected: false));
    }

    [TestMethod]
    public void ShouldWriteTestResult_ReturnsFalse_ForChildResult_WhenOutputIsRedirected()
    {
        Assert.IsFalse(TestRunner.ShouldWriteTestResult(Guid.NewGuid(), isOutputRedirected: true));
    }

    [TestMethod]
    public void ShouldWriteRunningTest_ReturnsTrue_ForChildResult_WhenOutputIsInteractive()
    {
        Assert.IsTrue(TestRunner.ShouldWriteRunningTest(Guid.NewGuid(), isOutputRedirected: false));
    }

    [TestMethod]
    public void ShouldWriteRunningTest_ReturnsFalse_ForChildResult_WhenOutputIsRedirected()
    {
        Assert.IsFalse(TestRunner.ShouldWriteRunningTest(Guid.NewGuid(), isOutputRedirected: true));
    }

    [TestMethod]
    public void ShouldWriteRunningTest_ReturnsFalse_ForRedirectedOutput_RegardlessOfParentExecId()
    {
        Assert.IsFalse(TestRunner.ShouldWriteRunningTest(Guid.Empty, isOutputRedirected: true));
        Assert.IsFalse(TestRunner.ShouldWriteRunningTest(Guid.NewGuid(), isOutputRedirected: true));
    }

    [TestMethod]
    public void ShouldWriteFailureDetails_ReturnsTrue_ForTopLevelFailureWithMessage()
    {
        Assert.IsTrue(TestRunner.ShouldWriteFailureDetails(Guid.Empty, isOutputRedirected: false, "boom"));
        Assert.IsTrue(TestRunner.ShouldWriteFailureDetails(Guid.Empty, isOutputRedirected: true, "boom"));
    }

    [TestMethod]
    public void ShouldWriteFailureDetails_ReturnsTrue_ForChildFailureWithMessage_WhenOutputIsInteractive()
    {
        Assert.IsTrue(TestRunner.ShouldWriteFailureDetails(Guid.NewGuid(), isOutputRedirected: false, "boom"));
    }

    [TestMethod]
    public void ShouldWriteFailureDetails_ReturnsFalse_ForChildFailureWithMessage_WhenOutputIsRedirected()
    {
        Assert.IsFalse(TestRunner.ShouldWriteFailureDetails(Guid.NewGuid(), isOutputRedirected: true, "boom"));
    }

    [TestMethod]
    public void ShouldWriteFailureDetails_ReturnsFalse_WhenFailureMessageIsMissing()
    {
        Assert.IsFalse(TestRunner.ShouldWriteFailureDetails(Guid.Empty, isOutputRedirected: false, null));
        Assert.IsFalse(TestRunner.ShouldWriteFailureDetails(Guid.Empty, isOutputRedirected: false, string.Empty));
        Assert.IsFalse(TestRunner.ShouldWriteFailureDetails(Guid.Empty, isOutputRedirected: true, null));
        Assert.IsFalse(TestRunner.ShouldWriteFailureDetails(Guid.Empty, isOutputRedirected: true, string.Empty));
    }

    [TestMethod]
    public void GetRedirectedOutcomeLabel_UsesStableLabels()
    {
        Assert.AreEqual("PASS", TestRunner.GetRedirectedOutcomeLabel(TestOutcome.Passed));
        Assert.AreEqual("FAIL", TestRunner.GetRedirectedOutcomeLabel(TestOutcome.Failed));
        Assert.AreEqual("SKIP", TestRunner.GetRedirectedOutcomeLabel(TestOutcome.Skipped));
        Assert.AreEqual("NONE", TestRunner.GetRedirectedOutcomeLabel(TestOutcome.None));
    }

    [TestMethod]
    public void GetRedirectedOutcomeLabel_PreservesNonPassFailOutcomeNames()
    {
        Assert.AreEqual("NOTFOUND", TestRunner.GetRedirectedOutcomeLabel(TestOutcome.NotFound));
    }

    [TestMethod]
    public void FormatRedirectedRunningTestLine_UsesRunPrefix()
    {
        Assert.AreEqual("RUN  Sample.Test", TestRunner.FormatRedirectedRunningTestLine("Sample.Test"));
    }

    [TestMethod]
    public void FormatRedirectedResultLine_UsesStableRedirectedFormat()
    {
        Assert.AreEqual("FAIL Sample.Test [1s 250ms]", TestRunner.FormatRedirectedResultLine(TestOutcome.Failed, "Sample.Test", TimeSpan.FromMilliseconds(1250)));
    }

    [TestMethod]
    public void FormatDuration_FormatsSubMillisecondDuration()
    {
        Assert.AreEqual("[< 1ms]", TimeSpan.Zero.FormatDuration());
    }

    [TestMethod]
    public void FormatDuration_FormatsSecondScaleDuration()
    {
        Assert.AreEqual("[1s 250ms]", TimeSpan.FromMilliseconds(1250).FormatDuration());
    }
}
