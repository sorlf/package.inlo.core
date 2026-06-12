# INLO Core Events

INLO Core Events는 Unity 프로젝트에서 이벤트 흐름을 일관되게 관리하기 위한 `ScriptableObject EventChannel` 기반 모듈입니다.

이 모듈의 목적은 단순히 이벤트를 발행하고 수신하는 것이 아니라, 팀원이 프로젝트 규모가 커져도 이벤트 흐름을 추적하고, 검증하고, 문서화할 수 있게 만드는 것입니다.

통신 방식 선택 이유와 경계는 [ADR-0003](../Architecture/ADR-0003-event-communication-policy.md)을 봅니다.

## 핵심 원칙

EventChannel은 상태 저장소가 아닙니다.  
EventChannel은 시스템 간 결합도를 낮추기 위한 이벤트 통로입니다.

이벤트는 다음 흐름을 따릅니다.

```text
Publisher
→ EventChannel
→ Listener
```

예를 들어 플레이어가 데미지를 받는 경우:

```text
PlayerHealth
→ DamageTakenChannel
→ DamageTakenLogger / HealthBarView / HitEffectPlayer
```

## 포함 기능

### Runtime

- `EventChannelBaseSO`
- `EventChannelSO<TEventData>`
- `VoidEventChannelSO`
- `EventListener<TEventData, TChannel>`
- `VoidEventListener`

### Editor Tools

- Event System Manager
- Browser tab
- Audit tab
- Graph tab
- Release Build Validation
- CI Validation

## 메뉴 구조

```text
Tools
└── INLO
    └── Events
        ├── Event System Manager
        └── Validation
            ├── Run CI Validation
            ├── Validate Release Build Rules
            └── Select First Debug Log Channel
```

## Event Channel Creator

경로:

```text
Tools > INLO > Events > Event System Manager > Creator/Browser workflow
```

Creator는 다음을 생성합니다.

```text
EventData.cs
EventChannelSO.cs
Channel.asset
```

예:

```text
Event Name: DamageTaken
Namespace: Game.Events
Fields:
- Target: GameObject
- DamageAmount: Int
```

생성 결과:

```text
Assets/GeneratedEvents/DamageTakenEventData.cs
Assets/GeneratedEvents/DamageTakenEventChannelSO.cs
Assets/GameEvents/DamageTakenChannel.asset
```

생성된 `.cs` 파일에는 `auto-generated` header가 붙습니다.  
생성 파일은 직접 수정하지 않습니다. 변경이 필요하면 Creator에서 다시 생성하거나 별도 확장 파일을 만듭니다.

## EventData 규칙

EventData는 이벤트 순간에 필요한 데이터만 담습니다.

좋은 예:

```csharp
public struct DamageTakenEventData
{
    public GameObject Target;
    public int DamageAmount;
}
```

나쁜 예:

```csharp
public struct DamageTakenEventData
{
    public GameObject Target;
    public int CurrentHp;
    public int MaxHp;
    public bool IsDead;
    public PlayerState WholeState;
}
```

EventData는 상태 객체가 아닙니다.  
상태가 필요하면 별도의 Model, Service, Store, Controller가 소유해야 합니다.

## Description 규칙

모든 EventChannel asset에는 Description을 작성합니다.

Description에는 다음을 적습니다.

```text
- 이 이벤트가 언제 발생하는가
- 이 이벤트는 어떤 상태를 저장하지 않는가
- 누가 들을 수 있는가
- 대표 Listener 예시
```

예:

```text
골드가 변경될 때 발생합니다.

이 이벤트는 골드 상태를 저장하지 않습니다.
골드 상태의 원본은 CurrencyController 또는 CurrencyService가 가져야 합니다.

수신 예:
- GoldTextView
- GoldChangedLogger
- RewardEffectPlayer
```

## Event Channel Browser

경로:

```text
Tools > INLO > Events > Event System Manager > Browser tab
```

Browser는 EventChannel을 관리하기 위한 창입니다.

주요 기능:

