# UniTest

[English](README.md)

UniTest는 모델의 상태와 가능한 동작을 조합해 테스트 경로를 생성하고 검증하는 .NET 테스트 프레임워크이다.

개별 경로를 일일이 나열하는 대신 현재 상태에서 가능한 동작과 기대 결과를 정의하면, 실행 이력을 바탕으로 다음 상태를 탐색한다.

## 주요 기능

- 상태 기반 경로 생성: 현재 Model에서 실행할 수 있는 Lab을 만들어 후속 상태를 탐색한다.
- AAA 검증: Arrange·Act·Assert로 준비, 실제 동작과 결과 검증을 구분한다.
- 독립된 상태 조합: TestCase, Merge와 Extend로 여러 상태 축의 테스트를 조합한다.
- 실패 재현: 저장한 Lab ID 경로를 다시 실행하거나 분리된 디버그 노드에서 실패 상황을 조사한다.
- 연속 실행과 JSON 리포트: 긴 실행 경로를 검사하고 실행 후 독립된 진단 스냅샷을 저장한다.

## 요구 사항과 설치

라이브러리는 `netstandard2.1`을 대상으로 하며 외부 런타임 패키지 의존성이 없다. 저장소의 빌드·테스트·샘플 실행에는 .NET 10 SDK가 필요하다. 모든 프로젝트는 C# 9를 사용한다.

소비 프로젝트는 검증된 `BlackThunder.UniTest.0.4.0.nupkg`를 자신의 `packages` 폴더에 보관한다. 아래 내용을 저장소 루트 `NuGet.config`에 넣고 패키지를 추가한다. 배포 방식은 저장소에 보관하는 로컬 패키지이며 공개 NuGet 피드에는 게시하지 않는다.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

```powershell
dotnet add package BlackThunder.UniTest --version "[0.4.0]"
```

패키지 ID, 네임스페이스와 DLL 이름은 각각 `BlackThunder.UniTest`, `BlackThunder.UniTest`, `BlackThunder.UniTest.dll`이다. 이전 버전 사용자는 [0.4.0 이전 안내](docs/Wiki.ko/05-Migration.md)를 따른다. DLL을 직접 사용하는 호스트에는 `lib/netstandard2.1`의 DLL을 전달한다.

## 빠른 시작

다음 코드를 패키지를 참조하는 콘솔 프로젝트에서 실행한다. 실제 Counter와 예상 값을 가진 Model을 만들고 Increment 경로를 깊이 3까지 검증한다.

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlackThunder.UniTest;

namespace Example.CounterTests
{
    public class Counter
    {
        public int Value { get; private set; }
        public void Increment() => Value++;
    }

    public class CounterModel : Model
    {
        public Counter Counter => (Counter)Subject;
        public int ExpectedValue;

        public CounterModel() => Subject = new Counter();
    }

    public class CounterProject : CompactProject<CounterModel>
    {
        public override IEnumerable<CompactLab<CounterModel>> CreateCompactLabs(CounterModel model)
        {
            yield return new CompactLab<CounterModel>("Increment",
                arranger: current => current.ExpectedValue++,
                actor: current => current.Counter.Increment(),
                asserter: current =>
                {
                    if (current.Counter.Value != current.ExpectedValue)
                        throw new InvalidOperationException("Counter state mismatched.");
                });
        }
    }

    public static class Program
    {
        public static async Task Main()
        {
            var root = await new CounterProject().Execute(3);
            Console.WriteLine(root.GetReport().ToJson());
        }
    }
}
```

여러 동작과 상태 조합을 포함한 실행 예제는 [샘플 안내](samples/README.md)를 따른다.

```powershell
dotnet run --project samples/BlackThunder.UniTest.Samples.csproj
dotnet run --project samples/BlackThunder.UniTest.Samples.csproj -- single
```

인자가 없으면 `sample>` 프롬프트를 열고, 명령 인자가 있으면 한 번 실행한 뒤 종료한다. 리포트는 샘플 실행 파일 옆의 `SampleOutput`에 저장된다.

## 주요 API

- `Model`: 실제 대상, 기대 상태와 실행 이력을 보관한다.
- `Project<TModel>`·`CompactProject<TModel>`: 상태별 Lab을 생성하고 경로를 실행한다.
- `Lab<TModel>`·`CompactLab<TModel>`·`CompositeLab<TModel>`: 준비·동작·검증을 정의하고 조합한다.
- `TestCase`·`Merge`·`Extend`: 상태 축의 조건과 동작을 결합한다.
- `Execute`·`ExecuteContinuously`: 전체 경로, ID 경로 또는 연속 경로를 실행한다.
- `GetReport`·`GetFailedReports`·`ExportJson`: 실행 결과를 조회하고 JSON으로 저장한다.

## 문서

- [테스트 방법론](docs/Wiki.ko/00-Unit-Test-Guideline.md): 상태-동작 표와 테스트 범위
- [개요](docs/Wiki.ko/01-Overview.md): 목적, 핵심 구조와 대표 흐름
- [구현](docs/Wiki.ko/02-Implementations.md): 구성 요소의 책임과 실행·종료 처리
- [확장 기능](docs/Wiki.ko/03-Uni-Test-Extensions.md): 실패 중단, 재실행과 연속 실행
- [사용법](docs/Wiki.ko/04-Usage.md): API 선택과 단일·다중 상태 튜토리얼
- [테스트 작성 워크플로](docs/Workflow.ko/01-Test-Authoring-Workflow.md): 에이전트 보조 작성과 결과 기록
- [외부 NUnit 실행](docs/Workflow.ko/02-External-NUnit-Executor-Workflow.md): 소비 프로젝트의 CLI 검증
- [기여 지침](CONTRIBUTING.md): 공통 컨벤션, 개발 환경과 패키지 검증

## 테스트

```powershell
dotnet restore BlackThunder.UniTest.sln
dotnet build BlackThunder.UniTest.sln --configuration Release --no-restore -m:1
dotnet test BlackThunder.UniTest.sln --configuration Release --no-build --no-restore -m:1
pwsh -File .github/scripts/verify-samples.ps1
```

[CI](.github/workflows/ci.yml)는 Windows와 Ubuntu에서 같은 빌드·전체 테스트·샘플 CLI 검증을 실행한다. TRX 결과는 7일간 보관하며 패키지는 배포하지 않는다.

## 라이선스

MIT 라이선스로 배포된다. 자세한 내용은 [LICENSE.md](LICENSE.md)를 참고한다.
