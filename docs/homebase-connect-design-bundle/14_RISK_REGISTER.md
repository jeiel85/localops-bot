# 14. Risk Register

| ID | Risk | Impact | Probability | Mitigation |
|---|---|---:|---:|---|
| R-01 | KDE wire compatibility drift | High | Medium | commit lock, fixture tests |
| R-02 | KDE Send Notifications wire compatibility drift | High | Medium | source-lock, fixtures, Telegram fallback |
| R-03 | MIT/GPL code mixing | High | Low/Medium | independent implementation, separate adapter |
| R-04 | SYSTEM Agent IPC privilege escalation | Critical | Medium | SID verification, narrow ACL, operation allowlist |
| R-05 | Raw remote command execution | Critical | Medium | Handler-only commands |
| R-06 | Notification feedback loop | High | High | trace/hop guard plus app filter |
| R-07 | File path traversal | Critical | Medium | sanitize, temp store, atomic move |
| R-08 | SQLite migration damages existing data | High | Low/Medium | backup, transaction, new tables |
| R-09 | Transport failure crashes Agent | High | Medium | supervisor and exception isolation |
| R-10 | Alert duplication during migration | Medium | High | shadow mode, delivery log, deterministic IDs |
| R-11 | Firewall/VPN discovery failure | Medium | High | diagnostics, Private profile rule |
| R-12 | Tray absent for user-session action | Medium | High | typed unavailable result, no silent failure |
| R-13 | Sensitive notification persistence | High | Medium | privacy mode before storage |
| R-14 | Certificate changed but device auto-trusted | Critical | Low | fingerprint mismatch forces re-pair |
| R-15 | Large payload memory pressure | High | Medium | streaming only |
| R-16 | Android background restrictions | Medium | Medium | stock app behavior verification |
| R-17 | Too many projects slow delivery | Medium | Medium | staged extraction |
| R-18 | Existing Telegram UX regression | High | Medium | golden tests and compatibility adapter |

## Stop conditions

다음 중 하나가 발생하면 해당 GOAL을 중단하고 설계를 재검토합니다.

- KDE identity/pair fixture가 Android 실기기와 재현되지 않음
- IPC에서 client identity를 신뢰성 있게 검증하지 못함
- 기존 Telegram 명령 출력이 대량 변경됨
- migration rollback이 불가능함
- file payload를 memory buffering해야만 구현 가능함
- 원격 명령이 raw shell 없이는 구현 불가하다는 결론
- GPL 코드를 직접 복사해야만 호환 가능하다는 결론

## Open decisions

- KDE Send Notifications에서 지원할 icon/action/urgency 범위
- Telegram fallback acknowledgement timeout
- multi-user Windows session 지원 시점
- notification content encryption at rest
- Homebase Android 전용 앱 시작 시점
- full KDE compatibility와 Homebase-native protocol의 장기 관계
