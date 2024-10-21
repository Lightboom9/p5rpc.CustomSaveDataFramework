using System.Drawing;
using p5rpc.CustomSaveDataFramework.Interfaces;
using Reloaded.Mod.Interfaces;
using p5rpc.CustomSaveDataFramework.Configuration;
using p5rpc.CustomSaveDataFramework.Template;
using p5rpc.CustomSaveDataFramework.Utils;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace p5rpc.CustomSaveDataFramework;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase, IExports
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    private readonly CustomSaveDataFramework _customSaveDataFramework;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;
        
        Log.Initialize("Custom Save Data Framework", _logger, Color.Lavender, _configuration.LogLevel);

        _modLoader.GetController<IStartupScanner>().TryGetTarget(out var scanner);

        _customSaveDataFramework = new CustomSaveDataFramework(new ScannerWrapper(scanner!, _hooks!), _configuration);
        _modLoader.AddOrReplaceController<ICustomSaveDataFramework>(_owner, _customSaveDataFramework);
    }
    
    public Type[] GetTypes() => [typeof(ICustomSaveDataFramework)];

    #region Standard Overrides

    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }

    #endregion

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion
}