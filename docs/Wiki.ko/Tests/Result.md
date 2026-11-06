# UniTest 테스트 구성과 결과

이 문서는 일반 단위 테스트와 UniTest 기반 자체 테스트의 구성, 대상별 문서와 전체 실행 결과를 연결하는 공통 기록이다.
모듈별 원본 산출물은 15개 모듈 폴더에 있고, 각 결과 문서가 해당 모듈의 최신 TRX와 로그를 연결한다. 이전 통합 산출물 28개는 삭제했으며 아래 과거 결과는 문서 이력으로 유지한다.
일반 테스트는 대상 API를 독립적으로 검증하고, 자체 테스트는 UniTest의 Project와 Lab으로 실행 경로를 만들어 프레임워크를 검증한다.
두 프로젝트 모두 NUnit이 테스트 발견, 실행 완료와 최종 성공·실패 판정을 담당한다.

테스트에서 확인한 문제와 처리 결과는 각 대상의 `03. Results.md`에 둔다.

## 목차

- 문서 구성
    - 일반 단위 테스트
    - UniTest 기반 자체 테스트

- 실행
- 확인된 결과
    - 공개 API 정리 후
    - Lab 생성 전용 전환 후
    - 스택 추적과 매개변수 이름 수정 후
    - 게시 전 검증
    - 예상 예외 판정 수정 후
    - 모듈별 실행과 직접 결과 보관
    - 신규 모듈 자체 테스트 추가 후
    - 기존 테스트 보강 후
    - 결과 저장 위치 확인
    - 일반 테스트 문서 분리와 공통 결과 경로 검증
    - 실행 범위별 산출물 배치

## 문서 구성

테스트 코드는 기존 프로젝트에 유지하고 상태표·계획·결과 문서를 다음과 같이 나눈다.

| 구분 | 코드 프로젝트 | 문서 위치 |
| --- | --- | --- |
| 일반 단위 테스트 | `tests/BlackThunder.UniTest.Tests.Unit` | `Unit/<대상>` |
| UniTest 기반 자체 테스트 | `tests/BlackThunder.UniTest.Tests.Recursion` | `UniTest/<대상>` |
| 전체 실행 요약 | 두 프로젝트의 통합 실행 | 이 문서 |
| 통합 검증용 프로젝트 전체 TRX | 일반 테스트 / 자체 테스트 | `Unit/` / `UniTest/` 바로 아래 |
| 모듈 단독 TRX·로그 | 실제 실행한 모듈 | `Unit/<대상>/` / `UniTest/<대상>/` 문서 옆 |
| 전체 빌드·통합 실행 로그 | 두 프로젝트 공통 | 이 문서 옆 |

아래 실행 사례 수는 2026-11-07 모듈별 재실행의 TRX를 기준으로 하며, 기존 전체 실행과 동일한 사례 집합을 확인했다.
NUnit 매개변수 전개 후의 실행 사례 수이며, 상태표의 계약 그룹 수나 UniTest가 생성한 내부 경로 수와는 다르다.
표와 계획은 이미 구현된 테스트를 설명하며, 각 결과 문서는 실제 모듈별 재실행과 이전 기록을 구분한다.

### 일반 단위 테스트

| 대상 | 테스트 파일 | 실행 사례 수 | 상태표 | 계획 | 결과 |
| --- | --- | --- | --- | --- | --- |
| Model | ModelTest | 36 | [표](Unit/Model/01.%20Tables.md) | [계획](Unit/Model/02.%20Plans.md) | [결과](Unit/Model/03.%20Results.md) |
| Lab | LabTest, LabExtensionTest, ToUncontinuableTest | 173 | [표](Unit/Lab/01.%20Tables.md) | [계획](Unit/Lab/02.%20Plans.md) | [결과](Unit/Lab/03.%20Results.md) |
| CompositeLab | CompositeLabTest | 174 | [표](Unit/CompositeLab/01.%20Tables.md) | [계획](Unit/CompositeLab/02.%20Plans.md) | [결과](Unit/CompositeLab/03.%20Results.md) |
| TestCase | TestCaseTest | 27 | [표](Unit/TestCase/01.%20Tables.md) | [계획](Unit/TestCase/02.%20Plans.md) | [결과](Unit/TestCase/03.%20Results.md) |
| Node | NodeTest, NodeExtensionTest | 48 | [표](Unit/Node/01.%20Tables.md) | [계획](Unit/Node/02.%20Plans.md) | [결과](Unit/Node/03.%20Results.md) |
| Project | ProjectTest, ProjectSchedulingTest, ProjectShutdownTest, ExecutionModeTest | 63 | [표](Unit/Project/01.%20Tables.md) | [계획](Unit/Project/02.%20Plans.md) | [결과](Unit/Project/03.%20Results.md) |
| TestDesigner | TestDesignerTest | 43 | [표](Unit/TestDesigner/01.%20Tables.md) | [계획](Unit/TestDesigner/02.%20Plans.md) | [결과](Unit/TestDesigner/03.%20Results.md) |
| TestExecutor | TestExecutorTest | 24 | [표](Unit/TestExecutor/01.%20Tables.md) | [계획](Unit/TestExecutor/02.%20Plans.md) | [결과](Unit/TestExecutor/03.%20Results.md) |
| Reporting | JsonReportTest | 16 | [표](Unit/Reporting/01.%20Tables.md) | [계획](Unit/Reporting/02.%20Plans.md) | [결과](Unit/Reporting/03.%20Results.md) |

