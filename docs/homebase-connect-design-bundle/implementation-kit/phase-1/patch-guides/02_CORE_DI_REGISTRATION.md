# Patch 02 — Core DI registration

File:

```text
src/LocalOpsBot.Core/ServiceCollectionExtensions.cs
```

Add:

```csharp
using LocalOpsBot.Core.Delivery;
```

Directly after:

```csharp
services.AddSingleton<ICommandRouter, CommandRouter>();
```

add:

```csharp
services.AddSingleton<IRemoteCommandRouter, LegacyRemoteCommandRouter>();
services.AddSingleton<IOutboundRouter, OutboundRouter>();
```

Keep all existing Handler registrations.
