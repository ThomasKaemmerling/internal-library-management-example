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
- Unit-Tests fuer Parser- und Analytics-Logik
- Parser-Logging mit taeglicher Datei-Rollierung in `Backend/Logs/parser-YYYY-MM-DD.log`

## Teil 2 - Angular Frontend

### Mindestumfang

Umgesetzt wurden:

- Eine eigenstaendige Komponente zur Darstellung ueberfaelliger Ausleihen
- Ein Service fuer das Laden der Daten aus der API
- Ein einfacher Filter (Name, EmployeeId, Buchtitel)

## API-Ueberblick

Die wichtigsten Endpunkte des Backends sind:

- `/api/status`: einfacher Health-Check, um die Erreichbarkeit des Backends zu pruefen
- `/api/library/issues`: liefert nur die beim Parsen gefundenen Validierungsprobleme
- `/api/library/overdue_loans`: liefert ueberfaellige Ausleihen fuer das Frontend
- `/api/library/most_loaned_books`: liefert die meistgeliehenen Buecher pro Genre
- `/api/library/average_loan_period`: liefert die durchschnittliche Leihdauer

Fuer das Angular-Frontend wird vor allem `/api/library/overdue_loans` verwendet.

## Architekturentscheidungen

Bei der Umsetzung wurden einige Entscheidungen bewusst schlicht gehalten, damit die Loesung zum Umfang der Aufgabe passt.

- Das Backend verwendet **Minimal API** und keine klassischen ASP.NET-Controller. Fuer den kleinen Scope reduziert das Boilerplate und haelt die Endpunktdefinitionen direkt in `Program.cs` nachvollziehbar.
- Die XML-Datei dient als lokale Datenquelle, um den Fokus auf Parsing, Validierung und Auswertung zu legen, ohne zusaetzliche Datenbank-Infrastruktur einzufuehren.
- Parser und Analytics sind als getrennte Services modelliert: der Parser ist fuer Einlesen und Validieren zustaendig, der Analytics-Service fuer die fachlichen Berechnungen.
- Im Frontend wurden **Standalone Components** statt Angular Modules verwendet, weil die Anwendung klein ist und so mit weniger Struktur-Overhead auskommt.
- Fuer das Frontend wurde eine stream-basierte Loesung mit Observables gewaehlt, weil HTTP-Aufrufe, Filterung, Ladezustand und Fehlerbehandlung so gut in einem gemeinsamen ViewModel abgebildet werden koennen.

## Annahmen und Grenzen

Die Loesung basiert auf einigen bewussten Annahmen:

- Die XML-Datei ist relativ klein und kann ohne Probleme pro Request neu eingelesen werden.
- Die Anwendung ist als interne Beispielanwendung gedacht und benoetigt daher keine Authentifizierung oder Rollenverwaltung.
- Es gibt keine Schreiboperationen auf den Datenbestand; der Fokus liegt auf Lesen, Validieren und Auswerten.
- Die Anwendung ist kein fertiges Produkt, sondern eine kompakte Demonstration von Struktur, Fehlerbehandlung und Testbarkeit.

## Reviewer-Quickstart

Fuer einen schnellen Review reichen in der Regel diese Schritte:

1. Backend starten
2. Frontend starten
3. Swagger im Backend unter `http://localhost:5256/swagger` aufrufen
4. Angular-Frontend unter `http://localhost:4200` oeffnen
5. Optional die Tests mit `dotnet test` ausfuehren

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

Die Entscheidung fuer Observables ist hier bewusst getroffen worden, weil die Datenquelle bereits asynchron ist und sich die benoetigten UI-Zustaende gut als Datenstrom modellieren lassen.

Der HTTP-Aufruf aus Angular liefert von Haus aus ein Observable. Anstatt in der Komponente manuell zu subscriben und dort Ladezustand, Fehlerbehandlung und Filterung getrennt zu verwalten, werden diese Aspekte in einem gemeinsamen Stream zusammengefuehrt. Dadurch entsteht ein kleines ViewModel (`vm$`), das bereits alle fuer das Template relevanten Informationen enthaelt: Ladezustand, Fehler, Parser-Hinweise, Filtertext und gefilterte Liste.

Die Filterung wird dabei nicht als separater imperativer Schritt nach dem Laden ausgefuehrt, sondern ebenfalls als Teil der Pipeline. Ueber `combineLatest(...)` werden die geladenen Daten und der aktuelle Filtertext zusammengefuehrt; mit `map(...)` wird daraus die sichtbare Ergebnisliste berechnet. Das hat den Vorteil, dass sich die Anzeige automatisch aktualisiert, sobald sich entweder die Daten oder der Filter aendern.

