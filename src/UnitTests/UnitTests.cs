using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class UnitTests
    {
        public TestContext TestContext { get; set; }
        [TestInitialize]
        public void TestInit()
        {
        }

        [TestMethod]
        [TestProperty("Test prop", "Prop val")]
        [WorkItem(1234)]
        [TestCategory("Synchronous tests")]
        [Owner("Morten")]
        [Description("This test passes")]
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
        [Ignore]
        [TestCategory("Synchronous tests")]
        public void TestSkipped()
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
}
