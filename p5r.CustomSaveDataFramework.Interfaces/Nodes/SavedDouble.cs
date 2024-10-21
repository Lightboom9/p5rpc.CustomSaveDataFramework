namespace p5r.CustomSaveDataFramework.Nodes;

public class SavedDouble : Node
{
    public double value { get; set; }

    public SavedDouble(double defaultValue = default, UnusedDataPolicy unusedDataPolicy = UnusedDataPolicy.Keep) : base(unusedDataPolicy)
    {
        value = defaultValue;
    }
}