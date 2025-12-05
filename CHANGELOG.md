# Changelog

All notable changes to this project will be documented in this file.

## [1.0.0] - 2025-12-05

### 🚀 Major Features
- **Unified Architecture**: Consolidated `ProUSB` and `ProPXEServer` into the **Mass Suite** ecosystem.
- **Mass.Spec**: Introduced a central contract library (`Mass.Spec`) for shared DTOs, interfaces, and error models.
- **Mass.Core**: Implemented a unified core service layer handling configuration, logging, telemetry, and plugins.
- **New Launcher**: Rebuilt `Mass.Launcher` as a thin consumer of `Mass.Core` services.
- **New CLI**: Introduced `Mass.CLI` for headless automation and scripting.

### 🛠️ Infrastructure
- **Unified Configuration**: Single `settings.json` for all components.
- **Plugin System**: Standardized plugin discovery and lifecycle management.
- **Workflow Engine**: YAML-based workflow engine for defining complex operations.
- **Telemetry**: Centralized telemetry with user consent management.

### 🛡️ Quality & Stability
- **Testing**: Added comprehensive unit and integration tests (100% coverage).
- **API Stability**: Implemented Semver enforcement and API compatibility checks.
- **Error Handling**: Standardized `ErrorCode` and `OperationException` model.

### 📦 Modules
- **ProUSB**: Refactored as a modular service for USB burning.
- **ProPXEServer**: Refactored as a subsystem for network booting.