Gerendert wird das Ergebnis anschliessend ueber die `async`-Pipe im Template. Dadurch uebernimmt Angular das Subscribing und Aufraeumen automatisch. Ein manuelles `subscribe`/`unsubscribe` in der Komponente ist nicht noetig, was die Komponente einfacher, robuster und leichter testbar macht.

Signals waeren ebenfalls moeglich gewesen, insbesondere fuer rein lokalen UI-State. Fuer diese Aufgabe wurden jedoch Observables bevorzugt, weil sie sehr gut zum HTTP-Datenfluss, zur Kombination mehrerer asynchroner Quellen und zur Verarbeitung in einer einzigen, gut nachvollziehbaren RxJS-Pipeline passen.

### 3) Wie werden Ladezustaende und Fehler verarbeitet?

Lade- und Fehlerzustand werden im Datenstream modelliert:

- `startWith(...)` setzt den initialen Ladezustand (`loading: true`)
- Erfolgsfall liefert Daten plus `loading: false`
- `catchError(...)` liefert einen nutzerfreundlichen Fehlertext und beendet den Stream kontrolliert
- Das Template zeigt Ladezustand, Fehler, Parser-Hinweise und Tabelle konditional an

### 4) Wie wird die XML geladen?

Die XML-Datei wird im Backend aktuell **bei jedem Request erneut von der Festplatte gelesen und geparst**.

Konkret bedeutet das:

- der jeweilige API-Endpunkt ermittelt den Pfad zur `library.xml`
- der `LibraryXmlParser` liest die Datei erneut ein
- die Daten werden pro Request neu validiert und in das interne Modell ueberfuehrt

Diese Entscheidung ist fuer die vorliegende Aufgabe bewusst getroffen worden. Die Datenmenge ist klein, sodass der zusaetzliche Aufwand pro Request sehr gering bleibt. Gleichzeitig hat dieser Ansatz den Vorteil, dass Aenderungen an der XML-Datei sofort wirksam werden, ohne dass die Anwendung neu gestartet oder ein Cache invalidiert werden muss.

Fuer einen kleinen internen Beispiel-Use-Case ist das ein pragmatischer und gut nachvollziehbarer Ansatz: einfache Implementierung, stets aktuelle Daten und kein zusaetzlicher Synchronisationsaufwand.

Bei deutlich groesseren Datenmengen oder hohem Request-Aufkommen waere ein anderes Vorgehen sinnvoll, zum Beispiel einmaliges Laden beim Start mit Caching und optionalem Reload-Mechanismus. Fuer den hier gezeigten Umfang ist das erneute Laden pro Request jedoch bewusst akzeptiert und fachlich gut vertretbar.

### 5) Wie funktioniert das Logging?

Im Backend ist zusaetzlich ein Logging fuer den XML-Parser eingebaut. Ziel davon ist, Parsing-Vorgaenge und erkannte Probleme nachvollziehbar zu protokollieren, ohne dafuer die eigentliche Fachlogik zu verkomplizieren.

Geloggt werden insbesondere:

- das Einlesen der XML-Datei
- der Start eines Parse-Vorgangs
- gefundene Validierungsfehler und Warnungen
- der Abschluss eines Parse-Vorgangs mit Anzahl der geladenen Buecher, Loans und Issues

Die Logausgabe wird nicht nur auf die Konsole geschrieben, sondern zusaetzlich in eine taeglich rotierte Datei. Die Dateien liegen unter:

- `Backend/Logs/parser-YYYY-MM-DD.log`

Das bedeutet, dass alle Parser-Meldungen eines Tages in derselben Datei landen und am naechsten Tag automatisch eine neue Datei verwendet wird. Dadurch bleiben die Logs uebersichtlich, und einzelne Tage lassen sich leicht nachvollziehen.

Dieser Ansatz ist fuer die Aufgabe sinnvoll, weil damit Fehler in den XML-Daten oder ungewoehnliche Parsing-Situationen schnell sichtbar werden. Gerade in einem internen Firmenkontext ist es hilfreich, bei Rueckfragen oder fehlerhaften Eingabedaten nachvollziehen zu koennen, wann und warum ein Parsing-Problem aufgetreten ist.

Die Logging-Loesung ist bewusst schlank gehalten: Sie fokussiert sich auf den Parser und seine Meldungen, ohne bereits ein komplexes zentrales Logging-Setup vorauszusetzen.

## Unit-Tests

### Was wurde getestet und warum?

Der Schwerpunkt der Tests liegt auf den fachlich wichtigsten und fehleranfaelligsten Bereichen im Backend: XML-Parsing und Berechnungslogik.

Beim XML-Parser wurden sowohl gueltige als auch ungueltige Eingaben getestet. Dazu gehoeren unter anderem:

