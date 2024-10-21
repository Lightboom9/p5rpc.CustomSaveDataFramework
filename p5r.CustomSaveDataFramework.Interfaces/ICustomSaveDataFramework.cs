using p5r.CustomSaveDataFramework.Nodes;

namespace p5r.CustomSaveDataFramework.Interfaces;

/// <summary>
/// Provides access to custom save data framework.
/// </summary>
public interface ICustomSaveDataFramework
{
    /// <summary>
    /// Adds a custom save data entry. It's value will be saved when the game is saved, and written when it's loaded.
    /// </summary>
    /// <param name="modId">The id of your mod, used as the id for the key/value collection of custom data.</param>
    public void AddEntry(string modId, string key, Node entry);

    /// <summary>
    /// Removes a custom save data entry, if it exists.
    /// </summary>
    /// <returns>Returns true if element was found and removed, false otherwise.</returns>
    public bool RemoveEntry(string modId, string key);
    
    /// <summary>
    /// Checks if currently loaded custom save data has entries for specified mod id.
    /// </summary>
    /// <returns>Returns true if it does, false otherwise.</returns>
    public bool ContainsModKey(string modId);
    
    /// <summary>
    /// Attempts to get a data entry from another mod.
    /// </summary>
    /// <param name="modId">The id of the mod that has the entry.</param>
    /// <returns>Returns true if a data entry for the specified mod id and key exists, false otherwise. Note that false may be returned either because there are no entries for the specified mod, or because that specific key does not exist.</returns>
    public bool TryGetEntry(string modId, string key, out Node? entry);

    /// <summary>
    /// Called after the game and custom data was saved.
    /// </summary>
    public event Action OnGameSaved;

    /// <summary>
    /// Called after the game and custom data was loaded.
    /// </summary>
    public event Action OnGameLoaded;
}