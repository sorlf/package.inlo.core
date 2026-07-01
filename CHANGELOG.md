# Changelog

## Unreleased

- Separated project-specific startup and UI Scene policies from the core Bootstrap.
- Added optional `IBootstrapInitializer` and `ISceneTransitionStep` extension contracts.
- Changed SceneLoader into a UI-independent general scene loader.
- Changed UiSceneLoader into an optional scene-transition step.
- Removed fixed Bootstrap scene guard, service lists, and open-scene UI architecture validation.
- Kept only optional Scene UI Binding Table data validation.

## Unreleased

- Reworked the DataTable Importer into a single Prepare/Diff/Apply workflow.
- Unified Row schema validation and value mapping rules.
- Added non-destructive previews, atomic bulk apply, Database candidate preview, and Undo support.
- Hardened Google Published CSV and xlsx source handling.
- Replaced unsafe immediate DataTable code generation with reviewed new-file-only C# Schema plans.
- Removed the unused Editor-only Pool/DataTable candidate scanning bridge and its tests; Pool and DataTable are now independent.
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Added explicit `LoadUiScene` and `NoUi` policies for GameScene UI preparation.
- Added scene and UI transition result events with failure reasons.
- Added Bootstrap initial GameScene startup flow and startup configuration validation.
- Added an explicit AppBootstrap initialization state, completion event, and first-instance ownership contract.
- Added duplicate AppBootstrap validation.
- Added 14 new assembly definition files (`.asmdef`) for Events, DataTable, Bootstrap, UI, and Pooling to establish clear dependency boundaries and domain isolation.
- Added temporary references in `INLO.Core.asmdef` and `INLO.Core.Editor.asmdef` to maintain external compatibility.
- Added team documentation router, AI collaboration guide, manual test status, and architecture decision records.
- Added UiSerializedSceneReferenceValidationRule to separate UI serialized scene-reference check logic into a testable rule.
- Added editor validation tests for UI serialized scene-reference rules.
- Added DataTable runtime model, CSV parsing, typed row access, and Editor import foundations.
- Added package structure guide.
- Added package-level INLO Core manual.
- Added Bootstrap and UI setup guide.
- Added Editor UI Toolkit guide for USS/UXML layout standards.
- Added DataTable Importer Window UXML and USS assets.
- Added Bootstrap and UI framework runtime foundations.
- Added UiButtonEventChannelPublisher for simple UI button requests through VoidEventChannelSO.
- Added open-scene Bootstrap/UI validation menu.
- Added Bootstrap service markers and UI button reference validation.
- Added SceneUiBindingTable validation for Bootstrap/UI setup checks.
- Added UI serialized scene-reference and RectTransform risk validation.
- Added UI script pattern validation for scene search and direct game-system references.
- Limited RuntimeBootstrapGuard automatic correction to Editor and Development builds.
- Added runtime guards for unavailable game scene loading and invalid safe-area screen size.
- Added runtime tests for UI button EventChannel publishing.
- Added code configuration helpers and runtime tests for UI layer Popup/Toast services.
- Added code configuration support and runtime tests for SceneUiBindingTable and UiSceneLoader.
- Added runtime tests for Bootstrap service markers and global Bootstrap singleton helpers.
- Added runtime tests for AppBootstrap default execution-foundation service requirements.
- Added testable SceneUiBindingTable validation rule and Editor tests.
- Added testable UI script pattern validation rule and Editor tests.
- Added testable UI RectTransform validation rule and Editor tests.

### Changed

- Changed Bootstrap `Ready` to mean the initial GameScene and its UI policy completed successfully.
- Changed direct GameScene play to use the same scene preparation result contract as normal startup.
- Changed `SceneLoader.LoadGameScene` to return request acceptance and reject overlapping scene transitions.
- Limited the package default Bootstrap required services to project-neutral execution foundations.
- Replaced deprecated ordered object-search calls in Bootstrap runtime and validation tooling with Unity 6 unordered search APIs.
- Clarified package documentation consistency for DataTable, Events, Pooling, Bootstrap/UI, and manual test workflows.
- Split DataTable documentation into primary Editor Importer workflow and secondary runtime CSV parser workflow.
- Updated Events and Pooling documentation to reference the consolidated manager windows.
- Updated Bootstrap/UI examples to use actual `PopupService.Open` and `ToastService.Show` prefab-reference APIs.
- Reorganized DataTable, Pool, Event Editor, and test folders around module ownership.
- Replaced Unity package documentation template content with INLO Core documentation.
- Removed unused template image documentation assets.
- Moved DataTable Importer Window stable frame and reusable visual rules toward UXML/USS.
- Clarified AI Editor-authoring workflow (Builder/Validator MenuItem pattern) in AI_COLLABORATION.md.

## [0.1.0] - 2026-05-31

### Added

- Added ScriptableObject-based Event Channel system.
- Added `EventChannelSO<T>` for typed event channels.
- Added `VoidEventChannelSO` for signal-only events.
- Added `EventListener<TEventData, TChannel>` for typed event listeners.
- Added `VoidEventListener` for Inspector-based responses.
- Added debug logging and description support to event channels.
- Added custom inspector for event channels.
- Added Event Channel Creator editor window.
- Added automatic EventData script generation.
- Added automatic EventChannelSO script generation.
- Added automatic channel asset creation.
- Added event field editor, folder picker, description injection, and preview validation.
