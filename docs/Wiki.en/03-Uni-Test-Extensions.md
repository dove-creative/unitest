# UniTest Execution Extensions

This document describes stopping after failure, reproducing a path, and extending a continuous test run. See [Usage](04-Usage.md), section 6, for executable API examples.

## Stop After Failure

`Project.Execute` can stop scheduling new work after a failure. It still waits for workers that already started, observes their faults, and then returns the result tree. Stopping does not forcibly interrupt a synchronous callback.

Use `GetFailedReports()` after execution finishes to keep failed nodes and their ancestor paths. Cancelled nodes have a distinct status and do not become ordinary assertion failures.

## Reproduce a Path

Read a node's execution history and pass its slash-separated Lab IDs to `Execute(ids)`. UniTest creates a fresh model and follows the same path. Lab IDs and the slash delimiter remain stable across the 0.4.0 namespace migration.

An unavailable ID raises `ArgumentException`. A failure inside a Lab remains recorded on its Node. The `Run(..., executionIds)` helper logs execution errors and returns `false` instead of propagating them.

## Debug a Node

1. Select the node from a completed result tree.
2. Call `node.DetachAndRestore()` to create a separate node and reconstruct the preceding history.
3. Set a breakpoint and call `restored.Execute()` to rerun the selected Lab.

The detached node does not change the original result graph. Reproduction still depends on the subject and test inputs being reproducible.

## Continue Along a Sustainable Path

Full exploration can grow exponentially with depth: a fixed branching factor `b` and depth `d` can produce on the order of `b^d` paths. This is not a general quadratic bound.

`ExecuteContinuously` checks the candidates at each step and continues along one sustainable path. It chooses that path deterministically from the execution history and seed. With a fixed candidate count `b`, the number of candidate executions is approximately proportional to `b × d`, although subject construction, replay, and callback costs also affect total runtime.

Continuous execution helps inspect long state histories. It does not cover every combination; combine it with bounded exploration and targeted path reproduction.
