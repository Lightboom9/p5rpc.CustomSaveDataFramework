namespace p5r.CustomSaveDataFramework.Nodes;

public class SavedInt : Node
{
    public int value { get; set; }

    public SavedInt(int defaultValue = default)
    {
        value = defaultValue;
    }
}