using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net.Http;
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
        [DataRow(2, 2, 4)]
        [DataRow(2, 3, 5)]
        [DataRow(3, 1, 4)]
        [DataRow(3, 2, 5)]
        [DataRow(3, 3, 6)]
        [DataRow(4, 1, 6)] //Will fail
        [DataRow(4, 2, 6)]
        [DataRow(4, 3, 7)]
        [TestCategory("Miscellanous tests")]
        public async Task TestDataTestMethodAsync(int value1, int value2, int result)
        {
            await Task.Delay(2000);
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

        [TestMethod]
        [TestCategory("Attachments")]
        public void TestAttachments()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), "TestResults");
            var di = new DirectoryInfo(folder);
            if (!di.Exists) di.Create();
            var file = Path.Combine(folder, Path.GetRandomFileName());
            File.WriteAllText(file, "File contents 1");
            TestContext.AddResultFile(file);
            file = Path.Combine(folder, Path.GetRandomFileName());
            File.WriteAllText(file, "File contents 2");
            TestContext.AddResultFile(file);
        }


        [DataTestMethod]
        [DataRow("File1.txt")]
        [DataRow("File2.txt")]
        [TestCategory("Attachments")]
        public void TestAttachmentsDatarows(string file)
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), "TestResults");
            var di = new DirectoryInfo(folder);
            if (!di.Exists) di.Create();
            var filename = Path.Combine(folder, file);
            File.WriteAllText(filename, "File contents - " + file);
            TestContext.AddResultFile(filename);
        }


        [TestMethod]
        [TestCategory("Attachments")]
        public async Task TestImageAttachment()
        {
            HttpClient c = new HttpClient();
            using (var stream = await c.GetStreamAsync("https://github.com/dotMorten/MSTestX/raw/master/src/TestAppRunner/TestAppRunner.iOS/Resources/Default.png"))
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), "TestResults");
                var di = new DirectoryInfo(folder);
                if (!di.Exists) di.Create();
                var filename = Path.Combine(folder, "Xamagon.png");
                using (var output = File.OpenWrite(filename))
                {
                    await stream.CopyToAsync(output);
                    await output.FlushAsync();
                }
                TestContext.AddResultFile(filename);
            }
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

// This test is mostly to test what the tests look like in the UI when the names are very long:
namespace A_Super_duper_long_namespace
{
    [TestClass]
    public class And_A_Very_Long_ClassName
    {
        [TestMethod]
        public void A_Very_long_Test_Name() { }
    }
}
