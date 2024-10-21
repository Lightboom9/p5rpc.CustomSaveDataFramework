using System.Text;
using p5r.CustomSaveDataFramework.Configuration;
using p5r.CustomSaveDataFramework.Interfaces;
using p5r.CustomSaveDataFramework.Nodes;
using p5r.CustomSaveDataFramework.Utils;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;

namespace p5r.CustomSaveDataFramework;

public class CustomSaveDataFramework : ICustomSaveDataFramework
{
    [Function(CallingConventions.Microsoft)]
    private unsafe delegate int SaveGameData(uint param_1, long param_2, long param_3, long param_4, long param_5, long param_6);
    
    [Function(CallingConventions.Microsoft)]
    private unsafe delegate uint LoadGameData(long param_1, long param_2, long param_3);
    
    [Function(CallingConventions.Microsoft)]
    private unsafe delegate void GetSteamSaveDataPath(byte* path);
    
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
    
    private readonly Config _configuration;
    private readonly ScannerWrapper _scanner;
    
    private string _steamSaveDataPath = "";
    private Dictionary<string, Dictionary<string, Node>> _nodes = new();
    
    private IHook<LoadGameData>? _loadGameDataHook;
    private IHook<SaveGameData>? _saveGameDataHook;
    private IHook<GetSteamSaveDataPath>? _getSteamSaveDataPathHook;

    public event Action? OnGameSaved;
    public event Action? OnGameLoaded;
    
    public CustomSaveDataFramework(ScannerWrapper scanner, Config configuration)
    {
        _configuration = configuration;
        _scanner = scanner;
        
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

    public void AddEntry(string modId, string key, Node entry)
    {
        if (!_nodes.TryGetValue(modId, out var modNodes))
        {
            modNodes = new Dictionary<string, Node>();
            _nodes.Add(modId, modNodes);
        }
        
        modNodes.Add(key, entry);
    }

    public bool RemoveEntry(string modId, string key)
    {
        if (_nodes.TryGetValue(modId, out var modNodes))
        {
            return modNodes.Remove(key);
        }

        return false;
    }

    public bool ContainsModKey(string modId)
    {
        return _nodes.ContainsKey(modId);
    }

    public bool TryGetEntry(string modId, string key, out Node? entry)
    {
        entry = null;
        
        if (!_nodes.TryGetValue(modId, out var modNodes))
        {
            return false;
        }

        if (!modNodes.TryGetValue(key, out var node))
        {
            return false;
        }

        entry = node;
        return true;
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
                
                // Unused data policy
                binaryWriter.Write((byte)node.Value.unusedDataPolicy);
                
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

                // Unused data policy
                var unusedDataPolicy = (Node.UnusedDataPolicy)binaryReader.ReadByte();
                if (!hasModKey && unusedDataPolicy == Node.UnusedDataPolicy.Keep)
                {
                    modNode = new();
                    _nodes.Add(modId, modNode);
                    hasModKey = true;
                }
                
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
                        else if (unusedDataPolicy == Node.UnusedDataPolicy.Keep)
                        {
                            savedInt = new SavedInt(intValue);
                            modNode!.Add(key, savedInt);
                        }
                        
                        break;
                    case 1:
                        var byteValue = binaryReader.ReadByte();
                        
                        if (hasModKey && modNode!.TryGetValue(key, out var savedByteRaw) && savedByteRaw is SavedByte savedByte)
                        {
                            savedByte.value = byteValue;
                        }
                        else if (unusedDataPolicy == Node.UnusedDataPolicy.Keep)
                        {
                            savedByte = new SavedByte(byteValue);
                            modNode!.Add(key, savedByte);
                        }
                        
                        break;
                    case 2:
                        var shortValue = binaryReader.ReadInt16();
                        
                        if (hasModKey && modNode!.TryGetValue(key, out var savedShortRaw) && savedShortRaw is SavedShort savedShort)
                        {
                            savedShort.value = shortValue;
                        }
                        else if (unusedDataPolicy == Node.UnusedDataPolicy.Keep)
                        {
                            savedShort = new SavedShort(shortValue);
                            modNode!.Add(key, savedShort);
                        }
                        
                        break;
                    case 3:
                        var longValue = binaryReader.ReadInt64();
                        
                        if (hasModKey && modNode!.TryGetValue(key, out var savedLongRaw) && savedLongRaw is SavedLong savedLong)
                        {
                            savedLong.value = longValue;
                        }
                        else if (unusedDataPolicy == Node.UnusedDataPolicy.Keep)
                        {
                            savedLong = new SavedLong(longValue);
                            modNode!.Add(key, savedLong);
                        }
                        
                        break;
                    case 4:
                        var floatValue = binaryReader.ReadSingle();
                        
                        if (hasModKey && modNode!.TryGetValue(key, out var savedFloatRaw) && savedFloatRaw is SavedFloat savedFloat)
                        {
                            savedFloat.value = floatValue;
                        }
                        else if (unusedDataPolicy == Node.UnusedDataPolicy.Keep)
                        {
                            savedFloat = new SavedFloat(floatValue);
                            modNode!.Add(key, savedFloat);
                        }
                        
                        break;
                    case 5:
                        var doubleValue = binaryReader.ReadDouble();
                        
                        if (hasModKey && modNode!.TryGetValue(key, out var savedDoubleRaw) && savedDoubleRaw is SavedDouble savedDouble)
                        {
                            savedDouble.value = doubleValue;
                        }
                        else if (unusedDataPolicy == Node.UnusedDataPolicy.Keep)
                        {
                            savedDouble = new SavedDouble(doubleValue);
                            modNode!.Add(key, savedDouble);
                        }
                        
                        break;
                    case 6:
                        var stringValue = binaryReader.ReadString();
                        
                        if (hasModKey && modNode!.TryGetValue(key, out var savedStringRaw) && savedStringRaw is SavedString savedString)
                        {
                            savedString.value = stringValue;
                        }
                        else if (unusedDataPolicy == Node.UnusedDataPolicy.Keep)
                        {
                            savedString = new SavedString(stringValue);
                            modNode!.Add(key, savedString);
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
        
        OnGameLoaded?.Invoke();

        return _loadGameDataHook!.OriginalFunction(param_1, param_2, param_3);
    }

    private int SaveGameData_Custom(uint param_1, long param_2, long param_3, long param_4, long param_5, long param_6)
    {
        WriteCustomSaveData(param_1);
        
        OnGameSaved?.Invoke();

        return _saveGameDataHook!.OriginalFunction(param_1, param_2, param_3, param_4, param_5, param_6);
    }
}