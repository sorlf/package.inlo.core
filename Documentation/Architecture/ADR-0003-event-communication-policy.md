# ADR-0003: Event Communication Policy

상태: Accepted

날짜: 2026-06-03

## 배경

Unity 프로젝트에서는 delegate, C# event, UnityEvent, ScriptableObject EventChannel, R3/UniRx 같은 통신 방식이 쉽게 섞입니다.

통신 방식이 섞이면 이벤트 흐름 추적, 테스트, 디버깅, 팀 이해 비용이 빠르게 커집니다.

## 결정

INLO Core의 기본 shared game event 통신은 ScriptableObject EventChannel입니다.

다른 통신 방식은 관계와 데이터 성격에 따라 제한적으로 사용합니다.

## 선택 기준

| 상황 | 방식 |
| --- | --- |
| 시스템 간 game/application event | EventChannel |
| 한 객체 내부 또는 짧은 parent-child callback | delegate/C# event/Action |
| Inspector의 단순 버튼 액션 | UnityEvent |
| 지속 상태 stream, filtering, combining, throttling | R3 또는 기존 reactive stack |
| 같은 prefab 내부의 명확한 소유 관계 | direct reference |

## 이유

- EventChannel은 sender와 receiver 결합을 줄이고 asset 기반으로 흐름을 추적할 수 있습니다.
- Browser/Graph/Audit tooling이 EventChannel 사용을 팀 단위로 보여줍니다.
- 상태 stream과 one-shot event를 구분해야 이벤트가 상태 저장소로 변질되지 않습니다.

## 결과

- EventChannel asset에는 Description을 작성합니다.
- 핵심 gameplay event는 Browser/Graph/Audit에서 추적 가능해야 합니다.
- EventData는 사건 순간의 payload만 담고 상태 전체를 담지 않습니다.

## 금지

- EventChannel을 상태 저장소처럼 사용 금지.
- 같은 책임에 EventChannel, delegate, UnityEvent, R3를 무분별하게 혼용 금지.
- Description 없는 EventChannel 추가 금지.
- release build에 Debug Log가 켜진 EventChannel 방치 금지.
