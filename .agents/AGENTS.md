# Xtate.DataModel.EcmaScript repository guide

Use this guide as the first source of repository context. Inspect the smallest relevant handler, evaluator, wrapper, or test area before widening the search.

## Project purpose

Xtate.DataModel.EcmaScript is the Jint-backed ECMAScript data-model extension for Xtate.Core.

| Path | Purpose |
| --- | --- |
| `src/Xtate.DataModel.EcmaScript/Xtate.DataModel.EcmaScript.csproj` | Multi-targeted library and NuGet package |
| `test/Xtate.DataModel.EcmaScript.Test/Xtate.DataModel.EcmaScript.Test.csproj` | MSTest behavior and integration tests |
| `Xtate.DataModel.EcmaScript.sln` | Repository solution |

The library targets `net11.0`, `net10.0`, `net9.0`, `net8.0`, `netstandard2.0`, and `net462`. Tests target the modern frameworks plus `net462` unless `SkipNetFrameworkTests=true`.

## Architecture

- `DependencyInjection/EcmaScriptDataModelHandlerModule.cs` is the composition root. It depends on Xtate.Core's `DataModelHandlerBaseModule`, registers evaluator factories, shares `EcmaScriptEngine` within a scope, and publishes the handler provider.
- `Services/EcmaScriptDataModelHandler.cs` parses expressions, records validation errors, and replaces state-machine expression nodes with ECMAScript evaluators.
- `Services/EcmaScriptEngine.cs` owns Jint execution context and variable/location interaction.
- `Services/Evaluators` adapts each Xtate expression or executable-content contract to the engine.
- `Internal/DataModelArrayWrapper.cs`, `DataModelObjectWrapper.cs`, and `EcmaScriptHelper.cs` preserve conversions and mutation semantics between Jint and Xtate data-model values.
- `Properties/Resources.resx` is the source of localized validation and execution messages.

The handler name is `ecmascript`. Evaluator coverage must remain aligned across values, conditions, locations, scripts, external content, `foreach`, and custom actions.

## Code conventions and hazards

- Follow `.editorconfig`: tabs, nullable annotations, analyzer rules, and existing naming/style.
- Preserve `ValueTask`-based APIs and required `ConfigureAwait(false)` calls.
- Use Xtate.IoC modules, scopes, `[InstantiatedByIoC]`, and `[SetByIoC]` patterns already present.
- Do not bypass the parser/error-processor flow when creating prepared scripts.
- Preserve bidirectional conversion, array indexing, object-property, and mutation semantics in wrappers.
- Guard compatibility code under `Polyfills` with precise target checks.
- Edit `Resources.resx`, not the generated `Resources.Designer.cs`.
- Treat `Directory.Build.props` and `Global.Packages.props` as generated; keep package versions in `Directory.Packages.props`.
- Ignore `bin`, `obj`, `TestResults`, and IDE metadata.

Path-specific rules in `.github/instructions` take precedence for matching files.

## Build and test

```powershell
dotnet restore
dotnet build Xtate.DataModel.EcmaScript.sln
dotnet test Xtate.DataModel.EcmaScript.sln
```

For a focused modern target:

```powershell
dotnet test test/Xtate.DataModel.EcmaScript.Test/Xtate.DataModel.EcmaScript.Test.csproj -f net10.0
```

Use `-p:SkipNetFrameworkTests=true` if the local environment cannot run `net462`. Validate both modern and legacy targets when changing Jint adapters, polyfills, or framework-dependent APIs.

## Change checklist

1. Identify the affected expression contract, evaluator, engine path, and conversion wrapper.
2. Add a focused regression test, including invalid-expression behavior when relevant.
3. Build and test the narrowest target, then the solution when practical.
4. Keep generated files and unrelated existing work untouched.
5. Update documentation when registration, supported behavior, or repository navigation changes.
