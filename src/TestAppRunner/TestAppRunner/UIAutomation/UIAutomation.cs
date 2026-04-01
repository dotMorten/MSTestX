using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSTestX
{
    /// <summary>
    /// Allows the unit test to reach out to the test runner and send UI automation messages to be performed by the host machine.
    /// This is useful for testing scenarios where the test needs to interact with the UI of the host machine, such as clicking buttons or entering text.
    /// </summary>
    public static class UIAutomation
    {
        /// <summary>
        /// Tests if the test runner is currently connected to a host runner. If this is false, any attempt to send a message will throw an exception.
        /// </summary>
        public static bool IsConnected => TestAppRunner.ViewModels.TestRunnerVM.Instance.Connection?.IsConnected ?? false;

        /// <summary>
        /// Sends a tap message to the host machine to simulate a tap at the specified coordinates.
        /// Coordinates are in physical pixels relative to the top left of the screen.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void Tap(int x, int y)
        {
            SendMessage(new TapMessage { X = x, Y = y });
        }

        /// <summary>
        /// Sends a swipe message to the host machine to simulate a swipe from the specified start coordinates to the specified end coordinates over the specified duration.
        /// Coordinates are in physical pixels relative to the top left of the screen.
        /// </summary>
        /// <param name="fromX"></param>
        /// <param name="fromY"></param>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="durationMs"></param>
        public static void SendSwipe(int fromX, int fromY, int toX, int toY, int durationMs = 500)
        {
            SendMessage(new SwipeMessage { FromX = fromX, FromY = fromY, ToX = toX, ToY = toY, DurationMs = durationMs });
        }

        /// <summary>
        /// Simulates a long press gesture at the specified screen coordinates for a given duration.
        /// Coordinates are in physical pixels relative to the top left of the screen.
        /// </summary>
        /// <param name="x">The horizontal coordinate, in pixels, where the long press is performed.</param>
        /// <param name="y">The vertical coordinate, in pixels, where the long press is performed.</param>
        /// <param name="durationMs">The duration of the long press gesture, in milliseconds. The default is 1000 milliseconds.</param>
        public static void SendLongPress(int x, int y, int durationMs = 1000)
        {
            SendMessage(new SwipeMessage { FromX = x, FromY = y, ToX = x, ToY = y, DurationMs = durationMs });
        }

        /// <summary>
        /// Sends a keyboard input message to the host machine to simulate typing the specified text.
        /// </summary>
        /// <param name="text"></param>
        public static void SendTextInput(string text) => SendMessage(new KeyboardMessage { Key = text });

        /// <summary>
        /// Sends a keyboard input message to the host machine to simulate typing the specified key.
        /// </summary>
        /// <param name="key"></param>
        public static void SendKeyboardInput(char key) => SendMessage(new KeyboardMessage { Key = key.ToString() });

        private static void SendMessage(UIAutomationMessage message)
        {
            if (!OperatingSystem.IsAndroid())
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Inconclusive("UI Automation is currently only supported on Android.");
            if (!IsConnected)
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Inconclusive("UI Automation not available. The test runner is not connected to MSTestX.Console");
            TestAppRunner.ViewModels.TestRunnerVM.Instance.Connection.SendMessage(message.MessageType, message);
        }
    }
}
