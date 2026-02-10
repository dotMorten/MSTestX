using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSTestX.Console.Adb
{
    internal partial class Device
    {
        // See https://commandmasters.com/commands/input-android/ for reference to ADB input commands.

        public async Task PerformUIAutomationAction(UIAutomationMessage message)
        {
            if (message is TapMessage tap)
            {
                // send a tap command to the device over ADB.
                await _client.SendShellCommandAsync($"adb shell input tap {tap.X} {tap.Y}", Serial);
            }
            if (message is SwipeMessage swipe)
            {
                // send a swipe command to the device over ADB.
                await _client.SendShellCommandAsync($"adb shell input swipe {swipe.FromX} {swipe.FromY} {swipe.ToX} {swipe.ToY} {swipe.DurationMs}", Serial);
            }
            else if (message is KeyboardMessage keyboard)
            {
                // TODO: Send modifiers
                if(keyboard.Key.Length == 1)
                    await _client.SendShellCommandAsync($"adb shell input keyevent {KeyToKeyEventCode(keyboard.Key[0])}", Serial);
                else
                    await _client.SendShellCommandAsync($"adb shell input text \"{keyboard.Key.Replace(" ", "%20")}\"", Serial);
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
