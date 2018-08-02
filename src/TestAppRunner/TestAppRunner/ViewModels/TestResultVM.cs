using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestAppRunner.ViewModels
{
    internal class TestResultVM : VMBase
    {
        public TestCase Test { get; set; }

        public TestResult Result { get; set; }

        public Microsoft.VisualStudio.TestTools.UnitTesting.UnitTestOutcome Outcome { get; set; } = Microsoft.VisualStudio.TestTools.UnitTesting.UnitTestOutcome.Unknown;

        public string Category
        {
            get
            {
                var c = Test.Properties.Where(p => p.Id == "MSTestDiscoverer.TestCategory").FirstOrDefault();
                if (c != null)
                    return (Test.GetPropertyValue(c) as string[])?.FirstOrDefault();
                return null;
            }
        }
        public string Duration
        {
            get
            {
                if (Result == null) return null;
                return Result.Duration.ToString("g");
            }
        }

        public string ClassName => Test.FullyQualifiedName.Substring(0, Test.FullyQualifiedName.LastIndexOf("."));

        public string Namespace => ClassName.Substring(0, ClassName.LastIndexOf("."));

        public override string ToString() => Test.FullyQualifiedName;
    }
}
