using System;
using System.Collections.Generic;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace MSTestX.Console.DeviceCtl
{
    public partial class Devices
    {
        [JsonPropertyName("info")]
        public Info Info { get; set; }

        [JsonPropertyName("result")]
        public Result Result { get; set; }
    }

    public partial class Result
    {
        [JsonPropertyName("devices")]
        public Device[] Devices { get; set; }
    }

    public partial class Device
    {
        [JsonPropertyName("capabilities")]
        public Capability[] Capabilities { get; set; }

        [JsonPropertyName("connectionProperties")]
        public ConnectionProperties ConnectionProperties { get; set; }

        [JsonPropertyName("deviceProperties")]
        public DeviceProperties DeviceProperties { get; set; }

        [JsonPropertyName("hardwareProperties")]
        public HardwareProperties HardwareProperties { get; set; }

        [JsonPropertyName("identifier")]
        public string Identifier { get; set; }

        [JsonPropertyName("tags")]
        public object[] Tags { get; set; }

        [JsonPropertyName("visibilityClass")]
        public string VisibilityClass { get; set; }
    }
}