# UniTest

[English README](README.md)

UniTest는 모델과 상태-동작 조합을 기반으로 실행 가능한 테스트 경로를 자동 생성하고 검증하는 .NET 테스트 프레임워크이다.

일반 유닛 테스트가 개별 테스트 케이스를 직접 나열하는 데 집중한다면, UniTest는 현재 `Model`에서 가능한 `Lab`을 생성하고 실행 이력에 따라 후속 상태를 확장한다.

## 에이전트 보조와 CLI 자동화

UniTest는 에이전트 보조 테스트 작성과 CLI 기반 반복 검증을 같은 모델 구조 위에서 이어갈 수 있도록, 테스트 작성 흐름과 외부 실행 자동화 워크플로를 함께 제공한다.

AI 에이전트는 UniTest 기반 테스트 작성이나 CLI 반복 검증을 맡는 경우, 실제 작업 절차를 아래 [문서](#문서)의 `docs/Workflow.ko` 항목에서 먼저 확인한다.

## 주요 기능

- 상태 기반 테스트 생성: `Project<TModel>.CreateLabs(...)`에서 현재 상태에 맞는 테스트 흐름을 만든다.
- AAA 실행 단위: `Lab<TModel>`과 `CompactLab<TModel>`로 Arrange, Act, Assert 흐름을 구성한다.
- 경로 확장: `Node<TModel>`가 실행 이력을 보존하고 다음 테스트 상태를 독립적으로 생성한다.
- 다중 상태 조합: `TestCase`, `Merge(...)`, `Extend(...)`로 여러 상태 축을 계층적으로 결합한다.
- 재실행과 연속 실행: `Run(...)`, `RunContinuously(...)`, `Execute(ids)`로 전체 경로, 긴 단일 경로, 실패 경로 재현을 선택한다.
- XML 리포트: 실행 결과와 실패 경로를 XML로 저장하고 확인할 수 있다.

## 설치

현재 별도 NuGet 패키지는 제공하지 않는다. 저장소를 체크아웃한 뒤 소비 프로젝트에서 `src/UniTest/UniTest.csproj`를 참조한다.

```xml
<ItemGroup>
  <ProjectReference Include="path/to/unitest/src/UniTest/UniTest.csproj" />
</ItemGroup>
```

샘플은 .NET 9 콘솔 애플리케이션이다.

```powershell
dotnet run --project samples/UniTest.Samples/UniTest.Samples.csproj
```

실행 후 `sample>` 프롬프트에서 `single`, `single-replay`, `multi`, `single-continuous`, `multi-continuous`, `help`, `exit` 중 하나를 입력한다. 리포트는 샘플 앱 출력 폴더의 `UniTest/Samples/NativeCSharp` 아래에 저장된다.

## 빠른 시작

아래 예시는 카운터의 현재 값과 기대값을 함께 들고 가며, 가능한 `Increment`와 `Decrement` 경로를 깊이 3까지 자동 확장한다.

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UniTest;

public sealed class Counter
{
    public int Value { get; private set; }

    public void Increment()
    {
        Value++;
    }

    public void Decrement()
    {
        Value--;
    }
}

public sealed class CounterModel : Model
{
    public Counter Counter
    {
        get => (Counter)Subject;
        set => Subject = value;
    }

    public int ExpectedValue;
}

public sealed class CounterProject : Project<CounterModel>
{
    public override IEnumerable<ILab<CounterModel>> CreateLabs(CounterModel model)
    {
        if (model.Counter == null)
        {
            yield return new CompactLab<CounterModel>("Ignite")
            {
                Actor = m =>
                {
                    m.Counter = new Counter();
                    m.ExpectedValue = 0;
                },
                Asserter = Check
            }.Build();

            yield break;
        }

        yield return new CompactLab<CounterModel>("Increment")
        {
            Arranger = m => m.ExpectedValue++,
            Actor = m => m.Counter.Increment(),
            Asserter = Check
        }.Build();

        yield return new CompactLab<CounterModel>("Decrement")
        {
            Arranger = m => m.ExpectedValue--,
            Actor = m => m.Counter.Decrement(),
            Asserter = Check
        }.Build();
    }

    private static void Check(CounterModel model)
    {
        if (model.Counter.Value != model.ExpectedValue)
            throw new InvalidOperationException("Counter state mismatched.");
    }
}

public static class CounterRunner
{
    public static Task<bool> RunAsync()
    {
        return new CounterProject()
            .Run(
                Path.Combine(AppContext.BaseDirectory, "UniTestReports"),
                "Counter",
                depth: 3,
                printResult: true);
    }
}
```

이 예시는 다음 흐름을 보여준다.

- `Model`은 실제 대상인 `Counter`와 기대 상태인 `ExpectedValue`를 함께 가진다.
- `CreateLabs(...)`는 시작 상태에서 `Ignite`를 만들고, 이후 상태에서 `Increment`와 `Decrement`를 만든다.
- `Run(...)`은 가능한 경로를 실행하고 XML 리포트를 출력한다.

## 주요 API

- `Model`: 테스트 대상 `Subject`, 실행 이력, 지속 가능 여부, 리포트에 남길 상태 문자열을 보관한다.
- `Project<TModel>`: 현재 `Model`에서 실행 가능한 `Lab` 목록을 생성하고 테스트 그래프를 실행한다.
- `Lab<TModel>`: metadata, 예상 예외, 지속 가능 여부를 포함한 AAA 테스트 단위이다.
- `CompactLab<TModel>`: 단순한 AAA 흐름을 delegate로 빠르게 작성하는 helper이다.
- `ILab<TModel>`: `Lab`과 `CompositeLab`이 공유하는 실행 단위 인터페이스이다.
- `TestCase`: 다중 상태 테스트에서 어떤 동작과 하위 조건을 생성할지 전달한다.
- `Merge(...)`: 상태 조건 Lab과 실제 동작 template을 하나의 Lab으로 합친다.
- `Extend(...)`: 기존 Lab 위에 다른 상태 계층의 Arrange/Assert를 덧붙인다.
- `Run(...)`: 전체 경로나 지정한 실행 이력을 실행하고 XML 리포트를 출력한다.
- `RunContinuously(...)`: 가능한 경로 중 하나를 결정적으로 선택해 긴 연속 실행을 수행한다.
- `Execute(ids)`: `/`로 구분한 실행 이력만 다시 실행한다.

## 문서

자세한 설명은 `docs/Wiki.ko` 폴더에 있다.

- [00-Unit-Test-Guideline.md](docs/Wiki.ko/00-Unit-Test-Guideline.md): 상태-동작 표 작성 기준
- [01-Overview.md](docs/Wiki.ko/01-Overview.md): 기능의 목적과 큰 흐름
- [02-Implementations.md](docs/Wiki.ko/02-Implementations.md): 구현 구조와 실행 단위
- [03-Uni-Test-Extensions.md](docs/Wiki.ko/03-Uni-Test-Extensions.md): 확장 API와 조합 방식
- [04-Usage.md](docs/Wiki.ko/04-Usage.md): 사용 예시와 호출 기준

AI 에이전트는 UniTest 기반 테스트를 작성하거나 도메인별 POCO 테스트 실행 경로를 정리할 때 `docs/Workflow.ko`의 워크플로 문서를 따른다.

- [01-Test-Authoring-Workflow.md](docs/Workflow.ko/01-Test-Authoring-Workflow.md): 테스트 작성, 계획, 결과 기록 흐름
- [02-External-NUnit-Executor-Workflow.md](docs/Workflow.ko/02-External-NUnit-Executor-Workflow.md): 도메인별 NUnit 테스트 프로젝트 구성 흐름

영어 문서는 `docs/Wiki.en` 폴더에 있다.

## 테스트

테스트 코드는 `tests` 폴더에 있으며 NUnit을 사용한다.

```powershell
dotnet test tests/UniTest.Test.UnitTest/UniTest.Test.UnitTest.csproj
dotnet test tests/UniTest.Test.RecursionTest/UniTest.Test.RecursionTest.csproj
```

## 라이선스

UniTest는 MIT 라이선스로 배포된다. 자세한 내용은 [LICENSE.md](LICENSE.md)를 참고한다.