일반 테스트는 15개 테스트 파일의 604개 실행 사례로 구성된다.
객체 간 연동 사례는 해당 테스트 파일을 소유한 문서에 한 번만 집계한다.
예를 들어 `ToUncontinuableTest`의 CompositeLab·Project 연동 사례도 Lab의 173개에 포함한다.

### UniTest 기반 자체 테스트

| 대상 | 검증 범위 | 실행 사례 수 | 상태표 | 계획 | 결과 |
| --- | --- | --- | --- | --- | --- |
| Model | 0 경계와 종료 상태, 상태별 후보·실행 수, 경로 재현 | 10 | [표](UniTest/Model/01.%20Tables.md) | [계획](UniTest/Model/02.%20Plans.md) | [결과](UniTest/Model/03.%20Results.md) |
| Lab | 기존 시나리오의 실행 누락 방지와 실제 반복 실행 | 12 | [표](UniTest/Lab/01.%20Tables.md) | [계획](UniTest/Lab/02.%20Plans.md) | [결과](UniTest/Lab/03.%20Results.md) |
| Node | 상태 전이, 분기 분리, 실패·취소, 반복 복원과 원본 보존 | 11 | [표](UniTest/Node/01.%20Tables.md) | [계획](UniTest/Node/02.%20Plans.md) | [결과](UniTest/Node/03.%20Results.md) |
| TestCase | 연속 조건 조합, 유한 집합 기준 모델, 이전 값 보존과 경계 입력 | 10 | [표](UniTest/TestCase/01.%20Tables.md) | [계획](UniTest/TestCase/02.%20Plans.md) | [결과](UniTest/TestCase/03.%20Results.md) |
| CompositeLab | 호출 순서·실패·예상 예외·제한 결합과 Extend 보존 | 6 | [표](UniTest/CompositeLab/01.%20Tables.md) | [계획](UniTest/CompositeLab/02.%20Plans.md) | [결과](UniTest/CompositeLab/03.%20Results.md) |
| Project | 전체 탐색·경로 재현·연속 실행, 실패와 단일 사용 수명 | 2 | [표](UniTest/Project/01.%20Tables.md) | [계획](UniTest/Project/02.%20Plans.md) | [결과](UniTest/Project/03.%20Results.md) |

자체 테스트는 51개 실행 사례이다. 두 실행 모드에 같은 계약을 적용하며, 외부 테스트의 성공 여부뿐 아니라 내부 경로·동작·시나리오 수를 확인한다.
잘못된 기대값 대조군과 의도적인 대상 실패도 사용한다. 난수 시드는 고정하며, 연속 실행은 제한된 깊이의 전체 탐색과 명시적 경로 재현을 보완한다.

Model의 기존 M02·M03과 Node의 N14는 일반 테스트 문서로 분리했다.
Model의 M01은 `Tests.Recursion`의 `ZeroRemainingCountReplay` 자체 테스트이므로 UniTest 문서에 유지한다.
세부 기록은 각 대상 문서를 참고한다.

## 실행

모듈을 검증하고 보고할 때는 해당 fixture를 필터링하고, TRX와 로그를 그 모듈 문서 옆에 저장한다.
여러 fixture를 가진 모듈은 소속 fixture를 모두 선택한다.
각 대상의 `03. Results.md`에서 최근 모듈 실행 결과와 실제 명령을 확인할 수 있다.

다음은 일반 Lab의 세 fixture를 실행하는 예시이다.

```powershell
$testRunStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
dotnet test tests/BlackThunder.UniTest.Tests.Unit/BlackThunder.UniTest.Tests.Unit.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~BlackThunder.UniTest.Tests.Unit.LabTest|FullyQualifiedName~BlackThunder.UniTest.Tests.Unit.LabExtensionTest|FullyQualifiedName~BlackThunder.UniTest.Tests.Unit.ToUncontinuableTest' --results-directory docs/Wiki.ko/Tests/Unit/Lab --logger "trx;LogFileName=$testRunStamp-unit-lab.trx" 2>&1 | Tee-Object -FilePath "docs/Wiki.ko/Tests/Unit/Lab/$testRunStamp-unit-lab.log"
```

