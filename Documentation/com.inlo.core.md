# INLO Core Manual

INLO Core is a Unity UPM package for shared systems that should stay reusable across INLO projects.

The package currently contains five modules:

- DataTable: CSV-style table parsing, schema validation, typed row access, and Editor import foundations.
- Events: ScriptableObject EventChannel runtime and Editor visibility tools.
- Pooling: GameObject pool configuration, runtime pool management, validation, and Editor utilities.
- Bootstrap: project-neutral initialization and scene-transition extension contracts.
- UI: optional additive UI scene binding plus independent UI layers, safe-area fitting, popup service, and toast service.

This document is the package-level manual. Module-specific rules live in each module document.

## Read This Way

Do not use this manual as a dump of every rule. Use it as the package map.

| Target Audience | Start here | Document |
| :--- | :--- | :--- |
| **초보 개발자 / 즉시 적용** | **당장 5분 만에 실무 씬에 연동해서 동작시켜야 할 때** | **[User Guide 5분 퀵스타트](UserGuide/USER_GUIDE_ROUTER.md)** |
| New team member | Onboarding guidelines & package standards | [Team Guide](TEAM_GUIDE.md) |
| AI agent | Codegen rules & plan-first constraint | [AI Collaboration Guide](AI_COLLABORATION.md) |
| Architect/reviewer | Decision logs & design philosophy | [Architecture Decisions](Architecture/ADR_DECISION_LOG.md) |
| QA/manual tester | Manual test cases & validation results | [Manual Test Status](MANUAL_TEST_STATUS.md) |

## Installation

Add the package through Unity Package Manager as `com.inlo.core`.

The package targets Unity `6000.0` and depends on Unity Test Framework `1.6.0`.

## Package Contents

| Location | Description |
| --- | --- |
| `Runtime/DataTable` | Runtime DataTable model, typed assets, parsing, and source abstractions. |
| `Runtime/Events` | Runtime EventChannel base types, typed channels, and listeners. |
| `Runtime/Pool` | Pool keys, configuration assets, runtime pool management, diagnostics, and return helpers. |
| `Editor/DataTable` | DataTable generation, import, validation, and importer windows. |
| `Editor/Events` | EventChannel browser, graph, audit, creation, inspector, and validation tools. |
| `Editor/Pool` | Pool config inspectors, validation, utilities, and windows. |
| `Documentation/DataTable` | DataTable technical guide and design boundaries. |
| `Documentation/Architecture` | Architecture decision records for repeated design choices. |
| `Documentation/EditorUI` | UI Toolkit USS/UXML structure and Editor window layout standards. |
| `Documentation/TEAM_GUIDE.md` | Human team onboarding and workflow routing. |
| `Documentation/AI_COLLABORATION.md` | Condensed AI collaboration rules and failure examples. |
| `Documentation/MANUAL_TEST_STATUS.md` | Current manual test availability and restoration status. |
| `Tests` | Runtime and Editor tests, grouped by validated module or workflow. |

## Architectural Boundaries

Runtime code must not reference `UnityEditor` or any Editor-only namespace.

DataTable runtime code is a data system. It must not know Pool types, EventChannel types, Addressables, Google Spreadsheet clients, or Editor UI.

Pool runtime code owns runtime pooling behavior. It must not read spreadsheets or generate assets at runtime.

Pool and DataTable are independent at runtime and in Editor tooling.

EventChannel remains the default shared game-event communication pattern. Do not replace it with delegate, event, `UnityEvent`, R3, UniRx, or another messaging package unless that change is explicitly planned and approved.

## Document Revision History

| Date | Reason |
| --- | --- |
| 2026-06-06 | Simplified document structure to align with Single Source of Truth (SSOT). |
| 2026-06-02 | Added Editor UI Toolkit standardization baseline for DataTable Importer Window. |
| 2026-06-02 | Replaced Unity package template content with the INLO Core package manual. |