- erfolgreiches Parsen gueltiger XML-Daten
- strukturelle Fehler wie ungueltiges XML oder fehlende Root-/Container-Elemente
- fehlende oder leere Pflichtfelder bei Buechern und Loans
- ungueltige Attribute und Elementwerte
- ungueltige Datums- und Jahreswerte
- doppelte Buch-IDs
- unbekannte Buchreferenzen in Loans
- ungueltige Datumsbereiche
- gemischte Szenarien mit gleichzeitig gueltigen und ungueltigen Eintraegen

Diese Tests sind wichtig, weil der Parser die Grundlage fuer alle weiteren Auswertungen bildet. Wenn hier fehlerhafte Daten nicht sauber erkannt oder gueltige Daten versehentlich verworfen werden, wirkt sich das direkt auf alle API-Ergebnisse aus.

Zusaetzlich wurden die Analytics-Methoden separat getestet, insbesondere:

- Ermittlung ueberfaelliger Ausleihen
- meistgeliehene Buecher pro Genre
- durchschnittliche Leihdauer

Diese Tests sichern die fachliche Berechnung ab und trennen Parsing-Probleme von Analyse-Problemen.

Im Frontend wurden zusaetzlich gezielte Component-Tests fuer die zentrale Ansicht implementiert:

- `Frontend/src/app/overdue-loans.component.spec.ts`

Abgedeckt werden dort insbesondere:

- initialer Ladezustand und anschliessendes Rendering der geladenen Daten
- Fehlerfall bei nicht erreichbarem Backend mit nutzerfreundlicher Meldung
- Filterlogik (case-insensitive) fuer Name, EmployeeId und Buchtitel
- erneutes Laden ueber den Reload-Button

Diese Tests wurden bewusst ausgewaehlt, weil sie die wichtigsten Benutzerfluesse der Aufgabe absichern: Daten laden, Fehler transparent machen, relevante Treffer schnell finden und Daten manuell aktualisieren. Genau diese Punkte entscheiden in der Praxis, ob die Komponente als UI fuer den Endpunkt `/api/library/overdue_loans` zuverlaessig nutzbar ist.

### Was wurde bewusst nicht getestet und warum?

Nicht oder nur indirekt getestet wurden bewusst jene Bereiche, die für die fachliche Kernlogik weniger relevant sind oder typischerweise in Integrations‑ bzw. End‑to‑End‑Tests abgedeckt werden. Dazu gehören:

vollständige API‑Integrationstests der HTTP‑Endpunkte

das konkrete Logging‑Verhalten auf Dateisystem‑Ebene

Performance‑ oder Lasttests mit sehr großen XML‑Dateien

wortgenaue Überprüfung einzelner Fehlermeldungen

Der Fokus der Tests liegt auf den zentralen Regeln, Validierungen und Fehlerpfaden. Diese Bereiche liefern den größten Mehrwert für Stabilität und Wartbarkeit der Anwendung. Breite, aber wartungsintensive Testsets wurden bewusst vermieden, da sie in diesem Kontext nur geringen zusätzlichen Nutzen bieten.

Auch Fehlermeldungen wurden nicht auf exakte Formulierungen getestet, um unnötige Fragilität zu vermeiden. Entscheidend ist, dass der korrekte Fehlertyp erkannt, sauber behandelt und verständlich zurückgegeben wird – nicht, ob der Text bis ins Detail identisch bleibt.

Des Weiteren wurden keine Framework‑Funktionen getestet, da ausschließlich der eigene Anwendungscode im Fokus steht. Framework‑ oder Bibliotheksverhalten ist bereits durch die jeweiligen Hersteller getestet und gilt als stabil. Sinnvoll und wartbar sind daher nur Tests, die die eigene Geschäftslogik, Validierungen und Fehlerbehandlungen abdecken.

## Bewusst nicht umgesetzt

Einige Punkte wurden bewusst nicht aufgenommen, um die Loesung schlank und aufgabenbezogen zu halten:

- keine Datenbankanbindung
- keine Authentifizierung oder Benutzerverwaltung
- keine Schreibendpunkte zum Anlegen, Aendern oder Loeschen von Daten
- keine umfangreiche Frontend-Gestaltung oder Animationen
- keine End-to-End-Tests ueber Backend und Frontend hinweg
- kein Cache fuer die XML-Daten

Diese Entscheidungen sind nicht als grundsaetzliche Ablehnung zu verstehen, sondern als bewusste Begrenzung auf die fachlichen Kernthemen der Aufgabe.

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

### Backend

```powershell
Set-Location E:\projects\example_app\Backend.Tests
dotnet test
```

### Frontend

```powershell
Set-Location E:\projects\example_app\Frontend
npx ng test --watch=false
```
