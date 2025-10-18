<!--
SYNC IMPACT REPORT
==================
Version Change: 1.4.0 → 1.5.0
Rationale: MINOR version bump - Material expansion of existing principles to emphasize C#-specific design patterns, strengthen KISS principle integration, enhance DDD tactical patterns, expand CQRS guidance, and add explicit C# code quality standards. These expansions provide significantly more actionable guidance for C# development teams following DDD/CQRS architecture.

Modified Principles:
  - Principle I: Test-First Development → Expanded with C# testing frameworks and patterns
  - Principle II: Code Quality Standards → Enhanced with C# specific conventions and ReSharper/Roslyn analyzer requirements
  - Principle VI: Domain-Driven Design → Significantly expanded tactical patterns with C# implementation guidance
  - Principle VII: Design Patterns & SOLID → Enhanced with C# specific patterns and explicit KISS principle integration
  - Principle VIII: CQRS → Expanded with C# implementation patterns (MediatR, command/query handlers)
  - Principle XII: Performance Standards → Added C# specific performance considerations

Added Sections:
  - Explicit KISS (Keep It Simple, Stupid) principle integration throughout all design sections
  - C# specific testing tools (xUnit, NUnit, MSTest, FluentAssertions)
  - C# async/await performance patterns
  - .NET memory management guidance

Removed Sections: N/A

Templates Status:
  ✅ .specify/templates/plan-template.md - Compatible (no changes required)
  ✅ .specify/templates/spec-template.md - Compatible (no changes required)
  ✅ .specify/templates/tasks-template.md - Compatible (no changes required)
  ✅ .specify/templates/checklist-template.md - Compatible (no changes required)
  ✅ .specify/templates/agent-file-template.md - Compatible (no changes required)

Follow-up TODOs:
  - Review C# code style guide compliance in existing codebase
  - Consider adding Roslyn analyzers to enforce SOLID principles automatically
  - Update code review checklist to include KISS principle validation
  - Add architecture decision records template for DDD bounded context decisions
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
- **C# Testing Frameworks**: Use xUnit (preferred), NUnit, or MSTest consistently across project
- **Assertion Libraries**: FluentAssertions recommended for readable assertions
- **Mocking**: Use Moq, NSubstitute, or FakeItEasy for test doubles
- Tests must be deterministic, isolated, and fast (<100ms per unit test ideal)
- Async tests must properly await and not use `.Result` or `.Wait()` which can cause deadlocks

**Rationale**: Test-first development ensures code is testable by design, reduces defects, provides living documentation, and prevents regression. Skipping tests creates technical debt that compounds over time. In C# development, proper async testing prevents production deadlocks and race conditions.

### II. Code Quality Standards

**All code must meet minimum quality thresholds before merge.**

