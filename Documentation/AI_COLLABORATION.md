# INLO Core AI Collaboration Guide

이 문서는 AI 에이전트가 INLO Core에서 작업할 때 매번 읽어야 하는 압축 규칙입니다.

긴 설명을 모두 읽지 않아도 같은 판단을 반복하게 만드는 것이 목적입니다. 세부 이유는 `Documentation/Architecture`의 ADR을 확인합니다.

## 항상 먼저 할 일

1. 루트 `AGENTS.md`를 따른다.
2. 작업 전에 계획 문서를 만들고 사람의 승인을 기다린다.
3. `Packages/com.inlo.core/README.md`에서 모듈 경계와 금지 사항을 확인한다.
4. 작업 모듈의 README와 관련 ADR만 추가로 읽는다.
5. 승인된 범위를 벗어나면 멈추고 다시 계획한다.

## 권한

AI가 할 수 있는 일:

- C# runtime/editor code 작성과 수정
- 문서 작성과 정합성 수정
- 테스트 코드 작성
- 코드/문서 검색, 빌드, 검증
- 설계 tradeoff 설명

AI가 직접 하지 않는 일:

- `.unity`, `.prefab`, `.meta` 직접 수정
- Project Settings 직접 변경
- 씬 구성, prefab 배치, inspector 연결
- asset import
- 승인 없는 코드/문서 수정

## 컨텍스트 라우팅

모든 문서를 한 번에 읽지 않습니다.

| 작업 | 필수 읽기 |
| --- | --- |
| 공통 | `AGENTS.md`, `Packages/com.inlo.core/README.md`, 이 문서 |
| DataTable | `Documentation/DataTable/README.md`, `Architecture/ADR-0001-datatable-primary-workflow.md` |
| DataTable 또는 Pool 경계 | `Architecture/ADR-0002-pool-datatable-boundary.md` |
| Events | `Documentation/Events/README.md`, `Architecture/ADR-0003-event-communication-policy.md` |
| Bootstrap/UI | `Documentation/BootstrapUI/README.md` |
| Editor UI Toolkit | `Documentation/EditorUI/README.md` |
| Manual test | `Documentation/MANUAL_TEST_STATUS.md` |

읽은 문서가 서로 충돌하면 `AGENTS.md`와 package README를 우선하고, 충돌 사실을 보고합니다.

## 금지 경계

DataTable 작업 중 금지:

- Runtime Google Spreadsheet request 추가
- DataTable과 Pool을 runtime 또는 Editor tooling에서 연결하기
- DataTable에서 PoolDatabase를 생성 또는 갱신하기
- EventChannel 구조 변경
- DataTable 작업에 R3/UniRx/MessagePipe 도입

Events 작업 중 금지:

- EventChannel을 상태 저장소처럼 사용
- 같은 책임에 delegate/R3/UnityEvent/EventChannel을 섞기
- Description 없는 EventChannel 추가
- release build에서 Debug Log 켜두기
- Browser/Graph/Audit에서 추적 불가능한 핵심 이벤트 만들기

UI/Bootstrap 작업 중 금지:

- UI scene object가 GameScene object를 직접 참조하게 만들기
- UI Button을 GameScene manager에 직접 연결하기
- TMP 대신 legacy `UnityEngine.UI.Text` 제안하기
- Bootstrap을 UI loader만으로 축소하기

Pooling 작업 중 금지:

- 반복 생성/파괴를 방치하기
- DataTable을 Pool 설정 원천으로 사용하기

## 흔한 나쁜 요청과 올바른 응답

요청:

```text
DataTable에서 PoolDatabase를 자동 생성해줘.
```

응답:

```text
거절합니다. DataTable과 Pool은 독립 시스템이며 자동 연결 workflow를 제공하지 않습니다. PoolDatabase는 Pool tooling에서 명시적으로 구성해야 합니다.
```

요청:

```text
Google Sheet를 게임 실행 중 바로 읽게 해줘.
```

응답:

```text
거절합니다. Runtime Google request는 빌드 안정성 때문에 금지입니다. Editor importer로 가져온 DataTableAsset을 사용해야 합니다.
```

요청:

```text
이벤트 채널에 현재 HP와 Max HP를 저장해줘.
```

응답:

```text
거절합니다. EventChannel은 상태 저장소가 아닙니다. HP 상태는 Model/Service/Controller가 소유하고, EventChannel은 변경 사건만 broadcast해야 합니다.
```

요청:

```text
작은 수정이니까 바로 고쳐줘.
```

응답:

```text
바로 수정할 수 없습니다. 이 저장소는 작은 수정도 계획 문서와 명시적 승인이 필요합니다.
```

## 완료 보고 전 검증

문서 변경:

- UTF-8 without BOM
- LF only
- 깨진 한글 없음
- `git diff --check` 통과
- 과거 메뉴 경로나 깨진 링크 검색

코드 변경:

- 관련 asmdef/project build 또는 Unity Test Runner 검증
- runtime code가 Editor namespace를 참조하지 않는지 확인
- 매 프레임 경로 allocation 여부 확인
- public API 변경 시 README, module doc, changelog 갱신

씬/프리팹/asset 작업 필요:

- 직접 수정하지 않고 사람에게 Unity Editor 작업 지시로 제공합니다.

## 보고 형식

완료 보고에는 다음을 포함합니다.

- 변경한 문서/파일
- 바뀐 판단 기준
- 검증 결과
- 사람이 Unity에서 해야 할 일
- 남은 위험
