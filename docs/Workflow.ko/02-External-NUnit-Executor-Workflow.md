# External NUnit Executor 워크플로

이 문서는 각 도메인 또는 프레임워크가 자기 테스트 폴더에 .NET NUnit 프로젝트를 구성하고 CLI에서 반복 실행하는 운영 흐름이다.

## 목차

1. CLI 테스트 자동화의 위치
2. 테스트 프로젝트 소유권
3. 테스트 프로젝트 구성
4. 실행 흐름
5. 운영 원칙

## 1. CLI 테스트 자동화의 위치

CLI 테스트 자동화는 다음 흐름에 배치한다.

1. 상태-동작 표를 바탕으로 테스트 코드를 작성한다.
2. 테스트 대상과 테스트 코드를 SDK 프로젝트로 빌드할 수 있는지 확인한다.
3. 해당 도메인 또는 프레임워크의 `tests` 폴더에 NUnit 프로젝트를 둔다.
4. 테스트 대상과 UniTest를 `ProjectReference`로 연결한다.
5. CLI에서 테스트를 실행하고 결과를 기록한다.
6. 실패가 발생하면 원인을 분석한 뒤 구현 또는 검증 단계로 되돌아간다.

## 2. 테스트 프로젝트 소유권

외부 테스트 프로젝트는 테스트 대상 도메인 또는 프레임워크가 소유한다. UniTest는 테스트 실행 모델과 런타임 API를 제공하지만 다른 프레임워크의 실행기를 이 저장소 안에 두지 않는다.

기본 배치는 다음과 같다.

```text
domain-framework/
  src/
    Domain/
      Domain.csproj
  tests/
    Domain.Tests/
      Domain.Tests.csproj
```

테스트 코드와 프로젝트 파일은 같은 폴더에 둔다. 여러 테스트 집합에 같은 전역 타입 이름이 있다면 별도 테스트 프로젝트로 분리한다.

## 3. 테스트 프로젝트 구성

기본 프로젝트 구성은 다음과 같다.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NUnit" Version="3.14.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.6.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="6.2.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Domain/Domain.csproj" />
    <ProjectReference Include="path/to/unitest/src/UniTest/UniTest.csproj" />
  </ItemGroup>
</Project>
```

`Microsoft.NET.Test.Sdk`와 `NUnit3TestAdapter`는 `dotnet test`가 테스트를 발견하고 실행하기 위한 패키지이다. 이 저장소는 복원된 DLL을 vendoring하지 않고 프로젝트 참조만 선언한다.

## 4. 실행 흐름

테스트는 `NUnit3TestAdapter`가 발견하고 `dotnet test`가 실행한다.

```powershell
dotnet restore tests/Domain.Tests/Domain.Tests.csproj
dotnet test tests/Domain.Tests/Domain.Tests.csproj --no-restore
```

테스트 작성 워크플로에서는 실행 결과를 `03-Results.md`의 테스트 실행 결과 요약에 포함한다.

## 5. 운영 원칙

- 테스트 프로젝트는 테스트 대상 도메인 또는 프레임워크의 `tests` 폴더가 소유한다.
- 런타임 소스를 복제하지 않고 `ProjectReference`로 연결한다.
- 일반 값 검증은 `Assert.That(..., Is/Has/Does/Contains...)` 중심으로 작성한다.
- NUnit 4 전용 API나 `NUnit.Framework.Legacy`는 사용하지 않는다.
- 예외 검증에는 `Assert.Throws` 또는 `Assert.ThrowsAsync`를 사용한다.
- CLI에서 실패한 테스트는 결과 문서에 포함하고 원인에 따라 구현, 검증 또는 테스트 단계로 되돌아간다.
