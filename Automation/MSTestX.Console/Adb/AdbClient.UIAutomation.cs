using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSTestX.Console.Adb
{
    internal partial class AdbClient
    {
        // See https://commandmasters.com/commands/input-android/ for reference to ADB input commands.

        public async Task PerformUIAutomationAction(UIAutomationMessage message)
        {
            if (message is TapMessage tap)
            {
                // send a tap command to the device over ADB.
                await SendShellCommandAsync($"adb shell input tap {tap.X} {tap.Y}", "deviceId");
            }
            if (message is SwipeMessage swipe)
            {
                // send a swipe command to the device over ADB.
                await SendShellCommandAsync($"adb shell input swipe {swipe.FromX} {swipe.FromY} {swipe.ToX} {swipe.ToY} {swipe.DurationMs}", "deviceId");
            }
            else if (message is KeyboardMessage keyboard)
            {
                // TODO: Send modifiers
                if(keyboard.Key.Length == 1)
                    await SendShellCommandAsync($"adb shell input keyevent {KeyToKeyEventCode(keyboard.Key[0])}", "deviceId");
                else
                    await SendShellCommandAsync($"adb shell input text \"{keyboard.Key.Replace(" ", "%20")}\"", "deviceId");
            }
        }

        private static int KeyToKeyEventCode(char key)
        {
            // This is a very simplified mapping. A real implementation would need to handle more keys and modifiers.
            if (char.IsLetterOrDigit(key))
            {
                return char.ToUpper(key) - 'A' + 29; // KeyEvent.KEYCODE_A starts at 29
            }
            switch (key)
            {
                case ' ':
                    return 62; // KeyEvent.KEYCODE_SPACE
                case '\n':
                    return 66; // KeyEvent.KEYCODE_ENTER
                default:
                    throw new NotSupportedException($"Key '{key}' is not supported.");
            }
        }
    }
}
