# External NUnit Executor 워크플로

이 문서는 Unity Editor 밖에서 POCO 테스트를 빌드하고 실행하기 위해 각 도메인 또는 프레임워크가 자기 테스트 폴더에 External NUnit Executor를 구성하는 운영 흐름이다.

이 문서는 Unity Test Runner를 대체하기 위한 문서가 아니다. Unity 생명주기, Scene, AssetDatabase, PlayMode, Editor API에 의존하는 테스트는 Unity Test Runner 또는 Unity batchmode를 사용한다. 반대로 순수 C# 로직, NUnit Assert, Unity 의존성을 분리한 테스트는 도메인 소유 external test project를 통해 CLI에서 빠르게 실행한다.

## 목차

1. CLI 테스트 자동화의 위치
2. External NUnit Executor 소유권
3. 테스트 프로젝트 구성
4. 실행 흐름
5. 운영 원칙

## 1. CLI 테스트 자동화의 위치

UniTest 테스트 작성 흐름에서는 먼저 테스트가 Unity 실행 환경을 실제로 필요로 하는지 구분한다.

Unity 실행 환경이 필요하지 않은 테스트는 Unity Editor를 열지 않고 CLI에서 빌드하고 실행한다. 이렇게 하면 구현자가 변경 사항을 더 빠르게 검증할 수 있고, 테스트 실패 결과를 테스트 결과 문서에 바로 연결할 수 있다.

CLI 테스트 자동화는 다음 흐름에 배치한다.

1. 상태-동작 표를 바탕으로 테스트 코드를 작성한다.
2. 테스트 코드가 Unity API 없이 컴파일될 수 있는지 확인한다.
3. 해당 도메인 또는 프레임워크의 테스트 폴더에 external test project를 둔다.
4. 필요한 런타임 소스와 테스트 소스를 external test project에 `Link`로 연결한다.
5. CLI에서 테스트를 실행하고 결과를 기록한다.
6. 실패가 발생하면 원인 분석 후 구현 단계 또는 검증 단계로 되돌아간다.

## 2. External NUnit Executor 소유권

External NUnit Executor는 테스트 대상 도메인 또는 프레임워크가 소유한다.

UniTest 패키지는 테스트 실행 모델과 런타임 API를 제공하지만, 다른 프레임워크의 외부 실행기를 UniTest 패키지 안에 두지 않는다. 특정 도메인이 UniTest를 사용해 자기 테스트를 실행해야 한다면, 해당 도메인의 `Tests` 폴더 아래에 별도 external test project를 둔다.

기본 배치는 다음과 같다.

```text
Packages/
  domain-package/
    Tests/
      ExternalNUnitExecutor~/
        ExternalNUnitExecutor.csproj
```

`ExternalNUnitExecutor.csproj`는 테스트 대상 파일을 복사하지 않고 `Link`로 연결한다. 이렇게 하면 테스트 코드는 원래 위치에 남고, 외부 실행기 프로젝트는 CLI용 빌드와 테스트 실행 경로만 담당한다.

외부 실행 폴더는 Unity가 import하지 않도록 `~` 접미사를 붙여 `ExternalNUnitExecutor~`로 둔다. 프로젝트 파일명은 `ExternalNUnitExecutor.csproj`를 유지하고, 폴더 `.meta`나 `.csproj.meta`는 만들지 않는다.

## 3. 테스트 프로젝트 구성

외부 실행기 프로젝트는 Unity 관련 define을 켜지 않는다. 따라서 테스트 파일 안의 Unity 전용 코드는 `UNITY_2017_1_OR_NEWER` 같은 Unity define 분기 안에 있어야 하며, Unity가 없는 분기에서는 순수 .NET API만 사용해야 한다.

예를 들어 `Console.WriteLine`을 사용하는 테스트 파일은 Unity 밖에서도 컴파일될 수 있도록 `using System;`을 명시하거나 `System.Console.WriteLine(...)` 형태로 작성한다.

