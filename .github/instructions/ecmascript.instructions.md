---
applyTo: "src/Xtate.DataModel.EcmaScript/{Services,Internal,DependencyInjection}/**/*.cs"
---

# ECMAScript handler instructions

## Handler and evaluator behavior

- Keep handler validation and evaluator substitution aligned for every supported Xtate expression contract.
- Prepare scripts through the existing parser/Jint flow and report parse errors through `IErrorProcessorService`.
- Preserve external script/data loading through Xtate.Core consumer abstractions.
- Keep the `ecmascript` provider name and scoped `EcmaScriptEngine` lifetime.

## Value conversion

- Preserve undefined, null, Boolean, number, string, object, and array semantics in both conversion directions.
- Preserve array indexes, `length`, enumeration, deletion, property descriptors, and mutation behavior.
- Keep execution-context entry/exit balanced, including exceptional paths.

## Verification

- Add tests for the affected expression/evaluator and for invalid syntax when relevant.
- Add round-trip and mutation tests when wrapper or helper conversion changes.
