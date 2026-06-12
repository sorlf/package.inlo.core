# Modular Bootstrap and UI Guide

INLO Core does not prescribe a scene layout, rendering style, or UI architecture. The same package can be used by 2D, 3D, single-scene, multi-scene, UI-scene, and in-scene UI projects.

## Minimal Bootstrap

`AppBootstrap` owns only application-level initialization:

- keeps the first instance
- optionally survives scene loads
- runs assigned `IBootstrapInitializer` components in Inspector order
- reports `Ready` after all assigned initializers succeed
- reports `Failed` when an initializer fails

An `AppBootstrap` with no initializers becomes `Ready` without loading scenes or creating services.

Consumers must check the current state before subscribing because initialization may already be complete.

```csharp
AppBootstrap bootstrap = AppBootstrap.Instance;
if (bootstrap != null && bootstrap.InitializationState == BootstrapInitializationState.Ready)
{
    StartDependentSystem();
}
else if (bootstrap != null)
{
    bootstrap.InitializationCompleted += OnInitializationCompleted;
}
```

## Optional Initial Scene

Add `InitialSceneBootstrapInitializer` only when a project should automatically load an initial scene.

Inspector setup:

1. Add `SceneLoader`.
2. Add `InitialSceneBootstrapInitializer`.
3. Assign the SceneLoader and initial scene name.
4. Add the initializer component to `AppBootstrap.Initializers`.

Projects that manage entry flow another way do not add this initializer.

## General Scene Transitions

`SceneLoader` is independent from UI.

```csharp
bool accepted = sceneLoader.LoadScene("LobbyScene");
sceneLoader.TransitionFinished += OnTransitionFinished;
```

`LoadScene` loads a scene and then runs every `ISceneTransitionStep` attached to the same GameObject. `PrepareLoadedScene` runs those steps for a scene that is already loaded.

Optional systems such as UI Scene loading, Addressables preparation, or lighting setup can implement `ISceneTransitionStep`. With no steps attached, SceneLoader completes after the Unity scene load.

## Optional UI Scene Binding

`UiSceneLoader` is an optional `ISceneTransitionStep`. Add it beside SceneLoader only when a project uses a scene-to-UI-scene mapping.

- no `UiSceneLoader`: SceneLoader performs no UI work
- no matching binding entry: transition succeeds without UI work
- `LoadUiScene`: loads the configured UI scene Additive
- `NoUi`: unloads the previously loaded UI scene

Create a table with:

```text
Create > INLO > Core > UI > Scene UI Binding Table
```

Validate tables manually with:

```text
Tools > INLO > UI Scene Binding > Validate Tables
```

The validator checks only table data. It does not require every Build Settings scene to have a binding.

## Independent UI Utilities

`UiRoot`, `UiLayerRegistry`, `SafeAreaFitter`, `PopupService`, `ToastService`, and `UiButtonEventChannelPublisher` are independent optional utilities.

The predefined layer slots are conveniences, not a mandatory project structure. Projects may use only the slots they need or omit the layer registry entirely.

The package does not require:

- a scene named `BootstrapScene`
- a separate UIScene
- an EventSystem in every project
- six UI layers
- a specific 2D or 3D setup
- direct-play automatic Bootstrap loading

## Current Project Migration

To preserve this project's current startup flow:

1. Place `SceneLoader`, `InitialSceneBootstrapInitializer`, and optional `UiSceneLoader` on the Bootstrap object.
2. Configure the initial scene as `LoginScene`.
3. Assign `SceneUiBindings` to UiSceneLoader.
4. Add InitialSceneBootstrapInitializer to `AppBootstrap.Initializers`.

Scene, prefab, asset, and Inspector setup remains project-owned.
