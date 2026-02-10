# SyncDemo - Bidirektionale Datensynchronisation

Demo-Projekt für bidirektionale Datensynchronisation zwischen .NET MAUI App und ASP.NET Core API.

## 🎯 Überblick

Dieses Projekt demonstriert eine produktionsreife Implementierung der bidirektionalen Datensynchronisation zwischen mobilen und Desktop-Apps und einem Backend-API mit folgenden Technologien:

- ✅ **.NET 8** - Moderne .NET-Plattform
- ✅ **ASP.NET Core Web API** - RESTful API mit SignalR für Echtzeit-Kommunikation
- ✅ **.NET MAUI** - Cross-Platform Mobile App (Android, iOS, Windows, macOS)
- ✅ **WPF Desktop Client** - Windows Desktop Application
- ✅ **Oracle Database** - Enterprise-Datenbank mit Dapper ORM
- ✅ **Oracle Advanced Queuing (AQ)** - Event-Driven Messaging für Echtzeit-Synchronisation
- ✅ **RabbitMQ** - Message Queue für asynchrone Kommunikation
- ✅ **Realm.NET** - Lokale Mobile-Datenbank
- ✅ **MVVM Pattern** - Mit CommunityToolkit.Mvvm
- ✅ **Docker Compose** - Einfaches Setup der Infrastruktur

## 🏗️ Architektur

```
┌─────────────────────┐  ┌─────────────────────┐
│   .NET MAUI App     │  │    WPF Desktop      │
│   ┌─────────────┐   │  │   ┌─────────────┐   │
│   │  Realm DB   │   │◄─┼───┤  Realm DB   │   │
│   └─────────────┘   │  │   └─────────────┘   │
│   ┌─────────────┐   │  │   ┌─────────────┐   │ Real-time
│   │  SignalR    │◄──┼──┼───┤  SignalR    │◄──┤ Sync
│   │  Client     │   │  │   │  Client     │   │
│   └─────────────┘   │  │   └─────────────┘   │
└─────────────────────┘  └─────────────────────┘
          │                        │
          │        HTTP/REST       │
          └───────────┬────────────┘
                      ▼
┌─────────────────────────────────────┐
│       ASP.NET Core API              │
│   ┌─────────────┐   ┌───────────┐  │
│   │ Controllers │   │  SignalR  │  │
│   └──────┬──────┘   │    Hub    │  │
│          │          └─────▲─────┘  │
│   ┌──────▼──────┐         │        │
│   │ Repository  │         │        │
│   └──────┬──────┘         │        │
│          │                │        │
│   ┌──────▼──────┐  ┌──────┴─────┐ │
│   │   Dapper    │  │  RabbitMQ  │ │
│   └──────┬──────┘  │  Service   │ │
│          │         └──────┬─────┘ │
└──────────┼────────────────┼───────┘
           │                │
     ┌─────▼─────┐    ┌─────▼─────┐
     │  Oracle   │    │ RabbitMQ  │
     │ Database  │    │   Queue   │
     └───────────┘    └───────────┘
```

## 🚀 Event-Driven Architecture mit Oracle Advanced Queuing

### Neue Architektur (ab Version 2.0)

Das System nutzt **Oracle Advanced Queuing (AQ)** für ereignis-gesteuerte Echtzeit-Synchronisation:

```
┌─────────────┐         ┌──────────────┐         ┌─────────────┐
│  API        │         │  Oracle DB   │         │  Clients    │
│  Controller │         │              │         │             │
└──────┬──────┘         └──────┬───────┘         └─────────────┘
       │                       │                          
       │ INSERT/UPDATE/DELETE  │                          
       └──────────────────────►│                          
                               │ Trigger                   
                               │   ↓                       
                               │ AQ Enqueue               
                               │   ↓                       
                        ┌──────▼───────┐                  
                        │ Oracle AQ    │                  
                        │ Queue        │                  
                        └──────┬───────┘                  
                               │ Event (Real-Time)                          
                        ┌──────▼───────┐                  
                        │ Queue        │                  
                        │ Listener     │                  
                        └──────┬───────┘                  
                               │                          
                     ┌─────────┴─────────┐               
                     │                   │               
              ┌──────▼───────┐   ┌──────▼───────┐       
              │  SignalR     │   │  RabbitMQ    │       
              │  (Online)    │   │  (Offline)   │       
              └──────┬───────┘   └──────┬───────┘       
                     │                   │               
                     └─────────┬─────────┘               
                               │                          
                        ┌──────▼───────┐                  
                        │   Clients    │                  
                        └──────────────┘                  
```

