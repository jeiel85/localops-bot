# Migration Test Matrix

## Fresh DB

Expected:

```text
schema_version rows: 1, 2
V1 tables exist
V2 tables exist
```

## Existing V1 DB

Before migration record row counts for:

- runtime_state
- command_log
- alert_log
- notification_event

After migration:

- counts unchanged
- V2 tables exist
- schema version 2

## Failure injection

Add invalid SQL halfway through V2.

Expected:

- rollback
- version 2 not inserted
- Agent startup fails visibly
- V1 data remains

## Repeated run

Run migrator twice.

Expected:

- no duplicate version
- no exception
- no data loss
