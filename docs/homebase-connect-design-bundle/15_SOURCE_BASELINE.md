# 15. Source Baseline

## 1. Homebase observations

설계 시 기본 브랜치에서 다음 파일을 확인했습니다.

| File | Observed role | Blob SHA |
|---|---|---|
| `README.md` | Telegram 중심 기능, Agent/Tray/SQLite 구조 | `1cf0ef85287c820df1dc861fa65b65d2cff86cd7` |
| `LocalOpsBot.sln` | Core, Infrastructure, Data, Agent, Tray, Tests | `460575041945135f2bf6a535a98f57fdd6d6f235` |
| `src/LocalOpsBot.Agent/Program.cs` | Hosted Service와 DI 등록 | `ea885a86adcb00a8b83f079f14d77b47ab0513a1` |
| `src/LocalOpsBot.Agent/Services/AlertDispatcher.cs` | Telegram 직접 의존 | `e9c40a8cbdbec3073d55ed2d65e60cbcaeba2b4c` |
| `src/LocalOpsBot.Agent/Services/TelegramPollingService.cs` | Telegram ingress와 reply | `11c4934cdf43523237867d3f1696f29a82ed66ea` |
| `src/LocalOpsBot.Core/Commands/CommandRouter.cs` | Handler dictionary routing | `abcbbc35855a48f1496816943189c735d869b62b` |
| `src/LocalOpsBot.Agent/Services/NotificationBridgeServer.cs` | 단방향 notification pipe | `608ee504ad27aa04e5fe29c1c5ba6947cb27be17` |
| `src/LocalOpsBot.Data/Migration/SqliteMigrator.cs` | V1 schema migration | `68fc231b9c8fad1d6bd306c34edc9d1c94e32463` |
| `config/appsettings.example.json` | 현재 config baseline | `3f21529aae4d906c4db75da282eb56b189bfe0ee` |
| `CHANGELOG.md` | 현재 구현 범위와 회귀 이력 | `3cd6815cc2bbc930939a7dc861fa6a8b62a8131bef` |

### 확인한 핵심 사실

- Agent는 Windows Service이고 Tray는 사용자 세션 앱입니다.
- 현재 자동 alert path는 Telegram에 직접 연결되어 있습니다.
- CommandRouter 자체는 Handler 기반이므로 재사용하기 좋습니다.
- 현재 notification IPC는 Tray → Agent 단방향입니다.
- 실제 Pipe ACL 구현은 `Authenticated Users`를 허용하므로, 원격 제어용 양방향 IPC로 확장하기 전에 축소해야 합니다.
- DB는 schema version 1이며 device/session/delivery 모델이 없습니다.

## 2. KDE Connect Android observations

| File | Observed role | Blob SHA |
|---|---|---|
| `README.md` | 주요 기능, Wi-Fi/TLS, Android/Desktop 조합 | `b6998390f0dc3ad86605d33cc3395db160e38518` |
| `src/org/kde/kdeconnect/plugins/Plugin.kt` | Plugin lifecycle와 packet capability | `da0a329aaa25de158b9f5e486d4a80e85bf0ca61` |
| `src/org/kde/kdeconnect/plugins/PluginFactory.kt` | capability 기반 plugin 선택 | `848ba409eec87eff211c1ee37d3e95646a01a04d` |
| `src/org/kde/kdeconnect/Device.kt` | device, links, pairing, plugin routing | `d6378419f11b59a5771bdf03d25f74d52456efa2` |
| `src/org/kde/kdeconnect/NetworkPacket.kt` | typed JSON body와 optional payload | `bb1fcd2d2f7c4533ef9f5fe3aade3bb5299886d0` |
| `src/org/kde/kdeconnect/PairingHandler.kt` | pairing state와 verification key | `8db21cf1f704b61e4ce749461558052b54c7d035` |
| `src/org/kde/kdeconnect/plugins/notifications/NotificationsPlugin.kt` | Android notification publish/update/remove | `5b2d23dc22c6f83c8e796fc37cdcc9a51b6d8cbb` |
| `src/org/kde/kdeconnect/plugins/share/SharePlugin.java` | text, URL, file payload | `b82471b230bc33dcd9904f9a61887290ce6688f3` |
| `src/org/kde/kdeconnect/backends/BaseLink.java` | packet transport abstraction | `a04abc07b41a891a7e3d4125f6a213ce468ff163` |
| `src/org/kde/kdeconnect/backends/BaseLinkProvider.java` | connection provider abstraction | `9195576f09df4fab2733c661ff3da9f098dbda66` |

## 3. Implementation warning

KDE protocol의 포트, TLS handshake 순서, identity fields, 각 plugin packet field는 production 구현 전에 반드시 GOAL-07에서 특정 commit으로 잠그고 fixture로 검증해야 합니다.

이 설계서는 architecture와 구현 경계를 고정하지만, 확인하지 않은 packet field를 임의로 만들어내지 않습니다.

## 4. License boundary

- Homebase는 현재 MIT로 표시됩니다.
- KDE Connect Android는 GPL v2/v3 계열로 표시됩니다.
- Homebase의 KDE Adapter는 외부 동작을 기준으로 독립 구현합니다.
- KDE Java/Kotlin 코드를 C# 프로젝트로 복사하거나 번역 이식하지 않습니다.
- Android fork 수정·배포는 해당 GPL 의무를 별도로 준수해야 합니다.


## 5. KDE official user documentation verification

Verified from KDE UserBase on 2026-07-14:

- desktop and phone components
- UDP broadcast automatic discovery
- TCP/UDP dynamic port range `1714-1764`
- phone notifications to desktop
- desktop notifications to phone
- predefined run commands
- file, URL and plain-text sharing

Reference:

```text
https://userbase.kde.org/KDEConnect
```

Exact plugin wire fields remain source-locked implementation details.
