# UniTest for TypeScript

[한국어](README.ko.md)

UniTest is a library for exploring and replaying test paths from model state in Node.js and browsers.

## Features

- List the operations available in the current state and explore branches up to a specified depth.
- Replay a path or repeat a continuous run with a fixed seed.
- Combine checks for multiple state axes with `TestCase`, `merge`, and `extend`.
- Run synchronous or asynchronous Arrange, Act, and Assert callbacks and export the results as JSON.

## Requirements and installation

Use Node.js 22 or later, or a browser with ES2022 module support. The package ships ESM and type declarations and has no runtime dependencies.

Build a local package from `typescript/` in the repository.

```sh
npm ci
npm pack
```

Install the resulting file in a consumer project.

```sh
npm install /path/to/blackthunder-unitest-0.1.0.tgz
```

Import the shared engine from `@blackthunder/unitest`. When loading ESM directly in a browser, map the package name to the deployed `dist/index.js` with an import map or use its module URL directly.

## Quick start

```ts
import { CompactLab, CompactProject, Model } from "@blackthunder/unitest";

class CounterModel extends Model<{ value: number }> {
    expected = 0;

    constructor() {
        super({ value: 0 });
    }
}

class CounterProject extends CompactProject<CounterModel> {
    constructor() {
        super(() => new CounterModel());
    }

    *createCompactLabs() {
        yield new CompactLab<CounterModel>("increment", {
            arranger: model => { model.expected++; },
            actor: async model => { model.subject.value++; },
            asserter: model => {
                if (model.subject.value !== model.expected) throw new Error("Unexpected counter value");
            },
        });
    }
}

const root = await new CounterProject().execute(2);
console.log(root.getReport().toJson());
```

Each project runs once. To replay a path, call `execute("increment/increment")` on a new instance.

## Execution control and reports

- `execute(depth | path, { stopOnFailure, signal })` explores all branches or runs a specified path. `stopOnFailure` defaults to `true`.
- `executeContinuously(depth, { seed, stopOnFailure, signal })` runs continuous exploration. The seed defaults to `0`. With matching model behavior, inputs, and candidate generation order, it selects the same path as C#.
- Request cooperative cancellation with an `AbortSignal` or `project.cancel()`. Callbacks receive the signal, and the run returns after the active callback settles.
- `node.detachAndRestore()` restores execution history for debugging in a separate node. `getReport()` returns the full report, and `getFailedReports()` retains failures and their ancestors.
- `saveReport(report, path)` from `@blackthunder/unitest/node` writes UTF-8 JSON to a specified path. `downloadReport(report, fileName)` from `@blackthunder/unitest/browser` starts a JSON download.

## Samples and verification

Run these commands from `typescript/`. The Node.js sample writes reports under `artifacts/typescript-samples/`. The browser sample runs the same scenarios at the local address printed in the terminal and offers downloads.

```sh
npm ci
npm run build
npm run sample -- all
npm run sample:browser
```

`npm run sample` opens an interactive prompt. Select `explore`, `replay`, `compose`, `continuous`, `async`, or `cancel`, or enter `help` to list commands.

See the [sample guide](samples/README.md) for what each scenario demonstrates.

```sh
npm run typecheck
npm test
npx playwright install chromium firefox webkit
npm run test:browser
npm run test:package
```

## Documentation

- [Designing states and operations](../docs/Wiki.en/00.%20Unit%20Test%20Guideline.md)
- [UniTest overview](../docs/Wiki.en/01.%20Overview.md)
- [C# usage guide](../csharp/README.md)

## License

Distributed under the MIT license. [License](../LICENSE.md)
