-- Connect to the PDB
ALTER SESSION SET CONTAINER = XEPDB1;

-- Create user
CREATE USER syncuser IDENTIFIED BY syncpass;

-- Grant privileges
GRANT CONNECT, RESOURCE TO syncuser;
GRANT CREATE SESSION TO syncuser;
GRANT UNLIMITED TABLESPACE TO syncuser;

-- Connect as the new user
CONNECT syncuser/syncpass@XEPDB1;

-- Create the SyncItems table
CREATE TABLE SyncItems (
    Id VARCHAR2(36) PRIMARY KEY,
    Name VARCHAR2(255) NOT NULL,
    Description VARCHAR2(1000),
    CreatedAt TIMESTAMP NOT NULL,
    ModifiedAt TIMESTAMP NOT NULL,
    IsDeleted NUMBER(1) DEFAULT 0 NOT NULL,
    Version NUMBER DEFAULT 1 NOT NULL
);

-- Create indexes
CREATE INDEX idx_syncitems_modified ON SyncItems(ModifiedAt);
CREATE INDEX idx_syncitems_deleted ON SyncItems(IsDeleted);

-- Insert sample data
INSERT INTO SyncItems (Id, Name, Description, CreatedAt, ModifiedAt, IsDeleted, Version)
VALUES (SYS_GUID(), 'Sample Item 1', 'This is a sample item created during initialization', SYSTIMESTAMP, SYSTIMESTAMP, 0, 1);

INSERT INTO SyncItems (Id, Name, Description, CreatedAt, ModifiedAt, IsDeleted, Version)
VALUES (SYS_GUID(), 'Sample Item 2', 'Another sample item', SYSTIMESTAMP, SYSTIMESTAMP, 0, 1);

COMMIT;
