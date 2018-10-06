using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TestAppRunner
{
    internal static class Logger
    {
        static System.Runtime.Serialization.Json.DataContractJsonSerializer testCaseSerializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TestCase));
        static System.Runtime.Serialization.Json.DataContractJsonSerializer testResultSerializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TestResult));

        static System.Reflection.MethodInfo logMethod;
        // http://www.florescu.org/archives/2010/10/15/android-usb-connection-to-pc/comment-page-2/
        public static SocketCommunicationManager Socket = new SocketCommunicationManager(JsonDataSerializer.Instance);

        public static void Log(string message, string category = "MSTestX", bool skipConsoleOut = false)
        {
            if(!skipConsoleOut)
                System.Diagnostics.Trace.WriteLine(message, category);
            if(Xamarin.Forms.Device.RuntimePlatform == Xamarin.Forms.Device.Android)
            {
                if (logMethod == null)
                {
                    var logType = Type.GetType("Android.Util.Log, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=84e04ff9cfb79065");
                    logMethod = logType.GetMethod("Info", new Type[] { typeof(string), typeof(string) });
                }
                logMethod.Invoke(null, new object[] { category, message });
            }
        }

        internal static void LogMessage(string messageType, object payload)
        {
            var json = JsonDataSerializer.Instance.SerializePayload(messageType, payload);
            Socket.SendMessage(messageType, payload);
            //Log(json, "MSTestXMessage", true);
        }

        internal static void LogMessage(string messageType)
        {
            var json = JsonDataSerializer.Instance.SerializeMessage(messageType);
            Socket.SendMessage(messageType);
            //Log(json, "MSTestXMessage", true);
        }

        internal static void LogMessage(TestMessageLevel testMessageLevel, string message)
        {
            LogMessage(MessageType.TestMessage, new TestMessagePayload { MessageLevel = testMessageLevel, Message = message });
        }

        internal static void LogTestStart(TestCase testCase)
        {
            LogMessage(MessageType.DataCollectionTestStart, new TestCaseStartEventArgs(testCase));            
            //Log("TEST STARTING: " + testCase.FullyQualifiedName);
        }

        internal static void LogResult(TestResult testResult)
        {
            LogMessage(MessageType.DataCollectionTestEnd, new Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection.TestResultEventArgs(testResult));

            var testName = testResult.TestCase.FullyQualifiedName;
            if (testResult.DisplayName != null)
            {
                var className = testResult.TestCase.FullyQualifiedName.Substring(0, testResult.TestCase.FullyQualifiedName.LastIndexOf("."));
                testName = $"{className}.{testResult.DisplayName}";
            }
            //Log($"TEST COMPLETED: {testName} - {testResult.Outcome}" + (testResult.Outcome == TestOutcome.Failed?  " " + testResult.ErrorMessage : ""));
        }
    }
}
