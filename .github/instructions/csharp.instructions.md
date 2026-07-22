---
applyTo: "src/**/*.cs"
---

# C# source instructions

## Style and compatibility

- Follow `.editorconfig`: tabs, nullable annotations, analyzer rules, using order, and existing naming conventions.
- Match the AGPL header and current style in adjacent source files.
- Preserve every target framework. Guard polyfills precisely and avoid conflicts with modern framework APIs.
- Preserve `ValueTask` and `ConfigureAwait(false)` patterns used by async evaluation code.

## Architecture

- Use Xtate.IoC modules, scoped services, and existing injection attributes.
- Keep expression parsing/validation in the handler, execution in evaluators/engine, and Jint/Xtate conversions in wrappers/helpers.
- Preserve the `ecmascript` provider identity and scoped engine lifetime.
- Keep valid and invalid expression handling aligned across evaluator types.

## Generated and dependency files

- Edit `Properties/Resources.resx`, not `Resources.Designer.cs`.
- Keep dependency versions in `Directory.Packages.props` and omit versions from `PackageReference` items.
- Do not edit generated build-property files or build output.

## Verification

- Add focused tests for behavior changes, including error paths when relevant.
- Build a modern target and compatibility targets affected by API or polyfill changes.
