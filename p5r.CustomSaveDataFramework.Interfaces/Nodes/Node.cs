namespace p5r.CustomSaveDataFramework.Nodes;

public abstract class Node {
    /// <summary>
    /// What to do with data that was loaded but has no entry.
    /// </summary>
    public enum UnusedDataPolicy : byte
    {
        /// <summary>
        /// An entry will be created for it
        /// </summary>
        Keep = 0,
        /// <summary>
        /// The data will be discarded
        /// </summary>
        Discard = 1
    }

    protected Node(UnusedDataPolicy unusedDataPolicy)
    {
        this.unusedDataPolicy = unusedDataPolicy;
    }
    
    /// <summary>
    /// Although the value is serialized, it is only used if the data is unused. Otherwise it is not changed when custom data is loaded, even if it's different.
    /// </summary>
    public UnusedDataPolicy unusedDataPolicy { get; set; }
}