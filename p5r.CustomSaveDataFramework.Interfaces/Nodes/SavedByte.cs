namespace p5r.CustomSaveDataFramework.Nodes;

public class SavedByte : Node
{
    public byte value { get; set; }
    
    /// <summary>
    /// Checks if the value equals 0 and returns false if it is, true otherwise.
    /// </summary>
    public bool isTrue => value != 0;

    public SavedByte(byte defaultValue = default, UnusedDataPolicy unusedDataPolicy = UnusedDataPolicy.Keep) : base(unusedDataPolicy)
    {
        value = defaultValue;
    }
}