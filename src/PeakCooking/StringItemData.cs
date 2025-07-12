using Zorro.Core.Serizalization;

public class StringItemData : DataEntryValue
{
    public string Value = "";

    public override void SerializeValue(BinarySerializer serializer)
    {
        serializer.WriteString(Value, System.Text.Encoding.UTF8);
    }

    public override void DeserializeValue(BinaryDeserializer deserializer)
    {
        Value = deserializer.ReadString(System.Text.Encoding.UTF8);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
