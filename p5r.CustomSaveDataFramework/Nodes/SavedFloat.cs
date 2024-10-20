namespace p5r.CustomSaveDataFramework.Nodes;

public class SavedFloat : Node
{
    public float value { get; set; }

    public SavedFloat(float defaultValue = default)
    {
        value = defaultValue;
    }
}