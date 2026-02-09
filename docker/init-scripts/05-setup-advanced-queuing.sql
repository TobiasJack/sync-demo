-- ============================================
-- Oracle Advanced Queuing Setup
-- WICHTIG: Benötigt Grants aus 00-grant-aq-permissions.sql!
-- ============================================

CONNECT syncuser/syncpass123@XEPDB1;

SET SERVEROUTPUT ON;

-- Prüfe ob AQ Permissions vorhanden sind
DECLARE
    v_aqadm_count NUMBER;
    v_aq_count NUMBER;
    v_role_count NUMBER;
BEGIN
    -- Prüfe EXECUTE auf DBMS_AQADM
    SELECT COUNT(*) INTO v_aqadm_count
    FROM USER_TAB_PRIVS
    WHERE TABLE_NAME = 'DBMS_AQADM' 
    AND PRIVILEGE = 'EXECUTE';
    
    -- Prüfe EXECUTE auf DBMS_AQ
    SELECT COUNT(*) INTO v_aq_count
    FROM USER_TAB_PRIVS
    WHERE TABLE_NAME = 'DBMS_AQ' 
    AND PRIVILEGE = 'EXECUTE';
    
    -- Prüfe AQ Roles
    SELECT COUNT(*) INTO v_role_count
    FROM USER_ROLE_PRIVS
    WHERE GRANTED_ROLE IN ('AQ_ADMINISTRATOR_ROLE', 'AQ_USER_ROLE');
    
    IF v_aqadm_count = 0 OR v_aq_count = 0 OR v_role_count = 0 THEN
        RAISE_APPLICATION_ERROR(-20001, 
            '❌ Missing AQ permissions! ' ||
            'DBMS_AQADM: ' || v_aqadm_count || ', ' ||
            'DBMS_AQ: ' || v_aq_count || ', ' ||
            'AQ Roles: ' || v_role_count || '. ' ||
            'Please run 00-grant-aq-permissions.sql as SYS first.');
    ELSE
        DBMS_OUTPUT.PUT_LINE('✅ AQ Permissions verified:');
        DBMS_OUTPUT.PUT_LINE('   - DBMS_AQADM: ' || v_aqadm_count);
        DBMS_OUTPUT.PUT_LINE('   - DBMS_AQ: ' || v_aq_count);
        DBMS_OUTPUT.PUT_LINE('   - AQ Roles: ' || v_role_count);
    END IF;
END;
/

-- 1. Erstelle Queue Payload Type (Object Type)
DBMS_OUTPUT.PUT_LINE('Creating SYNC_CHANGE_PAYLOAD type...');

CREATE OR REPLACE TYPE SYNC_CHANGE_PAYLOAD AS OBJECT (
    CHANGE_ID NUMBER,
    TABLE_NAME VARCHAR2(100),
    RECORD_ID NUMBER,
    OPERATION VARCHAR2(10),
    CHANGE_TIMESTAMP TIMESTAMP,
    DATA_JSON CLOB
);
/

DBMS_OUTPUT.PUT_LINE('✅ SYNC_CHANGE_PAYLOAD type created');

-- 2. Erstelle Queue Table
DBMS_OUTPUT.PUT_LINE('Creating Queue Table...');

BEGIN
    DBMS_AQADM.CREATE_QUEUE_TABLE(
        queue_table        => 'SYNC_CHANGES_QUEUE_TABLE',
        queue_payload_type => 'SYNC_CHANGE_PAYLOAD',
        sort_list          => 'PRIORITY,ENQ_TIME',
        multiple_consumers => TRUE,
        message_grouping   => DBMS_AQADM.NONE,
        comment            => 'Queue Table for real-time sync changes'
    );
    DBMS_OUTPUT.PUT_LINE('✅ Queue Table created');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -24001 THEN
            DBMS_OUTPUT.PUT_LINE('ℹ️  Queue Table already exists');
        ELSE
            RAISE;
        END IF;
END;
/

-- 3. Erstelle Queue
DBMS_OUTPUT.PUT_LINE('Creating Queue...');

BEGIN
    DBMS_AQADM.CREATE_QUEUE(
        queue_name     => 'SYNC_CHANGES_QUEUE',
        queue_table    => 'SYNC_CHANGES_QUEUE_TABLE',
        queue_type     => DBMS_AQADM.NORMAL_QUEUE,
        max_retries    => 5,
        retry_delay    => 2,
        retention_time => 86400, -- 24 Stunden
        comment        => 'Queue for real-time sync change events'
    );
    DBMS_OUTPUT.PUT_LINE('✅ Queue created');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -24006 THEN
            DBMS_OUTPUT.PUT_LINE('ℹ️  Queue already exists');
        ELSE
            RAISE;
        END IF;
END;
/

-- 4. Starte Queue
DBMS_OUTPUT.PUT_LINE('Starting Queue...');

BEGIN
    DBMS_AQADM.START_QUEUE(
        queue_name => 'SYNC_CHANGES_QUEUE'
    );
    DBMS_OUTPUT.PUT_LINE('✅ Queue started');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -24010 THEN
            DBMS_OUTPUT.PUT_LINE('ℹ️  Queue already started');
        ELSE
            RAISE;
        END IF;
END;
/

-- 5. Verify Queue Setup
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM USER_QUEUE_TABLES WHERE QUEUE_TABLE = 'SYNC_CHANGES_QUEUE_TABLE';
    DBMS_OUTPUT.PUT_LINE('Queue Tables: ' || v_count);
    
    SELECT COUNT(*) INTO v_count FROM USER_QUEUES WHERE NAME = 'SYNC_CHANGES_QUEUE';
    DBMS_OUTPUT.PUT_LINE('Queues: ' || v_count);
    
    IF v_count > 0 THEN
        DBMS_OUTPUT.PUT_LINE('✅ Oracle Advanced Queuing setup completed successfully');
    ELSE
        RAISE_APPLICATION_ERROR(-20002, '❌ Queue setup failed - Queue not found');
    END IF;