기본 프로젝트 구성은 다음 흐름을 따른다.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>disable</Nullable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <IsTestProject>true</IsTestProject>
    <DefineConstants>DOMAIN_TESTS;UNITEST;UNITEST_SINGLETHREAD</DefineConstants>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="..\..\Runtime\**\*.cs" LinkBase="Domain\Runtime" />
    <Compile Include="..\*.cs" LinkBase="Domain\Tests" />
    <Compile Include="..\UniTest\*.cs" LinkBase="Domain\Tests\UniTest" />
    <Compile Include="..\..\..\com.blackthunder.unitest\Runtime\Node.cs" LinkBase="UniTest" />
    <Compile Include="..\..\..\com.blackthunder.unitest\Runtime\TestCase.cs" LinkBase="UniTest" />
    <Compile Include="..\..\..\com.blackthunder.unitest\Runtime\Infrastructure\*.cs" LinkBase="UniTest\Infrastructure" />
    <Compile Include="..\..\..\com.blackthunder.unitest\Runtime\Lab\*.cs" LinkBase="UniTest\Lab" />
    <Compile Include="..\..\..\com.blackthunder.unitest\Runtime\Model\*.cs" LinkBase="UniTest\Model" />
    <Compile Include="..\..\..\com.blackthunder.unitest\Runtime\Project\*.cs" LinkBase="UniTest\Project" />
    <Compile Include="..\..\..\com.blackthunder.unitest\Runtime\Tools\NodeExtensions.cs" LinkBase="UniTest\Tools" />
    <PackageReference Include="NUnit" Version="3.14.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.6.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="6.2.0" />
  </ItemGroup>
</Project>
```

위 예시는 도메인 패키지의 런타임과 테스트 소스, 그리고 sibling 패키지인 `com.blackthunder.unitest`의 런타임 소스를 함께 링크하는 방식이다. 실제 경로, define, 테스트 폴더 이름은 도메인 패키지 구조에 맞게 조정한다.

NUnit framework는 NuGet `NUnit` 3.14.0 패키지로 복원한다. 따라서 External NUnit Executor는 Unity Editor와 Unity 프로젝트의 `Library` 캐시 없이도 .NET SDK와 NuGet 복원 환경만 있으면 빌드할 수 있다.

`Microsoft.NET.Test.Sdk`와 `NUnit3TestAdapter`는 `dotnet test`가 이 테스트 프로젝트를 발견하고 실행하기 위한 test platform 패키지이다. 테스트 코드가 사용하는 NUnit API의 기준은 Unity Test Runner와 NuGet NUnit 3.14.0이 함께 지원하는 공통 API이며, 외부 실행기는 같은 테스트 소스를 Unity 밖의 runner에서 실행하는 역할만 한다.

`NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`는 NuGet에서 복원되는 MIT 라이선스 패키지이다. 이 저장소는 해당 DLL을 vendoring하지 않고 프로젝트 참조만 선언한다.

## 4. 실행 흐름

외부 실행기는 자체 `Program.cs`를 갖지 않는다. 테스트는 `NUnit3TestAdapter`가 발견하고 `dotnet test`가 실행한다.

```powershell
dotnet test Packages\domain-package\Tests\ExternalNUnitExecutor~\ExternalNUnitExecutor.csproj
```

복원이 이미 끝난 상태에서 빌드와 실행만 반복해야 한다면 다음처럼 실행한다.

```powershell
dotnet test --no-restore Packages\domain-package\Tests\ExternalNUnitExecutor~\ExternalNUnitExecutor.csproj
```

테스트 작성 워크플로에서는 이 실행 결과를 `03-Results.md`의 테스트 실행 결과 요약에 포함한다.

## 5. 운영 원칙

- 외부 실행기는 Unity Editor 실행 여부를 검증하는 도구가 아니라, Unity에 의존하지 않는 테스트 코드를 빠르게 반복 실행하기 위한 도구이다.
- 테스트 파일은 가능하면 Unity 전용 코드와 순수 .NET 코드를 명확히 분기한다.
- `ExternalNUnitExecutor~` 폴더는 테스트 코드를 소유하지 않는다. 테스트 코드는 원래 모듈 또는 테스트 폴더에 남기고, 실행기 프로젝트는 해당 파일을 연결해서 실행한다.
- UniTest 패키지는 다른 도메인의 external test project를 소유하지 않는다.
- 도메인별 external test project는 각 도메인 또는 프레임워크의 `Tests` 폴더가 소유한다.
- 외부 실행기는 NuGet NUnit 3.14.0으로 실행하지만, 테스트 코드는 Unity의 NUnit 3.5 기반 커스텀 DLL에서도 컴파일할 수 있는 공통 NUnit API만 사용한다.
- 일반 값 검증 Assert는 `Assert.That(..., Is/Has/Does/Contains...)` 중심으로 작성하되, NUnit 4 전용 API나 `NUnit.Framework.Legacy`는 사용하지 않는다.
- 예외 검증은 Unity NUnit 3.5와 NuGet NUnit 3.14.0 양쪽에서 지원되는 `Assert.Throws` / `Assert.ThrowsAsync`를 허용한다.
- CLI에서 실패한 테스트는 테스트 결과 문서에 포함하고, 원인에 따라 구현 단계, 검증 단계, 또는 테스트 단계로 되돌아간다.
- Unity API, Scene, AssetDatabase, PlayMode 동작이 필요한 테스트는 이 경로로 억지로 실행하지 않고 Unity Test Runner 또는 Unity batchmode 검증으로 분리한다.
