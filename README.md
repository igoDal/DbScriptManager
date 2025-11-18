**DbScriptManager – narzędzie do metadanych Firebird 5.0**

Aplikacja konsolowa w .NET 8.0 służąca do pracy z metadanymi baz danych Firebird 5.0.
Umożliwia: zbudowanie nowej bazy ze skryptów, eksport metadanych (domeny, tabele, procedury) do plików .sql, 
aktualizację istniejącej bazy na podstawie skryptów.

**Funkcjonalności**
🔧 build-db

Tworzy nową bazę danych w wybranym katalogu i wykonuje skrypty: domen, tabel, procedur.

Po zakończeniu generowany jest raport z wykonania.

📤 export-scripts

Eksportuje metadane z istniejącej bazy Firebird 5.0 do oddzielnych plików .sql.

🔄 update-db

Wykonuje skrypty na istniejącej bazie danych (z transakcją per plik i raportem wyników).

**Wymagania**
- .NET 8.0
- Firebird 5.0 (SuperServer)
- fbclient.dll z Firebirda 5
- Windows 10/11

Aby zbudować aplikację, przejdź w terminalu do katalogu projektu i wykonaj:
dotnet build


**Uruchamianie**

W katalogu projektu:

1. Budowanie nowej bazy
dotnet run -- build-db ^
  --db-dir "C:\db\fb\NewDb" ^
  --scripts-dir "C:\scripts\meta"

2. Eksport metadanych
dotnet run -- export-scripts ^
  --connection-string "Database=C:\db\fb\DB1.FDB;DataSource=localhost;User=SYSDBA;Password=masterkey;Dialect=3;" ^
  --output-dir "C:\scripts\out"

3. Aktualizacja bazy
dotnet run -- update-db ^
  --connection-string "Database=C:\db\fb\DB2.FDB;DataSource=localhost;User=SYSDBA;Password=masterkey;Dialect=3;" ^
  --scripts-dir "C:\scripts\meta"

**Informacje**

Obsługiwane są: domeny, tabele, procedury.
Każdy skrypt jest wykonywany w osobnej transakcji.
Po każdej operacji wypisywany jest szczegółowy raport.
