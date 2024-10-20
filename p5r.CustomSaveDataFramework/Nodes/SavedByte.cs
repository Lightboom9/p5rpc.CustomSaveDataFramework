namespace p5r.CustomSaveDataFramework.Nodes;

public class SavedByte : Node
{
    public byte value { get; set; }
    public bool isTrue => value != 0;

    public SavedByte(byte defaultValue = default)
    {
        value = defaultValue;
    }
}