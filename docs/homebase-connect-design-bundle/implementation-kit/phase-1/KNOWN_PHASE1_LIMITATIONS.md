# Known Phase 1 Limitations

## OriginOnly

The initial `OutboundRouter` maps `OriginOnly` to Telegram because Phase 1 has no generic response endpoint registry.

Do not use `OriginOnly` for KDE commands until the Transport Registry is implemented.

## Delivery persistence

Phase 1 returns in-memory delivery results. Durable outbox and `message_delivery` persistence are Phase 2/GOAL-15 work.

## Handler migration

`LegacyRemoteCommandRouter` maps non-Telegram commands with `LegacyChatId = 0`.

Only expose existing Handlers to KDE after confirming they do not require Telegram chat identity.

## Notification actions

The Phase 1 notification model does not include KDE actions/reply. Add them only after conformance fixtures.

## Compilation

The files must be built in the user's .NET 9 environment.
