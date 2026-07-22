# Xtate.DataModel.EcmaScript

[![NuGet](https://img.shields.io/nuget/v/Xtate.DataModel.EcmaScript.svg)](https://www.nuget.org/packages/Xtate.DataModel.EcmaScript)
[![CodeQL](https://github.com/TSSArt/Xtate.DataModel.EcmaScript/actions/workflows/codeql.yml/badge.svg)](https://github.com/TSSArt/Xtate.DataModel.EcmaScript/actions/workflows/codeql.yml)
[![License: AGPL-3.0-or-later](https://img.shields.io/badge/license-AGPL--3.0--or--later-blue.svg)](LICENSE)

Xtate.DataModel.EcmaScript adds an ECMAScript data-model handler to [Xtate.Core](https://www.nuget.org/packages/Xtate.Core/). It uses [Jint](https://github.com/sebastienros/jint) to evaluate SCXML expressions and executable content identified by `datamodel="ecmascript"`.

## Features

- Evaluates value, condition, location, script, and external-script expressions.
- Supports SCXML `foreach`, inline content, external data, and custom actions.
- Converts between Jint values and Xtate `DataModelValue`/`DataModelList` values.
- Exposes the active Jint version through the data-model variables.
- Registers as a scoped Xtate data-model handler through `Xtate.IoC`.

## Installation

```shell
dotnet add package Xtate.DataModel.EcmaScript
```

## Usage

Register the ECMAScript handler alongside the Xtate modules used by the application:

```csharp
using Xtate.DataModel.EcmaScript.DependencyInjection;
using Xtate.IoC;

var services = new ServiceCollection();
services.AddModule<EcmaScriptDataModelHandlerModule>();
```

An SCXML document can then select the handler with `datamodel="ecmascript"`:

```xml
<scxml xmlns="http://www.w3.org/2005/07/scxml"
       version="1.0"
       datamodel="ecmascript"
       initial="done">
  <datamodel>
    <data id="message" expr="'Hello from ECMAScript'" />
  </datamodel>
  <final id="done" />
</scxml>
```

## Supported frameworks

The library targets .NET 11, .NET 10, .NET 9, .NET 8, .NET Standard 2.0, and .NET Framework 4.6.2.

## Building from source

```shell
git clone https://github.com/TSSArt/Xtate.DataModel.EcmaScript.git
cd Xtate.DataModel.EcmaScript
dotnet restore
dotnet build Xtate.DataModel.EcmaScript.sln
dotnet test Xtate.DataModel.EcmaScript.sln
```

## Repository layout

| Path | Description |
| --- | --- |
| `src/Xtate.DataModel.EcmaScript` | Handler, engine, evaluators, wrappers, and resources |
| `test/Xtate.DataModel.EcmaScript.Test` | MSTest behavior and integration tests |
| `.github/instructions` | Path-specific guidance for coding agents |
| `.github/workflows` | Security analysis and publishing workflows |
| `.agents` | Repository guide for maintainers and coding agents |

## Contributing

Contributions are welcome. Read the [repository guide](.agents/AGENTS.md), follow `.editorconfig`, and add focused tests for expression, conversion, or execution behavior changes.

Use [GitHub Issues](https://github.com/TSSArt/Xtate.DataModel.EcmaScript/issues) for bug reports and feature requests.

## License

Xtate.DataModel.EcmaScript is licensed under the [GNU Affero General Public License v3.0 or later](LICENSE).
