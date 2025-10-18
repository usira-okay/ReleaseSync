<!--
SYNC IMPACT REPORT
==================
Version Change: 1.2.0 → 1.3.0
Rationale: MINOR version bump - Added three new code organization and documentation principles (X, XI, XII) specific to .NET/C# projects. This is a material expansion of governance scope that introduces new documentation and code organization constraints without breaking existing principles.

Modified Principles: N/A

Added Sections:
  - Principle X: Program.cs Organization (Clean Entry Point)
  - Principle XI: XML Documentation Comments (API Documentation)
  - Principle XII: Inline Comments (Code Clarity)

Removed Sections: N/A

Templates Status:
  ✅ .specify/templates/plan-template.md - Compatible (new principles apply to .NET projects)
  ✅ .specify/templates/spec-template.md - Compatible (documentation principles support clearer specs)
  ✅ .specify/templates/tasks-template.md - Compatible (new principles may add documentation tasks)
  ✅ .specify/templates/checklist-template.md - Compatible (may add documentation verification items)
  ✅ .specify/templates/agent-file-template.md - Compatible (no changes required)

Follow-up TODOs:
  - Update plan-template.md "Constitution Check" section to include Program.cs organization, XML comments, and inline comment verification
  - Update code review checklist to verify XML documentation on public APIs
  - Document inline comment guidelines (when to use, what to explain)
  - Establish Program.cs organization standards (what belongs in Program.cs vs. separate configuration classes)
  - Update coding standards documentation with XML comment examples and inline comment best practices
  - Consider adding automated tooling to verify XML documentation coverage (e.g., treat missing docs as warnings/errors)
-->

# ReleaseSync Constitution

## Core Principles

### I. Test-First Development (NON-NEGOTIABLE)

**Tests must be written before implementation code.**

- All features begin with failing tests that define expected behavior
- Tests must fail initially to prove they are testing the right thing
- Implementation proceeds only after test approval
- Red-Green-Refactor cycle is strictly enforced: Write failing test → Implement minimal code to pass → Refactor for quality
- No production code without corresponding tests (unit, integration, or contract tests as appropriate)

**Rationale**: Test-first development ensures code is testable by design, reduces defects, provides living documentation, and prevents regression. Skipping tests creates technical debt that compounds over time.

### II. Code Quality Standards

**All code must meet minimum quality thresholds before merge.**

- Code reviews are mandatory for all changes
- Linting and formatting tools must pass without warnings
- No commented-out code in production branches
- Clear, descriptive naming for variables, functions, and classes
- Functions/methods should be focused on a single responsibility
- Cyclomatic complexity must be justified if exceeding reasonable thresholds (e.g., >10 for most languages)
- Documentation required for all public APIs, complex algorithms, and non-obvious business logic

**Rationale**: Quality standards prevent technical debt accumulation, improve maintainability, reduce onboarding time, and minimize bugs in production.

### III. Test Coverage & Types

**Multiple test layers ensure comprehensive validation.**

- **Contract Tests**: Required for all API endpoints, library interfaces, and service boundaries
- **Integration Tests**: Required when components interact, data flows between services, or external systems are involved
- **Unit Tests**: Required for business logic, algorithms, and complex computations
- Coverage targets are directional guides, not goals to game; focus on testing critical paths and edge cases
- Tests must be independent, repeatable, and fast
- Flaky tests must be fixed immediately or removed

**Rationale**: Different test types catch different failure modes. Contract tests prevent breaking changes, integration tests validate system behavior, and unit tests ensure algorithmic correctness.

### IV. Performance Requirements

**Systems must meet defined performance standards.**

- Performance budgets must be defined for each feature based on user impact
- Baseline performance metrics established before optimization
- No premature optimization—measure before optimizing
- Performance testing required for:
  - High-traffic endpoints (define threshold per project context)
  - Data-intensive operations
  - User-facing interactions with latency requirements
  - Resource-constrained environments
- Performance regressions treated as bugs and blocked from merge

**Rationale**: Performance is a feature. Degraded performance directly impacts user experience, operational costs, and system scalability.

### V. Observability & Debugging

**Systems must be instrumentable and debuggable in production.**

