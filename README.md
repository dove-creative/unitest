# UniTest

[한국어](README.ko.md)

UniTest is a model-based testing framework. Define the actions available in each model state and the results you expect. UniTest explores combinations of those actions and verifies the resulting paths.

| Implementation | Runtime | Package | Guide |
| --- | --- | --- | --- |
| C# | .NET Standard 2.1 compatible runtimes | `BlackThunder.UniTest` | [Installation and usage](csharp/README.md) |
| TypeScript | Node.js 22+ and ES2022 browsers | `@blackthunder/unitest` | [Installation and usage](typescript/README.md) |

## Features

- Explore state transitions using Arrange, Act, and Assert.
- Combine tests for independent state dimensions.
- Replay failures by Lab ID or restore an isolated debug node.
- Follow a seeded continuous path and save the results as JSON.

The implementations build, test, and package separately. They share the [path selection vectors](contracts/selection.json) and the JSON report format.

## Documentation

- [Test methodology](docs/Wiki.en/00.%20Unit%20Test%20Guideline.md)
- [Framework overview](docs/Wiki.en/01.%20Overview.md)
- [Contributing and verification](CONTRIBUTING.md)
- [Changes](CHANGELOG.md)
- [MIT license](LICENSE.md)
