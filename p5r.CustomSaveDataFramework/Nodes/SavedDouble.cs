namespace p5r.CustomSaveDataFramework.Nodes;

public class SavedDouble : Node
{
    public double value { get; set; }

    public SavedDouble(double defaultValue = default)
    {
        value = defaultValue;
    }
}