# Architektur-Dokumentation - SyncDemo

## Übersicht

Das SyncDemo-Projekt implementiert eine bidirektionale Datensynchronisation zwischen einer mobilen .NET MAUI-App und einem ASP.NET Core Web API Backend. Die Architektur nutzt moderne Technologien für Echtzeit-Kommunikation, persistente Speicherung und asynchrone Nachrichtenverarbeitung.

## Komponenten

### 1. ASP.NET Core Web API

**Verantwortlichkeiten:**
- RESTful API für CRUD-Operationen
- SignalR Hub für Echtzeit-Push-Benachrichtigungen
- Integration mit Oracle Database über Dapper
- Publishing von Änderungen an RabbitMQ

**Technologien:**
- ASP.NET Core 8.0
- SignalR für WebSocket-Kommunikation
- Dapper als Micro-ORM
- Oracle.ManagedDataAccess.Core
- RabbitMQ.Client

**Wichtige Klassen:**
- `SyncItemsController`: REST API Endpoints
- `SyncHub`: SignalR Hub für Echtzeit-Kommunikation
- `SyncItemRepository`: Datenzugriff mit Dapper
- `RabbitMqService`: Message Queue Integration

### 2. .NET MAUI Cross-Platform App

**Verantwortlichkeiten:**
- Benutzeroberfläche für alle Plattformen
- Lokale Datenspeicherung mit Realm
- Synchronisation mit Backend-API
- Empfang von Echtzeit-Updates via SignalR

**Technologien:**
- .NET MAUI 8.0
- MVVM Pattern mit CommunityToolkit.Mvvm
- Realm.NET für lokale Datenbank
- SignalR Client
- HttpClient für REST API Calls

**Wichtige Klassen:**
- `MainViewModel`: Haupt-ViewModel mit MVVM-Logik
- `RealmService`: Lokale Datenbankoperationen
- `SyncService`: Synchronisationslogik mit Backend
- `SignalRService`: Echtzeit-Verbindung zum Hub

### 3. Shared Library

**Verantwortlichkeiten:**
- Gemeinsame Datenmodelle zwischen API und App
- DTOs für Datentransfer

**Modelle:**
- `SyncItem`: Hauptdatenmodell
- `SyncMessage`: Nachrichtenformat für Sync-Operationen
- `SyncResult`: Ergebnis von Sync-Operationen

### 4. Oracle Database

**Verantwortlichkeiten:**
- Persistente Speicherung aller Daten
- ACID-Garantien
- Queryable Historie

**Schema:**
```sql
CREATE TABLE SyncItems (
    Id VARCHAR2(36) PRIMARY KEY,
    Name VARCHAR2(255) NOT NULL,
    Description VARCHAR2(1000),
    CreatedAt TIMESTAMP NOT NULL,
    ModifiedAt TIMESTAMP NOT NULL,
    IsDeleted NUMBER(1) DEFAULT 0,
    Version NUMBER DEFAULT 1
);
```

### 5. RabbitMQ Message Queue

**Verantwortlichkeiten:**
- Entkopplung von Komponenten
- Asynchrone Nachrichtenverarbeitung
- Garantierte Nachrichtenzustellung

**Queues:**
- `sync-queue`: Haupt-Queue für Sync-Nachrichten

### 6. Realm Local Database

**Verantwortlichkeiten:**
- Offline-First-Datenspeicherung in der App
- Schnelle lokale Queries
- Objekt-Modell für einfache Entwicklung

**Schema:**
```csharp
public class RealmSyncItem : RealmObject
{
    [PrimaryKey] string Id
    string Name
    string Description
    DateTimeOffset CreatedAt
    DateTimeOffset ModifiedAt
    bool IsDeleted
    int Version
}
```

## Synchronisations-Flows

### Flow 1: Create Operation (App → Server)

```
1. User erstellt Item in MAUI App
2. App speichert in Realm DB (lokal)
3. App sendet POST zu API
4. API speichert in Oracle DB
5. API publiziert zu RabbitMQ
6. API broadcastet via SignalR
7. Andere verbundene Clients empfangen Update
8. Clients aktualisieren lokale Realm DB
```

### Flow 2: Update Operation (App → Server)

```
1. User ändert Item in MAUI App
2. App aktualisiert Realm DB (lokal)
3. App sendet PUT zu API
4. API prüft Version (Optimistic Locking)
5. API aktualisiert Oracle DB
6. API erhöht Version
7. API publiziert zu RabbitMQ
8. API broadcastet via SignalR
9. Andere Clients empfangen Update
```

### Flow 3: Delete Operation (App → Server)

```
1. User löscht Item in MAUI App
2. App markiert als deleted in Realm (Soft Delete)
3. App sendet DELETE zu API
4. API markiert als deleted in Oracle (Soft Delete)
5. API publiziert zu RabbitMQ
6. API broadcastet via SignalR
7. Andere Clients empfangen Update
```

### Flow 4: Sync Operation (Server → App)

