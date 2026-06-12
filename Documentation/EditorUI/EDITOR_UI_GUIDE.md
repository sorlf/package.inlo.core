# INLO Core Editor UI Toolkit Guide

This guide defines the shared UI Toolkit structure and visual language for INLO Core Editor windows.

The shared visual source is `Editor/EditorUI/USS/InloWindowCommon.uss`.

The current visual direction is a premium champagne gold accent combined with a charcoal dark gray base. It features high-depth dark surfaces, ultra-soft gray borders, metallic gold primary actions, and semi-transparent HSL semantic tints for state feedback.

All key visual items are responsive. Interactive elements such as buttons, tabs, selectable list cards, and form inputs strictly enforce `flex-shrink: 1` to guarantee smooth, dynamic reflows under all editor window dimensions, avoiding visual overflows or text cut-offs.

## Folder Convention

Editor windows keep their C# partials and UXML under the owning module. Module USS files are optional and should be used only for visual rules that cannot reasonably be shared.

```text
Editor/<Module>/Windows/
  <Window>.cs
  <Window>Panels.cs
  <Window>Styles.cs
  UXML/
    <Window>.uxml
  USS/
    <Window>.uss
```

Use this structure for module-owned windows. Do not put module-specific window layouts in a shared root folder until at least two modules need the same reusable asset. Prefer `Editor/EditorUI/USS/InloWindowCommon.uss` for shared dark-gray/gold colors, spacing, cards, buttons, lists, badges, and table primitives.

## Responsibility Split

UXML owns stable layout slots:

- Window root
- Toolbar slot
- Status slot
- Summary slot
- Sidebar slot
- Main content slot

USS owns visual rules:

- Spacing
- Borders
- Colors
- Font weight
- Reusable rows, cards, labels, badges, tabs, and buttons
- Stable scroll view sizes

C# owns runtime behavior:

- Data loading and validation
- Event callbacks
- Enabled/disabled state
- Dynamic lists
- Dynamic preview rows and cells
- State classes such as selected, error, warning, or success
- Data-driven widths when the table shape is not known in UXML

## Naming Rules

Use an `inlo-` prefix for reusable INLO Editor UI classes. This is the default for Editor window visual classes.

Use a module-specific prefix only for residual styles that cannot be expressed as reusable components.

Use BEM-like modifiers for state:

```text
inlo-notice
inlo-notice--error
inlo-list-card
inlo-list-card--selected
```

## C# Rules

Editor window C# should load UXML and USS by package-relative `AssetDatabase.LoadAssetAtPath`.

Do not use `Resources.Load` for package Editor layouts.

Use `Editor/EditorUI/USS/InloWindowCommon.uss` for shared window visuals before adding module-specific USS.

Prefer `AddToClassList` and `EnableInClassList` over inline `style.*` assignments.

Inline style is allowed only when the value is runtime data:

- Table cell width
- Runtime validation color
- Value-derived enabled state
- Dynamic row count or computed height

If the same inline style appears in more than one place, move it to USS.

## Shared Editor Window Baseline

Pool, Events, and DataTable Editor windows use:

```text
Editor/EditorUI/USS/InloWindowCommon.uss
```

Module UXML assets still define stable frames. C# partials populate the named slots and generate dynamic content.

`DataTableImporterWindow` keeps its UXML at:

```text
Editor/DataTable/Windows/UXML/DataTableImporterWindow.uxml
```

Its old module USS file is no longer the visual baseline; shared `inlo-` classes are.

---

## Editor Window Design Principles & Layout Guidelines

To ensure professional-grade UI/UX for editor windows, all developers and AI agents must strictly follow these structural and visual design principles.

### 1. 3-Tier Information Prioritization (3단계 정보 설계 원칙)

To maximize readability and prevent information overload on complex screens, categorize UI components into three priorities:

*   **Priority 1: Status & Primary Action (1단계: High)**:
    *   *Content*: Window integrity summary (Total metrics, Error/Warning counts), Primary Hero Action (e.g., "Run Audit", "Generate Scripts").
    *   *Placement*: Positioned at the very top of the window as a global header, or at the top of the main workspace as a horizontal, elegant **Dashboard Row** (`.inlo-dashboard-row` with `.inlo-dashboard-card`).
