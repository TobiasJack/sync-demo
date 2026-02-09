namespace SyncDemo.Api.Infrastructure.OracleAQ;

public class OracleQueueMessage
{
    public int ChangeId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int RecordId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public DateTime ChangeTimestamp { get; set; }
    public string DataJson { get; set; } = string.Empty;
}
