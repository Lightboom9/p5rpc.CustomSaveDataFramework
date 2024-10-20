namespace p5r.CustomSaveDataFramework.Nodes;

public class SavedString : Node
{
    public string value { get; set; }

    public SavedString(string defaultValue = "")
    {
        value = defaultValue;
    }
}