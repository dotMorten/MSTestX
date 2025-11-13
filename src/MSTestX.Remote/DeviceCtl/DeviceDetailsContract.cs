using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace MSTestX.Console.DeviceCtl
{
    public partial class DeviceDetails
    {
        [JsonPropertyName("info")]
        public Info Info { get; set; }

        [JsonPropertyName("result")]
        public Result Result { get; set; }
    }

    public partial class Info
    {
        [JsonPropertyName("arguments")]
        public string[] Arguments { get; set; }

        [JsonPropertyName("commandType")]
        public string CommandType { get; set; }

        [JsonPropertyName("environment")]
        public Environment Environment { get; set; }

        [JsonPropertyName("jsonVersion")]
        public long JsonVersion { get; set; }

        [JsonPropertyName("outcome")]
        public string Outcome { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }

    public partial class Environment
    {
        [JsonPropertyName("TERM")]
        public string Term { get; set; }
    }

    public partial class Result
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

    public partial class Capability
    {
        [JsonPropertyName("featureIdentifier")]
        public string FeatureIdentifier { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public partial class ConnectionProperties
    {
        [JsonPropertyName("authenticationType")]
        public string AuthenticationType { get; set; }

        [JsonPropertyName("isMobileDeviceOnly")]
        public bool IsMobileDeviceOnly { get; set; }

        [JsonPropertyName("lastConnectionDate")]
        public DateTimeOffset LastConnectionDate { get; set; }

        [JsonPropertyName("localHostnames")]
        public string[] LocalHostnames { get; set; }

        [JsonPropertyName("pairingState")]
        public string PairingState { get; set; }

        [JsonPropertyName("potentialHostnames")]
        public string[] PotentialHostnames { get; set; }

        [JsonPropertyName("transportType")]
        public string TransportType { get; set; }

        [JsonPropertyName("tunnelIPAddress")]
        public string TunnelIpAddress { get; set; }

        [JsonPropertyName("tunnelState")]
        public string TunnelState { get; set; }

        [JsonPropertyName("tunnelTransportProtocol")]
        public string TunnelTransportProtocol { get; set; }
    }

    public partial class DeviceProperties
    {
        [JsonPropertyName("bootState")]
        public string BootState { get; set; }

        [JsonPropertyName("bootedFromSnapshot")]
        public bool BootedFromSnapshot { get; set; }

        [JsonPropertyName("bootedSnapshotName")]
        public string BootedSnapshotName { get; set; }

        [JsonPropertyName("ddiServicesAvailable")]
        public bool DdiServicesAvailable { get; set; }

        [JsonPropertyName("developerModeStatus")]
        public string DeveloperModeStatus { get; set; }

        [JsonPropertyName("hasInternalOSBuild")]
        public bool HasInternalOsBuild { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("osBuildUpdate")]
        public string OsBuildUpdate { get; set; }

        [JsonPropertyName("osVersionNumber")]
        public string OsVersionNumber { get; set; }

        [JsonPropertyName("rootFileSystemIsWritable")]
        public bool RootFileSystemIsWritable { get; set; }

        [JsonPropertyName("screenViewingURL")]
        public string ScreenViewingUrl { get; set; }
    }

    public partial class HardwareProperties
    {
        [JsonPropertyName("cpuType")]
        public CpuType CpuType { get; set; }

        [JsonPropertyName("deviceType")]
        public string DeviceType { get; set; }

        [JsonPropertyName("ecid")]
        public long Ecid { get; set; }

        [JsonPropertyName("hardwareModel")]
        public string HardwareModel { get; set; }

        [JsonPropertyName("internalStorageCapacity")]
        public long InternalStorageCapacity { get; set; }

        [JsonPropertyName("isProductionFused")]
        public bool IsProductionFused { get; set; }

        [JsonPropertyName("marketingName")]
        public string MarketingName { get; set; }

        [JsonPropertyName("platform")]
        public string Platform { get; set; }

        [JsonPropertyName("productType")]
        public string ProductType { get; set; }

        [JsonPropertyName("reality")]
        public string Reality { get; set; }

        [JsonPropertyName("serialNumber")]
        public string SerialNumber { get; set; }

        [JsonPropertyName("supportedCPUTypes")]
        public CpuType[] SupportedCpuTypes { get; set; }

        [JsonPropertyName("supportedDeviceFamilies")]
        public long[] SupportedDeviceFamilies { get; set; }

        [JsonPropertyName("thinningProductType")]
        public string ThinningProductType { get; set; }

        [JsonPropertyName("udid")]
        public string Udid { get; set; }
    }

    public partial class CpuType
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("subType")]
        public long SubType { get; set; }

        [JsonPropertyName("type")]
        public long Type { get; set; }
    }
}