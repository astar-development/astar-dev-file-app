<!--
SECTION PURPOSE: Frontmatter defines scope (which files are governed by these rules).
PROMPTING: Keep the YAML minimal; AI should respect this glob when proposing edits.
-->

---

## applyTo: "**/\*.cs, **/_.axaml, \*\*/_.razor, \*_/_.axaml.cs

<!--
SECTION PURPOSE: Introduce mandatory frontend guidance.
PROMPTING: Clear headings; concise bullets for scanability.
COMPLIANCE: Treat rules below as defaults unless project overrides exist.
-->

# Frontend Instructions

<CRITICAL_REQUIREMENT type="MANDATORY">

- Use AvaloniaUI / ReactiveUI for new code and Windows / components.
  </CRITICAL_REQUIREMENT>

<!--
SECTION PURPOSE: Universal rules for all frontend code.
PROMPTING: Imperative checklist for quick verification.
-->

## General Guidelines

1. **Code Structure**: Prefer small, reusable components and feature modules.
2. **Styling**: Follow repo standard (CSS Modules/Tailwind/SCSS). Avoid inline styles except small dynamic cases.
3. **Accessibility**: Prefer native controls, clear labels, visible focus, sufficient contrast.
4. **Testing**: Use the project's testing stack (e.g., XUnit.V3). See the Testing section below.

<!--
SECTION PURPOSE: Expectations when authoring components.
PROMPTING: Specify contract (props/state), error modes, and data flow norms.
-->

## Component Development

1. **Properties**: Define with C# properties; use nullable reference types (`string?`) for optional properties. Provide sensible defaults via property initializers or constructors. Use descriptive XML documentation comments on public properties.
2. **State Management**: Use ReactiveUI patterns - inherit ViewModels from `ReactiveObject`, expose observable state via `IObservable<T>` or reactive properties, and use `WhenAnyValue()` for reactive bindings. Prefer local ViewModel state; elevate to shared services (dependency-injected) only when cross-cutting.
3. **API Calls**: Use the shared API client; centralize endpoints and schemas; handle errors explicitly with `Result<T>` patterns from `AStar.Dev.Functional.Extensions`.
4. **Error Handling**: Implement error handling in ViewModels; expose error states via observable properties (e.g., `IObservable<string?> ErrorMessage`); provide user-friendly error messages bound to UI elements.

<!--
SECTION PURPOSE: Make testing guidance explicit and link to SSOTs (Tester chat mode and BDD instructions).
PROMPTING: Reference, don't duplicate. Keep actions concrete for frontend.
-->

## Testing

1. **SSOT References**
   - Tester chat mode: `.github/agents/Tester.agent.md`
   - BDD tests instructions: `.github/instructions/bdd-tests.instructions.md`

2. **Unit/UI Tests (default stack: XUnit.V3 + Shouldly Library unless overridden)**
   - Cover rendering, critical interactions (click, type, submit), and state transitions.
   - Include accessibility assertions (roles/labels/name, focus management, keyboard nav).
   - Assert async states: loading, success, and error paths; handle empty data gracefully.

3. **E2E/UI Flows (optional, if project uses Playwright/Cypress)**
   - Keep scenarios small and stable; tag appropriately (e.g., `@ui`, `@smoke`).
   - Prefer testids sparingly; select by role/name first.

4. **Coverage Policy**
   - Follow central Quality & Coverage Policy in `.github/copilot-instructions.md#quality-policy`.
   - Ensure hot paths and error paths are fully covered (100%).

<!--
SECTION PURPOSE: Keep apps fast and responsive.
PROMPTING: Short, actionable techniques.
-->

## Performance Optimization

1. **Lazy Loading**: Defer large routes and heavy components.
2. **Memoization**: Use AvaloniaUI's `ReactiveObject` and `ReactiveCommand` patterns to avoid unnecessary work.
3. **Code Splitting**: Split at route and major component boundaries.
4. **Minimize Re-renders**: Keep props stable; use selectors and derived memoized data.

<!--
SECTION PURPOSE: Enforce baseline quality gates.
PROMPTING: XML block for machine-checkable rules.
-->

<PROCESS_REQUIREMENTS type="MANDATORY">

- Run lints and tests locally before PR.
- Include accessibility checks (labels, keyboard nav, focus order) in reviews.
- Avoid `dynamic` or `object` types; use proper types with nullable reference types (`?`) or explicit type parameters. If unavoidable, annotate with a TODO and reason.
  </PROCESS_REQUIREMENTS>

<!-- © Capgemini 2026 -->
