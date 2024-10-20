namespace p5r.CustomSaveDataFramework.Nodes;

public class SavedShort : Node
{
    public short value { get; set; }

    public SavedShort(short defaultValue = default)
    {
        value = defaultValue;
    }
}