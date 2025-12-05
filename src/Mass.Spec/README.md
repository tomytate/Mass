# Mass.Spec

**The Single Source of Truth for Mass Suite.**

This library contains the immutable contracts, DTOs, and schemas used across the entire Mass ecosystem (Core, ProUSB, ProPXEServer, CLI, Launcher).

## Rules
1.  **No Logic:** This library must contain ONLY POCOs (Plain Old CLR Objects), Enums, and Exceptions.
2.  **No Dependencies:** Do not add references to other projects. Keep external dependencies to an absolute minimum (e.g., serialization attributes only if strictly necessary).
3.  **Do Not Duplicate:** If a type exists here, do NOT create a copy in another project. Reference `Mass.Spec` instead.
4.  **Versioning:** Breaking changes to these contracts require a MAJOR version bump of the entire suite.

## Structure
- `Contracts/`: Data Transfer Objects and Schemas.
- `Exceptions/`: Standardized exception types.
