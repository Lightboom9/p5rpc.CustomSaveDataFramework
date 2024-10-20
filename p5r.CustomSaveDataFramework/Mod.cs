using System.Drawing;
using System.Text;
using Reloaded.Mod.Interfaces;
using p5r.CustomSaveDataFramework.Template;
using p5r.CustomSaveDataFramework.Configuration;
using p5r.CustomSaveDataFramework.Nodes;
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

    private Dictionary<string, Dictionary<string, Node>> _nodes = new();

    private readonly Dictionary<Type, ushort> _typeToSerializedType = new()
    {
        { typeof(SavedInt), 0 },
        { typeof(SavedByte), 1 },
        { typeof(SavedShort), 2 },
        { typeof(SavedLong), 3 },
        { typeof(SavedFloat), 4 },
        { typeof(SavedDouble), 5 },
        { typeof(SavedString), 6 }
    };
    
    
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
        if (string.IsNullOrEmpty(_steamSaveDataPath) || _configuration.ForceOverrideCustomSaveDataLocation)
        {
            var baseFolder = string.IsNullOrEmpty(_configuration.CustomSaveDataLocationOverride)
                ? $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\My Games\P5R"
                : _configuration.CustomSaveDataLocationOverride;
            var customSaveDataFolderPath = $@"{baseFolder}\CustomSaveData\DATA{index:D2}\";
            
            if (!Directory.Exists(customSaveDataFolderPath))
            {
                Directory.CreateDirectory(customSaveDataFolderPath);
            }

            return $"{customSaveDataFolderPath}CUSTOMDATA.DAT";
        }
        
        return $@"{_steamSaveDataPath}DATA{index:D2}\CUSTOMDATA.DAT";
    }

    private void WriteCustomSaveData(uint index)
    {
        if (index is < 1 or > 16)
        {
            return;
        }

        var path = GetCustomSaveDataPath(index);
        using var writeStream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var binaryWriter = new BinaryWriter(writeStream);
        
        // Version
        binaryWriter.Write(0);
        
        // Amount of mods that store custom data
        binaryWriter.Write(_nodes.Count);

        foreach (var modNode in _nodes)
        {
            // Mod id
            binaryWriter.Write(modNode.Key);
            
            // Amount of nodes in the mod
            binaryWriter.Write(modNode.Value.Count);

            foreach (var node in modNode.Value)
            {
                // Custom data key
                binaryWriter.Write(node.Key);
                
                // Serialized type
                binaryWriter.Write(_typeToSerializedType[node.Value.GetType()]);

                // Value
                switch (node.Value)
                {
                    case SavedInt savedInt:
                        binaryWriter.Write(savedInt.value);
                        break;
                    case SavedByte savedByte:
                        binaryWriter.Write(savedByte.value);
                        break;
                    case SavedShort savedShort:
                        binaryWriter.Write(savedShort.value);
                        break;
                    case SavedLong savedLong:
                        binaryWriter.Write(savedLong.value);
                        break;
                    case SavedFloat savedFloat:
                        binaryWriter.Write(savedFloat.value);
                        break;
                    case SavedDouble savedDouble:
                        binaryWriter.Write(savedDouble.value);
                        break;
                    case SavedString savedString:
                        binaryWriter.Write(savedString.value);
                        break;
                    default:
                        throw new NotImplementedException($"Custom Save Data Framework cannot serialize data of type {node.Value.GetType()}");
                }
            }
        }
    }

    private void ReadCustomSaveData(uint index)
    {
        if (index is < 1 or > 16)
        {
            return;
        }

        var path = GetCustomSaveDataPath(index);
        if (!File.Exists(path))
        {
            return;
        }
        
        using var readStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var binaryReader = new BinaryReader(readStream);

        // Version
        binaryReader.ReadInt32();

        // Amount of mods that store custom data
        var modNodeCount = binaryReader.ReadInt32();

        for (var i = 0; i < modNodeCount; i++)
        {
            // Mod id
            var modId = binaryReader.ReadString();
            var hasModKey = _nodes.TryGetValue(modId, out var modNode);
            
            // Amount of nodes in the mod
            var nodeCount = binaryReader.ReadInt32();

            for (var j = 0; j < nodeCount; j++)
            {
                // Custom data key
                var key = binaryReader.ReadString();
                
                // Serialized type
                var dataType = binaryReader.ReadInt16();
                
                // Value
                switch (dataType)
                {
                    case 0:
                        var intValue = binaryReader.ReadInt32();
                        
                        if (hasModKey && modNode!.TryGetValue(key, out var savedIntRaw) && savedIntRaw is SavedInt savedInt)
                        {
                            savedInt.value = intValue;
                        }
                        
                        break;
                    case 1:
                        var byteValue = binaryReader.ReadByte();
                        
                        if (hasModKey && modNode!.TryGetValue(key, out var savedByteRaw) && savedByteRaw is SavedByte savedByte)
                        {
                            savedByte.value = byteValue;
                        }
                        
                        break;
                    case 2:
                        var shortValue = binaryReader.ReadInt16();
                        
                        if (hasModKey && modNode!.TryGetValue(key, out var savedShortRaw) && savedShortRaw is SavedShort savedShort)
                        {
                            savedShort.value = shortValue;
                        }
                        
                        break;
                    case 3:
                        var longValue = binaryReader.ReadInt64();
                        
                        if (hasModKey && modNode!.TryGetValue(key, out var savedLongRaw) && savedLongRaw is SavedLong savedLong)
                        {
                            savedLong.value = longValue;
                        }
                        
                        break;
                    case 4:
                        var floatValue = binaryReader.ReadSingle();
                        
                        if (hasModKey && modNode!.TryGetValue(key, out var savedFloatRaw) && savedFloatRaw is SavedFloat savedFloat)
                        {
                            savedFloat.value = floatValue;
                        }
                        
                        break;
                    case 5:
                        var doubleValue = binaryReader.ReadDouble();
                        
                        if (hasModKey && modNode!.TryGetValue(key, out var savedDoubleRaw) && savedDoubleRaw is SavedDouble savedDouble)
                        {
                            savedDouble.value = doubleValue;
                        }
                        
                        break;
                    case 6:
                        var stringValue = binaryReader.ReadString();
                        
                        if (hasModKey && modNode!.TryGetValue(key, out var savedStringRaw) && savedStringRaw is SavedString savedString)
                        {
                            savedString.value = stringValue;
                        }
                        
                        break;
                    default:
                        throw new NotImplementedException($"Custom Save Data Framework cannot deserialize data of type {dataType}");
                }
            }
        }
    }

    private unsafe void GetSteamSaveDataPath_Custom(byte* path)
    {
        var sb = new StringBuilder();
        var index = 0;
        while (path[index] != 0)
        {
            sb.Append((char)path[index]);

            index++;
        }

        _steamSaveDataPath = sb.ToString();
        
        _getSteamSaveDataPathHook!.OriginalFunction(path);
    }
    
    private uint LoadGameData_Custom(long param_1, long param_2, long param_3)
    {
        ReadCustomSaveData((uint)param_1);

        return _loadGameDataHook!.OriginalFunction(param_1, param_2, param_3);
    }

    private int SaveGameData_Custom(uint param_1, long param_2, long param_3, long param_4, long param_5, long param_6)
    {
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