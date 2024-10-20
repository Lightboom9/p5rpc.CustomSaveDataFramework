using System.Drawing;
using System.Text;
using Reloaded.Mod.Interfaces;
using p5r.CustomSaveDataFramework.Template;
using p5r.CustomSaveDataFramework.Configuration;
using p5r.CustomSaveDataFramework.Utils;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace p5r.CustomSaveDataFramework;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
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
    
    
    [Function(CallingConventions.Microsoft)]
    private unsafe delegate int SaveGameData(uint param_1, long param_2, long param_3, long param_4, long param_5, long param_6);
    private IHook<SaveGameData>? _saveGameDataHook;
    
    [Function(CallingConventions.Microsoft)]
    private unsafe delegate uint LoadGameData(long param_1, long param_2, long param_3);
    private IHook<LoadGameData>? _loadGameDataHook;
    
    [Function(CallingConventions.Microsoft)]
    private unsafe delegate void GetSteamSaveDataPath(byte* path);
    private IHook<GetSteamSaveDataPath>? _getSteamSaveDataPathHook;

    
    private readonly ScannerWrapper _scanner;
    
    private string _steamSaveDataPath = "";
    
    
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
        _scanner = new ScannerWrapper(scanner!, _hooks!);

        unsafe
        {
            _scanner.GetFunctionHook<SaveGameData>("SaveGameData", "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 81 EC C0 00 00 00 49 8B D8",
                SaveGameData_Custom, hook => _saveGameDataHook = hook);
            _scanner.GetFunctionHook<LoadGameData>("LoadGameData", "48 89 E0 55 56",
                LoadGameData_Custom, hook => _loadGameDataHook = hook);
            _scanner.GetFunctionHook<GetSteamSaveDataPath>("GetSteamSaveDataPath", "48 85 C9 74 ?? 48 8D 15 ?? ?? ?? ?? 48 29 CA",
                GetSteamSaveDataPath_Custom, hook => _getSteamSaveDataPathHook = hook);
        }
    }

    private string GetCustomSaveDataPath(uint index)
    {
        if (!string.IsNullOrEmpty(_steamSaveDataPath))
        {
            return $@"{_steamSaveDataPath}DATA{index:D2}\CUSTOMDATA.DAT";
        }

        return "Get a better platform lmao"; // TODO No but this is impolite
    }

    private void WriteCustomSaveData(uint index)
    {
        if (index is < 1 or > 16)
        {
            return;
        }

        var path = GetCustomSaveDataPath(index);
        Log.Information($"DEBUGTESTLOG WriteCustomSaveData({index}), path: {path}");
    }

    private unsafe void GetSteamSaveDataPath_Custom(byte* path)
    {
        // [Custom Save Data Framework] DEBUGTESTLOG GetSaveDataPath_Custom called, the path is: C:\Users\user\AppData\Roaming\SEGA\P5R\Steam\76561198104645103\savedata\
        
        var sb = new StringBuilder();
        var index = 0;
        while (path[index] != 0)
        {
            sb.Append((char)path[index]);

            index++;
        }

        _steamSaveDataPath = sb.ToString();
        
        Log.Information($"DEBUGTESTLOG GetSaveDataPath_Custom called, the path is: {_steamSaveDataPath}");
        
        _getSteamSaveDataPathHook!.OriginalFunction(path);
    }
    
    private uint LoadGameData_Custom(long param_1, long param_2, long param_3)
    {
        Log.Information($"DEBUGTESTLOG LoadGameData_Custom called, save index: {param_1}");

        return _loadGameDataHook!.OriginalFunction(param_1, param_2, param_3);
    }

    private int SaveGameData_Custom(uint param_1, long param_2, long param_3, long param_4, long param_5, long param_6)
    {
        Log.Information($"DEBUGTESTLOG SaveGameData_Custom called, save index: {param_1}");

        WriteCustomSaveData(param_1);

        return _saveGameDataHook!.OriginalFunction(param_1, param_2, param_3, param_4, param_5, param_6);
    }

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