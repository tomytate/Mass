# Contributing to Mass Suite

## Public API & Versioning

Mass.Spec follows [Semantic Versioning](https://semver.org/).

### Breaking Changes
Any change to the public API of `Mass.Spec` (adding, removing, or modifying public types/members) requires a version bump.

1. **Run Checks**:
   ```powershell
   ./scripts/Check-Semver.ps1
   ```

2. **If API Changed**:
   - Bump `<Version>` in `src/Mass.Spec/Mass.Spec.csproj`.
   - Update baselines:
     ```powershell
     ./scripts/Check-ApiDiff.ps1 -UpdateBaseline
     echo "NEW_VERSION" > src/Mass.Spec/version-baseline.txt
     ```

3. **CI Enforcement**:
   - CI will fail if API changes are detected without a corresponding version bump.
