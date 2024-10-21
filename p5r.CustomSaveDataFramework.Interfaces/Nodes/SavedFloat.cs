namespace p5r.CustomSaveDataFramework.Nodes;

public class SavedFloat : Node
{
    public float value { get; set; }

    public SavedFloat(float defaultValue = default, UnusedDataPolicy unusedDataPolicy = UnusedDataPolicy.Keep) : base(unusedDataPolicy)
    {
        value = defaultValue;
    }
}