이 예시는 현재 소스의 Release 빌드가 준비된 경우에 사용한다.
전체 프로젝트를 통합 검증할 때는 다음 명령을 사용한다.

```powershell
dotnet build BlackThunder.UniTest.sln --configuration Release --no-restore -m:1
dotnet test BlackThunder.UniTest.sln --configuration Release --no-build --no-restore -m:1 --logger trx
```

통합 실행의 프로젝트 전체 TRX는 일반 테스트가 `Unit/`, 자체 테스트가 `UniTest/` 바로 아래에 저장한다.
전체 빌드·통합 실행 로그는 이 문서 옆에 두며, 이 공유 파일들은 전체 실행 범위의 근거로 관리한다.
모듈별 검증 결과는 각 모듈의 TRX·로그를 직접 연결하며 전체 파일 링크만으로 대체하지 않는다.
별도 산출물 폴더는 만들지 않고 날짜와 목적을 파일명으로 구분한다.
생성 TRX·로그는 Git 추적 대상이 아니다.

아래 과거 실행 수치와 명령은 문서 이력이다. 이전 전체 TRX·통합 로그는 삭제했으며, 현재 검증의 원본은 각 모듈의 최신 결과 문서에서 연결한다.
자체 실행기와 대상이 공유하는 오류를 놓칠 수 있으므로 일반 단위 테스트를 함께 유지한다.
작업자 경쟁 조건, 종료 대기와 직렬화의 세부 경계 조건은 해당 일반 테스트 문서에서 관리한다.

## 확인된 결과

### 공개 API 정리 후

2026-11-07 04:57 한국 시간에 다음을 수정한 뒤 Release 빌드와 두 프로젝트 전체 테스트를 실제로 재실행했다.
Release 빌드는 경고 0개·오류 0개로 완료했다.

- Lab.MetadataTemplate을 읽기 전용 인터페이스 IReadOnlySubjectMetadata로 공개하고, SubjectMetadata의 필드를 속성으로 바꿨다.
- Node.Afters를 읽기 전용 목록으로 공개했다.
- 사용처가 없는 런타임 ProbeException을 제거했다. 두 테스트 프로젝트는 각자 정의한 같은 이름의 형식을 사용한다.
- Project.DividingLine을 internal로 바꿨다. 라이브러리 안의 ProjectRunner도 사용하므로 private으로는 바꿀 수 없다.
- 테스트 메서드 이름의 Execption 오타 15개를 고쳤다.

| 프로젝트 | 전체 | 통과 | 실패 | 건너뜀 |
| --- | --- | --- | --- | --- |
| Tests.Unit | 604 | 604 | 0 | 0 |
| Tests.Recursion | 51 | 51 | 0 | 0 |
| 합계 | 655 | 655 | 0 | 0 |

변경 범위의 4개 모듈은 따로 필터링하여 실행하고 결과를 각 모듈 폴더에 저장했다.

| 구분 | 모듈 | 전체 | 통과 | 실패 | 모듈별 결과 |
| --- | --- | --- | --- | --- | --- |
| Unit | Lab | 173 | 173 | 0 | [기록](Unit/Lab/03.%20Results.md) |
| Unit | Node | 48 | 48 | 0 | [기록](Unit/Node/03.%20Results.md) |
| Unit | TestDesigner | 43 | 43 | 0 | [기록](Unit/TestDesigner/03.%20Results.md) |
| Unit | TestExecutor | 24 | 24 | 0 | [기록](Unit/TestExecutor/03.%20Results.md) |

샘플은 help와 다섯 시나리오를 직접 실행하여 모두 종료 코드 0이었다.
보고서의 항목 수는 이전 실행과 같고(11,551·422·1,010·137·4개), 모든 비루트 항목이 Success였다.
샘플 검증 스크립트 전체는 이번에 다시 실행하지 않았으며 CI에서 확인한다.

```powershell
$testRunStamp = '20261107-045706'
dotnet build BlackThunder.UniTest.sln --configuration Release --no-restore -m:1
dotnet test tests/BlackThunder.UniTest.Tests.Unit/BlackThunder.UniTest.Tests.Unit.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=$testRunStamp-unit-all.trx"
dotnet test tests/BlackThunder.UniTest.Tests.Recursion/BlackThunder.UniTest.Tests.Recursion.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=$testRunStamp-unitest-all.trx"
```

- [일반 테스트 전체 TRX](Unit/20261107-045706-unit-all.trx)
- [자체 테스트 전체 TRX](UniTest/20261107-045706-unitest-all.trx)
- [빌드 로그](20261107-045706-build.log)
- [일반 테스트 전체 로그](20261107-045706-unit-all.log)
- [자체 테스트 전체 로그](20261107-045706-unitest-all.log)

