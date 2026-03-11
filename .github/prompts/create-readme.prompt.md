---
agent: 'agent'
description: 'Create a README.md file for the project'
---

# Create README Prompt

Primary objective: produce a concise, high-signal `README.md` that is attractive, accurate, and useful for first-time contributors/users.

## Sources of Truth

- Project standards: `.github/copilot-instructions.md`
- Architecture/details to summarize: `docs/implementation-overview.md`, `docs/implementation-plan.md`

## Required Output

<CRITICAL_REQUIREMENT type="MANDATORY">

- Review the repository/workspace before writing.
- Keep the README concise and practical; avoid emoji overuse.
- Do not include sections that belong in dedicated files (`LICENSE`, `CONTRIBUTING`, `CHANGELOG`, etc.).
- Use GFM formatting.
- Use GitHub admonitions where they improve clarity.
- Include project logo/icon in header if available.
- Include a `Features` section.
- Include a `Getting Started` section with install/run/use basics.
- Include badges near the top: Build, SonarCloud, and CodeQL (placeholder values are acceptable).

</CRITICAL_REQUIREMENT>

<PROCESS_REQUIREMENTS type="MANDATORY">

1. Inspect repository structure and docs for accurate content.
2. Draft a clear section structure before writing.
3. Verify links/paths and command examples are valid for this repository.
4. Keep wording specific to this project and avoid generic filler.

</PROCESS_REQUIREMENTS>

## Suggested Section Order

1. Title + logo
2. GitHub and SonarCloud Badges
3. Short project summary
4. Features
5. Getting Started
6. Architecture (brief, with links)
7. Project Structure (brief)
8. Documentation links