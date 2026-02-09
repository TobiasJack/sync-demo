using System.Data;
using Dapper;

namespace SyncDemo.Api.Data;

/// <summary>
/// Custom Dapper type handler for converting Oracle VARCHAR2 GUID strings to System.Guid
/// </summary>
public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value)
    {
        if (value is string stringValue)
        {
            return Guid.Parse(stringValue);
        }
        
        if (value is Guid guidValue)
        {
            return guidValue;
        }
        
        throw new DataException($"Cannot convert {value?.GetType().Name ?? "null"} to Guid");
    }

    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.Value = value.ToString();
        parameter.DbType = DbType.String;
    }
}
