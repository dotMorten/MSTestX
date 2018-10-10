using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MSTestX.Console.Adb
{
    internal class Device
    {
        private Device() { }
        const string AdbDeviceRegex = @"^(?<serial>[a-zA-Z0-9_-]+(?:\s?[\.a-zA-Z0-9_-]+)?(?:\:\d{1,})?)\s+(?<state>device|connecting|offline|unknown|bootloader|recovery|download|unauthorized|host|no permissions)(\s+usb:(?<usb>[^:]+))?(?:\s+product:(?<product>[^:]+))?(\s+model\:(?<model>[\S]+))?(\s+device\:(?<device>[\S]+))?(\s+features:(?<features>[^:]+))?(\s+transport_id:(?<transport_id>[^:]+))?$";
        static Regex Regex = new Regex(AdbDeviceRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Serial { get; private set; }
        public string State { get; private set; }
        public string Model { get; private set; }
        public string Product { get; private set; }
        public string Name { get; private set; }
        public string Features { get; private set; }
        public string Usb { get; private set; }
        public string TransportId { get; private set; }

        internal static Device FromDataString(string data)
        {
            Match m = Regex.Match(data);
            if (m.Success)
            {
                return new Device()
                {
                    Serial = m.Groups["serial"].Value,
                    State = m.Groups["state"].Value,
                    Model = m.Groups["model"].Value,
                    Product = m.Groups["product"].Value,
                    Name = m.Groups["device"].Value,
                    Features = m.Groups["features"].Value,
                    Usb = m.Groups["usb"].Value,
                    TransportId = m.Groups["transport_id"].Value
                };
            }
            return null;
        }
    }
}
