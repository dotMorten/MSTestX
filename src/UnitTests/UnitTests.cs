using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        [TestProperty("Test prop", "Prop val")]
        [WorkItem(1234)]
        [TestCategory("Category 1")]
        [Owner("Morten")]
        [Description("This test passes")]
        public void TestOK()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        [Ignore]
        public void TestFail()
        {
            Assert.IsTrue(false);
        }

        [TestMethod]
        public async Task TestOKAsync()
        {
            await Task.Delay(2000);
            Assert.IsTrue(true);
        }

        [TestMethod]
        public async Task TestFailAsync()
        {
            await Task.Delay(2000);
            Assert.IsTrue(false);
        }

        [TestMethod]
        [Timeout(1000)]
        public async Task TestTimeoutAsync()
        {
            await Task.Delay(5000);
            Assert.IsTrue(false);
        }
    }
}