*   **Priority 2: Navigation & Controls (2단계: Medium)**:
    *   *Content*: Filtering inputs, search textfields, and primary search/navigation list views.
    *   *Placement*: Placed inside a dedicated **Left Sidebar** with a fixed/constrained width (e.g., 330px to 370px) so the user can easily scan and filter the dataset.
*   **Priority 3: Workspace & Details (3단계: Low)**:
    *   *Content*: Dense metadata (key-value properties), validation logs, usage scanner trackers, or reactive previewers.
    *   *Placement*: Placed inside the **Right Workspace** as a flexible grid, scrollable list, or collapsible panels.

### 2. F-Pattern Eye-Tracking Layout (F-패턴 레이아웃 정렬)

Avoid stacking all controls and data tables vertically in a single linear column, which causes heavy mouse scrolling.

*   Always use a **Split-Pane / Tri-Pane layout** (`.inlo-split-container` + `.inlo-sidebar-left` + `.inlo-workspace-right`).
*   Users naturally scan from top-left (Header/Controls) to bottom-right (Main workspace results).
*   Enforce a clean **8px / 12px / 16px grid spacing** for margins and paddings using the spacing variables, ensuring the layout remains breathing and highly legible.

### 3. HSL Semantic Accent & Styling Rules (세만틱 컬러 및 액센트 규칙)

Color coding must remain consistent across all windows to avoid visual noise while maximizing immediate recognition:

*   **Status Colors**: Use HSL semantic color variables defined in `:root` alongside premium champagne gold:
    *   `--inlo-accent`: Metallic Champagne Gold (`rgb(226, 185, 107)`) for principal actions and selected states.
    *   `--inlo-color-ok`: Emerald HSL Green for valid/success states.
    *   `--inlo-color-warning`: Amber Gold HSL for warning states.
    *   `--inlo-color-error`: Coral Red HSL for critical compilation/runtime errors.
    *   `--inlo-color-info`: Steel Blue HSL for information/audit feedback.
*   **Semantic Borders & Tinting**: Apply unified classes for list items (`.inlo-list-item`) and cards (`.inlo-card`):
    *   *Contained Accents*: Apply state modifiers (`--ok`, `--warning`, `--error`, `--info`) to render a **4px left border accent** alongside a subtle, low-opacity (4% to 8%) background tint (e.g., `.inlo-card--error`).
    *   *Recommendations & Notices*: Give passive tip panels or recommendation blocks an informational border accent (`.inlo-card--info` or `.inlo-notice--warning`) to guide the user's attention.

### 4. Unified Workspace Manager & Component Panel Swapping (통합 워크스페이스 매니저 & 탭 전환 패널 아키텍처)

To prevent cluttering the Unity Editor with numerous minor windows, we enforce the **Unified Workspace Manager** pattern:

*   **Window Consolidation**: Do not create separate, minor floating windows for highly related sub-domains (e.g., separate windows for Pool Database, Validation Report, and Live Debugging). Instead, consolidate them into a single, high-fidelity **Domain Manager Window** (e.g., `PoolSystemManagerWindow`, `EventSystemManagerWindow`).
*   **Tab Navigation (Header Tab bar)**: Display a prominent tab-bar navigation (`.inlo-tab-button`) in the global top header. Switching tabs must swap the active panel within a dedicated `#manager-content-slot` element.
*   **Component Panels (`VisualElement`)**: Implement each tab workspace as a dedicated, self-contained `VisualElement` panel subclass (e.g., `PoolBrowserPanel`, `EventAuditPanel`, `EventCreatorPanel`).
*   **Caching & State Preservation**: To guarantee excellent UX, do not destroy panel instances during tab switches. Retain their instances as lazy-loaded members within the parent `InloBaseEditorWindow` container. This ensures that user inputs (e.g., active searches, filters, or form draft values) and scroll offsets are completely preserved when switching back and forth.
*   **Inline Creator Panel (Modal-less Embedded Views)**: Rather than spawning a separate floating popup window for asset creation (e.g. creating event channels), embed the Creator panel directly inside the workspace content slot as an inline panel. Provide an explicit back button (`◀ Back to Browser`) to seamlessly return to the prior list state and trigger a refresh.

