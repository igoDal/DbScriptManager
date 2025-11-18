using System;
using System.IO;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool
{
    public static class Program
    {
        // Przykładowe wywołania:
        // DbMetaTool build-db --db-dir "C:\db\fb5" --scripts-dir "C:\scripts"
        // DbMetaTool export-scripts --connection-string "..." --output-dir "C:\out"
        // DbMetaTool update-db --connection-string "..." --scripts-dir "C:\scripts"
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Użycie:");
                Console.WriteLine("  build-db --db-dir <ścieżka> --scripts-dir <ścieżka>");
                Console.WriteLine("  export-scripts --connection-string <connStr> --output-dir <ścieżka>");
                Console.WriteLine("  update-db --connection-string <connStr> --scripts-dir <ścieżka>");
                return 1;
            }

            try
            {
                var command = args[0].ToLowerInvariant();

                switch (command)
                {
                    case "build-db":
                    {
                        string dbDir = GetArgValue(args, "--db-dir");
                        string scriptsDir = GetArgValue(args, "--scripts-dir");

                        BuildDatabase(dbDir, scriptsDir);
                        Console.WriteLine("Baza danych została zbudowana pomyślnie.");
                        return 0;
                    }

                    case "export-scripts":
                    {
                        string connStr = GetArgValue(args, "--connection-string");
                        string outputDir = GetArgValue(args, "--output-dir");

                        ExportScripts(connStr, outputDir);
                        Console.WriteLine("Skrypty zostały wyeksportowane pomyślnie.");
                        return 0;
                    }

                    case "update-db":
                    {
                        string connStr = GetArgValue(args, "--connection-string");
                        string scriptsDir = GetArgValue(args, "--scripts-dir");

                        UpdateDatabase(connStr, scriptsDir);
                        Console.WriteLine("Baza danych została zaktualizowana pomyślnie.");
                        return 0;
                    }

                    default:
                        Console.WriteLine($"Nieznane polecenie: {command}");
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Błąd: " + ex.Message);
                return -1;
            }
        }

        private static string GetArgValue(string[] args, string name)
        {
            int idx = Array.IndexOf(args, name);
            if (idx == -1 || idx + 1 >= args.Length)
                throw new ArgumentException($"Brak wymaganego parametru {name}");
            return args[idx + 1];
        }

        /// <summary>
        /// Buduje nową bazę danych Firebird 5.0 na podstawie skryptów.
        /// </summary>
        public static void BuildDatabase(string databaseDirectory, string scriptsDirectory)
        {
            // TODO:
            // 1) Utwórz pustą bazę danych FB 5.0 w katalogu databaseDirectory.
            // 2) Wczytaj i wykonaj kolejno skrypty z katalogu scriptsDirectory
            //    (tylko domeny, tabele, procedury).
            // 3) Obsłuż błędy i wyświetl raport.

            if (string.IsNullOrWhiteSpace(databaseDirectory))
                throw new ArgumentException("Katalog bazy danych jest wymagany.", nameof(databaseDirectory));
            if (string.IsNullOrWhiteSpace(scriptsDirectory))
                throw new ArgumentException("Katalog skryptów jest wymagany.", nameof(scriptsDirectory));

            Directory.CreateDirectory(databaseDirectory);

            var dbPath = Path.Combine(databaseDirectory, "database.fdb");

            if (File.Exists(dbPath))
                throw new InvalidOperationException($"Plik bazy już istnieje: {dbPath}");

            var csb = new FbConnectionStringBuilder
            {
                Database = dbPath,
                DataSource = "localhost",
                UserID = "SYSDBA",
                Password = "masterkey",
                Dialect = 3
            };

            FbConnection.CreateDatabase(csb.ToString(), pageSize: 8192);

            using var connection = new FbConnection(csb.ToString());
            connection.Open();

            var results = ExecuteScriptsInDirectory(connection, scriptsDirectory);
            PrintScriptReport(results, "build-db");        }

        /// <summary>
        /// Generuje skrypty metadanych z istniejącej bazy danych Firebird 5.0.
        /// </summary>
        public static void ExportScripts(string connectionString, string outputDirectory)
        {
            // TODO:
            // 1) Połącz się z bazą danych przy użyciu connectionString.
            // 2) Pobierz metadane domen, tabel (z kolumnami) i procedur.
            // 3) Wygeneruj pliki .sql / .json / .txt w outputDirectory.
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string jest wymagany.", nameof(connectionString));
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("Katalog wyjściowy jest wymagany.", nameof(outputDirectory));

            Directory.CreateDirectory(outputDirectory);

            using var connection = new FbConnection(connectionString);
            connection.Open();

            ExportDomains(connection, outputDirectory);
            ExportTables(connection, outputDirectory);
            ExportProcedures(connection, outputDirectory);
        }

        /// <summary>
        /// Aktualizuje istniejącą bazę danych Firebird 5.0 na podstawie skryptów.
        /// </summary>
        public static void UpdateDatabase(string connectionString, string scriptsDirectory)
        {
            // TODO:
            // 1) Połącz się z bazą danych przy użyciu connectionString.
            // 2) Wykonaj skrypty z katalogu scriptsDirectory (tylko obsługiwane elementy).
            // 3) Zadbaj o poprawną kolejność i bezpieczeństwo zmian.
            
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string jest wymagany.", nameof(connectionString));
            if (string.IsNullOrWhiteSpace(scriptsDirectory))
                throw new ArgumentException("Katalog skryptów jest wymagany.", nameof(scriptsDirectory));

            using var connection = new FbConnection(connectionString);
            connection.Open();

            var results = ExecuteScriptsInDirectory(connection, scriptsDirectory);
            PrintScriptReport(results, "update-db");
        }

        private static List<ScriptExecutionResult> ExecuteScriptsInDirectory(
            FbConnection connection,
            string scriptsDirectory)
        {
            if (!Directory.Exists(scriptsDirectory))
                throw new DirectoryNotFoundException($"Katalog skryptów nie istnieje: {scriptsDirectory}");

            var files = Directory.GetFiles(scriptsDirectory, "*.sql", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var results = new List<ScriptExecutionResult>();

            foreach (var file in files)
            {
                var sql = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(sql))
                {
                    results.Add(new ScriptExecutionResult
                    {
                        FilePath = file,
                        Success = true,
                        ErrorMessage = null
                    });
                    continue;
                }

                var result = new ScriptExecutionResult
                {
                    FilePath = file
                };

                using var tx = connection.BeginTransaction();
                try
                {
                    using var cmd = new FbCommand(sql, connection, tx);
                    cmd.ExecuteNonQuery();
                    tx.Commit();

                    result.Success = true;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                }

                results.Add(result);
            }

            return results;
        }


        private static string MapFieldType(short fieldType, short fieldSubType, short fieldScale, int charLength)
        {

            switch (fieldType)
            {
                case 7: // SMALLINT
                    return "SMALLINT";
                case 8: // INTEGER
                    return "INTEGER";
                case 10: // FLOAT
                    return "FLOAT";
                case 12: // DATE
                    return "DATE";
                case 13: // TIME
                    return "TIME";
                case 14: // CHAR
                    return $"CHAR({charLength})";
                case 16: // BIGINT / NUMERIC/DECIMAL
                    if (fieldScale < 0)
                    {
                        // NUMERIC(18, scale)
                        return $"NUMERIC(18, {Math.Abs(fieldScale)})";
                    }

                    return "BIGINT";
                case 23: // BOOLEAN
                    return "BOOLEAN";
                case 27: // DOUBLE PRECISION
                    return "DOUBLE PRECISION";
                case 35: // TIMESTAMP
                    return "TIMESTAMP";
                case 37: // VARCHAR
                    return $"VARCHAR({charLength})";
                case 261: // BLOB
                    return "BLOB";
                default:
                    return $"UNKNOWN_TYPE_{fieldType}";
            }
        }

        private static void ExportDomains(FbConnection connection, string outputDirectory)
        {
            const string sql = @"
                SELECT
                    TRIM(f.RDB$FIELD_NAME) AS NAME,
                    f.RDB$FIELD_TYPE,
                    f.RDB$FIELD_SUB_TYPE,
                    f.RDB$FIELD_SCALE,
                    f.RDB$CHARACTER_LENGTH,
                    f.RDB$NULL_FLAG
                FROM RDB$FIELDS f
                WHERE COALESCE(f.RDB$SYSTEM_FLAG, 0) = 0
                  AND NOT (f.RDB$FIELD_NAME STARTING WITH 'RDB$')
                ORDER BY f.RDB$FIELD_NAME";

            using var cmd = new FbCommand(sql, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var name = reader.GetString(0);
                short fieldType = reader.GetInt16(1);
                short subType = reader.IsDBNull(2) ? (short)0 : reader.GetInt16(2);
                short scale = reader.IsDBNull(3) ? (short)0 : reader.GetInt16(3);
                int charLen = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                bool notNull = !reader.IsDBNull(5) && reader.GetInt16(5) == 1;

                var typeSql = MapFieldType(fieldType, subType, scale, charLen);
                var ddl = $"CREATE DOMAIN {name} AS {typeSql}" + (notNull ? " NOT NULL" : string.Empty);

                var fileName = Path.Combine(outputDirectory, $"1_domain_{name}.sql");
                File.WriteAllText(fileName, ddl + Environment.NewLine);
            }
        }

        private static void ExportTables(FbConnection connection, string outputDirectory)
        {
            const string tablesSql = @"
                SELECT TRIM(r.RDB$RELATION_NAME) AS NAME
                FROM RDB$RELATIONS r
                WHERE COALESCE(r.RDB$SYSTEM_FLAG, 0) = 0
                  AND r.RDB$VIEW_BLR IS NULL
                ORDER BY r.RDB$RELATION_NAME";

            using var tablesCmd = new FbCommand(tablesSql, connection);
            using var tablesReader = tablesCmd.ExecuteReader();

            var tableNames = new List<string>();
            while (tablesReader.Read())
            {
                tableNames.Add(tablesReader.GetString(0));
            }

            foreach (var table in tableNames)
            {
                var ddl = GenerateTableDdl(connection, table);
                var fileName = Path.Combine(outputDirectory, $"2_table_{table}.sql");
                File.WriteAllText(fileName, ddl + Environment.NewLine);
            }
        }

        private static string GenerateTableDdl(FbConnection connection, string tableName)
        {
            const string columnsSql = @"
                SELECT
                    TRIM(rf.RDB$FIELD_NAME) AS COLUMN_NAME,
                    f.RDB$FIELD_TYPE,
                    f.RDB$FIELD_SUB_TYPE,
                    f.RDB$FIELD_SCALE,
                    f.RDB$CHARACTER_LENGTH,
                    rf.RDB$NULL_FLAG,
                    rf.RDB$DEFAULT_SOURCE,
                    f.RDB$DEFAULT_SOURCE
                FROM RDB$RELATION_FIELDS rf
                JOIN RDB$FIELDS f ON f.RDB$FIELD_NAME = rf.RDB$FIELD_SOURCE
                WHERE rf.RDB$RELATION_NAME = @tbl
                ORDER BY rf.RDB$FIELD_POSITION";

            using var cmd = new FbCommand(columnsSql, connection);
            cmd.Parameters.AddWithValue("@tbl", tableName);
            using var reader = cmd.ExecuteReader();

            var cols = new List<string>();

            while (reader.Read())
            {
                var colName = reader.GetString(0);
                short fieldType = reader.GetInt16(1);
                short subType = reader.IsDBNull(2) ? (short)0 : reader.GetInt16(2);
                short scale = reader.IsDBNull(3) ? (short)0 : reader.GetInt16(3);
                int charLen = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                bool notNull = !reader.IsDBNull(5) && reader.GetInt16(5) == 1;

                string? defaultSource = null;
                if (!reader.IsDBNull(6))
                    defaultSource = reader.GetString(6)?.Trim();
                else if (!reader.IsDBNull(7))
                    defaultSource = reader.GetString(7)?.Trim();

                var typeSql = MapFieldType(fieldType, subType, scale, charLen);

                var colDef = $"{colName} {typeSql}";
                if (notNull)
                    colDef += " NOT NULL";

                if (!string.IsNullOrWhiteSpace(defaultSource))
                {
                    colDef += " " + defaultSource;
                }

                cols.Add(colDef);
            }

            var colsSql = string.Join("," + Environment.NewLine + "    ", cols);
            var ddl = $"CREATE TABLE {tableName} (" + Environment.NewLine +
                      "    " + colsSql + Environment.NewLine +
                      ")";

            return ddl;
        }

        private static void ExportProcedures(FbConnection connection, string outputDirectory)
        {
            const string procsSql = @"
                SELECT
                    TRIM(p.RDB$PROCEDURE_NAME) AS NAME,
                    p.RDB$PROCEDURE_SOURCE
                FROM RDB$PROCEDURES p
                WHERE COALESCE(p.RDB$SYSTEM_FLAG, 0) = 0
                ORDER BY p.RDB$PROCEDURE_NAME";

            using var cmd = new FbCommand(procsSql, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var name = reader.GetString(0);
                string source = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

                if (string.IsNullOrWhiteSpace(source))
                    continue;
                
                var ddl = source.Trim();

                var fileName = Path.Combine(outputDirectory, $"3_procedure_{name}.sql");
                File.WriteAllText(fileName, ddl + Environment.NewLine);
            }
        }
        
        private static void PrintScriptReport(IEnumerable<ScriptExecutionResult> results, string label)
        {
            var list = results.ToList();
            int ok = list.Count(r => r.Success);
            int fail = list.Count(r => !r.Success);

            Console.WriteLine();
            Console.WriteLine($"=== Raport wykonywania skryptów ({label}) ===");
            Console.WriteLine($"  Łącznie plików: {list.Count}");
            Console.WriteLine($"  Sukces:        {ok}");
            Console.WriteLine($"  Błędy:         {fail}");
            Console.WriteLine();

            if (fail > 0)
            {
                Console.WriteLine("  Pliki z błędami:");
                foreach (var r in list.Where(r => !r.Success))
                {
                    Console.WriteLine($"    - {r.FilePath}");
                    Console.WriteLine($"      Błąd: {r.ErrorMessage}");
                }
                Console.WriteLine();
            }
        }
    }

    sealed class ScriptExecutionResult
    {
        public string FilePath { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
