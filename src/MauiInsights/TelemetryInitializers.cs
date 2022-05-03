using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace MauiInsights
{
    internal class AdditionalPropertiesInitializer : ITelemetryInitializer
    {
        private readonly IDictionary<string, string> _additionalProperties;
        public AdditionalPropertiesInitializer(IDictionary<string, string> additionalProperties)
        {
            _additionalProperties = additionalProperties;
        }
        public void Initialize(ITelemetry item)
        {
            if (item is ISupportProperties supportProperties)
            {
                foreach(var property in _additionalProperties)
                {
                    supportProperties.Properties[property.Key] = property.Value;
                }
            }
        }
    }

    internal class ApplicationInfoInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry item)
        {
            item.Context.Component.Version = AppInfo.VersionString;
            item.Context.Device.Model = DeviceInfo.Model;
            item.Context.Device.OemName = DeviceInfo.Manufacturer;
            item.Context.Device.OperatingSystem = DeviceInfo.Platform.ToString();
            item.Context.Device.Type = DeviceInfo.DeviceType.ToString();
            item.Context.Cloud.RoleName = AppInfo.PackageName;
        }
    }
}
