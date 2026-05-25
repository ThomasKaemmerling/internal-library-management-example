# Bewerbungsaufgabe - Loesung

## Projektstruktur

- `Backend`: ASP.NET Core Web API fuer XML-Parsing und Auswertungen
- `Backend.Tests`: xUnit-Tests fuer Parser und Analytics
- `Frontend`: Angular-Frontend zur Darstellung ueberfaelliger Ausleihen

## Teil 1 - C# Backend (Kurzueberblick)

Umgesetzt wurden:

- XML-Parser mit Validierung und Fehlerlisten
- Endpunkte fuer:
  - `/api/library/overdue_loans`
  - `/api/library/most_loaned_books`
  - `/api/library/average_loan_period`
- Weitere hilfreiche Endpunkte:
  - `/api/status`
  - `/api/library/issues`
  - `/api/library/analysis`
- Unit-Tests fuer Parser- und Analytics-Logik
- Parser-Logging mit taeglicher Datei-Rollierung in `Backend/Logs/parser-YYYY-MM-DD.log`

## Teil 2 - Angular Frontend

### Mindestumfang

Umgesetzt wurden:

- Eine eigenstaendige Komponente zur Darstellung ueberfaelliger Ausleihen
- Ein Service fuer das Laden der Daten aus der API
- Ein einfacher Filter (Name, EmployeeId, Buchtitel)

## Antworten auf die Fragen

### 1) Standalone Components oder Modules - und warum?

Es wurden **Standalone Components** verwendet.

Warum:

- Weniger Boilerplate fuer eine kleine Anwendung
- Klarere Struktur pro Feature (Komponente + Template + Styles)
- Schnellere Erweiterbarkeit ohne zusaetzliches AppModule-Setup

Fuer diese Aufgabe mit bewusst kleinem Umfang ist Standalone die einfachere und passendere Wahl.

### 2) Wie werden die Daten geladen - Observables, Signals?

Im Frontend wird die Datenladung ueber **Observables** umgesetzt:

- API-Aufruf im Service (`getOverdueLoans()`)
- Komponentenlogik als Stream-basiertes ViewModel (`vm$`)
- Filterung in einer RxJS-Pipeline (`combineLatest` + `map`)
- Anzeige im Template ueber die `async`-Pipe

Dadurch ist kein manuelles `subscribe`/`unsubscribe` in der Komponente noetig.

### 3) Wie werden Ladezustaende und Fehler verarbeitet?

Lade- und Fehlerzustand werden im Datenstream modelliert:

- `startWith(...)` setzt den initialen Ladezustand (`loading: true`)
- Erfolgsfall liefert Daten plus `loading: false`
- `catchError(...)` liefert einen nutzerfreundlichen Fehlertext und beendet den Stream kontrolliert
- Das Template zeigt Ladezustand, Fehler, Parser-Hinweise und Tabelle konditional an

## Anwendung starten

### Backend

```powershell
Set-Location E:\projects\example_app\Backend
dotnet run
```

Backend laeuft standardmaessig auf `http://localhost:5256`.

### Frontend

```powershell
Set-Location E:\projects\example_app\Frontend
npm start
```

Frontend laeuft standardmaessig auf `http://localhost:4200`.

## Tests ausfuehren

```powershell
Set-Location E:\projects\example_app\Backend.Tests
dotnet test
```
