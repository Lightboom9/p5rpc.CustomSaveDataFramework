namespace p5r.CustomSaveDataFramework.Nodes;

public class SavedInt : Node
{
    public int value { get; set; }

    public SavedInt(int defaultValue = default, UnusedDataPolicy unusedDataPolicy = UnusedDataPolicy.Keep) : base(unusedDataPolicy)
    {
        value = defaultValue;
    }
}