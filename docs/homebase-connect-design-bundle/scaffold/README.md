# Scaffold Notes

이 폴더는 Homebase 저장소에 바로 복사하기 위한 완성 구현이 아니라, 설계의 핵심 계약을 고정하기 위한 시작점입니다.

적용 순서:

1. `LocalOpsBot.Protocol` 프로젝트 생성
2. `Messaging/DeviceEnvelope.cs` 복사
3. Core 아래의 계약 파일 복사
4. 기존 `BotCommand`, `ICommandHandler`, `AlertDispatcher`와 충돌 확인
5. compatibility adapter 작성
6. 단위 테스트 추가
7. 기존 Telegram 출력 회귀 확인

주의:

- `IServiceProvider`를 Plugin context에 직접 노출하는 것은 초기 단순화를 위한 초안입니다.
- 구현이 안정화되면 필요한 typed service만 노출하는 `IPluginServices`로 교체할 수 있습니다.
- 각 public type에 XML documentation과 nullable 설정을 추가하십시오.
