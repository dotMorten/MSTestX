using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

namespace TestAppRunner.iOS
{
    public class Application
    {
        private UnitTests.Tests test; //Necessary to force include of the class library. Xamarin seems to strip it out if it's not referenced in code.

        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}