### Lab 생성 전용 전환 후

2026-11-07 04:48 한국 시간에 Lab의 Id·SetMetadata·Arranger·Actor·Asserter를 생성 전용 속성으로 바꾼 뒤 Release 빌드와 두 프로젝트 전체 테스트를 실제로 재실행했다.
Release 빌드는 경고 0개·오류 0개로 완료했다.

| 프로젝트 | 전체 | 통과 | 실패 | 건너뜀 |
| --- | --- | --- | --- | --- |
| Tests.Unit | 602 | 602 | 0 | 0 |
| Tests.Recursion | 51 | 51 | 0 | 0 |
| 합계 | 653 | 653 | 0 | 0 |

일반 Lab은 [172개 사례](Unit/Lab/03.%20Results.md), 일반 CompositeLab은 [174개 사례](Unit/CompositeLab/03.%20Results.md)를 따로 필터링하여 실행했다.
생성 후 Lab 멤버에 대입하던 일반·자체 테스트의 준비 코드는 생성자와 객체 초기화로 옮겼으며 기대값은 변경하지 않았다.

샘플은 help와 다섯 시나리오를 직접 실행하여 모두 종료 코드 0이었다.
보고서의 항목 수는 Merge를 다시 작성하기 전과 같고(11,551·422·1,010·137·4개), 모든 비루트 항목이 Success였다.
샘플 검증 스크립트 전체는 이번에 다시 실행하지 않았으며 CI에서 확인한다.

```powershell
$testRunStamp = '20261107-044851'
dotnet build BlackThunder.UniTest.sln --configuration Release --no-restore -m:1
dotnet test tests/BlackThunder.UniTest.Tests.Unit/BlackThunder.UniTest.Tests.Unit.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=$testRunStamp-unit-all.trx"
dotnet test tests/BlackThunder.UniTest.Tests.Recursion/BlackThunder.UniTest.Tests.Recursion.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=$testRunStamp-unitest-all.trx"
```

- [일반 테스트 전체 TRX](Unit/20261107-044851-unit-all.trx)
- [자체 테스트 전체 TRX](UniTest/20261107-044851-unitest-all.trx)
- [빌드 로그](20261107-044851-build.log)
- [일반 테스트 전체 로그](20261107-044851-unit-all.log)
- [자체 테스트 전체 로그](20261107-044851-unitest-all.log)

### 스택 추적과 매개변수 이름 수정 후

2026-11-07 04:37 한국 시간에 다음 두 가지를 수정한 뒤 Release 빌드와 두 프로젝트 전체 테스트를 실제로 재실행했다.
Release 빌드는 경고 0개·오류 0개로 완료했다.

- Lab이 예상하지 않은 Actor 예외를 다시 던질 때 원래 스택 추적을 보존한다. 일반 Lab 테스트 UL15의 2개 사례를 추가했다.
- MissingExpectedException 생성자의 매개변수 이름 오타를 expectedExceptionType으로 고쳤다.

| 프로젝트 | 전체 | 통과 | 실패 | 건너뜀 |
| --- | --- | --- | --- | --- |
| Tests.Unit | 592 | 592 | 0 | 0 |
| Tests.Recursion | 51 | 51 | 0 | 0 |
| 합계 | 643 | 643 | 0 | 0 |

변경 범위의 일반 Lab은 따로 필터링하여 [162개 사례](Unit/Lab/03.%20Results.md)를 실행했다.
샘플 검증 스크립트는 이번에 다시 실행하지 않았으며 CI에서 확인한다.

```powershell
$testRunStamp = '20261107-043736'
dotnet build BlackThunder.UniTest.sln --configuration Release --no-restore -m:1
dotnet test tests/BlackThunder.UniTest.Tests.Unit/BlackThunder.UniTest.Tests.Unit.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=$testRunStamp-unit-all.trx"
dotnet test tests/BlackThunder.UniTest.Tests.Recursion/BlackThunder.UniTest.Tests.Recursion.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=$testRunStamp-unitest-all.trx"
```

- [일반 테스트 전체 TRX](Unit/20261107-043736-unit-all.trx)
- [자체 테스트 전체 TRX](UniTest/20261107-043736-unitest-all.trx)
- [빌드 로그](20261107-043736-build.log)
- [일반 테스트 전체 로그](20261107-043736-unit-all.log)
- [자체 테스트 전체 로그](20261107-043736-unitest-all.log)

### 게시 전 검증

