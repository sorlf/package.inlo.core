# AGENTS.md

## Purpose

This repository is a Unity Core Library.
Do not treat it as a game project.
Do not add sample scenes, demo gameplay systems, or project-specific logic unless explicitly requested.

## Required Reading Order

Before editing code, read the following files:

1. README.md
2. STRUCTURE_GUIDE.md
3. CHANGELOG.md

## Core Rule

Preserve the existing architecture.

Do not mix communication patterns casually.
Before adding a new event, callback, observable, or message flow, decide which communication method is appropriate.

## Communication Rules

Use delegate or C# event when:
- The relationship is simple and local.
- The caller and receiver are tightly related.
- The flow is 1:1 or very small in scope.

Use ScriptableObject EventChannel when:
- One system raises an event and multiple independent systems may react.
- The sender should not directly reference the receivers.
- The event represents a gameplay-level or application-level occurrence.

Use R3 when:
- The data is continuous or stream-like.
- The value changes over time.
- Filtering, combining, throttling, or reactive composition is needed.

Do not introduce UniRx unless:
- The project already depends on it for a specific reason.
- The requested task explicitly requires compatibility with existing UniRx code.

## Folder Rules

Runtime code must be placed under Runtime.
Editor-only code must be placed under Editor.
Documentation must be placed under Documentation or the repository root.
Do not place Editor code inside Runtime folders.
Do not place game-specific sample logic inside the core library.

## EventChannel Rules

Do not create duplicate EventChannel implementations.
Use the existing base classes and browser/editor tooling.
When adding a new EventChannel type, follow the existing naming and folder conventions.

## Prohibited Changes

Do not:
- Replace the EventChannel system with delegate-only code.
- Add R3 or UniRx to simple one-shot events.
- Add sample scenes unless explicitly requested.
- Add project-specific gameplay code to the core library.
- Rename public APIs without updating documentation and changelog.
- Modify folder structure without updating STRUCTURE_GUIDE.md.

## When Unsure

Stop and explain the tradeoff before changing the architecture.