using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace LocalOpsBot.Data.Migration;

public sealed class SqliteMigrator : IDatabaseMigrator
{
    private readonly LocalOpsDbContext _db;
    private readonly ILogger<SqliteMigrator> _logger;

    public SqliteMigrator(
        LocalOpsDbContext db,
        ILogger<SqliteMigrator> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken ct)
    {
        await _db.OpenAsync(ct);

        await ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS schema_version (
                version INTEGER PRIMARY KEY,
                applied_at TEXT NOT NULL
            );
            """, null, ct);

        var version = await GetCurrentVersionAsync(ct);
        _logger.LogInformation(
            "Current schema version: {Version}",
            version);

        if (version < 1)
        {
            await ApplyMigrationAsync(
                1,
                "initial-schema",
                V1Sql,
                ct);
            version = 1;
        }

        if (version < 2)
        {
            await ApplyMigrationAsync(
                2,
                "device-hub-foundation",
                V2Sql,
                ct);
        }
    }

    private async Task<int> GetCurrentVersionAsync(
        CancellationToken ct)
    {
        await using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText =
            "SELECT COALESCE(MAX(version), 0) FROM schema_version";

        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    private async Task ApplyMigrationAsync(
        int version,
        string name,
        string sql,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Applying migration V{Version}: {Name}",
            version,
            name);

        await using var transaction =
            (SqliteTransaction)await
                _db.Connection.BeginTransactionAsync(ct);

        try
        {
            await ExecuteAsync(sql, transaction, ct);

            await using var insert =
                _db.Connection.CreateCommand();

            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO schema_version (
                    version,
                    applied_at
                )
                VALUES (
                    $version,
                    $appliedAt
                );
                """;

            insert.Parameters.AddWithValue(
                "$version",
                version);
            insert.Parameters.AddWithValue(
                "$appliedAt",
                DateTimeOffset.UtcNow.ToString("O"));

            await insert.ExecuteNonQueryAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Migration V{Version} complete",
                version);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task ExecuteAsync(
        string sql,
        SqliteTransaction? transaction,
        CancellationToken ct)
    {
        await using var command =
            _db.Connection.CreateCommand();

        command.Transaction = transaction;
        command.CommandText = sql;

        await command.ExecuteNonQueryAsync(ct);
    }

    private const string V1Sql = """
        CREATE TABLE IF NOT EXISTS runtime_state (
            key TEXT PRIMARY KEY,
            value TEXT NOT NULL,
            updated_at TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS command_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            chat_id INTEGER NOT NULL,
            user_id INTEGER,
            command TEXT NOT NULL,
            args_json TEXT,
            raw_text TEXT,
            status TEXT NOT NULL,
            error TEXT,
            received_at TEXT NOT NULL,
            completed_at TEXT
        );

        CREATE INDEX IF NOT EXISTS
            ix_command_log_received_at
            ON command_log(received_at);

        CREATE INDEX IF NOT EXISTS
            ix_command_log_command
            ON command_log(command);

        CREATE TABLE IF NOT EXISTS alert_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            alert_id TEXT NOT NULL,
            kind TEXT NOT NULL,
            severity TEXT NOT NULL,
            title TEXT NOT NULL,
            body TEXT,
            dedup_key TEXT,
            source TEXT,
            status TEXT NOT NULL,
            error TEXT,
            created_at TEXT NOT NULL,
            sent_at TEXT
        );

        CREATE INDEX IF NOT EXISTS
            ix_alert_log_created_at
            ON alert_log(created_at);

        CREATE INDEX IF NOT EXISTS
            ix_alert_log_dedup_key
            ON alert_log(dedup_key);

        CREATE INDEX IF NOT EXISTS
            ix_alert_log_kind
            ON alert_log(kind);

        CREATE TABLE IF NOT EXISTS metric_sample (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            collected_at TEXT NOT NULL,
            cpu_usage_percent REAL,
            memory_usage_percent REAL,
            total_memory_bytes INTEGER,
            available_memory_bytes INTEGER,
            uptime_seconds INTEGER,
            disk_json TEXT,
            network_json TEXT
        );

        CREATE TABLE IF NOT EXISTS notification_event (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            event_id TEXT NOT NULL,
            source_app TEXT NOT NULL,
            title TEXT,
            body TEXT,
            body_hash TEXT,
            sensitivity TEXT NOT NULL,
            forwarded INTEGER NOT NULL,
            dropped_reason TEXT,
            created_at TEXT NOT NULL,
            processed_at TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS watch_status (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            watch_name TEXT NOT NULL,
            watch_type TEXT NOT NULL,
            status TEXT NOT NULL,
            status_json TEXT,
            changed_at TEXT NOT NULL
        );
        """;