### Vorteile der Event-Driven Architektur

✅ **Echtzeit statt Polling** - Keine Verzögerung, sofortige Benachrichtigung  
✅ **Geringere Datenbank-Last** - Keine ständigen SELECT-Queries mehr  
✅ **Hochskalierbar** - Oracle AQ ist für High-Throughput optimiert  
✅ **Transaktions-sicher** - AQ garantiert Delivery mit ACID-Eigenschaften  
✅ **Enterprise-Grade** - Professionelle Messaging-Lösung von Oracle  
✅ **Entkoppelt** - Controller kennen keine Clients, nur Datenbank-Operationen

### Workflow

1. **Controller** führt INSERT/UPDATE/DELETE auf `CUSTOMERS` oder `PRODUCTS` aus
2. **Oracle Trigger** wird automatisch ausgeführt und:
   - Schreibt Änderung in `SYNC_CHANGES` Tabelle (Audit)
   - Erstellt JSON-Payload mit allen Daten
   - Sendet Message an Oracle AQ Queue
3. **OracleQueueListener** (Background Service) empfängt Message sofort
4. **Permission Check** - Prüft welche Devices berechtigt sind
5. **Verteilung**:
   - **Online Devices**: Direktes Senden via SignalR (WebSocket)
   - **Offline Devices**: Speichern in RabbitMQ Queue für späteren Abruf

### Unterstützte Entitäten

- ✅ **CUSTOMERS** - Kundendaten mit Real-Time Sync
- ✅ **PRODUCTS** - Produktdaten mit Real-Time Sync
- ✅ **SYNCITEMS** - Legacy-Unterstützung (via Polling)

### Controller-Vereinfachung

Die Controller sind extrem vereinfacht - sie enthalten **keine** SignalR- oder RabbitMQ-Logik mehr:

```csharp
[HttpPost]
public async Task<ActionResult<int>> Create([FromBody] Customer customer)
{
    // Nur DB-Operation - Oracle Trigger + AQ übernehmen den Rest!
    var id = await _repository.CreateAsync(customer);
    
    return CreatedAtAction(nameof(GetById), new { id }, id);
}
```

Der gesamte Synchronisations-Workflow wird durch Oracle-Trigger und den OracleQueueService automatisch abgewickelt.

## 📂 Projektstruktur

```
sync-demo/
├── src/
│   ├── SyncDemo.Api/           # ASP.NET Core Web API
│   │   ├── Controllers/        # API Controllers
│   │   ├── Data/              # Repository & DB Connection
│   │   ├── Hubs/              # SignalR Hubs
│   │   └── Services/          # RabbitMQ Service
│   │
│   ├── SyncDemo.MauiApp/      # .NET MAUI Mobile App
│   │   ├── Data/              # Realm Service
│   │   ├── Models/            # Realm Models
│   │   ├── Services/          # Sync & SignalR Services
│   │   ├── ViewModels/        # MVVM ViewModels
│   │   ├── Views/             # XAML Views
│   │   └── Resources/         # App Resources
│   │
│   ├── SyncDemo.WpfApp/       # WPF Desktop Client
│   │   ├── Models/            # Realm Models
│   │   ├── Services/          # Sync & SignalR Services
│   │   ├── ViewModels/        # MVVM ViewModels
│   │   ├── Views/             # XAML Views
│   │   └── Converters/        # Value Converters
│   │
│   └── SyncDemo.Shared/       # Shared Models
│       └── Models/            # DTOs & Shared Types
│
├── scripts/
│   └── init-oracle.sql        # Oracle DB Initialization
│
├── docker-compose.yml          # Docker Compose Configuration
├── Dockerfile                  # API Docker Image
└── SyncDemo.slnx              # Solution File
```

## 🚀 Quick Start

