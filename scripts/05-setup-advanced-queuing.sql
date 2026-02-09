-- Connect to the PDB
CONNECT syncuser/syncpass@XEPDB1;

-- 1. Create Queue Payload Type (Object Type)
CREATE OR REPLACE TYPE SYNC_CHANGE_PAYLOAD AS OBJECT (
    CHANGE_ID NUMBER,
    TABLE_NAME VARCHAR2(100),
    RECORD_ID NUMBER,
    OPERATION VARCHAR2(10),
    CHANGE_TIMESTAMP TIMESTAMP,
    DATA_JSON CLOB
);
/

-- 2. Create Queue Table
BEGIN
    DBMS_AQADM.CREATE_QUEUE_TABLE(
        queue_table        => 'SYNC_CHANGES_QUEUE_TABLE',
        queue_payload_type => 'SYNC_CHANGE_PAYLOAD',
        sort_list          => 'PRIORITY,ENQ_TIME',
        multiple_consumers => TRUE,
        message_grouping   => DBMS_AQADM.NONE,
        comment            => 'Queue Table for Sync Changes'
    );
END;
/

-- 3. Create Queue
BEGIN
    DBMS_AQADM.CREATE_QUEUE(
        queue_name     => 'SYNC_CHANGES_QUEUE',
        queue_table    => 'SYNC_CHANGES_QUEUE_TABLE',
        queue_type     => DBMS_AQADM.NORMAL_QUEUE,
        max_retries    => 5,
        retry_delay    => 2,
        retention_time => 86400, -- 24 hours
        comment        => 'Queue for real-time sync changes'
    );
END;
/

-- 4. Start Queue
BEGIN
    DBMS_AQADM.START_QUEUE(
        queue_name => 'SYNC_CHANGES_QUEUE'
    );
END;
/

-- 5. Grant Permissions
BEGIN
    DBMS_AQADM.GRANT_QUEUE_PRIVILEGE(
        privilege     => 'ALL',
        queue_name    => 'SYNC_CHANGES_QUEUE',
        grantee       => 'syncuser',
        grant_option  => FALSE
    );
END;
/

-- 6. Create SYNC_CHANGES table for audit/history
CREATE TABLE SYNC_CHANGES (
    CHANGE_ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    TABLE_NAME VARCHAR2(100) NOT NULL,
    RECORD_ID NUMBER NOT NULL,
    OPERATION VARCHAR2(10) NOT NULL,
    DATA_JSON CLOB,
    CHANGE_TIMESTAMP TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    PROCESSED NUMBER(1) DEFAULT 0 NOT NULL
);

CREATE INDEX IDX_SYNC_CHANGES_PROCESSED ON SYNC_CHANGES(PROCESSED);
CREATE INDEX IDX_SYNC_CHANGES_TIMESTAMP ON SYNC_CHANGES(CHANGE_TIMESTAMP);

-- 7. Create CUSTOMERS table
CREATE TABLE CUSTOMERS (
    ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    NAME VARCHAR2(200) NOT NULL,
    EMAIL VARCHAR2(200),
    PHONE VARCHAR2(50),
    CREATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    UPDATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL
);

-- 8. Create PRODUCTS table
CREATE TABLE PRODUCTS (
    ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    NAME VARCHAR2(200) NOT NULL,
    DESCRIPTION VARCHAR2(1000),
    PRICE NUMBER(10, 2),
    STOCK NUMBER DEFAULT 0,
    CREATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    UPDATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL
);

-- 9. CUSTOMERS Trigger with AQ
CREATE OR REPLACE TRIGGER TRG_CUSTOMERS_SYNC_AQ
AFTER INSERT OR UPDATE OR DELETE ON CUSTOMERS
FOR EACH ROW
DECLARE
    v_operation VARCHAR2(10);
    v_data_json CLOB;
    v_change_id NUMBER;
    v_enqueue_options DBMS_AQ.ENQUEUE_OPTIONS_T;
    v_message_properties DBMS_AQ.MESSAGE_PROPERTIES_T;
    v_message_handle RAW(16);
    v_payload SYNC_CHANGE_PAYLOAD;
BEGIN
    -- Determine operation and JSON
    IF INSERTING THEN
        v_operation := 'INSERT';
        SELECT JSON_OBJECT(
            'Id' VALUE :NEW.ID,
            'Name' VALUE :NEW.NAME,
            'Email' VALUE :NEW.EMAIL,
            'Phone' VALUE :NEW.PHONE,
            'CreatedAt' VALUE TO_CHAR(:NEW.CREATED_AT, 'YYYY-MM-DD"T"HH24:MI:SS.FF3"Z"'),
            'UpdatedAt' VALUE TO_CHAR(:NEW.UPDATED_AT, 'YYYY-MM-DD"T"HH24:MI:SS.FF3"Z"')
        ) INTO v_data_json FROM DUAL;
    ELSIF UPDATING THEN
        v_operation := 'UPDATE';
        SELECT JSON_OBJECT(
            'Id' VALUE :NEW.ID,
            'Name' VALUE :NEW.NAME,
            'Email' VALUE :NEW.EMAIL,
            'Phone' VALUE :NEW.PHONE,
            'CreatedAt' VALUE TO_CHAR(:NEW.CREATED_AT, 'YYYY-MM-DD"T"HH24:MI:SS.FF3"Z"'),
            'UpdatedAt' VALUE TO_CHAR(:NEW.UPDATED_AT, 'YYYY-MM-DD"T"HH24:MI:SS.FF3"Z"')
        ) INTO v_data_json FROM DUAL;
    ELSIF DELETING THEN
        v_operation := 'DELETE';
        SELECT JSON_OBJECT('Id' VALUE :OLD.ID) INTO v_data_json FROM DUAL;
    END IF;
    
    -- Write to SYNC_CHANGES table (for audit/history)
    INSERT INTO SYNC_CHANGES (TABLE_NAME, RECORD_ID, OPERATION, DATA_JSON)
    VALUES ('CUSTOMERS', COALESCE(:NEW.ID, :OLD.ID), v_operation, v_data_json)
    RETURNING CHANGE_ID INTO v_change_id;
    
    -- Create payload
    v_payload := SYNC_CHANGE_PAYLOAD(
        v_change_id,
        'CUSTOMERS',
        COALESCE(:NEW.ID, :OLD.ID),
        v_operation,
        SYSTIMESTAMP,
        v_data_json
    );
    
    -- Enqueue in Oracle AQ (Event-Driven!)
    DBMS_AQ.ENQUEUE(
        queue_name         => 'SYNC_CHANGES_QUEUE',
        enqueue_options    => v_enqueue_options,
        message_properties => v_message_properties,
        payload            => v_payload,
        msgid              => v_message_handle
    );
    
    -- Mark as processed (AQ takes over now)
    UPDATE SYNC_CHANGES SET PROCESSED = 1 WHERE CHANGE_ID = v_change_id;