- Structured logging required for all significant operations
- Log levels used appropriately: ERROR for failures, WARN for degraded state, INFO for key events, DEBUG for diagnostic detail
- Errors must include context: what failed, why it failed, relevant identifiers
- No logging of sensitive data (PII, credentials, tokens)
- Text-based inputs/outputs preferred for CLI tools to enable pipeline debugging
- Metrics instrumentation for critical paths and resource usage
- Distributed tracing for multi-service operations

**Rationale**: Production issues are inevitable. Observability enables rapid diagnosis and resolution, reducing MTTR and customer impact.

### VI. Documentation Language Standards

**All specifications, plans, and user-facing documentation MUST be written in Traditional Chinese (zh-TW).**

- Feature specifications (spec.md files) must use Traditional Chinese
- Implementation plans (plan.md files) must use Traditional Chinese
- User-facing documentation (README, quickstart guides, API documentation) must use Traditional Chinese
- Code comments explaining business logic or domain concepts should use Traditional Chinese
- Technical artifacts may use English where industry-standard terminology is clearer (e.g., API endpoint names, code identifiers)
- Commit messages, pull request descriptions, and issue discussions may use English for international collaboration
- Exception: Internal technical documentation, architecture decision records, and code-level documentation may use English if the team agrees

**Rationale**: Consistent language in user-facing artifacts ensures accessibility for the primary user base, reduces translation overhead, prevents miscommunication, and maintains cultural alignment with the target audience. Traditional Chinese is the standard for this project's market and stakeholders.

### VII. Domain-Driven Design (DDD)

**Architecture must align with business domain and use ubiquitous language.**

- **Bounded Contexts**: Identify and document explicit boundaries where domain models apply
- **Ubiquitous Language**: Use domain terminology consistently in code, documentation, and communication
- **Tactical Patterns**: Apply DDD building blocks appropriately:
  - **Entities**: Objects with unique identity that persists over time
  - **Value Objects**: Immutable objects defined by their attributes, no identity
  - **Aggregates**: Clusters of entities and value objects with consistency boundaries
  - **Aggregate Roots**: Single entry point for accessing and modifying aggregates
  - **Repositories**: Abstract data access for aggregates (not individual entities)
  - **Domain Services**: Stateless operations that don't belong to entities
  - **Domain Events**: Capture significant business occurrences for decoupling
- **Strategic Patterns**: Use context mapping to define relationships between bounded contexts
- Anti-corruption layers required when integrating with external systems or legacy code
- Business logic MUST reside in domain layer, not in application or infrastructure layers

**Rationale**: DDD aligns software structure with business needs, improves communication between technical and domain experts, reduces coupling, and makes systems more maintainable as business requirements evolve. Proper boundaries prevent model contamination and ensure each context can evolve independently.

### VIII. Design Patterns & SOLID Principles

**Use proven design patterns and follow SOLID principles for maintainable code.**

- **SOLID Principles** (mandatory):
  - **Single Responsibility**: Each class/module has one reason to change
  - **Open/Closed**: Open for extension, closed for modification
  - **Liskov Substitution**: Subtypes must be substitutable for base types
  - **Interface Segregation**: Clients should not depend on interfaces they don't use
  - **Dependency Inversion**: Depend on abstractions, not concretions
- **Approved Patterns** (use when appropriate):
  - **Creational**: Factory, Abstract Factory, Builder, Singleton (use sparingly)
  - **Structural**: Adapter, Decorator, Facade, Composite
  - **Behavioral**: Strategy, Observer, Command, Template Method, Chain of Responsibility
  - **Architectural**: Repository, Unit of Work, Specification, Dependency Injection
- Pattern usage must be justified—never use patterns for their own sake
- Prefer composition over inheritance
- Favor immutability where practical
- Document pattern choices in code comments or architecture decision records

**Rationale**: SOLID principles reduce coupling and increase cohesion. Design patterns provide proven solutions to recurring problems, improve code readability for experienced developers, and facilitate maintenance. However, over-engineering with unnecessary patterns creates complexity without benefit.

### IX. Command Query Responsibility Segregation (CQRS)

**Separate read and write operations for clarity and scalability.**

- **Commands**: Operations that change state must:
  - Have clear intent (verbs: CreateOrder, UpdateInventory, CancelSubscription)
  - Validate business rules before execution
  - Return success/failure status, not data (except identifiers)
  - Emit domain events for side effects
  - Be idempotent where possible
