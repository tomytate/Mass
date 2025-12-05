# Known Issues & Deferred Items (v1.0.0)

The following items were identified during the final audit and have been deferred to future releases.

## Technical Debt
- [ ] **Mass.Core/Workflows/WorkflowExecutor.cs**: Replace hardcoded step execution switch with dynamic `RegistryService` resolution.
- [ ] **Mass.Core/Services/IpcService.cs**: Fix CA2022 warning (inexact read) in pipe stream reading.
- [ ] **ProPXEServer**: Complete implementation of `DhcpService` architecture detection logic.

## Enhancements
- [ ] **CLI**: Add interactive mode for workflow selection.
- [ ] **Launcher**: Implement real-time progress bar for long-running workflows.
- [ ] **Docs**: Add detailed API reference for `Mass.Spec`.

## Testing
- [ ] **Integration**: Add more scenarios for complex workflow branching.
- [ ] **Performance**: Benchmark concurrent workflow execution.
