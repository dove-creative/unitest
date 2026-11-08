# TypeScript용 UniTest

[English](README.md)

UniTest는 Node.js와 브라우저에서 모델의 상태를 기준으로 테스트 경로를 탐색하고 재현하는 라이브러리입니다.

## 주요 기능

- 현재 상태에서 가능한 동작을 나열하고 지정 깊이까지 분기를 탐색합니다.
- 경로를 재현하거나 seed를 고정해 연속 실행을 반복합니다.
- `TestCase`, `merge`, `extend`로 여러 상태 축에 대한 검증을 조합합니다.
- 동기 또는 비동기 Arrange·Act·Assert를 실행하고, 결과를 JSON으로 내보냅니다.

## 요구 사항과 설치

Node.js 22 이상 또는 ES2022 모듈을 지원하는 브라우저가 필요합니다. 패키지는 ESM과 타입 선언을 제공하며 런타임 의존성 없이 동작합니다.

저장소의 `typescript/`에서 로컬 패키지를 생성합니다.

```sh
npm ci
npm pack
```

소비 프로젝트에서 생성된 파일을 설치합니다.

```sh
npm install /path/to/blackthunder-unitest-0.1.0.tgz
```

공통 엔진은 `@blackthunder/unitest`로 가져옵니다. 브라우저에서 직접 ESM을 로드할 때는 import map으로 패키지 이름을 배포한 `dist/index.js`에 연결하거나 해당 모듈 URL을 직접 사용합니다.

## 빠른 시작

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

프로젝트는 한 번 실행합니다. 경로를 재현할 때는 새 인스턴스에서 `execute("increment/increment")`를 호출합니다.

## 실행 제어와 리포트

- `execute(depth | path, { stopOnFailure, signal })`로 전체 분기나 지정 경로를 실행합니다. `stopOnFailure`의 기본값은 `true`입니다.
- `executeContinuously(depth, { seed, stopOnFailure, signal })`로 연속 실행합니다. seed 기본값은 `0`입니다. 동일한 모델 동작·입력·후보 생성 순서에서는 C#과 같은 경로를 선택합니다.
- `AbortSignal` 또는 `project.cancel()`로 협력 취소합니다. 콜백에 신호가 전달되며, 진행 중인 콜백이 종료된 뒤 결과가 반환됩니다.
- `node.detachAndRestore()`로 이력을 복원해 별도 노드에서 디버깅합니다. `getReport()`는 전체 리포트를, `getFailedReports()`는 실패 노드와 조상을 보존한 리포트를 반환합니다.
- `@blackthunder/unitest/node`의 `saveReport(report, path)`는 지정 경로에 UTF-8 JSON을 저장합니다. `@blackthunder/unitest/browser`의 `downloadReport(report, fileName)`은 JSON 다운로드를 시작합니다.

## 샘플과 검증

`typescript/`에서 실행합니다. Node.js 샘플은 리포트를 `artifacts/typescript-samples/`에 저장합니다. 브라우저 샘플은 터미널에 표시된 로컬 주소에서 같은 시나리오를 실행하고 다운로드를 제공합니다.

```sh
npm ci
npm run build
npm run sample -- all
npm run sample:browser
```

`npm run sample`은 대화형 프롬프트를 시작합니다. `explore`, `replay`, `compose`, `continuous`, `async`, `cancel` 중 하나를 선택하거나 `help`로 명령을 확인합니다.

각 시나리오의 목적은 [샘플 안내](samples/README.md)에서 확인할 수 있습니다.

```sh
npm run typecheck
npm test
npx playwright install chromium firefox webkit
npm run test:browser
npm run test:package
```

## 문서

- [상태와 동작을 설계하는 방법](../docs/Wiki.ko/00.%20Unit%20Test%20Guideline.md)
- [UniTest 개요](../docs/Wiki.ko/01.%20Overview.md)
- [C# 구현 사용 안내](../csharp/README.ko.md)

## 라이선스

MIT 라이선스로 배포합니다. [라이선스](../LICENSE.md)