- Code reviews are mandatory for all changes
- Linting and formatting tools must pass without warnings
  - **C# Specific**: Use .editorconfig for consistent formatting
  - Enable StyleCop, SonarAnalyzer, or Roslyn analyzers in projects
  - Treat warnings as errors in CI builds (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- No commented-out code in production branches
- Clear, descriptive naming for variables, functions, and classes
  - **C# Conventions**: PascalCase for public members, camelCase for private fields, `_camelCase` for private fields (if prefixed)
  - Async methods must end with `Async` suffix
  - Interface names must start with `I` prefix
  - Avoid Hungarian notation and abbreviations
- Functions/methods should be focused on a single responsibility
- Cyclomatic complexity must be justified if exceeding threshold (max 10 recommended, max 15 absolute)
- Documentation required for all public APIs, complex algorithms, and non-obvious business logic
- **KISS Principle**: Prefer simple, obvious solutions over clever or complex ones
  - Avoid premature abstraction
  - Refactor when adding third instance (Rule of Three), not before
  - Question every layer, pattern, or abstraction: "Is this complexity necessary?"
  - Delete unused code immediately—don't comment it out "just in case"

**Rationale**: Quality standards prevent technical debt accumulation, improve maintainability, reduce onboarding time, and minimize bugs in production. KISS principle prevents over-engineering and ensures code remains understandable and maintainable. Complex code is expensive code—it costs more to write, test, debug, and modify.

### III. Test Coverage & Types

**Multiple test layers ensure comprehensive validation.**

- **Contract Tests**: Required for all API endpoints, library interfaces, and service boundaries
  - Verify request/response contracts don't break
  - Use schema validation tools (e.g., JSON Schema, Swagger/OpenAPI)
- **Integration Tests**: Required when components interact, data flows between services, or external systems are involved
  - Test with real database instances (in-memory or containerized via Testcontainers)
  - Verify dependency injection container configuration
  - Test middleware pipeline behavior
- **Unit Tests**: Required for business logic, algorithms, and complex computations
  - Test domain entities, value objects, and domain services in isolation
  - Mock infrastructure dependencies (repositories, external services)
  - Fast execution (<100ms per test), deterministic results
- Coverage targets are directional guides, not goals to game; focus on testing critical paths and edge cases
- Tests must be independent, repeatable, and fast
- Flaky tests must be fixed immediately or removed
- **C# Specific**:
  - Use `IClassFixture<T>` or `ICollectionFixture<T>` for expensive setup (xUnit)
  - Leverage `Theory` and `InlineData` for parameterized tests
  - Use `async Task` for async tests, never `async void`

**Rationale**: Different test types catch different failure modes. Contract tests prevent breaking changes, integration tests validate system behavior, and unit tests ensure algorithmic correctness. Proper async testing in C# prevents deadlocks and ensures thread-safe code.

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
- **C# Specific**:
  - Use `async/await` for I/O-bound operations (never block with `.Result` or `.Wait()`)
  - Use `ValueTask<T>` for hot paths where allocation matters
  - Avoid LINQ in tight loops for large collections (prefer `for`/`foreach` with direct operations)
  - Use `StringBuilder` for string concatenation in loops
  - Leverage `Span<T>` and `Memory<T>` for high-performance buffer operations
  - Profile with BenchmarkDotNet before optimizing
  - Monitor allocations and GC pressure with tools like dotMemory or PerfView

**Rationale**: Performance is a feature. Degraded performance directly impacts user experience, operational costs, and system scalability. C# async/await prevents thread starvation and improves resource utilization in server applications.

### V. Observability & Debugging

**Systems must be instrumentable and debuggable in production.**

- Structured logging required for all significant operations
  - **C# Specific**: Use ILogger<T> with Microsoft.Extensions.Logging
  - Prefer structured logging with named parameters: `logger.LogInformation("Order {OrderId} created for customer {CustomerId}", orderId, customerId)`
  - Use Serilog or NLog for advanced scenarios (sinks, enrichment)
- Log levels used appropriately: ERROR for failures, WARN for degraded state, INFO for key events, DEBUG for diagnostic detail
- Errors must include context: what failed, why it failed, relevant identifiers
  - Log exception objects fully: `logger.LogError(ex, "Failed to process order {OrderId}", orderId)`
- No logging of sensitive data (PII, credentials, tokens)
- Text-based inputs/outputs preferred for CLI tools to enable pipeline debugging
- Metrics instrumentation for critical paths and resource usage
  - **C# Specific**: Use System.Diagnostics.Metrics API for OpenTelemetry compatibility
  - Instrument custom metrics: counters, histograms, gauges
- Distributed tracing for multi-service operations
  - Use Activity API with W3C Trace Context propagation
  - Integrate with OpenTelemetry or Application Insights

**Rationale**: Production issues are inevitable. Observability enables rapid diagnosis and resolution, reducing MTTR and customer impact. Structured logging in C# enables correlation across distributed systems and efficient log querying.

### VI. Domain-Driven Design (DDD)

**Architecture must align with business domain and use ubiquitous language.**

- **Bounded Contexts**: Identify and document explicit boundaries where domain models apply
  - Each bounded context has its own domain model, database schema (if applicable), and ubiquitous language
  - Context boundaries prevent model contamination and enable independent evolution
  - Document context maps showing relationships (Shared Kernel, Customer-Supplier, Conformist, Anti-Corruption Layer)
- **Ubiquitous Language**: Use domain terminology consistently in code, documentation, and communication
  - Class, method, and variable names must reflect domain concepts
  - Avoid technical jargon in domain layer (no "Manager", "Helper", "Processor" suffixes)
  - Domain experts should understand code structure without technical translation
- **Tactical Patterns** (apply appropriately, following KISS—only when complexity justifies):
  - **Entities**: Objects with unique identity that persists over time
    - **C# Implementation**: Override `Equals()` and `GetHashCode()` based on identity
    - Encapsulate state changes with methods (no public setters for domain invariants)
    - Example: `Order`, `Customer`, `Product` (entities with `Id` property)
  - **Value Objects**: Immutable objects defined by their attributes, no identity
    - **C# Implementation**: Use `record` types (C# 9+) for immutability: `public record Address(string Street, string City, string ZipCode);`
    - Override equality based on all properties
    - No public setters—create new instance for changes
    - Example: `Money`, `Address`, `DateRange`
  - **Aggregates**: Clusters of entities and value objects with consistency boundaries
    - Enforce business invariants within aggregate boundary
    - Modifications must go through aggregate root
    - **C# Implementation**: Aggregate root exposes public methods, child entities are private or internal
    - Keep aggregates small to avoid performance and transaction issues
  - **Aggregate Roots**: Single entry point for accessing and modifying aggregates
    - Only aggregate roots have repositories
    - Child entities cannot be accessed directly from outside aggregate
    - **C# Implementation**: Make child entity constructors internal, expose via aggregate root methods
    - Example: `Order` (root) contains `OrderLines` (child entities)
  - **Repositories**: Abstract data access for aggregates (not individual entities)
    - **C# Interface**: `IRepository<TAggregate>` or specific interfaces like `IOrderRepository`
    - Return domain entities, not DTOs or database models
    - Queries return aggregates in valid state (always enforce invariants)
  - **Domain Services**: Stateless operations that don't belong to entities
    - **When to use**: Operations involving multiple aggregates or external domain concepts
    - **C# Implementation**: Stateless service classes with dependencies injected via constructor
    - Example: `PricingService`, `InventoryAllocationService`
  - **Domain Events**: Capture significant business occurrences for decoupling
    - **C# Implementation**: Immutable record types: `public record OrderPlaced(Guid OrderId, DateTime OccurredAt);`
    - Publish after aggregate state changes committed
    - Use MediatR or similar for in-process event handling
    - Consider outbox pattern for guaranteed delivery across bounded contexts
- **Strategic Patterns**: Use context mapping to define relationships between bounded contexts
  - Shared Kernel: Shared domain model subset (use sparingly)
  - Customer-Supplier: Downstream depends on upstream
  - Conformist: Downstream conforms to upstream model
  - Anti-Corruption Layer: Translate external models to internal domain model
- Anti-corruption layers required when integrating with external systems or legacy code
  - Prevent external models from leaking into domain layer
  - **C# Implementation**: Adapter pattern with explicit translation layer
- Business logic MUST reside in domain layer, not in application or infrastructure layers
  - Application layer orchestrates use cases, calls domain logic
  - Infrastructure layer handles technical concerns (database, messaging, external APIs)
  - **No business logic in controllers, repositories, or infrastructure code**

**Rationale**: DDD aligns software structure with business needs, improves communication between technical and domain experts, reduces coupling, and makes systems more maintainable as business requirements evolve. Proper boundaries prevent model contamination and ensure each context can evolve independently. C# record types and modern language features make value object implementation concise and safe.

### VII. Design Patterns & SOLID Principles

**Use proven design patterns and follow SOLID principles for maintainable code. Always apply KISS—prefer simplicity over pattern sophistication.**

- **SOLID Principles** (mandatory):
  - **Single Responsibility**: Each class/module has one reason to change
    - **C# Example**: Separate `OrderValidator` from `OrderRepository` from `OrderNotificationService`
    - Avoid "God classes" with multiple responsibilities
  - **Open/Closed**: Open for extension, closed for modification
    - **C# Example**: Use abstract base classes or interfaces for extension points
    - Strategy pattern for varying algorithms
  - **Liskov Substitution**: Subtypes must be substitutable for base types
    - **C# Example**: Derived classes honor base class contracts (preconditions, postconditions, invariants)
    - Avoid throwing `NotImplementedException` in overrides
  - **Interface Segregation**: Clients should not depend on interfaces they don't use
    - **C# Example**: Split large interfaces into smaller, role-specific ones
    - Prefer `IReadOnlyRepository<T>` vs. `IRepository<T>` when only querying
  - **Dependency Inversion**: Depend on abstractions, not concretions
    - **C# Example**: Constructor injection of interfaces, not concrete classes
    - Use .NET Dependency Injection container for lifetime management
- **KISS Principle Integration**:
  - **Question every pattern**: "Is this abstraction earning its complexity cost?"
  - **Prefer direct solutions**: Simple if/else often beats Strategy pattern for 2-3 cases
  - **Avoid speculative generality**: Don't add abstraction for hypothetical future needs
  - **Rule of Three**: Refactor to pattern on third duplication, not first
  - **Delete over abstract**: If code isn't needed, delete it—don't make it "reusable"
- **C# Design Patterns** (use when appropriate, following KISS):
  - **Creational**:
    - **Factory**: Create objects without specifying exact class (`OrderFactory.Create()`)
    - **Builder**: Construct complex objects step by step (`new OrderBuilder().WithCustomer().WithItems().Build()`)
    - **Singleton**: Use sparingly—prefer DI container with `AddSingleton<T>()` for lifetime management
  - **Structural**:
    - **Adapter**: Convert interface to another interface (e.g., wrapping external APIs)
    - **Decorator**: Add behavior without modifying original class (e.g., caching, logging decorators)
    - **Facade**: Simplify complex subsystem with unified interface
  - **Behavioral**:
    - **Strategy**: Select algorithm at runtime (interface with multiple implementations)
    - **Observer**: Event-driven notifications (C# events, delegates, or MediatR notifications)
    - **Command**: Encapsulate request as object (CQRS commands with MediatR)
    - **Template Method**: Define skeleton in base class, override specific steps
  - **Architectural**:
    - **Repository**: Abstract data access for domain aggregates
    - **Unit of Work**: Group database operations into single transaction (DbContext in EF Core acts as UoW)
    - **Specification**: Encapsulate query logic as reusable objects
    - **Dependency Injection**: Invert control for loose coupling (built into .NET)
- Pattern usage must be justified—never use patterns for their own sake
- Prefer composition over inheritance
  - **C# Example**: Use interfaces and composition instead of deep inheritance hierarchies
  - Favor `has-a` relationships over `is-a` when possible
- Favor immutability where practical
  - Use `record` types for DTOs and value objects
  - Use `readonly` fields and properties where possible
  - Use `init` accessors for immutable property initialization
- Document pattern choices in code comments or architecture decision records

**Rationale**: SOLID principles reduce coupling and increase cohesion. Design patterns provide proven solutions to recurring problems, improve code readability for experienced developers, and facilitate maintenance. However, over-engineering with unnecessary patterns creates complexity without benefit—KISS principle ensures patterns serve actual needs, not theoretical elegance. In C#, modern language features (records, pattern matching, interfaces) make implementing patterns more concise and safe.

### VIII. Command Query Responsibility Segregation (CQRS)

**Separate read and write operations for clarity and scalability.**

- **Commands**: Operations that change state must:
  - Have clear intent (verbs: `CreateOrderCommand`, `UpdateInventoryCommand`, `CancelSubscriptionCommand`)
  - Validate business rules before execution
  - Return success/failure status, not data (except identifiers like created entity ID)
  - Emit domain events for side effects
  - Be idempotent where possible
  - **C# Implementation with MediatR**:
    ```csharp
    public record CreateOrderCommand(Guid CustomerId, List<OrderLineDto> Lines) : IRequest<Result<Guid>>;

    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
    {
        private readonly IOrderRepository _repository;
        private readonly IMediator _mediator;

        public async Task<Result<Guid>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
        {
            // Validate business rules
            // Create aggregate
            // Save via repository
            // Publish domain event
            await _mediator.Publish(new OrderCreatedEvent(orderId), cancellationToken);
            return Result.Success(orderId);
        }
    }
    ```
  - **Validation**: Use FluentValidation or Data Annotations for input validation before handler execution
  - **Error Handling**: Return `Result<T>` or similar pattern instead of throwing exceptions for expected failures
- **Queries**: Operations that read data must:
  - Not modify state (no side effects)
  - Be optimized for read performance
  - Return DTOs or view models, not domain entities
    - **C# Example**: `public record OrderSummaryDto(Guid OrderId, string CustomerName, decimal TotalAmount);`
  - Support required projections and filters
  - **C# Implementation with MediatR**:
    ```csharp
    public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDetailDto>;

    public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDetailDto>
    {
        private readonly IDbConnection _dbConnection; // Direct Dapper query for performance

        public async Task<OrderDetailDto> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
        {
            // Execute optimized SQL query
            // Map to DTO
            return orderDto;
        }
    }
    ```
- Read and write models may differ based on performance and complexity requirements
  - Write model enforces invariants (rich domain model with aggregates)
  - Read model optimized for queries (denormalized views, projections)
- Command handlers encapsulate business logic and coordinate domain operations
  - Load aggregate from repository
  - Call domain methods (not public setters)
  - Save aggregate
  - Publish domain events
- Query handlers may bypass domain layer for performance (direct database access acceptable)
  - Use Dapper, EF Core raw SQL, or optimized LINQ for queries
  - No need to load full aggregates for read-only views
- CQRS does not require event sourcing (use event sourcing only when justified)
- Document consistency guarantees (eventual vs. strong) for each operation
- **C# Infrastructure**:
  - Use MediatR for command/query dispatching
  - Use pipeline behaviors for cross-cutting concerns (logging, validation, transactions)
  - Register handlers in DI container automatically via assembly scanning

**Rationale**: CQRS separates concerns, enables independent scaling of reads and writes, simplifies complex domains by removing query logic from command handlers, and allows optimization of each path independently. Clear separation prevents accidental state modifications during queries and makes intent explicit. In C#, MediatR provides clean CQRS infrastructure with minimal boilerplate.

### IX. Program.cs Organization (Clean Entry Point)

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
  - **C# Example**:
    ```csharp
    // Program.cs
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddDatabaseServices(builder.Configuration);
    builder.Services.AddDomainServices();
    builder.Services.AddApplicationServices();

    // Infrastructure/DependencyInjection/DatabaseServiceExtensions.cs
    public static class DatabaseServiceExtensions
    {
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("Default")));
            services.AddScoped<IOrderRepository, OrderRepository>();
            return services;
        }
    }
    ```
- Keep Program.cs readable at a glance—each service registration should be one line or a clearly named extension method
- Configuration values should come from configuration sources, not hardcoded in Program.cs
- **KISS Principle**: Don't extract to extension methods prematurely
  - If configuration is 2-3 lines, keep it inline in Program.cs
  - Extract when complexity exceeds ~10 lines or when grouping related services

**Rationale**: A clean Program.cs makes application structure immediately obvious, simplifies testing (configuration can be tested separately), reduces merge conflicts in team environments, and follows the Single Responsibility Principle. Complex logic in Program.cs creates a maintenance bottleneck and obscures the application's architecture.

### X. XML Documentation Comments (API Documentation)

**All public APIs must have XML documentation comments (summary tags).**

- XML summary comments (`/// <summary>`) MUST be present for:
  - All public classes, interfaces, and structs
  - All public methods, properties, and events
  - All public constructors
  - All public fields (though public fields should be rare—prefer properties)
- XML comments SHOULD include:
  - `<param>` tags for all method parameters explaining their purpose
  - `<returns>` tags for methods with return values explaining what is returned
  - `<exception>` tags for documented exceptions the method may throw
  - `<remarks>` tags for additional context, usage examples, or important notes
  - `<example>` tags for complex APIs showing typical usage
  - **C# Specific**: Use `<see cref="TypeName"/>` for cross-references to types/members
- XML comments MUST:
  - Be written in Traditional Chinese for business domain concepts
  - May use English for purely technical APIs where standard terminology applies
  - Be complete sentences with proper punctuation
  - Describe what the code does, not how it does it (implementation details belong in inline comments)
  - Be updated when the code changes
- Internal and private members MAY have XML comments for complex cases but inline comments are often more appropriate
- **C# Code Quality**: Enable `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in .csproj to treat missing docs as warnings

**Rationale**: XML documentation enables IntelliSense support, generates API documentation automatically, improves developer experience when consuming APIs, serves as a contract for public interfaces, and reduces the need to read implementation code to understand usage. Missing documentation increases cognitive load and slows down development.

### XI. Inline Comments (Code Clarity)

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
- **KISS Principle Applied to Comments**:
  - If code needs extensive comments to be understood, it's probably too complex—refactor first
  - Good naming eliminates most comments: `CalculateMonthlyInterest()` needs no comment explaining what it does
  - Delete obsolete comments immediately—stale comments mislead more than they help

**Rationale**: Inline comments explain the intent behind code when the code itself cannot. They preserve knowledge about business rules, design decisions, and non-obvious constraints that would otherwise be lost. Good comments reduce onboarding time, prevent bugs from misunderstanding, and serve as a second channel of documentation complementing the code. However, excessive or redundant comments create noise and maintenance burden.

### XII. Performance Standards

**Systems must meet defined performance standards.**

- **Response Time Requirements**:
  - User-facing operations: <200ms p95 latency target (adjust based on domain requirements)
  - Batch operations: Must provide progress feedback for operations >5 seconds
  - API endpoints: Define SLOs per endpoint criticality
  - Background jobs: Must not starve foreground operations
- **Resource Constraints**:
  - Memory usage must be bounded and predictable
  - No unbounded collections or memory leaks
  - Resource cleanup in error paths (files, connections, handles)
    - **C# Specific**: Use `using` statements or `IDisposable` pattern for deterministic cleanup
    - Leverage `IAsyncDisposable` for async resource cleanup
  - Graceful degradation under resource pressure
- **Scalability**:
  - Systems must handle 10x current load without architecture changes (adjust multiplier based on growth expectations)
  - Horizontal scaling preferred over vertical where applicable
  - Database queries must be indexed for production data volumes
  - N+1 query patterns must be eliminated
    - **C# / EF Core**: Use `.Include()` for eager loading or explicit loading to avoid N+1
    - Monitor with logging or tools like MiniProfiler
- **C# Specific Performance**:
  - **Async I/O**: Use `async/await` for database, HTTP, file I/O operations
    - Never block with `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` in async contexts
    - Use `ConfigureAwait(false)` in library code to avoid unnecessary context capture
  - **Memory Allocation**: Minimize allocations in hot paths
    - Use `Span<T>`, `Memory<T>`, `ArrayPool<T>` for buffer operations
    - Avoid LINQ in tight loops for large collections
    - Use `StringBuilder` for string concatenation in loops
  - **GC Pressure**: Monitor Gen 2 collections and Large Object Heap fragmentation
  - **Benchmarking**: Use BenchmarkDotNet for micro-benchmarks before optimization

**Rationale**: Performance is a feature. Degraded performance directly impacts user experience, operational costs, and system scalability. Defined standards prevent performance regression and ensure system reliability under load. In C#, proper async patterns prevent thread pool exhaustion, and memory-efficient coding reduces GC pauses.

---

## 📋 XIII. Documentation Language Standards ⭐

> **CRITICAL**: This principle governs all project communication and documentation.

**All specifications, plans, and user-facing documentation MUST be written in Traditional Chinese (繁體中文, zh-TW).**

### Required Traditional Chinese Usage

- ✅ **Feature specifications** (spec.md files) - MUST use Traditional Chinese
- ✅ **Implementation plans** (plan.md files) - MUST use Traditional Chinese
- ✅ **User-facing documentation** (README, quickstart guides, API documentation) - MUST use Traditional Chinese
- ✅ **Code comments explaining business logic or domain concepts** - SHOULD use Traditional Chinese
- ✅ **Commit messages** - SHOULD use Traditional Chinese for consistency
- ✅ **Pull request descriptions** - SHOULD use Traditional Chinese
- ✅ **Issue discussions** - SHOULD use Traditional Chinese

### Permitted English Usage

- ✅ **Technical artifacts** - May use English where industry-standard terminology is clearer (e.g., API endpoint names, code identifiers, variable names)
- ✅ **Internal technical documentation** - May use English if team agrees (architecture decision records, low-level implementation notes)
- ✅ **International collaboration** - English permitted for external contributors or cross-border teams
- ✅ **Code identifiers** - Use English for class names, method names, variable names following C# conventions

### Enforcement & Quality

- Code reviews MUST verify documentation language compliance
- Public API documentation without Traditional Chinese summaries will be REJECTED
- Specifications submitted in other languages require translation before approval
- Mixed-language documentation (except for technical terms) is NOT permitted

### Examples

**✅ Correct**:
```csharp
/// <summary>
/// 建立新的訂單並驗證庫存可用性
/// Creates a new order and validates inventory availability
/// </summary>
/// <param name="customerId">客戶識別碼</param>
/// <returns>已建立的訂單實體</returns>
public Order CreateOrder(string customerId) { ... }
```

**❌ Incorrect**:
```csharp
/// <summary>
/// Creates a new order and validates inventory availability
/// </summary>
public Order CreateOrder(string customerId) { ... }
```

**Rationale**: Consistent language in user-facing artifacts ensures accessibility for the primary user base (Traditional Chinese speakers in Taiwan, Hong Kong, Macau), reduces translation overhead, prevents miscommunication, and maintains cultural alignment with the target audience. Traditional Chinese is the standard for this project's market and stakeholders. Clear language policy eliminates ambiguity about documentation expectations and ensures all team members can access and contribute to project knowledge.

---

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
  - **KISS principle followed** (no unnecessary complexity or premature abstraction)
  - Program.cs remains clean (service registration only)
  - XML documentation present for public APIs
  - Inline comments explain non-obvious logic appropriately
  - **C# specific**: Async methods use `async/await` properly, no blocking calls
  - **C# specific**: Dispose pattern implemented for resources (IDisposable)
  - **Documentation Language Standards compliance (Traditional Chinese for specs/plans/docs)**
- Self-review checklist completed before requesting review
- Review comments addressed or discussed before merge

### Testing Gates

- All tests must pass before merge
- No bypassing test failures without explicit approval and remediation plan
- Integration tests run in CI/CD pipeline
- Performance tests run for designated high-impact changes
- **C# Specific**: Code coverage reports generated (but not used as sole quality metric)

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
   - **MINOR**: New principles added, material reorganization, or expansion of existing guidance
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
- **C# Specific**: Enable Roslyn analyzers and StyleCop to automatically enforce code quality standards
- **KISS Principle Reviews**: Periodically review codebase for unnecessary abstractions and delete dead code
- **Documentation language verification required in pre-merge checklist**

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
- **KISS principle exceptions**: Complexity justified by measurable benefits (performance, maintainability, extensibility) with evidence
- **Documentation Language Standards exceptions only for international collaboration or external libraries**

**Version**: 1.5.0 | **Ratified**: 2025-10-18 | **Last Amended**: 2025-10-18