END;
/

-- 10. PRODUCTS Trigger with AQ
CREATE OR REPLACE TRIGGER TRG_PRODUCTS_SYNC_AQ
AFTER INSERT OR UPDATE OR DELETE ON PRODUCTS
FOR EACH ROW
DECLARE
    v_operation VARCHAR2(10);
    v_data_json CLOB;
    v_change_id NUMBER;
    v_enqueue_options DBMS_AQ.ENQUEUE_OPTIONS_T;
    v_message_properties DBMS_AQ.MESSAGE_PROPERTIES_T;
    v_message_handle RAW(16);
    v_payload SYNC_CHANGE_PAYLOAD;
BEGIN
    IF INSERTING THEN
        v_operation := 'INSERT';
        SELECT JSON_OBJECT(
            'Id' VALUE :NEW.ID,
            'Name' VALUE :NEW.NAME,
            'Description' VALUE :NEW.DESCRIPTION,
            'Price' VALUE :NEW.PRICE,
            'Stock' VALUE :NEW.STOCK,
            'CreatedAt' VALUE TO_CHAR(:NEW.CREATED_AT, 'YYYY-MM-DD"T"HH24:MI:SS.FF3"Z"'),
            'UpdatedAt' VALUE TO_CHAR(:NEW.UPDATED_AT, 'YYYY-MM-DD"T"HH24:MI:SS.FF3"Z"')
        ) INTO v_data_json FROM DUAL;
    ELSIF UPDATING THEN
        v_operation := 'UPDATE';
        SELECT JSON_OBJECT(
            'Id' VALUE :NEW.ID,
            'Name' VALUE :NEW.NAME,
            'Description' VALUE :NEW.DESCRIPTION,
            'Price' VALUE :NEW.PRICE,
            'Stock' VALUE :NEW.STOCK,
            'CreatedAt' VALUE TO_CHAR(:NEW.CREATED_AT, 'YYYY-MM-DD"T"HH24:MI:SS.FF3"Z"'),
            'UpdatedAt' VALUE TO_CHAR(:NEW.UPDATED_AT, 'YYYY-MM-DD"T"HH24:MI:SS.FF3"Z"')
        ) INTO v_data_json FROM DUAL;
    ELSIF DELETING THEN
        v_operation := 'DELETE';
        SELECT JSON_OBJECT('Id' VALUE :OLD.ID) INTO v_data_json FROM DUAL;
    END IF;
    
    INSERT INTO SYNC_CHANGES (TABLE_NAME, RECORD_ID, OPERATION, DATA_JSON)
    VALUES ('PRODUCTS', COALESCE(:NEW.ID, :OLD.ID), v_operation, v_data_json)
    RETURNING CHANGE_ID INTO v_change_id;
    
    v_payload := SYNC_CHANGE_PAYLOAD(
        v_change_id,
        'PRODUCTS',
        COALESCE(:NEW.ID, :OLD.ID),
        v_operation,
        SYSTIMESTAMP,
        v_data_json
    );
    
    DBMS_AQ.ENQUEUE(
        queue_name         => 'SYNC_CHANGES_QUEUE',
        enqueue_options    => v_enqueue_options,
        message_properties => v_message_properties,
        payload            => v_payload,
        msgid              => v_message_handle
    );
    
    UPDATE SYNC_CHANGES SET PROCESSED = 1 WHERE CHANGE_ID = v_change_id;
END;
/

-- 11. Insert sample data
INSERT INTO CUSTOMERS (NAME, EMAIL, PHONE) 
VALUES ('John Doe', 'john.doe@example.com', '+1-555-0100');

INSERT INTO CUSTOMERS (NAME, EMAIL, PHONE) 
VALUES ('Jane Smith', 'jane.smith@example.com', '+1-555-0101');

INSERT INTO PRODUCTS (NAME, DESCRIPTION, PRICE, STOCK) 
VALUES ('Laptop', 'High-performance laptop', 1299.99, 10);

INSERT INTO PRODUCTS (NAME, DESCRIPTION, PRICE, STOCK) 
VALUES ('Mouse', 'Wireless mouse', 29.99, 50);

COMMIT;

-- 12. Test Query (Check Queue)
-- SELECT * FROM AQ$SYNC_CHANGES_QUEUE_TABLE;
