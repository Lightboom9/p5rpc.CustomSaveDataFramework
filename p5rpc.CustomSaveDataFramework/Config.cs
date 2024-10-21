using System.ComponentModel;
using p5rpc.CustomSaveDataFramework.Template.Configuration;
using p5rpc.CustomSaveDataFramework.Utils;
using Reloaded.Mod.Interfaces.Structs;

namespace p5rpc.CustomSaveDataFramework.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Log level")]
    [DefaultValue(LogLevel.Information)]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    
    [DisplayName("Force override custom save data location")]
    [Description("Always use the the location specified below to store custom save data.")]
    [DefaultValue(false)]
    public bool ForceOverrideCustomSaveDataLocation { get; set; } = false;
    
    [DisplayName("Custom save data location override")]
    [Description("If not on Steam or force override is enabled, uses the specified folder to save custom save data. If empty, defaults to \"Documents\\My Games\\P5R\".")]
    [DefaultValue("")]
    [FolderPickerParams(
        initialFolderPath: Environment.SpecialFolder.MyDocuments,
        userCanEditPathText: false,
        title: "Custom Folder Select",
        okButtonLabel: "Choose Folder",
        fileNameLabel: "ModFolder",
        multiSelect: false,
        forceFileSystem: true)]
    public string CustomSaveDataLocationOverride { get; set; } = "";
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}