### Voraussetzungen

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) oder [VS Code](https://code.visualstudio.com/)
- **Für MAUI-Entwicklung**: 
  - Windows: MAUI Workload mit Visual Studio 2022
  - macOS: MAUI Workload und Xcode
  - Linux: MAUI ist nur für API-Entwicklung verfügbar (kein App-Build)
  - Workload installieren: `dotnet workload install maui` (nur auf Windows/macOS)

**Hinweis:** Die MAUI App kann nur auf Windows oder macOS gebaut werden. Unter Linux kann nur die API entwickelt und getestet werden.

### 1. Infrastruktur starten (Oracle & RabbitMQ)

```bash
# Docker Container starten
docker-compose up -d

# Container-Status prüfen
docker-compose ps

# Logs anzeigen
docker-compose logs -f
```

**Services:**
- Oracle Database: `localhost:1521` (User: `syncuser`, Password: `syncpass123`)
- RabbitMQ Management: http://localhost:15672 (User: `guest`, Password: `guest`)
- RabbitMQ AMQP: `localhost:5672`

**Test-Benutzer:**

Die Datenbank wird mit drei Test-Benutzern initialisiert:
- `admin` (Rolle: ADMIN) - Voller Zugriff
- `user1` (Rolle: USER) - Standard-Benutzerrechte
- `viewer` (Rolle: VIEWER) - Nur Lesezugriff

### 2. API starten

```bash
cd src/SyncDemo.Api
dotnet restore
dotnet run
```

Die API läuft auf: http://localhost:5000

**Swagger UI:** http://localhost:5000/swagger

**SignalR Hub:** http://localhost:5000/synchub

### 3. MAUI App starten

```bash
cd src/SyncDemo.MauiApp
dotnet restore

# Für Android
dotnet build -f net8.0-android

# Für iOS
dotnet build -f net8.0-ios

# Für Windows
dotnet build -f net8.0-windows10.0.19041.0

# Für macOS
dotnet build -f net8.0-maccatalyst
```

### 4. WPF Desktop Client starten (Alternative zur MAUI App)

**Windows:**
```bash
cd src/SyncDemo.WpfApp
dotnet restore
dotnet run
```

Der WPF Client bietet:
- ✅ Vollständige Desktop-Erfahrung für Windows
- ✅ Gleiche Synchronisations-Features wie MAUI App
- ✅ DataGrid-basierte Anzeige von Sync Items
- ✅ Modern WPF UI Design
- ✅ SignalR Echtzeit-Updates
- ✅ Realm DB für lokale Datenspeicherung
- ✅ Mehrere Instanzen parallel (verschiedene Device-IDs)

**Hinweis:** Der WPF Client kann nur auf Windows gebaut und ausgeführt werden.

**Weitere Informationen:** Siehe [WPF Client Dokumentation](docs/WPF_CLIENT.md)

## 🔧 Konfiguration

### API Configuration (appsettings.json)

```json
{
  "ConnectionStrings": {
    "OracleConnection": "Data Source=localhost:1521/XEPDB1;User Id=syncuser;Password=syncpass123;"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest"
  }
}
```

### MAUI App Configuration

Die API-URLs sind in den Service-Klassen konfiguriert:
- `SignalRService.cs`: SignalR Hub URL
- `SyncService.cs`: API Base URL

Für Entwicklung auf Android-Emulator verwenden Sie `10.0.2.2` statt `localhost`.

## 🔄 Synchronisations-Workflow

### 1. Datenerstellung in der App

```
App → Realm DB (lokal speichern)
    → REST API (POST /api/syncitems)
    → Oracle DB (persistieren)
    → RabbitMQ (Message Queue)
    → SignalR (broadcast an alle Clients)
```

### 2. Datenänderung in der App

```
App → Realm DB (lokal aktualisieren)
    → REST API (PUT /api/syncitems/{id})
    → Oracle DB (aktualisieren)
    → RabbitMQ (Message Queue)
    → SignalR (broadcast an alle Clients)
```

### 3. Datenlöschung

```
App → Realm DB (soft delete)
    → REST API (DELETE /api/syncitems/{id})
    → Oracle DB (soft delete)
    → RabbitMQ (Message Queue)
    → SignalR (broadcast an alle Clients)
```

### 4. Synchronisation vom Server

```
App → REST API (GET /api/syncitems/sync?since={datetime})
    → Oracle DB (geänderte Daten abrufen)
    → App → Realm DB (lokale Daten aktualisieren)
```

## 📡 API Endpoints

### SyncItems Controller

- `GET /api/syncitems` - Alle Items abrufen
- `GET /api/syncitems/{id}` - Item nach ID abrufen
- `POST /api/syncitems` - Neues Item erstellen
- `PUT /api/syncitems/{id}` - Item aktualisieren
- `DELETE /api/syncitems/{id}` - Item löschen (soft delete)
- `GET /api/syncitems/sync?since={datetime}` - Geänderte Items seit Zeitpunkt abrufen

### Customers Controller (Event-Driven mit Oracle AQ)

- `GET /api/customers` - Alle Kunden abrufen
- `GET /api/customers/{id}` - Kunde nach ID abrufen
- `POST /api/customers` - Neuen Kunden erstellen (Oracle Trigger + AQ)
- `PUT /api/customers/{id}` - Kunde aktualisieren (Oracle Trigger + AQ)
- `DELETE /api/customers/{id}` - Kunde löschen (Oracle Trigger + AQ)

### Products Controller (Event-Driven mit Oracle AQ)

- `GET /api/products` - Alle Produkte abrufen
- `GET /api/products/{id}` - Produkt nach ID abrufen
- `POST /api/products` - Neues Produkt erstellen (Oracle Trigger + AQ)
- `PUT /api/products/{id}` - Produkt aktualisieren (Oracle Trigger + AQ)
- `DELETE /api/products/{id}` - Produkt löschen (Oracle Trigger + AQ)

### SignalR Hub Events

- `SendSyncUpdate(SyncMessage)` - Update an Server senden
- `ReceiveSyncUpdate(SyncMessage)` - Update vom Server empfangen

## 🧪 Testen

### API testen mit curl

```bash
# Alle Items abrufen
curl http://localhost:5000/api/syncitems

# Neues Item erstellen
curl -X POST http://localhost:5000/api/syncitems \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Item",
    "description": "Created via curl"
  }'

# Sync abrufen
curl "http://localhost:5000/api/syncitems/sync?since=2024-01-01T00:00:00Z"

# Customer erstellen (Oracle AQ Event-Driven)
curl -X POST http://localhost:5000/api/customers \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Max Mustermann",
    "email": "max@example.com",
    "phone": "+49-123-456789"
  }'

# Alle Kunden abrufen
curl http://localhost:5000/api/customers

# Produkt erstellen (Oracle AQ Event-Driven)
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Premium Laptop",
    "description": "High-End Workstation",
    "price": 1999.99,
    "stock": 5
  }'

# Alle Produkte abrufen
curl http://localhost:5000/api/products
```

### RabbitMQ Management UI

Öffnen Sie http://localhost:15672 und melden Sie sich mit `guest`/`guest` an, um:
- Queues zu überwachen
- Messages zu verfolgen
- Exchange-Konfiguration zu prüfen

### Oracle Database

```bash
# Mit Oracle verbinden
docker exec -it syncdemo-oracle sqlplus syncuser/syncpass123@XEPDB1

# Tabelle abfragen
SELECT * FROM SyncItems;
```

## 🛠️ Entwicklung

### Solution bauen

```bash
dotnet build SyncDemo.sln
```

### Tests ausführen (wenn vorhanden)

```bash
dotnet test
```

### Docker Image bauen

```bash
docker build -t syncdemo-api .
```

## 📚 Technologie-Details

### ASP.NET Core API
- **SignalR**: WebSocket-basierte Echtzeit-Kommunikation
- **Dapper**: Leichtgewichtiger ORM für Oracle
- **RabbitMQ.Client**: AMQP-Client für Message Queue

### .NET MAUI App
- **CommunityToolkit.Mvvm**: Source Generators für MVVM
- **Realm.NET**: Mobile-first NoSQL-Datenbank
- **SignalR Client**: Echtzeit-Verbindung zum Backend

### Datenbank
- **Oracle Express Edition**: Enterprise-Datenbank
- **Soft Deletes**: Daten werden markiert, nicht gelöscht
- **Version Control**: Optimistic Locking mit Version-Feld

## 🔐 Device-spezifische Berechtigungen

Das System unterstützt **granulare Zugriffskontrolle** auf Geräte- und Benutzerebene:

### Rollen

Das System definiert drei Standard-Benutzerrollen:

- **ADMIN**: Voller Zugriff auf alle Daten und Operationen
- **USER**: Read-Zugriff auf SyncItems (erweiterbar für Customers & Products)
- **VIEWER**: Nur Read-Zugriff auf ausgewählte Entity-Typen

### Device-Registrierung

Jedes Gerät muss sich vor der Synchronisation beim Server registrieren:

```bash
POST /api/device/register
Content-Type: application/json

{
  "deviceId": "unique-device-id",
  "deviceName": "My-Desktop",
  "deviceType": "WPF",
  "username": "user1"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Device registered successfully",
  "device": {
    "deviceId": "unique-device-id",
    "deviceName": "My-Desktop",
    "userId": 2,
    "deviceType": "WPF",
    "registeredAt": "2024-01-15T10:30:00Z",
    "lastSeen": "2024-01-15T10:30:00Z",
    "isActive": true
  },
  "permissions": [
    {
      "permissionId": 1,
      "deviceId": "unique-device-id",
      "entityType": "SYNCITEMS",
      "entityId": null,
      "permissionType": "READ",
      "grantedAt": "2024-01-15T10:30:00Z",
      "grantedBy": null
    }
  ]
}
```

### Berechtigungs-System

#### Datenbank-Schema

Das System verwendet vier Haupttabellen für Berechtigungen:

1. **USERS**: Benutzer-Verwaltung mit Rollen
2. **DEVICES**: Registrierte Geräte
3. **DEVICE_PERMISSIONS**: Granulare Berechtigungen pro Gerät und Entity
4. **USER_DATA_SCOPE**: (Optional) User-basierte Daten-Einschränkungen

#### Berechtigungs-Typen

- **READ**: Lesezugriff auf Daten
- **WRITE**: Schreibzugriff (erstellen/aktualisieren)
- **DELETE**: Löschzugriff
- **ALL**: Alle Berechtigungen

#### Entity-Typen

- **SYNCITEMS**: Sync-Items (aktuell implementiert)
- **CUSTOMERS**: Kundendaten (vorbereitet für Erweiterung)
- **PRODUCTS**: Produktdaten (vorbereitet für Erweiterung)
- **ALL**: Alle Entity-Typen

### Client-Verwendung

#### WPF Client

```csharp
// Bei der Verbindung Username angeben
await _syncService.ConnectAsync(deviceId, username);
```

#### MAUI Client

```csharp
// Device zuerst registrieren
await _syncService.RegisterDeviceAsync(deviceId, username);

// Dann synchronisieren
var result = await _syncService.SyncWithServerAsync(deviceId);
```

### Testing-Szenario

```bash
# Client 1: Admin User
Username: admin
Device ID: admin-device-001
→ Sieht alle SyncItems mit vollen Rechten

# Client 2: Regular User  
Username: user1
Device ID: user-device-001
→ Sieht alle SyncItems (READ only)

# Client 3: Viewer User
Username: viewer
Device ID: viewer-device-001
→ Sieht nur SyncItems (READ only)

# API: Erstelle neues SyncItem
→ Update wird an alle berechtigten Devices gesendet
```

### Erweiterung für Customers & Products

Das System ist vorbereitet für die Erweiterung mit weiteren Entity-Typen wie Customers und Products. Dazu müssen folgende Schritte durchgeführt werden:

1. Neue Repository-Klassen erstellen (z.B. `CustomerRepository`, `ProductRepository`)
2. Permission-Checks in entsprechenden Controllern implementieren
3. Standard-Berechtigungen in `DeviceController.GrantDefaultPermissionsAsync()` erweitern
4. Client-Modelle und Views für neue Entity-Typen hinzufügen

## 🔒 Sicherheit

**Hinweis:** Dies ist ein Demo-Projekt. Für Produktion sollten Sie:
- Authentifizierung/Autorisierung implementieren (z.B. JWT)
- HTTPS verwenden
- Starke Passwörter verwenden
- Secrets Management einrichten
- Input-Validierung verstärken
- Rate Limiting hinzufügen

## 🐳 Docker Setup

### Infrastruktur starten

```bash
cd docker
docker-compose up -d
```

### Oracle Init Scripts

Alle SQL Scripts im `docker/init-scripts/` Verzeichnis werden **automatisch** beim ersten Container-Start ausgeführt:

```
docker/init-scripts/
├── 00-grant-aq-permissions.sql    ← Als SYS (AQ Permissions)
├── 01-create-user.sql             ← Als SYS (User Creation)
├── 02-init-oracle.sql             ← Als syncuser (Basis-Tabellen)
└── 05-setup-advanced-queuing.sql  ← Als syncuser (Oracle AQ + CUSTOMERS/PRODUCTS)
```

**Wichtig:** Scripts werden **alphabetisch** ausgeführt!

**Hinweis:** Die Nummern 03 und 04 sind für zukünftige Init Scripts reserviert (z.B. Test-Daten, zusätzliche Permissions).

### Container neu aufsetzen

Wenn du Änderungen an den Init Scripts machst:

```bash
# Stoppe Container UND lösche Volumes
docker-compose down -v

# WICHTIG: -v löscht persistente Daten!
# Ohne -v werden alte Daten behalten und Scripts NICHT neu ausgeführt

# Starte neu
docker-compose up -d

# Logs verfolgen
docker logs -f syncdemo-oracle

# Warte auf: "DATABASE IS READY TO USE!"
```

### Init Script Logs prüfen

```bash
# Alle Init Script Logs anzeigen
docker logs syncdemo-oracle | grep -E "(Script|✅|❌)"

# Erwartete Ausgabe:
# [Script 00] ✅ AQ Permissions granted to syncuser
# [Script 01] ✅ Basic privileges granted to syncuser
# [Script 02] ✅ Tables created
# [Script 05] ✅ Oracle Advanced Queuing setup completed successfully!
```

### Verify Setup

```bash
docker exec -it syncdemo-oracle sqlplus syncuser/syncpass123@XEPDB1 <<EOF
SELECT table_name FROM user_tables;
EXIT;
EOF
```

## 🐛 Troubleshooting

### Oracle AQ Permission Fehler

**Problem:**
```
PLS-00201: ID 'DBMS_AQADM' muss deklariert werden
```

**Lösung:**

Die Oracle Init Scripts müssen in der richtigen Reihenfolge ausgeführt werden:

```bash
# 1. Container komplett neu aufsetzen
cd docker
docker-compose down -v  # -v löscht Volumes!

# 2. Container neu starten
docker-compose up -d

# 3. Logs prüfen (dauert 2-3 Minuten)
docker logs -f syncdemo-oracle

# Erfolgsmeldungen:
# ✅ AQ Permissions granted to syncuser
# ✅ SYNC_CHANGE_PAYLOAD type created
# ✅ Queue Table created
# ✅ Queue created
# ✅ Queue started
```

**Manuelle Verification:**

```bash
# Als syncuser einloggen
docker exec -it syncdemo-oracle sqlplus syncuser/syncpass123@XEPDB1

-- Prüfe AQ Permissions
SELECT PRIVILEGE FROM USER_SYS_PRIVS WHERE PRIVILEGE LIKE '%AQ%';
SELECT GRANTED_ROLE FROM USER_ROLE_PRIVS WHERE GRANTED_ROLE LIKE '%AQ%';

-- Prüfe Queue Setup
SELECT COUNT(*) FROM USER_QUEUE_TABLES;  -- Sollte 1 sein
SELECT COUNT(*) FROM USER_QUEUES;        -- Sollte 1 sein
SELECT COUNT(*) FROM USER_TYPES WHERE TYPE_NAME = 'SYNC_CHANGE_PAYLOAD';  -- Sollte 1 sein

EXIT;
```

**Falls Init Scripts fehlschlagen:**

Führe manuell als SYSTEM User aus:

```bash
docker exec -it syncdemo-oracle sqlplus system/OraclePwd123@XEPDB1

GRANT EXECUTE ON DBMS_AQADM TO syncuser;
GRANT EXECUTE ON DBMS_AQ TO syncuser;
GRANT AQ_ADMINISTRATOR_ROLE TO syncuser;
GRANT AQ_USER_ROLE TO syncuser;
GRANT CREATE TYPE TO syncuser;
COMMIT;
EXIT;
```

Dann führe `05-setup-advanced-queuing.sql` erneut aus.

### Oracle Container startet nicht
```bash
docker-compose logs oracle
# Warten Sie ca. 2-3 Minuten beim ersten Start
```

### API kann nicht mit Oracle verbinden
```bash
# Prüfen Sie, ob Oracle bereit ist
docker exec syncdemo-oracle sqlplus -s sys/OraclePwd123@localhost:1521/XE as sysdba <<< 'SELECT 1 FROM DUAL;'
```

### MAUI App kann nicht mit API verbinden
- Für Android-Emulator: Verwenden Sie `10.0.2.2` statt `localhost`
- Für iOS-Simulator: Verwenden Sie `localhost`
- Prüfen Sie Firewall-Einstellungen

## 📄 Lizenz

Dieses Projekt ist ein Demo-Projekt für Bildungszwecke.

## 👥 Autor

TobiasJack

## 🙏 Danksagungen

- Microsoft .NET Team
- MAUI Community
- Oracle
- RabbitMQ Team