- **Queries**: Operations that read data must:
  - Not modify state (no side effects)
  - Be optimized for read performance
  - Return DTOs or view models, not domain entities
  - Support required projections and filters
- Read and write models may differ based on performance and complexity requirements
- Command handlers encapsulate business logic and coordinate domain operations
- Query handlers may bypass domain layer for performance (direct database access acceptable)
- CQRS does not require event sourcing (use event sourcing only when justified)
- Document consistency guarantees (eventual vs. strong) for each operation

**Rationale**: CQRS separates concerns, enables independent scaling of reads and writes, simplifies complex domains by removing query logic from command handlers, and allows optimization of each path independently. Clear separation prevents accidental state modifications during queries and makes intent explicit.

### X. Program.cs Organization (Clean Entry Point)

**Program.cs (or application entry points) must remain clean and focused solely on service registration and application bootstrapping.**

- Program.cs MUST contain ONLY:
  - Service registration and dependency injection configuration
  - Middleware pipeline configuration
  - Application startup/shutdown logic
  - Host builder configuration
- Program.cs MUST NOT contain:
  - Business logic implementation
  - Complex configuration logic (extract to configuration classes)
  - Route handlers or endpoint implementations (use controllers/minimal APIs)
  - Data access or database queries
  - Complex conditional logic (extract to extension methods or configuration classes)
- Extract complex configuration to separate extension methods (e.g., `AddDatabaseServices()`, `AddAuthenticationServices()`)
- Keep Program.cs readable at a glance—each service registration should be one line or a clearly named extension method
- Configuration values should come from configuration sources, not hardcoded in Program.cs

**Rationale**: A clean Program.cs makes application structure immediately obvious, simplifies testing (configuration can be tested separately), reduces merge conflicts in team environments, and follows the Single Responsibility Principle. Complex logic in Program.cs creates a maintenance bottleneck and obscures the application's architecture.

### XI. XML Documentation Comments (API Documentation)

**All public APIs must have XML documentation comments (summary tags).**

- XML summary comments (`/// <summary>`) MUST be present for:
  - All public classes, interfaces, and structs
  - All public methods, properties, and events
  - All public constructors
  - All public fields (though public fields should be rare)
- XML comments SHOULD include:
  - `<param>` tags for all method parameters explaining their purpose
  - `<returns>` tags for methods with return values explaining what is returned
  - `<exception>` tags for documented exceptions the method may throw
  - `<remarks>` tags for additional context, usage examples, or important notes
  - `<example>` tags for complex APIs showing typical usage
- XML comments MUST:
  - Be written in Traditional Chinese for business domain concepts
  - May use English for purely technical APIs where standard terminology applies
  - Be complete sentences with proper punctuation
  - Describe what the code does, not how it does it (implementation details belong in inline comments)
  - Be updated when the code changes
- Internal and private members MAY have XML comments for complex cases but inline comments are often more appropriate

**Rationale**: XML documentation enables IntelliSense support, generates API documentation automatically, improves developer experience when consuming APIs, serves as a contract for public interfaces, and reduces the need to read implementation code to understand usage. Missing documentation increases cognitive load and slows down development.

### XII. Inline Comments (Code Clarity)

**Add inline comments to explain non-obvious code, business rules, and design decisions.**

- Inline comments (`//`) SHOULD be used for:
  - Explaining WHY code does something (not what it does—code should be self-explanatory)
  - Documenting business rules or domain logic that isn't obvious from code alone
  - Clarifying complex algorithms or non-trivial logic
  - Explaining workarounds or temporary solutions (with TODO/FIXME tags)
  - Documenting assumptions or preconditions
  - Highlighting important constraints or invariants
- Inline comments MUST:
  - Be written in Traditional Chinese for business logic and domain concepts
  - May use English for technical implementation details
  - Be concise and to the point
  - Be updated or removed when code changes
  - Add value—delete comments that merely repeat what the code says
- Inline comments SHOULD NOT:
  - Explain what well-named variables, methods, or classes already make clear
  - Apologize for bad code—refactor instead
  - Be used as version control (use Git for history)
  - Replace refactoring—if code needs extensive comments to be understood, consider simplifying it