2026-11-07 04:11 한국 시간에 링크와 번역을 정리한 뒤 Release 빌드, 두 테스트 프로젝트 전체와 샘플 CLI 검증을 다시 수행했다.
이 문서 정리에서는 도메인·테스트·샘플 코드와 빌드 설정을 변경하지 않았다. 빌드는 경고 0개·오류 0개로 완료했다.

| 검증 범위 | 결과 |
| --- | --- |
| Unit 전체 | 590/590 통과, 실패·건너뜀 0개 |
| Recursion 전체 | 51/51 통과, 실패·건너뜀 0개 |
| 전체 테스트 | 641/641 통과 |
| 샘플 CLI | 5개 시나리오, help, 잘못된 명령·추가 인자, 대화형 입력과 I/O 오류 복구 모두 통과 |

이번에는 pwsh를 사용할 수 있어 이전에 미실행으로 남긴 샘플 검증 스크립트 전체를 실행했다.
Ubuntu 검증은 CI에서 확인할 범위이다. 모듈별 단독 실행의 기록은 기존 결과 문서에 유지하고, 이번 전체 실행은 별도 근거로 구분한다.

```powershell
$testRunStamp = '20261107-041136'
dotnet build BlackThunder.UniTest.sln --configuration Release --no-restore -m:1
dotnet test tests/BlackThunder.UniTest.Tests.Unit/BlackThunder.UniTest.Tests.Unit.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=$testRunStamp-publish-unit.trx"
dotnet test tests/BlackThunder.UniTest.Tests.Recursion/BlackThunder.UniTest.Tests.Recursion.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=$testRunStamp-publish-unitest.trx"
pwsh -NoProfile -File .github/scripts/verify-samples.ps1
```

- [전체 Unit TRX](Unit/20261107-041136-publish-unit.trx)
- [전체 Recursion TRX](UniTest/20261107-041136-publish-unitest.trx)
- [빌드 로그](20261107-041136-publish-build.log)
- [Unit 로그](20261107-041136-publish-unit.log)
- [Recursion 로그](20261107-041136-publish-unitest.log)
- [샘플 검증 로그](20261107-041136-publish-samples.log)

### 예상 예외 판정 수정 후

2026-11-07 04:01 한국 시간에 CompositeLab의 [예상 예외 판정 결함 2건](UniTest/CompositeLab/03.%20Results.md)을 수정한 뒤 Release 빌드와 두 프로젝트 전체 테스트를 실제로 재실행했다.
Release 빌드는 경고 0개·오류 0개로 완료했다.

| 프로젝트 | 전체 | 통과 | 실패 | 건너뜀 |
| --- | --- | --- | --- | --- |
| Tests.Unit | 590 | 590 | 0 | 0 |
| Tests.Recursion | 51 | 51 | 0 | 0 |
| 합계 | 641 | 641 | 0 | 0 |

변경 범위의 4개 모듈은 같은 시점에 따로 필터링하여 실행하고 결과를 각 모듈 폴더에 저장했다.
나머지 11개 모듈은 이 전체 실행에 포함되어 통과했으며, 모듈별 기록은 다음 절의 수정 전 실행을 유지한다.
자체 테스트 사례 수는 51개로 그대로이지만 CompositeLab의 내부 시나리오는 45개에서 51개로 늘었다.

| 구분 | 모듈 | 전체 | 통과 | 실패 | 모듈별 결과 |
| --- | --- | --- | --- | --- | --- |
| Unit | Lab | 160 | 160 | 0 | [기록](Unit/Lab/03.%20Results.md) |
| Unit | CompositeLab | 174 | 174 | 0 | [기록](Unit/CompositeLab/03.%20Results.md) |
| UniTest | Lab | 12 | 12 | 0 | [기록](UniTest/Lab/03.%20Results.md) |
| UniTest | CompositeLab | 6 | 6 | 0 | [기록](UniTest/CompositeLab/03.%20Results.md) |

샘플은 help와 다섯 시나리오를 직접 실행하여 모두 종료 코드 0이었고, 보고서의 모든 비루트 항목이 Success였다.
다만 이 환경에 pwsh가 없어 [verify-samples.ps1](../../../.github/scripts/verify-samples.ps1) 전체는 실행하지 못했다.

```powershell
$testRunStamp = '20261107-040058'
dotnet build BlackThunder.UniTest.sln --configuration Release --no-restore -m:1
dotnet test tests/BlackThunder.UniTest.Tests.Unit/BlackThunder.UniTest.Tests.Unit.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=$testRunStamp-unit-all.trx"
dotnet test tests/BlackThunder.UniTest.Tests.Recursion/BlackThunder.UniTest.Tests.Recursion.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=$testRunStamp-unitest-all.trx"
```

