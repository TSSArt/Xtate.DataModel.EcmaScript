---
applyTo: "test/**/*.cs"
---

# Test source instructions

## Test style

- Use MSTest attributes and the assertion style already used by nearby tests.
- Keep tests focused, deterministic, independent, and safe under parallel execution.
- Reuse existing Xtate.Core state-machine, interpreter, logging, and Xtate.IoC setup patterns.
- Do not depend on external services, network access, console inspection, or timing.

## Coverage

- Cover valid and invalid parsing for the affected expression type.
- Assert evaluation, assignment, scoping, mutation, conversion, and error reporting as applicable.
- Include array/object and undefined/null cases when changing Jint/Xtate conversion behavior.
- Keep test scripts and state machines minimal and name tests for the observed behavior.

## Verification

- Run the narrowest matching test on one modern framework first.
- Run broader solution tests and legacy targets when conversion, polyfill, or compatibility-sensitive behavior changes.
