# WPF Desktop Client - Dokumentation

## Überblick

Der **SyncDemo.WpfApp** ist ein vollständiger WPF Desktop Client, der parallel zur MAUI App funktioniert und die gleiche Synchronisations-Logik verwendet.

## Features

### ✅ Implementierte Features

1. **Connection Management**
   - Device-ID Eingabe (automatisch generierte GUID)
   - Connect/Disconnect Buttons
   - Verbindungsstatus-Anzeige in Echtzeit
   - Automatisches Reconnect bei Verbindungsabbruch

2. **Daten-Anzeige**
   - Sync Items Liste mit DataGrid
   - Echtzeit-Updates bei Änderungen
   - Letzte Update-Zeit Anzeige
   - Sortier- und Filtermöglichkeiten im DataGrid

3. **Synchronisation**
   - Initiale Synchronisation beim Connect
   - Echtzeit-Updates via SignalR
   - Automatisches Nachholen verpasster Updates bei Reconnect
   - Realm DB für lokale Datenspeicherung

4. **Modern UI**
   - ModernWPF UI für modernes Design
   - Responsive Layout
   - Accentfarbe-basiertes Header-Design

## Architektur

### Technologie-Stack

- **.NET 8 WPF** (net8.0-windows)
- **RealmDB** für lokale Datenspeicherung
- **SignalR Client** für Echtzeit-Updates
- **MVVM Pattern** mit CommunityToolkit.Mvvm
- **ModernWPF** für modernes UI
- **Dependency Injection** (Microsoft.Extensions.DependencyInjection)

### Projektstruktur

```
src/SyncDemo.WpfApp/
├── SyncDemo.WpfApp.csproj
├── App.xaml                          # Application Entry Point
├── App.xaml.cs                       # DI Container Setup
├── MainWindow.xaml                   # Main Window UI
├── MainWindow.xaml.cs                # Main Window Code-Behind
├── Models/
│   └── RealmSyncItem.cs             # Realm Model für Sync Items
├── Services/
│   ├── RealmService.cs              # Realm DB Service
│   └── SyncService.cs               # SignalR & Sync Logic
├── ViewModels/
│   ├── MainViewModel.cs             # Main Window ViewModel
│   └── SyncItemsViewModel.cs        # Items List ViewModel
├── Views/
│   ├── SyncItemsView.xaml           # Items DataGrid View
│   └── SyncItemsView.xaml.cs        # View Code-Behind
└── Converters/
    └── InvertedBooleanConverter.cs  # Boolean Inversion Converter
```

## Verwendung

### Voraussetzungen

- Windows 10/11
- .NET 8 SDK
- Laufender API-Server (siehe Hauptprojekt README)

### Starten

```bash
cd src/SyncDemo.WpfApp
dotnet restore
dotnet run
```

### Workflow

1. **Starten Sie die Infrastruktur** (Oracle, RabbitMQ)
   ```bash
   docker-compose up -d
   ```

2. **Starten Sie die API**
   ```bash
   cd src/SyncDemo.Api
   dotnet run
   ```

3. **Starten Sie den WPF Client**
   ```bash
   cd src/SyncDemo.WpfApp
   dotnet run
   ```

4. **Verbinden**
   - Die Device-ID wird automatisch generiert
   - Klicken Sie auf "Verbinden"
   - Der Status zeigt "Verbunden als [GUID]"

5. **Daten beobachten**
   - Alle Sync Items werden im DataGrid angezeigt
   - Änderungen werden in Echtzeit synchronisiert
   - Mehrere Clients können parallel laufen

## Mehrere Instanzen

Sie können mehrere WPF Client-Instanzen gleichzeitig ausführen:

```bash
# Terminal 1
cd src/SyncDemo.WpfApp
dotnet run

# Terminal 2
cd src/SyncDemo.WpfApp
dotnet run

# Terminal 3
cd src/SyncDemo.WpfApp
dotnet run
```

Jede Instanz erhält eine eigene Device-ID und eine eigene Realm-Datenbank. Alle Instanzen empfangen Updates in Echtzeit.

## Service-Implementierung

### RealmService

Der `RealmService` verwaltet die lokale Realm-Datenbank:

- `GetAllItemsAsync()` - Alle nicht-gelöschten Items abrufen
- `GetItemById(id)` - Item nach ID abrufen
- `AddOrUpdateItemAsync(item)` - Item hinzufügen oder aktualisieren
- `DeleteItemAsync(id)` - Item als gelöscht markieren (Soft Delete)
- `GetItemCountAsync()` - Anzahl der Items abrufen

### SyncService

Der `SyncService` verwaltet die Synchronisation:

