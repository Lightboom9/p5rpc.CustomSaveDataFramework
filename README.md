# Custom Save Data Framework

Custom Save Data Framework is a Reloaded-II mod that allows modders to store custom data in saves.

## Usage

Download the `p5rpc.CustomSaveDataFramework.Interfaces` package from nuget and add `p5rpc.CustomSaveDataFramework` as a dependency to your mod.
Then get the controller `ICustomSaveDataFramework` using `IModLoader`. For example, in your `Mod.cs`:
```C#
_modLoader.GetController<ICustomSaveDataFramework>().TryGetTarget(out var customSaveDataFramework);
```
`ICustomSaveDataFramework` provides access to all functions of Custom Save Data Framework.

### Adding entries

Custom Save Data Framework serializes its data as a collection of collections of key/value pairs. You can use `ICustomSaveDataFramework.AddEntry` to add an entry to the collections, that can later be saved and loaded.

The entries are simple wrappers around primitive types, such as `SavedInt` for `int`. After creating an entry and adding it to the framework, its value will be automatically stored when the game is saved, and set when it's loaded (provided the save contains it).

`AddEntry` method has three args. The first is the id of you mod, which can be any constant you like, or you can use `IModConfig.ModId`. The second is they key for the collection, and the third is the value, which is one of the previously mentioned wrappers.

A simple example for `Mod.cs`:
```C#
private readonly IModConfig _modConfig;

private readonly ICustomSaveDataFramework _customSaveDataFramework;
private readonly SavedInt _testValue;

public Mod(ModContext context) {
    _modConfig = context.ModConfig;
    
    ...
    
    _modLoader.GetController<ICustomSaveDataFramework>().TryGetTarget(out _customSaveDataFramework);
    
    _customSaveDataFramework.AddEntry(_modConfig.ModId, "test_int", _testValue = new SavedInt(123));
}

public void DoStuff() {
    _testValue.value -= 10;
}
```
In the example above, a new `SavedInt _testValue` value is created under the key `test_int` in your mod, with the default value being `123`.
That value will now be saved and loaded alongside the game's saves.

If you were to call the `DoStuff` method, that value will be reduced by `10`.
If the mod user later saves the game, the current value of `_testValue` will be saved.
If the mod user loads the game, the value of `_testValue` will be set to whatever value is in their save, if any. If the loaded save does not contain such a key/value pair, it will not be changed. As such, the wrapper may retain its default value until the first time the game is loaded.

### Unused data policy and replacing existing entries

It may so happen that when a save is loaded, it contains custom save data for which no entry was created. This may happen if you create entries conditionally in your mod, or if the mod user has disabled your mod.
If the mod user then makes a new save, the data may be lost.

`Node.UnusedDataPolicy` handles such cases, and is used to decide what to do with such data - to keep it or discard it.
Every entry has its own unused data policy, and by default it's set to `Node.UnusedDataPolicy.Keep`. You can set it using the `Node.unusedDataPolicy` property, or in the constructor:
```C#
_customSaveDataFramework.AddEntry(_modConfig.ModId, "test_int", _testValue = new SavedInt(123, Node.UnusedDataPolicy.Discard));
```

The available policies are:
- `Node.UnusedDataPolicy.Keep`: if the entry doesn't exist, it is created and added to the collection. The type, value and unused data policy of the entry remain unchanged. If the mod user makes a new save, the created but potentially unused entry will be saved.
- `Node.UnusedDataPolicy.Discard`: if the entry doesn't exist, the data will be discarded. If the mod user makes a new save, the data will be lost.

Another thing to note is the replacement of existing entries. If you were to create an entry for the key that is already in use (if, for example, you created another entry earlier for the same key or it was created by unused data policy), the entry's data wrapper will be replaced with the new one.
If both the old wrapper and the new one are of the same type, the value (and only the value) of the old one will be copied to the new one. If the wrappers are of different types, the data of the old one will be discarded.
If you want to avoid the value being copied, you can remove the entry first with the `ICustomSaveDataFramework.RemoveEntry` method.

### Overview of other functions of `ICustomSaveDataFramework`

- `bool RemoveEntry(string modId, string key)`: removes the entry for the specified mod id and key. Returns true if the entry was found and removed, false otherwise.
- `bool ContainsModKey(string modId)`: returns true if a collection of key/value pairs exists for the specified mod, false otherwise. Note that false may be returned either because there's no collection for the mod id, or because the collection does not contain they key.
- `bool TryGetEntry(string modId, string key, out Node? entry)`: attempts to retrieve an entry for the specified mod id and key. Returns true if the entry was found, false otheriwse. Note that false may be returned either because there's no collection for the mod id, or because the collection does not contain they key.
- `event Action OnGameSaved`: called after the game was saved.
- `event Action OnGameLoaded`: called after a game save was loaded.

### Available wrapper types:
- `SavedByte` (can also work with `bool` values)
- `SavedShort`
- `SavedInt`
- `SavedLong`
- `SavedFloat`
- `SavedDouble`
- `SavedString`