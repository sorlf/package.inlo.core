# INLO Core Structure Guide

This guide defines the folder ownership rules for `com.inlo.core`.

The package is a shared Unity core library. It must stay project-neutral and must not contain scene setup, prefab placement, sample gameplay systems, or game-specific data.

## Root

```text
Packages/com.inlo.core/
  Runtime/
  Editor/
  Documentation/
  Samples/
  Tests/
```

Root files:

- `README.md`: package entry point and module overview.
- `AGENTS.md`: package-local rules for AI and automated coding agents.
- `STRUCTURE_GUIDE.md`: folder ownership and placement rules.
- `CHANGELOG.md`: package-level change history.
- `package.json`: UPM manifest.

## Runtime

Runtime code must not reference `UnityEditor` or any `Editor` namespace.

```text
Runtime/
  INLO.Core.asmdef (임시 어셈블리 결합용 루트 asmdef)
  Bootstrap/
    INLO.Core.Bootstrap.asmdef
  DataTable/
    INLO.Core.DataTable.asmdef
  Events/
    INLO.Core.Events.asmdef
  UI/
    INLO.Core.UI.asmdef
  Pool/
    INLO.Core.Pooling.asmdef
```

`Runtime` 하위 모듈들은 의존성 검증 및 물리 분리 준비를 위해 개별 `asmdef`로 세분화되어 컴파일됩니다. `INLO.Core.asmdef`는 외부 빌드 호환성을 유지하기 위해 신규 분할된 하위 어셈블리들을 모두 임시 결합 참조 형태로 유지합니다.

### Runtime/Bootstrap

Contains project-neutral application initialization and scene-transition contracts such as `AppBootstrap`, `IBootstrapInitializer`, `SceneLoader`, and `ISceneTransitionStep`.

Bootstrap must not assume a fixed scene name, UI architecture, EventSystem, or project-specific service list. Optional project behavior is attached through initializer and transition-step components.

### Runtime/DataTable

```text
Runtime/DataTable/
  Core/
  Assets/
  Parsing/
  Sources/
```

- `Core/`: schema, row, table, value type, and DataTable exceptions.
- `Assets/`: ScriptableObject-based typed table assets and row attributes.
- `Parsing/`: CSV reader and value parsing.
- `Sources/`: source abstractions for future import workflows.

DataTable runtime code must not know Pool, EventChannel, Addressables, Google Spreadsheet, or Editor UI types.

### Runtime/Events

Contains ScriptableObject EventChannel runtime types.

EventChannel is for broadcasting events. It must not become a state store.

### Runtime/UI

Contains optional UI scene loading and independent UI utilities such as `SceneUiBindingTable`, `UiSceneLoader`, `UiRoot`, `UiLayerRegistry`, `SafeAreaFitter`, `UiButtonEventChannelPublisher`, `PopupService`, and `ToastService`.

Runtime UI code must not reference game-scene objects directly. Scene and prefab composition remains project-owned.

### Runtime/Pool

```text
Runtime/Pool/
  Bootstrap/
  Config/
  Core/
  Diagnostics/
  Management/
  Returners/
```

Pool runtime owns pool keys, pool entries, pool databases, bootstrap behavior, diagnostics, pool managers, and object return helpers.

Pool runtime must not read Google Spreadsheet data and must not generate PoolDatabase assets at runtime.

## Editor

Editor code may reference runtime assemblies. Runtime code must not reference editor code.

```text
Editor/
  INLO.Core.Editor.asmdef (임시 어셈블리 결합용 루트 에디터 asmdef)
  Bootstrap/
    INLO.Core.Bootstrap.Editor.asmdef
  DataTable/
    INLO.Core.DataTable.Editor.asmdef
  Events/
    INLO.Core.Events.Editor.asmdef
  Pool/
    INLO.Core.Pooling.Editor.asmdef
  EditorUI/
    INLO.Core.EditorUI.Editor.asmdef
```