- Use TODO, FIXME, HACK, or NOTE prefixes to categorize special comments:
  - `// TODO: <description>` - planned improvements
  - `// FIXME: <description>` - known issues needing fixes
  - `// HACK: <description>` - non-ideal solutions requiring future work
  - `// NOTE: <description>` - important information for maintainers

**Rationale**: Inline comments explain the intent behind code when the code itself cannot. They preserve knowledge about business rules, design decisions, and non-obvious constraints that would otherwise be lost. Good comments reduce onboarding time, prevent bugs from misunderstanding, and serve as a second channel of documentation complementing the code. However, excessive or redundant comments create noise and maintenance burden.

## Performance Standards

### Response Time Requirements

- **User-facing operations**: <200ms p95 latency target (adjust based on domain requirements)
- **Batch operations**: Must provide progress feedback for operations >5 seconds
- **API endpoints**: Define SLOs per endpoint criticality
- **Background jobs**: Must not starve foreground operations

### Resource Constraints

- Memory usage must be bounded and predictable
- No unbounded collections or memory leaks
- Resource cleanup in error paths (files, connections, handles)
- Graceful degradation under resource pressure

### Scalability

- Systems must handle 10x current load without architecture changes (adjust multiplier based on growth expectations)
- Horizontal scaling preferred over vertical where applicable
- Database queries must be indexed for production data volumes
- N+1 query patterns must be eliminated

## Development Workflow

### Code Review Process

- All changes require approval from at least one peer reviewer
- Reviewers must verify:
  - Tests exist and fail before implementation (for new features)
  - Tests pass after implementation
  - Code meets quality standards
  - Performance implications considered
  - Documentation updated
  - No security vulnerabilities introduced
  - DDD patterns applied correctly (bounded contexts, aggregates, value objects)
  - SOLID principles followed
  - CQRS separation maintained (commands vs. queries)
  - Program.cs remains clean (service registration only)
  - XML documentation present for public APIs
  - Inline comments explain non-obvious logic appropriately
- Self-review checklist completed before requesting review
- Review comments addressed or discussed before merge

### Testing Gates

- All tests must pass before merge
- No bypassing test failures without explicit approval and remediation plan
- Integration tests run in CI/CD pipeline
- Performance tests run for designated high-impact changes

### Deployment Standards

- Deployments must be reversible (rollback capability)
- Database migrations must be backward-compatible for zero-downtime deployments
- Feature flags used for high-risk changes
- Deployment runbooks maintained for non-trivial releases
- Monitoring alerts configured before feature activation

## Governance

This constitution supersedes all other development practices and guidelines. All team members are expected to follow these principles.

### Amendment Process

1. Proposed amendments must be documented with rationale
2. Impact analysis required for changes affecting existing workflows
3. Team consensus required for principle changes
4. Migration plan required for breaking changes to existing practices
5. Version number incremented according to semantic versioning:
   - **MAJOR**: Backward-incompatible principle removals or redefinitions
   - **MINOR**: New principles added or material expansion of existing guidance
   - **PATCH**: Clarifications, wording improvements, non-semantic refinements

### Compliance & Enforcement

- All pull requests must demonstrate constitutional compliance
- Complexity violations require explicit justification and approval
- Regular constitution reviews (quarterly recommended) to ensure principles remain relevant
- Retrospectives should reference constitutional principles when identifying process improvements
- New team members must review and acknowledge constitution during onboarding
- Architecture reviews required for features introducing new bounded contexts or major pattern changes
- Code review tools should enforce XML documentation requirements (treat missing docs as warnings)
- Linting tools should flag Program.cs files containing business logic or complex implementations

### Exceptions

- Exceptions to constitutional principles require:
  - Written justification with specific rationale
  - Time-bound exception period
  - Remediation plan with timeline
  - Approval from technical lead or equivalent authority
- Emergency production fixes may bypass non-critical requirements but must be addressed in follow-up work
- DDD patterns may be relaxed for simple CRUD operations with explicit approval
- XML documentation may be deferred for internal utilities or prototypes but MUST be added before production release
- Program.cs complexity exceptions may be granted for framework limitations but should be revisited when framework evolves

**Version**: 1.3.0 | **Ratified**: 2025-10-18 | **Last Amended**: 2025-10-18
