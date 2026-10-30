# UniTest Implementation

UniTest connects nodes that reconstruct state and Labs that verify actions through a project's execution flow.

## Contents

1. Component responsibilities
2. Labs and state composition
3. Node creation and history replay
4. Project execution and shutdown
5. Result snapshots and JSON
6. Design and extension guidelines

## 1. Component Responsibilities

| Component | Stored information | Responsibility |
| --- | --- | --- |
| `Model` | Subject, expected state, MetadataGroup, history, sustainability, and continuability | Hold the subject state and the state expected by the test. |
| `Lab` | Id, preparation/action/assertion delegates, and metadata template | Perform AAA verification and apply execution limits for one action. |
| `CompositeLab` | Ordered Labs | Combine preparation and verification from multiple state axes with the original action. |
| `Node` | Model, Lab, parent/children, result, and diagnostic values | Maintain independent path state and execution results. |
| `Project` | Execution policy, queues, workers, and cancellation state | Coordinate Lab generation/execution and wait for started work to finish. |
| `TestReport` | A value snapshot of nodes | Supply JSON output data independent of executable objects. |

Detailed calling examples and test-authoring procedures belong in [Usage](04-Usage.md).

## 2. Labs and State Composition

### 2-1. A Single Lab

Execution begins by recording the action in model history and resetting per-Lab metadata. Arrange copies the template and prepares action metadata and expected state. Act calls the actual subject; Assert compares its state with the expectation.

The current Lab's assertions still run after an expected exception. A missing expected exception or an unexpected exception is a failure. Stage-specific errors are returned as `ExecutionException` values and stop subsequent execution.

`ToUncontinuable`, `ToUnsustainable`, and remaining execution counts take effect after current verification. In particular, `ToUncontinuable` does not skip the current assertions.

### 2-2. Merge and Extend

Merge combines template behavior and metadata with a copied Lab. Extend appends another state-axis Lab to the original action, creating a CompositeLab.

CompositeLab arranges from the original toward the extensions and executes only the original Act. Assertions run from the extensions back to the original. Expected exceptions are checked across the composed metadata, and execution limits are combined after all assertions succeed. The smallest remaining-execution limit wins.

Composition direction determines preparation and verification order. Extension is not simply a mechanism for invoking the same action repeatedly.

### 2-3. TestCase

TestCase is a readonly value type describing allowed and excluded actions/state conditions. Append, Confine, and Exclude operations return new definitions. Generators narrow each axis's conditions to select concrete action templates.

## 3. Node Creation and History Replay

A root node creates an initial model. A child node replays the preceding path into a new model before its next Lab runs. Different paths do not accumulate expected state in one shared model instance.

Node status distinguishes preparation, execution, success, failure, and cancellation. Failure and cancellation affect continuability, while model descriptions and path history are retained as diagnostic values.

`DetachAndRestore` creates a detached debug node without changing the original tree. Its reports retain the original ancestor path, including after repeated detachment or the addition of descendants.

Lab IDs may contain spaces, Unicode, and punctuation. `/` is reserved for replay paths and is rejected during node construction. The `Id` property name is distinct from its stored string value.

## 4. Project Execution and Shutdown

### 4-1. Connecting Design and Execution

1. The project prepares execution depth and cancellation state, then creates a root node.
2. TestDesigner generates Labs from the current model and queues child nodes for execution.
3. TestExecutor executes prepared nodes and records their results and completion.
4. The coordinator observes completed work and schedules continuable nodes for further design.
5. At termination, it stops scheduling new work and waits for all started work.

Prepared nodes can run while Lab generation is still in progress. If a generator's iterator fails, already-created children are preserved. When no work is available, the coordinator asynchronously waits for registration/completion signals.

### 4-2. Execution Modes

`ThreadPool` dispatches design and execution work through Task.Run. `Inline` handles it after Task.Yield without another Task.Run. Both modes use the same asynchronous public API and result contract; Inline does not guarantee execution on the same OS thread.

Depth-based execution explores multiple paths. ID-based execution reproduces a specified path. Continuous execution checks the current candidates and continues along a sustainable path chosen deterministically from history and the seed.

### 4-3. Cancellation and Failure

External and project cancellation reach workers through linked cancellation state. When stop-on-failure is enabled, observed failures stop new work from being scheduled. Started work and its exceptions are observed before synchronization resources are released, so workers do not mutate the result tree after the call returns.

Test failures and cancellations recorded on nodes are available in the result tree. Non-cancellation exceptions not recorded on a node propagate to the Execute caller. Run helpers log these exceptions and return failure.

## 5. Result Snapshots and JSON

GetReport creates a value snapshot without references to executable objects. Its flat node list uses `nodeId` and `parentId` for relationships. Model descriptions, paths, exceptions, and report-generation errors are retained separately. A model's failing ToString does not lose execution errors or child nodes.

GetFailedReports retains failed nodes and their ancestors while preserving node IDs. If nothing failed, it keeps the first node. Detached debug-node ancestor diagnostics use the same structure.

The built-in JSON writer serializes strings and exception values after execution. It escapes control characters and lone surrogates; ExportJson writes BOM-free UTF-8 files. No external runtime package is required for JSON generation.

## 6. Design and Extension Guidelines

- Extend new domains through Model and Project instead of adding domain-specific executors to the framework.
- Compute expected state from inputs and contracts instead of copying the subject's actual result.
- Verify registration, completion observation, cancellation, and resource cleanup together when changing concurrency behavior.
- Update samples, usage documentation, migration guidance, and relevant tests when public names or behavior change.

See [Overview](01-Overview.md) for the overall structure and [Extensions](03-Uni-Test-Extensions.md) for choosing failure replay and continuous execution.
