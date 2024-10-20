using System.ComponentModel;
using p5r.CustomSaveDataFramework.Template.Configuration;
using p5r.CustomSaveDataFramework.Utils;

namespace p5r.CustomSaveDataFramework.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Log level")]
    [DefaultValue(LogLevel.Information)]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}