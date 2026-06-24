# External NUnit Executor 워크플로

이 문서는 UniTest 패키지의 External NUnit Executor를 사용하여 Unity Editor 밖에서 POCO 테스트를 빌드하고 실행하는 운영 흐름이다.

이 문서는 Unity Test Runner를 대체하기 위한 문서가 아니다. Unity 생명주기, Scene, AssetDatabase, PlayMode, Editor API에 의존하는 테스트는 Unity Test Runner 또는 Unity batchmode를 사용한다. 반대로 순수 C# 로직, NUnit Assert, Unity 의존성을 분리한 테스트는 External NUnit Executor를 통해 CLI에서 빠르게 실행한다.

---

## 목차

1. CLI 테스트 자동화의 위치
2. External NUnit Executor 구조
3. 테스트 프로젝트 구성
4. 실행 흐름
5. 운영 원칙

---

## 1. CLI 테스트 자동화의 위치

UniTest 테스트 작성 흐름에서는 먼저 테스트가 Unity 실행 환경을 실제로 필요로 하는지 구분한다.

Unity 실행 환경이 필요하지 않은 테스트는 Unity Editor를 열지 않고 CLI에서 빌드하고 실행한다. 이렇게 하면 구현자가 변경 사항을 더 빠르게 검증할 수 있고, 테스트 실패 결과를 테스트 결과 문서에 바로 연결할 수 있다.

CLI 테스트 자동화는 다음 흐름에 배치한다.

1. 상태-동작 표를 바탕으로 테스트 코드를 작성한다.
2. 테스트 코드가 Unity API 없이 컴파일될 수 있는지 확인한다.
3. `ExternalNUnitExecutor` 프로젝트에 테스트 파일을 연결한다.
4. CLI에서 테스트를 실행하고 결과를 기록한다.
5. 실패가 발생하면 원인 분석 후 구현 단계 또는 검증 단계로 되돌아간다.

---

## 2. External NUnit Executor 구조

외부 NUnit 실행기는 UniTest 패키지 아래의 `Packages/com.blackthunder.unitest/Tools~/ExternalNUnitExecutor` 폴더에 둔다.

`Assets` 아래에 두지 않는 이유는 Unity가 해당 실행기 프로젝트를 일반 Asset처럼 임포트하거나 asmdef 흐름에 섞지 않도록 하기 위해서이다. 이 실행기는 UniTest 패키지에 포함된 개발 도구이지만, Unity 런타임의 일부는 아니다.

권장 구조는 다음과 같다.

```text
Packages/
  com.blackthunder.unitest/
    Tools~/
      ExternalNUnitExecutor/
        ExternalNUnitExecutor.csproj
        ExternalNUnitExecutor.NoBlackbox.csproj
        ExternalNUnitExecutor.Usm.csproj
```

`ExternalNUnitExecutor*.csproj`는 테스트 대상 파일을 복사하지 않고 `Link`로 연결한다. 이렇게 하면 테스트 코드는 원래 위치에 남고, 외부 실행기 프로젝트는 CLI용 빌드와 테스트 실행 경로만 담당한다.

현재 이 작업공간에서는 다음 실행기 변형을 사용한다.

| 프로젝트 | 역할 |
| --- | --- |
| `ExternalNUnitExecutor.csproj` | Blackbox의 NUnit/UniTest 기반 외부 실행 |
| `ExternalNUnitExecutor.NoBlackbox.csproj` | 동일 실행 경로의 별도 비교용 구성 |
| `ExternalNUnitExecutor.Usm.csproj` | USM POCO 테스트 실행 |

---

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
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="..\..\Runtime\Node.cs" LinkBase="UniTest" />
    <Compile Include="..\..\Runtime\TestCase.cs" LinkBase="UniTest" />
    <Compile Include="..\..\Runtime\Infrastructure\*.cs" LinkBase="UniTest\Infrastructure" />
    <Compile Include="..\..\Runtime\Lab\*.cs" LinkBase="UniTest\Lab" />
    <Compile Include="..\..\Runtime\Model\*.cs" LinkBase="UniTest\Model" />
    <Compile Include="..\..\Runtime\Project\*.cs" LinkBase="UniTest\Project" />
    <Compile Include="..\..\Runtime\Tools\LabExtensions.cs" LinkBase="UniTest\Tools" />
    <Compile Include="..\..\Runtime\Tools\NodeExtensions.cs" LinkBase="UniTest\Tools" />
    <Compile Include="..\..\..\com.blackthunder.blackbox-system\Runtime\**\*.cs" LinkBase="Blackbox\Runtime" />
    <Compile Include="..\..\..\com.blackthunder.blackbox-system\Tests\**\*.cs" LinkBase="Blackbox\Tests" />
    <Reference Include="nunit.framework">
      <HintPath>..\..\..\..\Library\PackageCache\com.unity.ext.nunit@031a54704bff\net40\unity-custom\nunit.framework.dll</HintPath>
    </Reference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.6.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="6.2.0" />
  </ItemGroup>
