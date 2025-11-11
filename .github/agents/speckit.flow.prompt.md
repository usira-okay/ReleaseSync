---
description: Spec-Kit is a structured software development workflow that provides comprehensive phase-by-phase guidance and quality assurance mechanisms from requirements definition through technical planning to implementation.
---

# Spec-Kit Workflow

## Important Instructions

When receiving a slash command (e.g., `/speckit.constitution`, `/speckit.specify`, etc.), you must:
1. Locate the corresponding prompt file as specified in the command mapping below
2. Strictly follow and execute the instructions contained in that prompt file
3. Do not deviate from the guidelines and procedures defined in the prompt

## Core Workflow (Required Phases)

### 1. Constitution Phase
- **Command**: `/speckit.constitution`
- **Prompt**: `speckit.constitution.prompt.md`
- **Purpose**: Establish project governance principles and development guidelines

### 2. Specify Phase
- **Command**: `/speckit.specify`
- **Prompt**: `speckit.specify.prompt.md`
- **Purpose**: Define requirements and user stories (focus on what and why, not technical stack)

### 3. Plan Phase
- **Command**: `/speckit.plan`
- **Prompt**: `speckit.plan.prompt.md`
- **Purpose**: Create technical implementation plan (specify tech stack and architecture choices)

### 4. Tasks Phase
- **Command**: `/speckit.tasks`
- **Prompt**: `speckit.tasks.prompt.md`
- **Purpose**: Break down implementation plan into actionable task list

### 5. Implement Phase
- **Command**: `/speckit.implement`
- **Prompt**: `speckit.implement.prompt.md`
- **Purpose**: Execute all tasks and build features according to plan

## Optional Workflow (Optional Phases)

### Clarify Phase (Recommended before Plan)
- **Command**: `/speckit.clarify`
- **Prompt**: `speckit.clarify.prompt.md`
- **Purpose**: Clarify ambiguous or undefined areas

### Analyze Phase (Recommended after Tasks, before Implement)
- **Command**: `/speckit.analyze`
- **Prompt**: `speckit.analyze.prompt.md`
- **Purpose**: Cross-artifact consistency and coverage analysis

### Checklist Phase (Can be used anytime)
- **Command**: `/speckit.checklist`
- **Prompt**: `speckit.checklist.prompt.md`
- **Purpose**: Generate custom quality checklists to verify requirements completeness, clarity, and consistency

## Recommended Complete Flow Sequence

```
1. /speckit.constitution
2. /speckit.specify
3. /speckit.clarify (optional)
4. /speckit.plan
5. /speckit.tasks
6. /speckit.analyze (optional)
7. /speckit.implement
8. /speckit.checklist (optional, can be used at any phase)
```
