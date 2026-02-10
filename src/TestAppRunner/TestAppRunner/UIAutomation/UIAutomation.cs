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

        public static void Tap(int x, int y)
        {
            SendMessage(new TapMessage { X = x, Y = y });
        }

        public static void SendSwipe(int fromX, int fromY, int toX, int toY, int durationMs = 500)
        {
            SendMessage(new SwipeMessage { FromX = fromX, FromY = fromY, ToX = toX, ToY = toY, DurationMs = durationMs });
        }

        public static void SendLongPress(int x, int y, int durationMs = 1000)
        {
            SendMessage(new SwipeMessage { FromX = x, FromY = y, ToX = x, ToY = y, DurationMs = durationMs });
        }

        public void SendTextInput(string text) => SendMessage(new KeyboardMessage { Key = text });

        public static void SendKeyboardInput(char key) => SendMessage(new KeyboardMessage { Key = key.ToString() });

        private static void SendMessage(UIAutomationMessage message)
        {
            if (!IsConnected) throw new InvalidOperationException("Test Runner is not connected to host runner");
            TestAppRunner.ViewModels.TestRunnerVM.Instance.Connection.SendMessage(message.MessageType, message);
        }
    }
}
