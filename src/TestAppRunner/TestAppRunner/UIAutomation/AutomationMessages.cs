#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Newtonsoft.Json;

namespace MSTestX
{
    internal class UIAutomationMessage
    {      
        internal static string AutomationMessageType => "UIAutomation";

        public string MessageType => $"{AutomationMessageType}.{GetType().Name}";

        public static UIAutomationMessage? FromMessageType(string messageType, Newtonsoft.Json.Linq.JToken payload)
        {
            switch (messageType)
            {
                case var s when s == $"{AutomationMessageType}.{nameof(TapMessage)}":
                    return payload.ToObject<TapMessage>();
                case var s when s == $"{AutomationMessageType}.{nameof(KeyboardMessage)}":
                    return payload.ToObject<KeyboardMessage>();
                case var s when s == $"{AutomationMessageType}.{nameof(SwipeMessage)}":
                    return payload.ToObject<SwipeMessage>();
                default:
                    throw new InvalidOperationException($"Unknown message type: {messageType}");
            }
        }
    }

    internal class TapMessage : UIAutomationMessage
    {
        public int X { get; init; }
        public int Y { get; init; }
    }

    internal class SwipeMessage : UIAutomationMessage
    {
        public int FromX { get; init; }
        public int FromY { get; init; }
        public int ToX { get; init; }
        public int ToY { get; init; }
        public int DurationMs { get; init; }
    }

    internal class KeyboardMessage : UIAutomationMessage
    {
        public string? Key { get; set; }

        public string[]? Modifiers { get; set; }
    }
}