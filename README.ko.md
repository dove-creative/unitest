# UniTest

[English README](README.md)

UniTest는 동작 순서에 따라 가능한 다음 테스트가 달라지는 시스템을 검증하기 위한 Unity/C# 테스트 프레임워크이다.

각 동작을 `Lab`으로 정의하면, UniTest가 현재 `Model` 상태와 실행 이력을 바탕으로 실행 가능한 테스트 경로를 자동으로 확장한다.

## 에이전트 보조와 CLI 자동화

UniTest는 에이전트 보조 테스트 작성과 CLI 기반 반복 검증을 같은 모델 구조 위에서 이어갈 수 있도록, 테스트 작성 흐름과 외부 실행 자동화 워크플로를 함께 제공한다.

AI 에이전트는 UniTest 기반 테스트 작성이나 CLI 반복 검증을 맡는 경우, 실제 작업 절차를 아래 [문서](#문서)의 `Documentation~/Workflow.ko` 항목에서 먼저 확인한다.

## 주요 기능

- 상태 기반 테스트 생성: `Project<TModel>.CreateLabs(...)`에서 현재 상태에 맞는 테스트 흐름을 만든다.
- AAA 실행 단위: `Lab<TModel>`과 `CompactLab<TModel>`로 Arrange, Act, Assert 흐름을 구성한다.
- 경로 확장: `Node<TModel>`가 실행 이력을 보존하고 다음 테스트 상태를 독립적으로 생성한다.
- 다중 상태 조합: `TestCase`, `Merge(...)`, `Extend(...)`로 여러 상태 축을 계층적으로 결합한다.
- 재실행과 연속 실행: `Run(...)`, `RunContinuously(...)`, `Execute(ids)`로 전체 경로, 긴 단일 경로, 실패 경로 재현을 선택한다.
- XML 리포트: 실행 결과와 실패 경로를 XML로 저장하고 확인할 수 있다.

## 설치

UniTest는 Unity 프로젝트에서는 폴더형 Unity 패키지로 사용할 수 있고, Native C# 프로젝트에서는 런타임 소스를 직접 포함해 사용할 수 있다.

### Unity에서 사용

Package Manager의 `Add package from git URL`에 아래 주소를 입력해 설치할 수 있다.

```text
https://github.com/dove-creative/unitest.git#v0.1.0
```

로컬 개발이나 embedded package 사용이 필요하면 아래처럼 직접 배치한다.

1. 이 폴더를 Unity 프로젝트의 `Packages/com.blackthunder.unitest` 위치에 둔다.
2. Player Settings의 Scripting Define Symbols에 `UNITEST`를 추가한다.
3. 테스트 어셈블리나 샘플 어셈블리에서 `UniTest` asmdef를 참조한다.

`UniTest` 런타임 asmdef는 `UNITEST` define constraint를 사용한다. 이 심볼이 없으면 `Project<TModel>`, `Model`, `Lab<TModel>` 같은 핵심 타입이 컴파일되지 않는다.

Unity 샘플은 Package Manager의 `Samples` 영역에서 `Unity Usage`를 import하거나, 패키지 내부 `Samples~/Unity`에서 확인할 수 있다.

### Native C#에서 사용

현재 별도 NuGet 패키지는 제공하지 않는다. Native C# 프로젝트에서는 이 패키지 폴더를 소스 의존성으로 두고, `Runtime` 소스를 컴파일에 포함한다.

```xml
<ItemGroup>
  <Compile Include="path/to/com.blackthunder.unitest/Runtime/Infrastructure/**/*.cs" LinkBase="UniTest/Infrastructure" />
  <Compile Include="path/to/com.blackthunder.unitest/Runtime/Lab/**/*.cs" LinkBase="UniTest/Lab" />
  <Compile Include="path/to/com.blackthunder.unitest/Runtime/Model/**/*.cs" LinkBase="UniTest/Model" />
  <Compile Include="path/to/com.blackthunder.unitest/Runtime/Project/**/*.cs" LinkBase="UniTest/Project" />
  <Compile Include="path/to/com.blackthunder.unitest/Runtime/Tools/**/*.cs" LinkBase="UniTest/Tools" />
  <Compile Include="path/to/com.blackthunder.unitest/Runtime/Node.cs" Link="UniTest/Node.cs" />
  <Compile Include="path/to/com.blackthunder.unitest/Runtime/TestCase.cs" Link="UniTest/TestCase.cs" />
</ItemGroup>
```

Native C# 샘플은 Unity API 없이 실행된다.

```powershell
cd Samples~/NativeCSharp
dotnet run --project UniTest.NativeCSharp.Samples.csproj
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

Unity 프로젝트에서는 같은 패턴을 MonoBehaviour나 Editor 테스트 진입점에서 호출할 수 있다.

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

자세한 설명은 `Documentation~/Wiki.ko` 폴더에 있다.

- [00-Unit-Test-Guideline.md](Documentation~/Wiki.ko/00-Unit-Test-Guideline.md): 상태-동작 표 작성 기준
- [01-Overview.md](Documentation~/Wiki.ko/01-Overview.md): 기능의 목적과 큰 흐름
- [02-Implementations.md](Documentation~/Wiki.ko/02-Implementations.md): 구현 구조와 실행 단위
- [03-Uni-Test-Extensions.md](Documentation~/Wiki.ko/03-Uni-Test-Extensions.md): 확장 API와 조합 방식
- [04-Usage.md](Documentation~/Wiki.ko/04-Usage.md): 사용 예시와 호출 기준

AI 에이전트는 UniTest 기반 테스트를 작성하거나 도메인별 Unity 밖 POCO 테스트 실행 경로를 정리할 때 `Documentation~/Workflow.ko`의 워크플로 문서를 따른다. 먼저 테스트 작성 모드와 문서 기록 흐름을 확인하고, Unity 실행 환경이 필요 없는 테스트에 한해 External NUnit Executor 흐름을 적용한다.

- [01-Test-Authoring-Workflow.md](Documentation~/Workflow.ko/01-Test-Authoring-Workflow.md): 테스트 작성, 계획, 결과 기록 흐름
- [02-External-NUnit-Executor-Workflow.md](Documentation~/Workflow.ko/02-External-NUnit-Executor-Workflow.md): 도메인별 외부 NUnit 실행기 구성 흐름

영어 문서는 `Documentation~/Wiki.en` 폴더에 있다.

## 테스트

테스트 코드는 `Tests` 폴더에 있으며, Unity Test Framework와 NUnit을 사용한다.

Unity에서 패키지 자체 테스트를 실행하려면 `UNITEST`, `UNITY_INCLUDE_TESTS`와 함께 대상 테스트 asmdef에 맞는 `UNITEST_TEST_UT` 또는 `UNITEST_TEST_RT` 심볼을 활성화한다. 패키지 형태로 분리해 사용하는 경우 Unity 프로젝트의 testables 설정과 테스트 asmdef 참조도 함께 확인한다.

## 라이선스

UniTest는 MIT 라이선스로 배포된다. 자세한 내용은 [LICENSE.md](LICENSE.md)를 참고한다.