```
1. App sendet GET /sync?since={lastSyncTime}
2. API fragt Oracle nach Änderungen seit lastSyncTime
3. API gibt geänderte Items zurück
4. App vergleicht mit lokalem Realm
5. App wendet Änderungen an:
   - Neue Items: INSERT
   - Geänderte Items: UPDATE (basierend auf Version)
   - Gelöschte Items: SOFT DELETE
```

### Flow 5: Real-time Push (Server → App)

```
1. SignalR Hub broadcast "ReceiveSyncUpdate"
2. App empfängt SyncMessage
3. App extrahiert Operation (CREATE/UPDATE/DELETE)
4. App wendet Operation auf Realm an
5. App aktualisiert UI (via MVVM ObservableCollection)
```

## Konfliktauflösung

### Optimistic Locking
Jedes Item hat ein `Version`-Feld:
- Bei jedem Update wird Version erhöht
- Clients senden Version mit Update-Request
- Server prüft Version vor Update
- Bei Konflikt: Server gewinnt (Last Write Wins)

### Soft Deletes
- Items werden nie physisch gelöscht
- `IsDeleted` Flag wird gesetzt
- Erlaubt Wiederherstellung und Audit Trail

## Sicherheitsaspekte

### Aktuelle Implementierung (Demo)
- Keine Authentifizierung
- Keine Autorisierung
- Offene CORS-Policy

### Empfohlene Produktions-Maßnahmen
1. **Authentifizierung**: JWT-Tokens
2. **Autorisierung**: Role-based Access Control
3. **HTTPS**: Verschlüsselte Kommunikation
4. **API Keys**: Für externe Clients
5. **Rate Limiting**: DDoS-Schutz
6. **Input Validation**: Gegen Injection-Angriffe

## Skalierbarkeit

### Horizontale Skalierung
- **API**: Stateless, kann beliebig skaliert werden
- **SignalR**: Backplane mit Redis für Multi-Server
- **RabbitMQ**: Clustering möglich
- **Oracle**: RAC für High Availability

### Vertikale Skalierung
- Mehr CPU/RAM für intensive Workloads
- SSD-Storage für Datenbank

### Caching-Strategien
- Redis für häufig abgerufene Daten
- Client-side Caching in Realm
- HTTP Caching Headers

## Monitoring & Logging

### Empfohlene Tools
- **Application Insights**: Performance Monitoring
- **Seq/ELK**: Zentralisiertes Logging
- **Prometheus/Grafana**: Metriken
- **SignalR Backplane**: Connection Monitoring

### Key Metrics
- API Response Time
- SignalR Connection Count
- Queue Depth (RabbitMQ)
- Database Query Performance
- Sync Success Rate

## Deployment

### Entwicklung
```bash
docker-compose up -d
dotnet run --project src/SyncDemo.Api
```

### Produktion
```bash
docker-compose -f docker-compose.prod.yml up -d
```

**Empfehlungen:**
- Kubernetes für Orchestrierung
- Azure/AWS für Cloud Hosting
- CI/CD mit GitHub Actions
- Blue-Green Deployment

## Testing-Strategie

### Unit Tests
- Repository Tests mit In-Memory Oracle
- Service Tests mit Mocked Dependencies
- ViewModel Tests mit Mocked Services

### Integration Tests
- API Tests mit TestServer
- Database Tests mit Docker Oracle
- SignalR Tests mit WebSocketSharp

### E2E Tests
- Selenium für Web
- Appium für Mobile
- Postman für API

## Erweiterungsmöglichkeiten

### Kurzfristig
1. Authentifizierung/Autorisierung
2. Paginierung für große Datasets
3. Filtering/Sorting in API
4. Offline Queue für App

### Mittelfristig
1. Conflict Resolution UI
2. Batch Sync Operations
3. Incremental Sync
4. Attachments/Files Support

### Langfristig
1. Multi-Tenant Support
2. GraphQL API
3. Event Sourcing
4. CQRS Pattern

## Abhängigkeiten

### Produktionsabhängigkeiten
- Oracle Database Express (oder Enterprise)
- RabbitMQ Server
- .NET 8 Runtime
- Mobile Platform SDKs

### Entwicklungsabhängigkeiten
- .NET 8 SDK
- Docker Desktop
- Visual Studio 2022 / VS Code
- MAUI Workload

## Performance-Überlegungen

### Database
- Indexe auf ModifiedAt und IsDeleted
- Connection Pooling
- Prepared Statements via Dapper

### API
- Async/Await durchgängig
- Response Caching
- Compression

### Mobile App
- Lazy Loading in CollectionView
- Background Sync
- Delta Sync statt Full Sync

## Fazit

Die Architektur bietet eine solide Grundlage für eine produktionsreife Sync-Lösung mit:
- ✅ Echtzeit-Fähigkeiten
- ✅ Offline-First Design
- ✅ Skalierbarkeit
- ✅ Erweiterbarkeit
- ✅ Wartbarkeit

Weitere Verbesserungen sollten sich auf Sicherheit, Testing und Production-Readiness konzentrieren.
