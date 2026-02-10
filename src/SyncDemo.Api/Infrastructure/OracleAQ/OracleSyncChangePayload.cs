using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace SyncDemo.Api.Infrastructure.OracleAQ;

/// <summary>
/// Factory class for Oracle UDT SYNC_CHANGE_PAYLOAD
/// Maps between Oracle UDT and C# OracleQueueMessage
/// </summary>
public class OracleSyncChangePayloadFactory : IOracleCustomTypeFactory
{
    public IOracleCustomType CreateObject()
    {
        return new OracleSyncChangePayload();
    }
}

/// <summary>
/// Oracle UDT representation of SYNC_CHANGE_PAYLOAD
/// </summary>
[OracleCustomTypeMapping("SYNCUSER.SYNC_CHANGE_PAYLOAD")]
public class OracleSyncChangePayload : IOracleCustomType, INullable
{
    private bool _isNull;

    [OracleObjectMapping("CHANGE_ID")]
    public decimal? ChangeId { get; set; }

    [OracleObjectMapping("TABLE_NAME")]
    public string? TableName { get; set; }

    [OracleObjectMapping("RECORD_ID")]
    public decimal? RecordId { get; set; }

    [OracleObjectMapping("OPERATION")]
    public string? Operation { get; set; }

    [OracleObjectMapping("CHANGE_TIMESTAMP")]
    public OracleTimeStamp? ChangeTimestamp { get; set; }

    [OracleObjectMapping("DATA_JSON")]
    public string? DataJson { get; set; }

    public bool IsNull => _isNull;

    public void FromCustomObject(OracleConnection con, object udt)
    {
        if (udt == null)
        {
            _isNull = true;
            return;
        }

        var payload = (OracleSyncChangePayload)udt;
        OracleUdt.SetValue(con, udt, 0, payload.ChangeId);
        OracleUdt.SetValue(con, udt, 1, payload.TableName);
        OracleUdt.SetValue(con, udt, 2, payload.RecordId);
        OracleUdt.SetValue(con, udt, 3, payload.Operation);
        OracleUdt.SetValue(con, udt, 4, payload.ChangeTimestamp);
        OracleUdt.SetValue(con, udt, 5, payload.DataJson);
    }

    public void ToCustomObject(OracleConnection con, object udt)
    {
        if (udt == null)
        {
            _isNull = true;
            return;
        }

        ChangeId = (decimal?)OracleUdt.GetValue(con, udt, 0);
        TableName = (string?)OracleUdt.GetValue(con, udt, 1);
        RecordId = (decimal?)OracleUdt.GetValue(con, udt, 2);
        Operation = (string?)OracleUdt.GetValue(con, udt, 3);
        ChangeTimestamp = (OracleTimeStamp?)OracleUdt.GetValue(con, udt, 4);
        DataJson = (string?)OracleUdt.GetValue(con, udt, 5);
    }

    /// <summary>
    /// Converts to OracleQueueMessage
    /// </summary>
    public OracleQueueMessage ToQueueMessage()
    {
        return new OracleQueueMessage
        {
            ChangeId = ChangeId.HasValue ? (int)ChangeId.Value : 0,
            TableName = TableName ?? string.Empty,
            RecordId = RecordId.HasValue ? (int)RecordId.Value : 0,
            Operation = Operation ?? string.Empty,
            ChangeTimestamp = ChangeTimestamp?.Value ?? DateTime.UtcNow,
            DataJson = DataJson ?? string.Empty
        };
    }
}