</Project>
```

위 예시는 embedded UPM 패키지로 이동한 Blackbox 소스와 테스트를 현재 패키지 경로에서 링크하는 방식이다. UniTest 기반 테스트를 함께 실행해야 하는 경우에는 `Packages/com.blackthunder.unitest/Runtime` 아래의 필요한 런타임 파일도 같은 방식으로 링크한다.

USM처럼 별도 POCO 프레임워크를 실행하는 경우에는 테스트 대상 소스만 해당 프레임워크 경로로 바꾼다.

```xml
<Compile Include="..\..\..\..\Assets\UniEngine\Services\StateMachines\Usm\**\*.cs" LinkBase="StateMachines\Usm" />
```

NUnit framework는 Unity Test Runner와의 소스 호환성을 위해 Unity 프로젝트의 `PackageCache`에 있는 `com.unity.ext.nunit` DLL을 직접 참조한다. `NUnit` NuGet 패키지는 추가하지 않는다.

`Microsoft.NET.Test.Sdk`와 `NUnit3TestAdapter`는 `dotnet test`가 이 테스트 프로젝트를 발견하고 실행하기 위한 test platform 패키지이다. 테스트 코드가 사용하는 NUnit API의 기준은 Unity의 커스텀 NUnit DLL이며, 외부 실행기는 같은 테스트 소스를 다른 runner에서 실행하는 역할만 한다.

---

## 4. 실행 흐름

외부 실행기는 자체 `Program.cs`를 갖지 않는다. 테스트는 `NUnit3TestAdapter`가 발견하고 `dotnet test`가 실행한다.

```powershell
dotnet test Packages\com.blackthunder.unitest\Tools~\ExternalNUnitExecutor\ExternalNUnitExecutor.csproj
```

복원이 이미 끝난 상태에서 빌드와 실행만 반복해야 한다면 다음처럼 실행한다.

```powershell
dotnet test --no-restore Packages\com.blackthunder.unitest\Tools~\ExternalNUnitExecutor\ExternalNUnitExecutor.csproj
```

테스트 작성 워크플로에서는 이 실행 결과를 `03-Results.md`의 테스트 실행 결과 요약에 포함한다.

---

## 5. 운영 원칙

- 외부 실행기는 Unity Editor 실행 여부를 검증하는 도구가 아니라, Unity에 의존하지 않는 테스트 코드를 빠르게 반복 실행하기 위한 도구이다.
- 테스트 파일은 가능하면 Unity 전용 코드와 순수 .NET 코드를 명확히 분기한다.
- `ExternalNUnitExecutor`는 테스트 코드를 소유하지 않는다. 테스트 코드는 원래 모듈 또는 테스트 폴더에 남기고, 실행기 프로젝트는 해당 파일을 연결해서 실행한다.
- 테스트 코드는 Unity의 NUnit 3.5 기반 커스텀 DLL과 외부 `dotnet test` 경로가 함께 컴파일할 수 있는 공통 NUnit API만 사용한다.
- 일반 값 검증 Assert는 `Assert.That(..., Is/Has/Does/Contains...)` 중심으로 작성하되, NUnit 4 전용 API나 `NUnit.Framework.Legacy`는 사용하지 않는다.
- 예외 검증은 Unity의 NUnit 3.5 커스텀 DLL과 외부 실행기 양쪽에서 지원되는 `Assert.Throws` / `Assert.ThrowsAsync`를 허용한다.
- CLI에서 실패한 테스트는 테스트 결과 문서에 포함하고, 원인에 따라 구현 단계, 검증 단계, 또는 테스트 단계로 되돌아간다.
- Unity API, Scene, AssetDatabase, PlayMode 동작이 필요한 테스트는 이 경로로 억지로 실행하지 않고 Unity Test Runner 또는 Unity batchmode 검증으로 분리한다.
