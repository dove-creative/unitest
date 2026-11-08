# TypeScript test organization and results

| Scope | State-action table | Plan | Results |
| --- | --- | --- | --- |
| Unit tests | [Table](Unit/01.%20Tables.md) | [Plan](Unit/02.%20Plans.md) | [Results](Unit/03.%20Results.md) |
| UniTest self-tests | [Table](UniTest/01.%20Tables.md) | [Plan](UniTest/02.%20Plans.md) | [Results](UniTest/03.%20Results.md) |
| Browser and package | [Table](Environment/01.%20Tables.md) | [Plan](Environment/02.%20Plans.md) | [Results](Environment/03.%20Results.md) |

All 23 tests passed on both Node.js 22.23.4 and 24.15.0 on 2026-11-08.
Shared vectors and execution scenarios passed in Chromium, Firefox, and WebKit. A separate consumer project verified package installation, types, and report output.