- [일반 테스트 전체 TRX](Unit/20261107-040058-unit-all.trx)
- [자체 테스트 전체 TRX](UniTest/20261107-040058-unitest-all.trx)
- [빌드 로그](20261107-040058-build.log)
- [일반 테스트 전체 로그](20261107-040058-unit-all.log)
- [자체 테스트 전체 로그](20261107-040058-unitest-all.log)

### 모듈별 실행과 직접 결과 보관

이 절은 위 수정 전의 기록이다.
2026-11-07에 일반 테스트 9개와 자체 테스트 6개를 각각 모듈별로 필터링해 실제 재실행했다.
각 모듈 폴더에는 해당 모듈만 담긴 TRX 1개와 실행 로그 1개를 두었으며, 각 `03. Results.md`의 첫 실행 절에서 두 파일을 직접 연결한다.
예를 들어 일반 Lab의 160개 사례는 `Unit/Lab`, 자체 Lab의 12개 사례는 `UniTest/Lab`에 각각 저장했다.

| 구분 | 모듈 | 전체 | 통과 | 실패 | 모듈별 결과 |
| --- | --- | --- | --- | --- | --- |
| Unit | Lab | 160 | 160 | 0 | [기록](Unit/Lab/03.%20Results.md) |
| Unit | Node | 47 | 47 | 0 | [기록](Unit/Node/03.%20Results.md) |
| Unit | Model | 36 | 36 | 0 | [기록](Unit/Model/03.%20Results.md) |
| Unit | CompositeLab | 174 | 174 | 0 | [기록](Unit/CompositeLab/03.%20Results.md) |
| Unit | TestCase | 27 | 27 | 0 | [기록](Unit/TestCase/03.%20Results.md) |
| Unit | Project | 63 | 63 | 0 | [기록](Unit/Project/03.%20Results.md) |
| Unit | TestDesigner | 43 | 43 | 0 | [기록](Unit/TestDesigner/03.%20Results.md) |
| Unit | TestExecutor | 24 | 24 | 0 | [기록](Unit/TestExecutor/03.%20Results.md) |
| Unit | Reporting | 16 | 16 | 0 | [기록](Unit/Reporting/03.%20Results.md) |
| UniTest | Lab | 12 | 12 | 0 | [기록](UniTest/Lab/03.%20Results.md) |
| UniTest | Model | 10 | 10 | 0 | [기록](UniTest/Model/03.%20Results.md) |
| UniTest | Node | 11 | 11 | 0 | [기록](UniTest/Node/03.%20Results.md) |
| UniTest | TestCase | 10 | 10 | 0 | [기록](UniTest/TestCase/03.%20Results.md) |
| UniTest | CompositeLab | 6 | 4 | 2 | [기록](UniTest/CompositeLab/03.%20Results.md) |
| UniTest | Project | 2 | 2 | 0 | [기록](UniTest/Project/03.%20Results.md) |
| 합계 | 15개 모듈 실행 | 641 | 639 | 2 | |

기존 전체 실행의 사례 집합과 모듈별 TRX를 대조하여 중복·누락이 없는 것을 확인했다.
각 TRX의 fixture도 해당 모듈에 속하는 것만 포함한다.
실패 두 항목은 자체 CompositeLab의 `AllScenariosExecute(Inline)`과 `AllScenariosExecute(ThreadPool)`이며, 기존 CL30·CL31 결함을 재현한 결과이다.

소스가 직전 성공한 Release 빌드와 같은 것을 확인한 뒤 `--no-build --no-restore`로 실행했으며 새 빌드는 수행하지 않았다.
기존 테스트 호스트 연결 제약에 따라 실행은 샌드박스 밖에서 수행했다.
이전 전체 프로젝트 TRX와 통합 로그는 정리하여 삭제했고, 현재 모듈별 결과의 직접 근거는 각 폴더에 있는 최신 파일을 사용한다.
산출물 파일명은 같은 실행 묶음의 식별자를 사용하며, 각 모듈의 실제 시작 시각은 해당 TRX와 결과 문서에 기록했다.

### 신규 모듈 자체 테스트 추가 후

2026-11-06 실행에서 새 NUnit 항목 29개 중 27개가 통과했다. 기존 598개는 모두 통과하여 전체 결과는 625/627이다. 실패 2개는 원본과 확장에 같은 예상 예외를 설정한 CompositeLab 조합의 결함을 ThreadPool·Inline에서 각각 재현한다. [결함과 재현 조건](UniTest/CompositeLab/03.%20Results.md)을 기록했으며, 사용자 지정 범위에 따라 런타임 수정은 포함하지 않았다.

### 기존 테스트 보강 후

