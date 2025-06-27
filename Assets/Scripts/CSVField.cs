using System;

[AttributeUsage(AttributeTargets.Field)]
public class CSVField : Attribute
{
    public CSVFIledType HeaderType;

    public CSVField(CSVFIledType headerType)
    {
        HeaderType = headerType;
    }
}
