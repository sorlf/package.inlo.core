# INLO Core

INLO Core is a Unity UPM package for shared runtime and editor systems used by INLO projects.

Current modules & tools:

- **Bootstrap**: project-neutral initialization and scene-transition extension contracts.
- **DataTable**: Excel (.xlsx) / Google Sheets runtime generation and parsing.
- **Events**: ScriptableObject EventChannel runtime and graph visibility.
- **UI**: Additive UI scene binding and popup/toast services.
- **Pooling**: GameObject pool configuration and runtime diagnostics.
- **INLO Control Center (v2.5.0)**: Premium decoupled unified hub for all core package operations.
- **INLO Inspector Kit**: Modern, attribute-driven inspector engine (`[InloButton]`, `[InloRequired]`) with Midnight Orchid (Dark) and Modern Lavender White (Light) themes.

See [Package Manual](Documentation/com.inlo.core.md) for the package-level technical overview.

## Documentation Router

Read only the documents needed for the current job.

| Target Audience | Purpose / Action | Document |
| :--- | :--- | :--- |
| **초보 개발자 / 즉시 적용** | **당장 5분 만에 실무 씬에 연동해서 동작시켜야 할 때** | **[User Guide 5분 퀵스타트](Documentation/UserGuide/USER_GUIDE_ROUTER.md)** |
| 온보딩 팀원 (Onboarding) | 프로젝트 환경 파악 및 패키지 개발 지침 습득 | [Team Guide](Documentation/TEAM_GUIDE.md) |
| AI 협업 작업 (AI Agents) | AI 코딩 어시스턴트 협업 및 자동화 규칙 준수 | [AI Collaboration Guide](Documentation/AI_COLLABORATION.md) |
| 아키텍트 / 리뷰어 | 아키텍처 의사결정 내역 및 철학 확인 | [Architecture Decisions](Documentation/Architecture/ADR_DECISION_LOG.md) |
| QA / 매뉴얼 테스터 | 현재 수동 테스트 케이스 및 검증 상태 점검 | [Manual Test Status](Documentation/MANUAL_TEST_STATUS.md) |
| 모듈별 상세 구현자 | Bootstrap & UI 상세 설정 및 사용법 확인 | [Bootstrap & UI 상세 설정 매뉴얼](Documentation/UserGuide/BOOTSTRAP_UI_GUIDE.md) |
| 모듈별 상세 구현자 | DataTable, Events, Pooling의 세부 사용법 확인 | [DataTable, Events, Pooling 사용 가이드](Documentation/UserGuide/DATATABLE_EVENTS_POOLING.md) |
| 모듈별 상세 구현자 | DataTable 모듈 구조 및 런타임 상세 스펙 확인 | [DataTable Technical Guide](Documentation/DataTable/DATATABLE_REFERENCE.md) |
| 모듈별 상세 구현자 | Editor UI USS/UXML 디자인 규격 준수 | [Editor UI Toolkit Guide](Documentation/EditorUI/EDITOR_UI_GUIDE.md) |

---

## Project Rules

All AI-assisted work in this repository follows the root `AGENTS.md`.

Core rule:
```text
Plan first -> human approval -> execute only inside the approved scope
```

*   Do not change scene, prefab, or `.meta` files directly. Scene composition, prefab placement, inspector setup, and asset import are human-owned tasks.
*   See [Structure Guide](STRUCTURE_GUIDE.md) for package folder ownership and placement rules.

---

## Architectural Boundaries

To ensure package reusability and stability, the following architectural boundaries are strictly enforced:

1.  **No Editor References in Runtime**: Runtime code must not reference `UnityEditor` or any Editor-only namespace.
2.  **DataTable Isolation**: DataTable is a pure data system. It must not reference or depend on Pooling, Events, Addressables, Google Spreadsheet clients, or Editor UI at runtime.
3.  **Pooling Independence**: Pooling manages GameObject reusability. It must not read spreadsheets or generate source code/assets at runtime.
4.  **Event System Centrality**: `EventChannel` is the default shared messaging pattern. Do not replace it with customized delegate events or other messaging packages without prior approval.

For more information, see the [Package Manual](Documentation/com.inlo.core.md) or [Architecture Decisions](Documentation/Architecture/ADR_DECISION_LOG.md).
