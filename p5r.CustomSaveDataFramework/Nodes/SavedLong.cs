namespace p5r.CustomSaveDataFramework.Nodes;

public class SavedLong : Node
{
    public long value { get; set; }

    public SavedLong(long defaultValue = default)
    {
        value = defaultValue;
    }
}