- EventChannel 목록 확인
- Debug Log 직접 On/Off
- Description 품질 확인
- Usage Count 확인
- Unused 후보 확인
- 사용처 추적
- Listener / Publisher Candidate / Reference 구분

Usage 분류 기준:

```text
Listener
= EventListener 계열 컴포넌트

Publisher Candidate
= Listener는 아니지만 채널을 참조하고 발행자일 가능성이 있는 컴포넌트

Reference
= 그 외 일반 참조
```

`Publisher Candidate`는 확정 Publisher가 아닙니다.  
`RaiseEvent()` 호출 여부까지 완전하게 코드 분석한 것은 아닙니다.

## Event Graph View

경로:

```text
Tools > INLO > Events > Event System Manager > Graph tab
```

Graph View는 선택한 EventChannel의 흐름을 시각적으로 확인합니다.

```text
Publisher Candidates
→ Event Channel
→ Listeners
```

이 창은 기획자나 팀원이 이벤트 연결 흐름을 빠르게 파악하는 용도입니다.

## Event Audit Report

경로:

```text
Tools > INLO > Events > Event System Manager > Audit tab
```

Audit Report는 전체 EventChannel의 상태를 검사합니다.

검사 항목:

- Description 없음
- Description 너무 짧음
- Debug Log 켜짐
- 사용처 0개인 Unused 후보
- Listener 없음
- Asset dependency는 있지만 상세 사용처 없음

주의:

```text
Addressables key
Resources.Load
문자열 경로
런타임 동적 연결
닫혀 있는 Scene 내부의 상세 GameObject 사용처
```

위 항목은 스캔에 잡히지 않을 수 있습니다.

## Release Build Validation

경로:

```text
Tools > INLO > Events > Validation > Validate Release Build Rules
```

Release Build에서는 Debug Log가 켜진 EventChannel을 허용하지 않습니다.

정책:

```text
Development Build
→ Debug Log 허용

Release Build
→ Debug Log가 켜진 EventChannel이 있으면 빌드 실패
```

## CI Validation

경로:

```text
Tools > INLO > Events > Validation > Run CI Validation
```

배치모드 예:

```bash
Unity.exe -batchmode -quit -projectPath "<PROJECT_PATH>" -executeMethod INLO.Core.Editor.Events.EventChannelCiValidator.Run
```

옵션 예:

```bash
Unity.exe -batchmode -quit -projectPath "<PROJECT_PATH>" -executeMethod INLO.Core.Editor.Events.EventChannelCiValidator.Run -inloEventScanUsages true -inloEventFailOnWarnings true
```

기본 정책:

```text
Error 있음 → 실패
Warning 있음 → 실패
Info만 있음 → 통과
```

## Debug Log 규칙

Debug Log는 개발 중 이벤트 흐름 확인용입니다.

사용 가능:

```text
- 이벤트 연결 테스트
- 예제 검증
- 발행/수신 순서 확인
```

금지:

```text
- Release Build에 켜둔 상태
- 모든 채널에 기본으로 켜기
- 상태 추적을 Debug Log에 의존하기
```

## Unused Candidate 규칙

`UNUSED?`는 삭제 확정이 아니라 검토 후보입니다.

다음 방식은 스캔에 잡히지 않을 수 있습니다.

```text
- Addressables key
- Resources.Load
- 문자열 path
- 런타임 동적 연결
```

삭제 전 반드시 실제 사용 계획을 확인합니다.

## 권장 이벤트 예시

```text
DamageTaken
HealthChanged
GameStateChanged
GoldChanged
ItemAcquired
HitEnemy
```

## 금지 사항

다음은 금지합니다.

```text
- EventChannel을 상태 저장소처럼 사용
- 생성된 auto-generated 파일 직접 수정
- Description 없는 Channel 추가
- Debug Log 켜진 상태로 Release Build
- 같은 기능에 Delegate / R3 / UniRx / EventChannel을 무분별하게 혼용
- 이벤트 흐름을 코드 안에서만 숨기고 Browser/Audit에서 추적 불가능하게 만들기
```
