using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace SyncDemo.Api.Infrastructure.OracleAQ;

public class OracleQueueListener : IOracleQueueListener
{
    private readonly string _connectionString;
    private readonly ILogger<OracleQueueListener> _logger;
    private OracleConnection? _connection;
    private bool _isListening;

    public event EventHandler<OracleQueueMessage>? MessageReceived;

    public OracleQueueListener(string connectionString, ILogger<OracleQueueListener> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        _isListening = true;
        _connection = new OracleConnection(_connectionString);
        await _connection.OpenAsync(cancellationToken);

        _logger.LogInformation("Oracle AQ Listener started");

        while (_isListening && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var message = await DequeueMessageAsync(cancellationToken);
                
                if (message != null)
                {
                    _logger.LogInformation($"Received AQ message: {message.TableName} - {message.Operation} (ID: {message.RecordId})");
                    
                    // Trigger Event
                    MessageReceived?.Invoke(this, message);
                }
                else
                {
                    // No message, wait briefly
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in Oracle AQ Listener");
                await Task.Delay(5000, cancellationToken); // Retry after 5s
            }
        }
    }

    private async Task<OracleQueueMessage?> DequeueMessageAsync(CancellationToken cancellationToken)
    {
        if (_connection == null || _connection.State != ConnectionState.Open)
            return null;

        using var cmd = _connection.CreateCommand();
        
        // Dequeue with PL/SQL Block
        cmd.CommandText = @"
DECLARE
    v_dequeue_options DBMS_AQ.DEQUEUE_OPTIONS_T;
    v_message_properties DBMS_AQ.MESSAGE_PROPERTIES_T;
    v_message_handle RAW(16);
    v_payload SYNC_CHANGE_PAYLOAD;
BEGIN
    v_dequeue_options.wait := 1; -- wait 1 second
    v_dequeue_options.navigation := DBMS_AQ.FIRST_MESSAGE;
    
    DBMS_AQ.DEQUEUE(
        queue_name         => 'SYNC_CHANGES_QUEUE',
        dequeue_options    => v_dequeue_options,
        message_properties => v_message_properties,
        payload            => v_payload,
        msgid              => v_message_handle
    );
    
    :change_id := v_payload.CHANGE_ID;
    :table_name := v_payload.TABLE_NAME;
    :record_id := v_payload.RECORD_ID;
    :operation := v_payload.OPERATION;
    :change_timestamp := v_payload.CHANGE_TIMESTAMP;
    :data_json := v_payload.DATA_JSON;
    
    :has_message := 1;
EXCEPTION
    WHEN OTHERS THEN
        :has_message := 0;
END;";

        // Output Parameters
        cmd.Parameters.Add("change_id", OracleDbType.Int32, ParameterDirection.Output);
        cmd.Parameters.Add("table_name", OracleDbType.Varchar2, 100, null, ParameterDirection.Output);
        cmd.Parameters.Add("record_id", OracleDbType.Int32, ParameterDirection.Output);
        cmd.Parameters.Add("operation", OracleDbType.Varchar2, 10, null, ParameterDirection.Output);
        cmd.Parameters.Add("change_timestamp", OracleDbType.TimeStamp, ParameterDirection.Output);
        cmd.Parameters.Add("data_json", OracleDbType.Clob, ParameterDirection.Output);
        cmd.Parameters.Add("has_message", OracleDbType.Int32, ParameterDirection.Output);

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        var hasMessage = ((OracleDecimal)cmd.Parameters["has_message"].Value).ToInt32();
        
        if (hasMessage == 0)
            return null;

        var message = new OracleQueueMessage
        {
            ChangeId = ((OracleDecimal)cmd.Parameters["change_id"].Value).ToInt32(),
            TableName = cmd.Parameters["table_name"].Value?.ToString() ?? string.Empty,
            RecordId = ((OracleDecimal)cmd.Parameters["record_id"].Value).ToInt32(),
            Operation = cmd.Parameters["operation"].Value?.ToString() ?? string.Empty,
            ChangeTimestamp = ((OracleTimeStamp)cmd.Parameters["change_timestamp"].Value).Value,
            DataJson = ((OracleClob)cmd.Parameters["data_json"].Value).Value
        };

        return message;
    }

    public Task StopListeningAsync()
    {
        _isListening = false;
        _connection?.Close();
        _connection?.Dispose();
        
        _logger.LogInformation("Oracle AQ Listener stopped");
        
        return Task.CompletedTask;
    }
}
