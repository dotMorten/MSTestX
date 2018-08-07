using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class Tests
    {
        public TestContext TestContext { get; set; }
        [TestInitialize]
        public void TestInit()
        {
        }

        [TestMethod]
        [TestCategory("Synchronous tests")]
        public void TestOK()
        {
            Assert.IsNotNull(TestContext);
            Assert.IsTrue(true);
        }

        [TestMethod]
        [TestCategory("Synchronous tests")]
        [TestCategory("Failing Tests")]
        public void TestFail()
        {
            Assert.IsTrue(false);
        }

        [TestMethod]
        [TestCategory("Synchronous tests")]
        [Description("This test writes out some messages")]
        public void TestWithMessages()
        {
            Assert.IsNotNull(TestContext);
            TestContext.WriteLine("This is a message from a 'TestContext.WriteLine' call.");
            TestContext.WriteLine("And here's a second message.");
        }

        [TestMethod]
        [TestCategory("Synchronous tests")]
        [Description("This test writes out one message then fails")]
        public void TestFailWithMessages()
        {
            Assert.IsNotNull(TestContext);
            TestContext.WriteLine("Oh oh... I think we're about to fail...");
            Assert.Fail();
            TestContext.WriteLine("This message won't make it to the output");
        }


        [TestMethod]
        [TestProperty("Test prop", "Prop val")]
        [WorkItem(1234)]
        [Owner("Morten")]
        [Description("This test contains 4 properties")]
        public void TestWithProperties()
        {

        }

        [TestMethod]
        [Ignore]
        [TestCategory("Synchronous tests")]
        public void TestSkipped()
        {
        }

        [TestMethod]
        [Ignore("Ignore this test for now...")]
        [TestCategory("Synchronous tests")]
        public void TestSkippedWithMessage()
        {
        }

        [TestMethod]
        [TestCategory("Synchronous tests")]
        public async Task TestOKAsync()
        {
            await Task.Delay(100);
            Assert.IsTrue(true);
        }

        [TestMethod]
        [TestCategory("Asynchronous tests")]
        [TestCategory("Failing Tests")]
        public async Task TestFailAsync()
        {
            await Task.Delay(100);
            Assert.IsTrue(false);
        }

        [TestMethod]
        [Timeout(500)]
        [TestCategory("Asynchronous tests")]
        public async Task TestTimeoutAsync()
        {
            await Task.Delay(5000);
            Assert.IsTrue(true);
        }

        [DataTestMethod]
        [DataRow(1, 2, 3)]
        [DataRow(2, 2, 4)]
        [DataRow(2, 3, 5)]
        [TestCategory("Miscellanous tests")]
        public void TestDataTestMethod(int value1, int value2, int result)
        {
            Assert.AreEqual(result, value1 + value2);
        }

        [DataTestMethod]
        [DataRow(1, 2, 3)]
        [DataRow(2, 2, 9)]
        [DataRow(2, 3, 5)]
        [TestCategory("Miscellanous tests")]
        [TestCategory("Failing Tests")]
        public void TestDataTestMethod_Fail(int value1, int value2, int result)
        {
            Assert.AreEqual(result, value1 + value2);
        }
    }

    [TestClass]
    public class MoreTests
    {
        [TestMethod]
        public void MoreTests_1() { }
        [TestMethod]
        public void MoreTests_2() { }
        [TestMethod]
        public void MoreTests_3() { }
    }
}