`Editor` 하위 모듈들 역시 개별 `asmdef`로 세분화되어 컴파일됩니다. `INLO.Core.Editor.asmdef`는 외부 빌드 호환성 유지를 위해 신규 분할된 하위 에디터 어셈블리들을 임시 결합 참조 형태로 유지합니다.

### Editor/Bootstrap

Contains optional UI Scene binding-table validation.
Editor validation may inspect scenes, but it must not save scenes, modify prefabs, or change Project Settings without explicit approval.
Validation must not require a fixed Bootstrap scene, EventSystem, UI Scene split, UiRoot, or layer layout.

### Editor/DataTable

```text
Editor/DataTable/
  Generation/
  Import/
  Validation/
  Windows/
```

- `Generation/`: DataTable asset and database generation helpers.
- `Import/`: source readers, grid models, row mapping, row type reflection, and import services.
- `Validation/`: DataTable schema and import validation rules.
- `Windows/`: editor windows, UI helper partials, and window-owned UI Toolkit assets.

DataTable windows use this UI Toolkit asset layout:

```text
Editor/DataTable/Windows/
  UXML/
  USS/
```

- `UXML/`: stable window layout slots.
- `USS/`: window visual rules, spacing, borders, colors, and reusable classes.

DataTable editor tooling must not reference Pool or own PoolDatabase workflows.

### Editor/Pool

```text
Editor/Pool/
  Config/
  Utilities/
  Validation/
  Windows/
```

- `Validation/`: PoolDatabase validation logic and menus.
- `Windows/`: Pool editor windows.
- `Utilities/`: bulk operations and editor helpers.
- `Config/`: custom inspectors for Pool config assets.

Pool editor tooling and DataTable tooling must remain independent.

### Editor/Events

```text
Editor/Events/
  Audit/
  Browser/
  Creation/
  Graph/
  Inspectors/
  Validation/
```

- `Audit/`: EventChannel audit models, runner, and report window.
- `Browser/`: EventChannel browser, channel info, usage scanning, and browser styles.
- `Creation/`: EventChannel creator window.
- `Graph/`: EventChannel graph view.
- `Inspectors/`: custom inspectors.
- `Validation/`: build and CI validation.

Do not replace EventChannel with another communication pattern from this folder.

## Documentation

```text
Documentation/
  BootstrapUI/
  DataTable/
  EditorUI/
  Events/
```

Module documentation should explain behavior, rules, boundaries, and workflows. Do not bury package-wide rules inside one module document.
Bootstrap and UI setup rules belong in `Documentation/BootstrapUI/`.
Editor UI Toolkit layout standards belong in `Documentation/EditorUI/`.

## Tests

```text
Tests/
  Runtime/
    Bootstrap/
      INLO.Core.Bootstrap.Tests.asmdef
    DataTable/
      INLO.Core.DataTable.Tests.asmdef
    UI/
      INLO.Core.UI.Tests.asmdef
  Editor/
    Bootstrap/
      INLO.Core.Bootstrap.Editor.Tests.asmdef
```

각 테스트 하위 모듈은 각각의 고유 테스트 `asmdef` 파일에 의해 컴파일 범위를 소유하며, 상위 루트 테스트 어셈블리(`INLO.Core.Tests`, `INLO.Core.Editor.Tests`)는 하위 테스트를 명시적으로 참조하지 않는 독립적인 레거시 셸 구조를 유지합니다.

Test folders should mirror the module or workflow they validate. Avoid generic example test names.

## Placement Rules

- Runtime features go under `Runtime/<Module>`.
- Editor-only features go under `Editor/<Module>`.
- Runtime code must not reference editor code.
- DataTable and Pool must not reference each other.
- EventChannel structure must not be changed while working on DataTable or Pool.
- UI Toolkit, USS, and UXML layout work must be planned as a separate editor workflow.
- Public API renames require documentation and changelog updates.
- Folder structure changes require updating this guide.