2026-11-07 한국 시간 기준으로 기존 Model·Lab 자체 테스트와 NUnit의 Model·Node 사례를 보강하고 전체 솔루션을 실행했다.
Model의 0 경계 기대값과 종료 상태 준비를 교정했으며, 자체 테스트의 시나리오 누락·미실행 검증과 두 실행 모드를 보강했다.
Lab은 기존 133개 시나리오를 보존하고 실제 반복 경로를 추가했다. Node의 재현 중 취소는 시간 지연 대신 진입 신호로 동기화했다.

| 보강 대상 | 결과 |
| --- | --- |
| Model 자체 테스트: 전수 2, 연속 6, 0 경계 재현 2 | 10/10 통과 |
| Lab 자체 테스트: 기존 시나리오 2, 반복 전수 2, 연속 6, 실패 대조 2 | 12/12 통과 |
| 기존 NUnit Model 종료 상태 사례 | 2/2 통과 |
| 기존 NUnit Node 재현 중 취소 사례 | 1/1 통과 |
| 보강 대상 합계 | 25/25 통과 |

Release 빌드는 경고 0개·오류 0개로 완료했다. 전체 NUnit 결과는 Unit 590/590, Recursion 49/51로 합계 639/641이다.
실패 두 항목은 기존 CompositeLab의 AllScenariosExecute(ThreadPool)과 AllScenariosExecute(Inline)이며,
CL30·CL31의 같은 예상 예외 처리 결함을 다시 검출한 결과이다. 이번 보강으로 추가된 실패는 없다.
런타임·샘플·영어 문서·프로젝트 설정 등 보호 대상 64개 파일은 작업 전후 SHA-256이 같음을 확인했다.

실행 명령은 다음과 같다.

```powershell
dotnet build BlackThunder.UniTest.sln --configuration Release --no-restore -m:1
dotnet test BlackThunder.UniTest.sln --configuration Release --no-build --no-restore -m:1 --logger trx
```

당시 원본 TRX·로그는 삭제했으며 위 실행 결과를 문서 이력으로 유지한다.
이 산출물은 Git 추적 대상이 아니다. 기존 testhost 연결 제약 때문에 테스트 실행은 샌드박스 밖에서 수행했다.

### 결과 저장 위치 확인

2026-11-07의 초기 정리에서는 기존 TRX 11개와 실행 로그 4개를 당시의 `Results` 폴더로 옮겼으며, 이동 전후 SHA-256이 같음을 확인했다.
당시 기본 저장 경로와 CI의 출력·업로드 경로도 같은 위치로 변경했다.
다음과 같이 결과 경로 옵션을 생략하여 전체 테스트를 다시 실행한 뒤, 두 TRX가 당시 `Results`에 생성되고
저장소 루트에 이전 결과 폴더가 재생성되지 않는 것을 확인했다.

```powershell
dotnet test BlackThunder.UniTest.sln --configuration Release --no-build --no-restore -m:1 --logger trx
```

결과는 Unit 590/590, Recursion 49/51로 기존과 같은 639/641이며, 실패는 기존 CompositeLab 두 항목이다.
당시 원본 TRX·로그는 삭제했으며 위 경로 검증 결과를 문서 이력으로 유지한다.

### 일반 테스트 문서 분리와 공통 결과 경로 검증

2026-11-07에 한국어 문서를 `Unit/<대상>`과 `UniTest/<대상>`으로 나누었다.
일반 테스트 9개 대상에 상태표·계획·결과 27개 문서를 작성하고, 기존 15개 테스트 파일의 테스트 메서드 214개를 계획에 연결했다.
Model M02·M03과 Node N14는 일반 테스트 문서로 옮겼으며 자체 테스트의 나머지 식별자는 유지했다.

이 단계에서는 두 프로젝트의 공통 실행 요약을 이 문서로, 기존 TRX 13개와 로그 5개를 당시의 공통 `Results`로 옮겼다.
기존 산출물의 내용을 수정하지 않았으며, 새 검증 로그와 TRX도 같은 공통 위치에 추가했다.
`Directory.Build.props`의 기본 결과 경로, CI의 출력·업로드 경로와 Git 제외 경로를 함께 갱신했다.
그 단계에서는 두 테스트 프로젝트가 같은 결과 폴더를 기본값으로 사용하고, 명시적인 결과 경로 재정의도 유지됨을 확인했다. 현재 기본값은 실행 범위별로 나뉘며 문서 상단의 배치를 따른다.

Release 빌드는 경고 0개·오류 0개로 완료했다.
첫 샌드박스 실행은 테스트 호스트 연결이 90초 후 실패하여 중단했으며, 테스트 성공이나 실패 집계에 포함하지 않았다.
샌드박스 밖에서 결과 경로 옵션 없이 전체 테스트를 다시 실행한 결과는 다음과 같다.

