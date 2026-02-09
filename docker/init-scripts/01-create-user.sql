-- ============================================
-- User Creation & Basic Privileges
-- Hinweis: User könnte bereits durch 00-grant-aq-permissions.sql existieren
-- ============================================

ALTER SESSION SET CONTAINER = XEPDB1;

-- Prüfe ob User bereits existiert (idempotent)
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count 
    FROM DBA_USERS 
    WHERE USERNAME = 'SYNCUSER';
    
    IF v_count = 0 THEN
        -- User existiert noch nicht
        EXECUTE IMMEDIATE 'CREATE USER syncuser IDENTIFIED BY syncpass123';
        DBMS_OUTPUT.PUT_LINE('✅ User syncuser created');
    ELSE
        DBMS_OUTPUT.PUT_LINE('ℹ️  User syncuser already exists - skipping creation');
    END IF;
END;
/

-- Grant Basic Privileges (idempotent - kann mehrfach ausgeführt werden)
GRANT CONNECT TO syncuser;
GRANT RESOURCE TO syncuser;
GRANT CREATE VIEW TO syncuser;
GRANT CREATE TRIGGER TO syncuser;
GRANT UNLIMITED TABLESPACE TO syncuser;

COMMIT;

DBMS_OUTPUT.PUT_LINE('✅ Basic privileges granted to syncuser');

EXIT;
