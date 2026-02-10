-- ============================================
-- Oracle Advanced Queuing - Permission Setup
-- Dieses Script läuft als SYS/SYSTEM User
-- und muss VOR allen anderen Scripts ausgeführt werden
-- ============================================

WHENEVER SQLERROR EXIT SQL.SQLCODE

-- Wechsel zur Pluggable Database
ALTER SESSION SET CONTAINER = XEPDB1;

-- Stelle sicher dass syncuser existiert
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count 
    FROM DBA_USERS 
    WHERE USERNAME = 'SYNCUSER';
    
    IF v_count = 0 THEN
        -- User existiert noch nicht - erstelle ihn
        EXECUTE IMMEDIATE 'CREATE USER syncuser IDENTIFIED BY syncpass123';
        EXECUTE IMMEDIATE 'GRANT CONNECT, RESOURCE TO syncuser';
        EXECUTE IMMEDIATE 'GRANT UNLIMITED TABLESPACE TO syncuser';
        DBMS_OUTPUT.PUT_LINE('✅ User syncuser created');
    ELSE
        DBMS_OUTPUT.PUT_LINE('ℹ️  User syncuser already exists');
    END IF;
END;
/

-- Grant Oracle AQ Execute Permissions
GRANT EXECUTE ON SYS.DBMS_AQADM TO syncuser;
GRANT EXECUTE ON SYS.DBMS_AQ TO syncuser;
GRANT EXECUTE ON SYS.DBMS_AQIN TO syncuser;

-- Grant Oracle AQ Roles
GRANT AQ_ADMINISTRATOR_ROLE TO syncuser;
GRANT AQ_USER_ROLE TO syncuser;

-- Grant Object Creation Privileges
GRANT CREATE TYPE TO syncuser;
GRANT CREATE SEQUENCE TO syncuser;
GRANT CREATE VIEW TO syncuser;
GRANT CREATE TRIGGER TO syncuser;

-- Grant Database Trigger Administration
GRANT ADMINISTER DATABASE TRIGGER TO syncuser;

-- Grant Access to AQ Catalog Views
GRANT SELECT ON SYS.DBA_QUEUES TO syncuser;
GRANT SELECT ON SYS.DBA_QUEUE_TABLES TO syncuser;
GRANT SELECT ON SYS.USER_QUEUE_TABLES TO syncuser;
GRANT SELECT ON SYS.USER_QUEUES TO syncuser;
GRANT SELECT ON SYS.AQ$_QUEUES TO syncuser;

-- Grant für Queue Monitoring und Management
GRANT EXECUTE ON SYS.DBMS_LOCK TO syncuser;

COMMIT;

-- Verify Grants
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM DBA_SYS_PRIVS 
    WHERE GRANTEE = 'SYNCUSER' 
    AND (PRIVILEGE LIKE '%AQ%' OR PRIVILEGE = 'EXECUTE');
    
    DBMS_OUTPUT.PUT_LINE('✅ AQ Permissions granted to syncuser');
    DBMS_OUTPUT.PUT_LINE('   Total privileges: ' || v_count);
END;
/

-- Zeige vergebene Privileges (für Debugging)
SELECT 
    PRIVILEGE,
    ADMIN_OPTION
FROM DBA_SYS_PRIVS 
WHERE GRANTEE = 'SYNCUSER'
ORDER BY PRIVILEGE;

SELECT 
    GRANTED_ROLE,
    ADMIN_OPTION
FROM DBA_ROLE_PRIVS 
WHERE GRANTEE = 'SYNCUSER'
AND GRANTED_ROLE LIKE '%AQ%';

EXIT;
