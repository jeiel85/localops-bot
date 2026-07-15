PRAGMA foreign_keys = ON;

BEGIN IMMEDIATE;

CREATE TABLE IF NOT EXISTS schema_migration_history (
    version INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    checksum TEXT NOT NULL,
    started_at TEXT NOT NULL,
    completed_at TEXT,
    success INTEGER NOT NULL DEFAULT 0,
    error TEXT
);

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

CREATE INDEX IF NOT EXISTS ix_device_trust_state
    ON device(trust_state);
CREATE INDEX IF NOT EXISTS ix_device_last_seen_at
    ON device(last_seen_at);

CREATE TABLE IF NOT EXISTS device_capability (
    device_id TEXT NOT NULL,
    direction TEXT NOT NULL,
    capability TEXT NOT NULL,
    capability_version INTEGER NOT NULL DEFAULT 1,
    updated_at TEXT NOT NULL,
    PRIMARY KEY(device_id, direction, capability),
    FOREIGN KEY(device_id) REFERENCES device(device_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS device_plugin_setting (
    device_id TEXT NOT NULL,
    plugin_id TEXT NOT NULL,
    enabled INTEGER NOT NULL,
    settings_json TEXT,
    updated_at TEXT NOT NULL,
    PRIMARY KEY(device_id, plugin_id),
    FOREIGN KEY(device_id) REFERENCES device(device_id) ON DELETE CASCADE
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
    FOREIGN KEY(device_id) REFERENCES device(device_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_device_session_device_connected
    ON device_session(device_id, connected_at);

CREATE TABLE IF NOT EXISTS device_state (
    device_id TEXT NOT NULL,
    state_type TEXT NOT NULL,
    state_json TEXT NOT NULL,
    observed_at TEXT NOT NULL,
    expires_at TEXT,
    PRIMARY KEY(device_id, state_type),
    FOREIGN KEY(device_id) REFERENCES device(device_id) ON DELETE CASCADE
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

CREATE UNIQUE INDEX IF NOT EXISTS ux_command_execution_request
    ON command_execution_log(request_id);
CREATE INDEX IF NOT EXISTS ix_command_execution_received
    ON command_execution_log(received_at);

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

CREATE UNIQUE INDEX IF NOT EXISTS ux_message_delivery_attempt
    ON message_delivery(message_id, transport_id, target_endpoint, attempt);
CREATE INDEX IF NOT EXISTS ix_message_delivery_status
    ON message_delivery(status, queued_at);

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

CREATE INDEX IF NOT EXISTS ix_outbox_claim
    ON outbox_message(status, available_at, priority);

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
    FOREIGN KEY(device_id) REFERENCES device(device_id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS ix_payload_transfer_status
    ON payload_transfer(status, created_at);

COMMIT;