    private const string V2Sql = """
        PRAGMA foreign_keys = ON;

        CREATE TABLE IF NOT EXISTS device (
            device_id TEXT PRIMARY KEY,
            display_name TEXT NOT NULL,
            device_type TEXT NOT NULL,
            transport_hint TEXT,
            protocol_version INTEGER,
            certificate_fingerprint TEXT,
            trust_state TEXT NOT NULL,
            is_enabled INTEGER NOT NULL DEFAULT 1,
            first_seen_at TEXT NOT NULL,
            last_seen_at TEXT NOT NULL,
            last_connected_at TEXT,
            metadata_json TEXT
        );

        CREATE INDEX IF NOT EXISTS
            ix_device_trust_state
            ON device(trust_state);

        CREATE INDEX IF NOT EXISTS
            ix_device_last_seen_at
            ON device(last_seen_at);

        CREATE TABLE IF NOT EXISTS device_capability (
            device_id TEXT NOT NULL,
            direction TEXT NOT NULL,
            capability TEXT NOT NULL,
            capability_version INTEGER NOT NULL DEFAULT 1,
            updated_at TEXT NOT NULL,
            PRIMARY KEY(
                device_id,
                direction,
                capability
            ),
            FOREIGN KEY(device_id)
                REFERENCES device(device_id)
                ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS device_plugin_setting (
            device_id TEXT NOT NULL,
            plugin_id TEXT NOT NULL,
            enabled INTEGER NOT NULL,
            settings_json TEXT,
            updated_at TEXT NOT NULL,
            PRIMARY KEY(device_id, plugin_id),
            FOREIGN KEY(device_id)
                REFERENCES device(device_id)
                ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS device_session (
            session_id TEXT PRIMARY KEY,
            device_id TEXT NOT NULL,
            transport_id TEXT NOT NULL,
            state TEXT NOT NULL,
            local_endpoint TEXT,
            remote_endpoint TEXT,
            connected_at TEXT NOT NULL,
            disconnected_at TEXT,
            close_reason TEXT,
            metadata_json TEXT,
            FOREIGN KEY(device_id)
                REFERENCES device(device_id)
                ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS
            ix_device_session_device_connected
            ON device_session(device_id, connected_at);

        CREATE TABLE IF NOT EXISTS device_state (
            device_id TEXT NOT NULL,
            state_type TEXT NOT NULL,
            state_json TEXT NOT NULL,
            observed_at TEXT NOT NULL,
            expires_at TEXT,
            PRIMARY KEY(device_id, state_type),
            FOREIGN KEY(device_id)
                REFERENCES device(device_id)
                ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS command_execution_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            request_id TEXT NOT NULL,
            transport_id TEXT NOT NULL,
            endpoint_id TEXT NOT NULL,
            principal_id TEXT NOT NULL,
            source_device_id TEXT,
            command TEXT NOT NULL,
            args_json TEXT,
            raw_input TEXT,
            risk_level TEXT NOT NULL,
            status TEXT NOT NULL,
            error_code TEXT,
            error TEXT,
            received_at TEXT NOT NULL,
            started_at TEXT,
            completed_at TEXT
        );

        CREATE UNIQUE INDEX IF NOT EXISTS
            ux_command_execution_request
            ON command_execution_log(request_id);

        CREATE TABLE IF NOT EXISTS message_delivery (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            message_id TEXT NOT NULL,
            trace_id TEXT NOT NULL,
            message_type TEXT NOT NULL,
            transport_id TEXT NOT NULL,
            target_endpoint TEXT NOT NULL,
            target_device_id TEXT,
            attempt INTEGER NOT NULL,
            status TEXT NOT NULL,
            failure_kind TEXT,
            error TEXT,
            queued_at TEXT NOT NULL,
            started_at TEXT,
            sent_at TEXT,
            acknowledged_at TEXT,
            expires_at TEXT
        );

        CREATE UNIQUE INDEX IF NOT EXISTS
            ux_message_delivery_attempt
            ON message_delivery(
                message_id,
                transport_id,
                target_endpoint,
                attempt
            );

        CREATE TABLE IF NOT EXISTS outbox_message (
            message_id TEXT PRIMARY KEY,
            envelope_json TEXT NOT NULL,
            priority INTEGER NOT NULL,
            status TEXT NOT NULL,
            available_at TEXT NOT NULL,
            attempt_count INTEGER NOT NULL DEFAULT 0,
            last_error TEXT,
            created_at TEXT NOT NULL,
            completed_at TEXT
        );

        CREATE INDEX IF NOT EXISTS
            ix_outbox_claim
            ON outbox_message(
                status,
                available_at,
                priority
            );

        CREATE TABLE IF NOT EXISTS payload_transfer (
            transfer_id TEXT PRIMARY KEY,
            message_id TEXT,
            device_id TEXT,
            direction TEXT NOT NULL,
            file_name TEXT,
            mime_type TEXT,
            size_bytes INTEGER NOT NULL,
            sha256 TEXT,
            temp_path TEXT,
            final_path TEXT,
            status TEXT NOT NULL,
            bytes_transferred INTEGER NOT NULL DEFAULT 0,
            created_at TEXT NOT NULL,
            completed_at TEXT,
            error TEXT,
            FOREIGN KEY(device_id)
                REFERENCES device(device_id)
                ON DELETE SET NULL
        );
        """;
}
