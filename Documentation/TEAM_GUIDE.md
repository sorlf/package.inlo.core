# INLO Core Team Guide

이 문서는 INLO Core를 처음 쓰는 팀원이 가장 적은 문맥으로 올바른 일을 시작하기 위한 입구입니다.

전체 문서를 한 번에 읽지 마세요. 지금 하려는 일에 맞는 최소 문서만 읽고, 작업 중 애매해지는 순간 관련 ADR을 확인합니다.

## 15분 시작 경로

1. `Packages/com.inlo.core/README.md`에서 현재 모듈과 금지 경계를 확인합니다.
2. 이 문서의 "현재 상태" 표를 확인합니다.
3. 작업 영역에 맞는 모듈 문서 하나만 읽습니다.
4. 구현이나 문서 수정을 시작하기 전 `AGENTS.md`의 계획/승인 규칙을 따릅니다.
5. 작업이 끝나면 문서의 검증 항목과 인코딩/줄끝 검증을 확인합니다.

## 현재 상태

| 영역 | 상태 | 팀원이 기대해도 되는 것 |
| --- | --- | --- |
| Bootstrap/UI runtime | Stable | BootstrapScene, UI scene binding, safe area, popup/toast prefab instantiation |
| DataTable Editor Importer | Stable | xlsx/Google Published CSV를 `DataTableAsset<TRow>`로 import |
| DataTable runtime parser | Secondary | 타입 행이 있는 CSV 문자열을 직접 파싱 |
| Events runtime | Stable | ScriptableObject EventChannel 발행/수신 |
| Events editor tools | Stable | Event System Manager와 build/CI validation |
| Pooling runtime | Stable | PoolDatabase 등록, PoolKey spawn/release |
| DataTable C# Schema preparation | Controlled | 단일 Importer에서 Preview 후 신규 파일만 생성 |
| Manual DataTable sample | Needs Restoration | 현재 샘플 타입 일부가 없어 전체 수동 테스트 불가 |

상태 의미:

- Stable: 일반 프로젝트에서 사용 가능. 문서와 테스트 기준을 유지해야 합니다.
- Experimental: 사용은 가능하지만 API나 workflow가 바뀔 수 있습니다.
- Secondary: 주 사용 경로가 아닌 보조 API입니다.
- Planned: 문서상 방향만 있고 구현은 없습니다.
- Needs Restoration: 현재 저장소 상태만으로는 온전한 사용/테스트가 어렵습니다.
- Prohibited: 설계상 금지입니다.

## 작업별 읽을 문서

| 작업 | 먼저 읽을 문서 |
| --- | --- |
| 어떤 일을 해도 공통 | `AGENTS.md`, `Packages/com.inlo.core/README.md` |
| 신규 팀원 온보딩 | `Documentation/TEAM_GUIDE.md`, `Documentation/com.inlo.core.md` |
| AI에게 작업 지시 | `Documentation/AI_COLLABORATION.md` |
| DataTable import/row/table | `Documentation/DataTable/DATATABLE_REFERENCE.md`, `Documentation/Architecture/ADR-0001-datatable-primary-workflow.md` |
| DataTable과 Pool 경계 | `Documentation/Architecture/ADR-0002-pool-datatable-boundary.md` |
| EventChannel 설계 | `Documentation/Events/EVENTS_REFERENCE.md`, `Documentation/Architecture/ADR-0003-event-communication-policy.md` |
| Bootstrap/UI scene setup | `Documentation/BootstrapUI/BOOTSTRAP_UI_REFERENCE.md` |
| Editor UI Toolkit window | `Documentation/EditorUI/EDITOR_UI_GUIDE.md` |
| 전체 수동 테스트 | `Documentation/MANUAL_TEST_STATUS.md` |

## 팀원용 성공 경로

### DataTable을 처음 import할 때

1. Row 타입과 `DataTableAsset<TRow>` 타입을 프로젝트 코드에 만듭니다.
2. 원본 xlsx 또는 Google Published CSV의 첫 행을 Row 필드명과 맞춥니다.
3. Unity에서 `Tools > INLO > DataTable > Importer`를 엽니다.
4. 대상 table asset을 선택하고 `변경 준비`를 실행합니다.
5. 실제 값 변환과 Diff를 확인한 뒤 `준비된 변경 적용`을 실행합니다.
6. runtime에서는 `DataTableAsset<TRow>.TryGet(id, out row)`로 조회합니다.

### EventChannel을 추가할 때

1. 이 이벤트가 상태인지 사건인지 먼저 구분합니다.
2. 사건이면 EventChannel, 지속 상태면 Model/Service/Store가 소유합니다.
3. `Tools > INLO > Events > Event System Manager`에서 channel과 관련 파일을 생성합니다.
4. Description에는 발생 시점, 저장하지 않는 상태, 대표 listener를 적습니다.
5. Browser/Graph/Audit 탭에서 추적 가능해야 합니다.

### Pool을 사용할 때

1. prefab을 직접 spawn/despawn하지 않고 PoolDatabase entry를 만듭니다.
2. `PoolBootstrapper`로 PoolDatabase를 등록합니다.
3. gameplay code는 `PoolKey` 기반 `PoolManager.Get/TryGet`을 우선 사용합니다.

## 팀원이 직접 하지 말아야 할 기대

- DataTable과 PoolDatabase가 자동으로 연결되리라 기대하지 않습니다.
- 런타임에서 Google Spreadsheet를 읽으리라 기대하지 않습니다.
- EventChannel이 상태 저장소가 되리라 기대하지 않습니다.
- Manual Test Guide가 현재 그대로 전체 통과하리라 기대하지 않습니다.
- AI가 승인 없이 코드를 고치리라 기대하지 않습니다.

## 문서 갱신 규칙

- 기능 상태가 바뀌면 이 문서의 현재 상태 표를 같이 갱신합니다.
- 새 금지 경계가 생기면 `AI_COLLABORATION.md`와 관련 ADR을 같이 갱신합니다.
- 수동 테스트가 복구되면 `MANUAL_TEST_STATUS.md`를 먼저 갱신합니다.
- public API나 메뉴 경로가 바뀌면 `CHANGELOG.md`를 갱신합니다.
