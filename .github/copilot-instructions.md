# Xtate.DataModel.EcmaScript Copilot instructions

## Repository at a glance

Xtate.DataModel.EcmaScript is the Jint-backed ECMAScript data-model handler for Xtate.Core. The production project is `src/Xtate.DataModel.EcmaScript/Xtate.DataModel.EcmaScript.csproj`; tests are in `test/Xtate.DataModel.EcmaScript.Test`.

Read [`.agents/AGENTS.md`](../.agents/AGENTS.md) for architecture and hazards. Apply every matching file in [`.github/instructions`](instructions); those rules are more specific than this guide.

## Working approach

1. Identify the affected Xtate expression or executable-content contract.
2. Trace it through `EcmaScriptDataModelHandler`, its evaluator, `EcmaScriptEngine`, and Jint/Xtate conversion helpers.
3. Check module registration and scoped lifetime behavior when adding or replacing a service.
4. Add focused valid and invalid-expression tests as applicable.
5. Run the narrowest useful test before solution-wide validation.

## Build and test

```powershell
dotnet restore
dotnet build Xtate.DataModel.EcmaScript.sln
dotnet test Xtate.DataModel.EcmaScript.sln
```

Focused example:

```powershell
dotnet test test/Xtate.DataModel.EcmaScript.Test/Xtate.DataModel.EcmaScript.Test.csproj -f net10.0
```

The library targets `net11.0`, `net10.0`, `net9.0`, `net8.0`, `netstandard2.0`, and `net462`. Tests target modern frameworks plus optional `net462`.

## Shared coding rules

- Follow `.editorconfig`; C# uses tabs, nullable annotations, analyzers, and preview language features.
- Match the AGPL header and current style in adjacent source files.
- Preserve `ValueTask`, async evaluation, and `ConfigureAwait(false)` patterns.
- Use Xtate.IoC modules, scopes, `[InstantiatedByIoC]`, and `[SetByIoC]` patterns.
- Keep package versions in `Directory.Packages.props`.
- Treat `Directory.Build.props`, `Global.Packages.props`, and `Resources.Designer.cs` as generated. Edit `Resources.resx` instead.
- Ignore `bin`, `obj`, `TestResults`, and IDE metadata.

## Architecture guardrails

- The provider name remains `ecmascript`.
- Parse errors must flow through the handler's validation/error processor.
- Evaluator replacements must continue to implement the corresponding Xtate expression contracts.
- `EcmaScriptEngine` is shared within the state-machine scope; do not promote it to container scope.
- Preserve conversions for undefined/null, primitives, lists, objects, array indexes, properties, and mutations.
- External script/data loading stays integrated with Xtate.Core abstractions.
- Polyfills must be precisely guarded and must not conflict with modern framework APIs.

## Tests and documentation

- Use MSTest and existing state-machine/interpreter setup patterns.
- Cover expression parsing, execution, assignment, `foreach`, conversion, and error reporting where relevant.
- Avoid tests that depend on external services or timing.
- Update README or repository guidance when registration, handler behavior, supported targets, or commands change.

## Before finishing

Confirm parser and conversion semantics, scoped lifetime, focused tests, generated-file safety, compatibility targets, and task-scoped changes.
