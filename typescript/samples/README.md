# UniTest TypeScript samples

The samples explore, replay, and compose counter state transitions in Node.js and in a browser.

## Run

From `typescript/`, build once and run the interactive prompt or a single scenario.

```sh
npm ci
npm run build
npm run sample
npm run sample -- explore
```

For the browser, run `npm run sample:browser` and open the local address printed in the terminal. Select a scenario, run it, and use **Download JSON** to save its report.

## Commands

| Command | Scenario |
| --- | --- |
| `explore` | Explore all counter branches to depth 3. |
| `replay` | Run `increment/increment/decrement`. |
| `compose` | Combine the counter and lock state rules. |
| `continuous` | Run a continuous path with seed `42`. |
| `async` | Await asynchronous Arrange, Act, and Assert. |
| `cancel` | Request cancellation from a running callback and inspect the partial result. |
| `all` | Run all six scenarios in Node.js. |
| `help` | List Node.js commands. |
| `exit` | Close the Node.js prompt. |

## Output

The Node.js sample saves UTF-8 reports as `<command>.json` under the repository’s `artifacts/typescript-samples/` directory. Each browser scenario shows the JSON directly and offers the same file through its download button.
