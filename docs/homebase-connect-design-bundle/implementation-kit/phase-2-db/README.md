# Phase 2 DB Kit

This kit provides a replacement candidate for:

```text
src/LocalOpsBot.Data/Migration/SqliteMigrator.cs
```

It preserves V1 and adds V2 device-hub tables inside a transaction.

Before applying:

1. back up a real V1 DB
2. compare the observed source SHA
3. apply on a feature branch
4. run `MIGRATION_TEST_MATRIX.md`