- `ConnectAsync(deviceId)` - Verbindung zum SignalR Hub herstellen
- `DisconnectAsync()` - Verbindung trennen
- `IsConnected` - Verbindungsstatus abfragen
- `DataUpdated` Event - Wird ausgelöst bei neuen Updates

**Features:**
- Automatische Initiale Synchronisation beim Verbinden
- Automatisches Reconnect mit Sync bei Verbindungsverlust
- Echtzeit-Updates via SignalR
- Event-basierte UI-Benachrichtigung

## MVVM Pattern

### MainViewModel

Verwaltet den Verbindungsstatus und die Gesamtanwendung:

- `DeviceId` - Eindeutige Geräte-ID
- `IsConnected` - Verbindungsstatus
- `StatusText` - Statusnachricht für Benutzer
- `ConnectCommand` - Verbinden Command
- `DisconnectCommand` - Trennen Command

### SyncItemsViewModel

Verwaltet die Liste der Sync Items:

- `Items` - ObservableCollection für DataGrid
- Automatisches Reload bei DataUpdated Events
- Thread-sicheres Update über Dispatcher

## UI-Komponenten

### MainWindow

Das Hauptfenster besteht aus:

1. **Connection Panel** (Header)
   - Device-ID Textbox
   - Connect/Disconnect Buttons
   - Status-Anzeige

2. **TabControl** (Content)
   - Tab "Sync Items" mit SyncItemsView

### SyncItemsView

DataGrid mit Spalten:
- ID (GUID)
- Name
- Beschreibung
- Erstellt (Timestamp)
- Aktualisiert (Timestamp)
- Version (Int)

## Konfiguration

### API URLs

Die URLs sind in `SyncService.cs` konfiguriert:

```csharp
private readonly string _hubUrl = "http://localhost:5000/synchub";
private readonly string _apiBaseUrl = "http://localhost:5000/api";
```

Für andere Umgebungen anpassen.

### Realm Datenbank

Die Realm-Datenbank wird in `RealmService.cs` konfiguriert:

```csharp
var config = new RealmConfiguration("syncdemo-wpf.realm")
{
    SchemaVersion = 1
};
```

Die Datenbankdatei wird im Benutzerverzeichnis gespeichert.

## Troubleshooting

### "To build a project targeting Windows..."

Der WPF Client kann nur auf Windows gebaut und ausgeführt werden. Unter Linux/macOS ist der Build nicht möglich.

### Verbindung schlägt fehl

1. Prüfen Sie, ob die API läuft: http://localhost:5000/swagger
2. Prüfen Sie die URL-Konfiguration in `SyncService.cs`
3. Prüfen Sie Firewall-Einstellungen

### Keine Updates empfangen

1. Prüfen Sie die SignalR-Verbindung (Status im UI)
2. Prüfen Sie die API-Logs
3. Testen Sie mit mehreren Client-Instanzen

### Items werden nicht angezeigt

1. Prüfen Sie, ob die initiale Synchronisation erfolgreich war
2. Prüfen Sie die Realm-Datenbank (sollte im Benutzerverzeichnis sein)
3. Prüfen Sie die Debug-Ausgabe in Visual Studio

## Erweiterungsmöglichkeiten

### Geplante Features

- 🎨 Dark/Light Theme Toggle
- 📊 Sync-Status Indikator (Connected/Disconnected Icon)
- 🔔 Toast-Benachrichtigungen bei Updates
- 📋 Detailansicht für Items beim Doppelklick
- ➕ Create/Update/Delete Funktionen im UI
- 🔍 Erweiterte Such- und Filterfunktionen

### Anpassungen

Um eigene Datenmodelle zu verwenden:

1. Erstellen Sie neue Realm-Modelle in `Models/`
2. Passen Sie den `RealmService` an
3. Erstellen Sie neue ViewModels
4. Erstellen Sie neue Views mit DataGrid
5. Registrieren Sie alles in `App.xaml.cs`

## Best Practices

### Realm Datenbank

- Verwenden Sie asynchrone Schreiboperationen (`WriteAsync`)
- Implementieren Sie Soft Deletes (IsDeleted Flag)
- Nutzen Sie Version Control für Konfliktauflösung

### SignalR

- Implementieren Sie Reconnect-Logik
- Synchronisieren Sie bei Reconnect
- Behandeln Sie Verbindungsfehler graceful

### MVVM

- Halten Sie ViewModels unabhängig von Views
- Nutzen Sie ObservableObject und ObservableProperty
- Verwenden Sie RelayCommand für Buttons

### Thread-Sicherheit

- UI-Updates nur im Dispatcher-Thread
- Realm-Zugriffe in Worker-Threads

## Lizenz

Dieses Projekt ist ein Demo-Projekt für Bildungszwecke.
