# lightr Development Patterns

> Auto-generated skill from repository analysis

## Overview

The lightr repository is a .NET library project with TypeScript components that provides OpenAPI/Swagger integration capabilities. The codebase follows a monorepo structure with a main library (`src/Lightr/Lightr/`) and a sample application (`sample/SampleLightrApp/`) to demonstrate usage. The project emphasizes conventional commit patterns and maintains compatibility across .NET framework versions.

## Coding Conventions

### File Naming
- **TypeScript files**: Use camelCase naming convention
- **Project files**: Follow standard .NET naming with `.csproj` extensions
- **Test files**: Use `*.test.*` pattern for test file identification

### Import/Export Style
- **Mixed approach**: Both named and default imports/exports are used
- **Consistency**: Maintain existing patterns within each file type

### Commit Messages
- **Format**: Conventional commits with prefixes: `chore`, `fix`, `build`, `feat`
- **Length**: Keep commit messages around 47 characters average
- **Examples**:
  ```
  chore: update .NET framework to 8.0
  fix: resolve sample app compatibility issue
  feat: add new OpenAPI endpoint support
  ```

## Workflows

### .NET Framework Version Update
**Trigger:** When a new .NET version is released or needs to be updated
**Command:** `/update-dotnet`

1. Update `TargetFramework` in main library project file `src/Lightr/Lightr/Lightr.csproj`
   ```xml
   <TargetFramework>net8.0</TargetFramework>
   ```

2. Update `TargetFramework` in sample application project file `sample/SampleLightrApp/SampleLightrApp.csproj`
   ```xml
   <TargetFramework>net8.0</TargetFramework>
   ```

3. Ensure compatibility across both projects by building and testing
4. Commit with message: `chore: update .NET framework to [version]`

### Individual Dependency Update
**Trigger:** When individual package updates are available via dependabot/renovate
**Command:** `/update-dependency`

1. Identify the specific NuGet package that needs updating
2. Update package version in the appropriate `.csproj` file:
   ```xml
   <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
   ```

3. Test the update to ensure no breaking changes
4. Commit with conventional commit message including dependency metadata:
   ```
   chore: update Microsoft.Extensions.DependencyInjection to 8.0.0
   ```

### OpenAPI/Swagger Specification Update
**Trigger:** When the external API specification changes or new endpoints are added
**Command:** `/update-api-spec`

1. Update the OpenAPI JSON specification in `src/Lightr/Lightr/OpenAPIs/docs.json`
2. Review changes for any breaking modifications to existing endpoints
3. Modify sample application `Program.cs` to accommodate new API changes:
   ```csharp
   // Update service configuration or endpoint usage
   builder.Services.AddLightr(options => {
       // Configure new API endpoints
   });
   ```

4. Test sample application compatibility with updated specification
5. Commit with message: `feat: update OpenAPI specification`

### CI/CD Configuration Update
**Trigger:** When build system needs updates or GitHub Actions require changes
**Command:** `/update-ci-config`

1. Update workflow YAML files in `.github/workflows/build-ci.yml`:
   ```yaml
   - name: Setup .NET
     uses: actions/setup-dotnet@v3
     with:
       dotnet-version: '8.0.x'
   ```

2. Update deployment workflow in `.github/workflows/deploy.yml`
3. Update build target versions to match project requirements
4. Test workflow changes in a feature branch
5. Commit with message: `build: update CI workflow configuration`

### Sample Application Fixes
**Trigger:** When sample application breaks due to API changes or dependency updates
**Command:** `/fix-sample-app`

1. Identify compatibility issues by examining build/runtime errors
2. Update `Program.cs` configuration to match current API:
   ```csharp
   var builder = WebApplication.CreateBuilder(args);
   
   // Fix service registration
   builder.Services.AddLightr();
   
   var app = builder.Build();
   // Update middleware configuration
   ```

3. Test sample application functionality end-to-end
4. Commit with message: `fix: resolve sample app compatibility issues`

## Testing Patterns

### Test File Organization
- Test files follow the `*.test.*` naming pattern
- Tests are likely organized alongside source files or in dedicated test directories
- Framework details are project-specific and should be determined from existing test files

### Test Structure
- Follow existing test patterns found in the codebase
- Ensure tests cover both library functionality and sample application scenarios
- Maintain test coverage when updating dependencies or API specifications

## Commands

| Command | Purpose |
|---------|---------|
| `/update-dotnet` | Update .NET framework version across monorepo |
| `/update-dependency` | Update individual NuGet package dependencies |
| `/update-api-spec` | Update OpenAPI/Swagger documentation |
| `/update-ci-config` | Update CI/CD workflow configurations |
| `/fix-sample-app` | Fix sample application compatibility issues |