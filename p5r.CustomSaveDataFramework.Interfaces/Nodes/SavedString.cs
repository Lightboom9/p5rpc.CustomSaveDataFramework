namespace p5r.CustomSaveDataFramework.Nodes;

public class SavedString : Node
{
    public string value { get; set; }

    public SavedString(string defaultValue = "", UnusedDataPolicy unusedDataPolicy = UnusedDataPolicy.Keep) : base(unusedDataPolicy)
    {
        value = defaultValue;
    }
}