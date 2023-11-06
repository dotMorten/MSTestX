using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class AttachmentTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [TestCategory("Attachments")]
        public void TestAttachments()
        {
            ValidateDirectories();
            var folder = TestContext.TestRunResultsDirectory;
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
            ValidateDirectories();
            var folder = TestContext.TestRunResultsDirectory;
            var filename = Path.Combine(folder, file);
            File.WriteAllText(filename, "File contents - " + file);
            TestContext.AddResultFile(filename);
        }

        private void ValidateDirectories()
        {
            var folder = TestContext.TestRunResultsDirectory;
            if (folder == null)
                throw new InvalidOperationException("TestRunResultsDirectory not set");
            if (!Directory.Exists(folder))
                throw new InvalidOperationException("TestRunResultsDirectory not created");

        }

        [TestMethod]
        [TestCategory("Attachments")]
        public async Task TestImageAttachment()
        {
            ValidateDirectories();
            var folder = TestContext.TestRunResultsDirectory;
            HttpClient c = new HttpClient();
            using (var stream = await c.GetStreamAsync("https://github.com/dotMorten/MSTestX/assets/1378165/979a60c1-2c88-4492-9a57-b2ba2cc6fbfe"))
            {
                folder = Path.Combine(folder, nameof(TestImageAttachment));
                var di = new DirectoryInfo(folder);
                if (!di.Exists) di.Create();
                var filename = Path.Combine(folder, "AttachedImage.png");
                using (var output = File.OpenWrite(filename))
                {
                    await stream.CopyToAsync(output);
                    await output.FlushAsync();
                }
                TestContext.AddResultFile(filename);
            }
        }
    }
}