END;
/

-- 6. Create SYNC_CHANGES table for audit/history
DBMS_OUTPUT.PUT_LINE('Creating SYNC_CHANGES table...');

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

DBMS_OUTPUT.PUT_LINE('✅ SYNC_CHANGES table created');

-- 7. Create CUSTOMERS table
DBMS_OUTPUT.PUT_LINE('Creating CUSTOMERS table...');

CREATE TABLE CUSTOMERS (
    ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    NAME VARCHAR2(200) NOT NULL,
    EMAIL VARCHAR2(200),
    PHONE VARCHAR2(50),
    CREATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    UPDATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL
);

DBMS_OUTPUT.PUT_LINE('✅ CUSTOMERS table created');

-- 8. Create PRODUCTS table
DBMS_OUTPUT.PUT_LINE('Creating PRODUCTS table...');

CREATE TABLE PRODUCTS (
    ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    NAME VARCHAR2(200) NOT NULL,
    DESCRIPTION VARCHAR2(1000),
    PRICE NUMBER(10, 2),
    STOCK NUMBER DEFAULT 0,
    CREATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    UPDATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL
);

DBMS_OUTPUT.PUT_LINE('✅ PRODUCTS table created');

-- 9. Aktualisiere CUSTOMERS Trigger mit AQ
DBMS_OUTPUT.PUT_LINE('Creating CUSTOMERS trigger with AQ...');

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
    -- Bestimme Operation und erstelle JSON
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
    
    -- Schreibe in SYNC_CHANGES Table (für Audit/History)
    INSERT INTO SYNC_CHANGES (TABLE_NAME, RECORD_ID, OPERATION, DATA_JSON)
    VALUES ('CUSTOMERS', COALESCE(:NEW.ID, :OLD.ID), v_operation, v_data_json)
    RETURNING CHANGE_ID INTO v_change_id;
    
    -- Erstelle Payload für Oracle AQ
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
    
    -- Markiere Change als verarbeitet (AQ übernimmt jetzt)
    UPDATE SYNC_CHANGES SET PROCESSED = 1 WHERE CHANGE_ID = v_change_id;
    
EXCEPTION
    WHEN OTHERS THEN
        -- Log Error aber verhindere nicht die ursprüngliche Transaction
        DBMS_OUTPUT.PUT_LINE('❌ Error in CUSTOMERS trigger: ' || SQLERRM);
        -- In Produktion: Schreibe in Error-Log Table
        RAISE;
END;
/

DBMS_OUTPUT.PUT_LINE('✅ CUSTOMERS trigger created');

-- 10. Aktualisiere PRODUCTS Trigger mit AQ
DBMS_OUTPUT.PUT_LINE('Creating PRODUCTS trigger with AQ...');

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
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('❌ Error in PRODUCTS trigger: ' || SQLERRM);
        RAISE;
END;
/

DBMS_OUTPUT.PUT_LINE('✅ PRODUCTS trigger created');

-- 11. Insert sample data
DBMS_OUTPUT.PUT_LINE('Inserting sample data...');

INSERT INTO CUSTOMERS (NAME, EMAIL, PHONE) 
VALUES ('John Doe', 'john.doe@example.com', '+1-555-0100');

INSERT INTO CUSTOMERS (NAME, EMAIL, PHONE) 
VALUES ('Jane Smith', 'jane.smith@example.com', '+1-555-0101');

INSERT INTO PRODUCTS (NAME, DESCRIPTION, PRICE, STOCK) 
VALUES ('Laptop', 'High-performance laptop', 1299.99, 10);

INSERT INTO PRODUCTS (NAME, DESCRIPTION, PRICE, STOCK) 
VALUES ('Mouse', 'Wireless mouse', 29.99, 50);

DBMS_OUTPUT.PUT_LINE('✅ Sample data inserted');

COMMIT;

-- 12. Final Verification und Test
DBMS_OUTPUT.PUT_LINE('');
DBMS_OUTPUT.PUT_LINE('=== Oracle AQ Setup Verification ===');

-- Zeige Queue Table
SELECT 
    QUEUE_TABLE,
    OBJECT_TYPE,
    RECIPIENTS
FROM USER_QUEUE_TABLES;

-- Zeige Queue
SELECT 
    NAME,
    QUEUE_TABLE,
    ENQUEUE_ENABLED,
    DEQUEUE_ENABLED,
    RETENTION
FROM USER_QUEUES;

-- Zeige Trigger
SELECT 
    TRIGGER_NAME,
    TRIGGERING_EVENT,
    STATUS
FROM USER_TRIGGERS
WHERE TRIGGER_NAME LIKE '%SYNC_AQ';

DBMS_OUTPUT.PUT_LINE('');
DBMS_OUTPUT.PUT_LINE('✅ Oracle Advanced Queuing setup completed successfully!');
DBMS_OUTPUT.PUT_LINE('   Queue: SYNC_CHANGES_QUEUE');
DBMS_OUTPUT.PUT_LINE('   Queue Table: SYNC_CHANGES_QUEUE_TABLE');
DBMS_OUTPUT.PUT_LINE('   Payload Type: SYNC_CHANGE_PAYLOAD');
DBMS_OUTPUT.PUT_LINE('   Triggers: TRG_CUSTOMERS_SYNC_AQ, TRG_PRODUCTS_SYNC_AQ');

EXIT;