| 프로젝트 | 전체 | 통과 | 실패 | 건너뜀 |
| --- | --- | --- | --- | --- |
| Tests.Unit | 590 | 590 | 0 | 0 |
| Tests.Recursion | 51 | 49 | 2 | 0 |
| 합계 | 641 | 639 | 2 | 0 |

실패 항목은 기존 CompositeLab의 `AllScenariosExecute(ThreadPool)`과 `AllScenariosExecute(Inline)` 두 개이다.
CL30·CL31의 알려진 예상 예외 처리 결함이며 [기존 결함 기록](UniTest/CompositeLab/03.%20Results.md)과 같은 결과이다.
도메인 코드와 테스트 코드는 변경하지 않았다.

```powershell
dotnet build BlackThunder.UniTest.sln --configuration Release --no-restore -m:1
dotnet test BlackThunder.UniTest.sln --configuration Release --no-build --no-restore -m:1 --logger trx
```

두 TRX가 새 기본 경로에 생성되었고, 이전 자체 테스트 전용 산출물 폴더와 저장소 루트의 별도 결과 폴더가 재생성되지 않은 것을 확인했다.
한국어 문서의 관련 링크·제목·LF·파일 끝 개행과 변경분 공백 검사를 통과했다.
런타임·테스트·샘플 코드와 영문 문서 등을 포함한 보호 대상 84개 파일은 전후 SHA-256이 같다.
영문 문서의 구성과 본문·경로 표기는 이번 한국어 정리 범위에 포함하지 않았다.

당시 원본 TRX·로그는 삭제했으며 실행 결과를 문서 이력으로 유지한다.

### 실행 범위별 산출물 배치

2026-11-07에 별도 Results 폴더를 없애고 실제 실행 범위에 맞춰 산출물을 문서 옆으로 옮겼다.
기존 파일에는 모듈 단독 실행 기록이 없으므로 전체 프로젝트 TRX를 특정 모듈에 나누어 넣지 않았다.

| 당시 산출물 | 삭제 전 위치 | 개수 |
| --- | --- | --- |
| Unit 전체 실행·중단 기록 | `Unit/` 바로 아래 | 8 |
| Recursion 전체 실행 기록 | `UniTest/` 바로 아래 | 7 |
| 전체 빌드·통합 실행 로그 | 이 문서 옆 | 8 |
| 소속 프로젝트를 확인할 수 없는 연결 실패 TRX | 이 문서 옆 | 1 |

마지막 항목은 이번 정리에서 함께 삭제한 이전 연결 실패 기록이다.
실행 사례가 없고 프로젝트 식별 정보도 없어 일반·자체 테스트 중 한쪽의 실행 결과로 간주하지 않는다.
기존 24개 파일은 이동 전후 SHA-256이 같으며, TRX 내부의 당시 경로와 실행 정보도 보존했다.
한국어 문서의 원본 링크와 재실행 명령은 현재 배치로 정리했다.

기본 출력 경로를 Unit 프로젝트는 `Unit/`, Recursion 프로젝트는 `UniTest/`로 나누었다.
모듈별 명시적 출력 경로가 기본값보다 우선하는 것도 MSBuild 속성 조회로 확인했다.
CI는 이 기본값으로 실행하고 양쪽 TRX를 수집한다.
Git 제외 규칙은 테스트 문서 아래의 TRX·로그에만 적용하며 Markdown 문서는 추적한다.

Release 빌드는 경고 0개·오류 0개로 완료했다.
기존 샌드박스의 테스트 호스트 연결 제약을 고려하여 샌드박스 밖에서 다음 명령을 실행했다.

```powershell
dotnet build BlackThunder.UniTest.sln --configuration Release --no-restore -m:1
dotnet test BlackThunder.UniTest.sln --configuration Release --no-build --no-restore -m:1 --logger trx
```

| 프로젝트 | 전체 | 통과 | 실패 | 건너뜀 |
| --- | --- | --- | --- | --- |
| Tests.Unit | 590 | 590 | 0 | 0 |
| Tests.Recursion | 51 | 49 | 2 | 0 |
| 합계 | 641 | 639 | 2 | 0 |

실패는 기존 CompositeLab의 `AllScenariosExecute(Inline)`과 `AllScenariosExecute(ThreadPool)`이다.
[알려진 예상 예외 처리 결함](UniTest/CompositeLab/03.%20Results.md)의 재현 결과이며 도메인·테스트 코드를 수정하지 않았다.
새 TRX가 각각 Unit·UniTest 문서 폴더에 생성되고, 별도 Results 폴더와 저장소 루트의 결과 폴더가 재생성되지 않은 것을 확인했다.
이동된 산출물과 이번 검증 파일은 모두 Git에서 제외되며, 코드·프로젝트 파일·영문 문서 등을 포함한 보호 대상 88개 파일의 SHA-256은 같다.
