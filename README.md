# SyncDemo - Bidirektionale Datensynchronisation

Demo-Projekt für bidirektionale Datensynchronisation zwischen .NET MAUI App und ASP.NET Core API.

## 🎯 Überblick

Dieses Projekt demonstriert eine produktionsreife Implementierung der bidirektionalen Datensynchronisation zwischen einer mobilen App und einem Backend-API mit folgenden Technologien:

- ✅ **.NET 8** - Moderne .NET-Plattform
- ✅ **ASP.NET Core Web API** - RESTful API mit SignalR für Echtzeit-Kommunikation
- ✅ **.NET MAUI** - Cross-Platform Mobile App (Android, iOS, Windows, macOS)
- ✅ **Oracle Database** - Enterprise-Datenbank mit Dapper ORM
- ✅ **RabbitMQ** - Message Queue für asynchrone Kommunikation
- ✅ **Realm.NET** - Lokale Mobile-Datenbank
- ✅ **MVVM Pattern** - Mit CommunityToolkit.Mvvm
- ✅ **Docker Compose** - Einfaches Setup der Infrastruktur

## 🏗️ Architektur

```
┌─────────────────────┐
│   .NET MAUI App     │
│   ┌─────────────┐   │
│   │  Realm DB   │   │◄──┐
│   └─────────────┘   │   │
│   ┌─────────────┐   │   │ Real-time
│   │  SignalR    │◄──┼───┤ Sync
│   │  Client     │   │   │
│   └─────────────┘   │   │
└─────────────────────┘   │
          │               │
          │ HTTP/REST     │
          ▼               │
┌─────────────────────────┼───┐
│   ASP.NET Core API      │   │
│   ┌─────────────┐   ┌───▼──┐│
│   │ Controllers │   │SignalR││
│   └──────┬──────┘   │  Hub  ││
│          │          └───────┘│
│   ┌──────▼──────┐            │
│   │ Repository  │            │
│   └──────┬──────┘            │
│          │                   │
│   ┌──────▼──────┐  ┌────────┤
│   │   Dapper    │  │RabbitMQ│
│   └──────┬──────┘  │Service │
│          │         └────┬───┘
└──────────┼──────────────┼────┘
           │              │
     ┌─────▼─────┐  ┌─────▼─────┐
     │  Oracle   │  │ RabbitMQ  │
     │ Database  │  │   Queue   │
     └───────────┘  └───────────┘
```

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
- Oracle Database: `localhost:1521` (User: `syncuser`, Password: `syncpass`)
- RabbitMQ Management: http://localhost:15672 (User: `guest`, Password: `guest`)
- RabbitMQ AMQP: `localhost:5672`

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

## 🔧 Konfiguration

### API Configuration (appsettings.json)

```json
{
  "ConnectionStrings": {
    "OracleConnection": "Data Source=localhost:1521/XEPDB1;User Id=syncuser;Password=syncpass;"
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
```

### RabbitMQ Management UI

Öffnen Sie http://localhost:15672 und melden Sie sich mit `guest`/`guest` an, um:
- Queues zu überwachen
- Messages zu verfolgen
- Exchange-Konfiguration zu prüfen

### Oracle Database

```bash
# Mit Oracle verbinden
docker exec -it syncdemo-oracle sqlplus syncuser/syncpass@XEPDB1

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

## 🔒 Sicherheit

**Hinweis:** Dies ist ein Demo-Projekt. Für Produktion sollten Sie:
- Authentifizierung/Autorisierung implementieren (z.B. JWT)
- HTTPS verwenden
- Starke Passwörter verwenden
- Secrets Management einrichten
- Input-Validierung verstärken
- Rate Limiting hinzufügen

## 🐛 Troubleshooting

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
