# 0.4.0 이전 안내

UniTest 0.4.0은 공통 컨벤션에 맞춰 공개 네임스페이스와 어셈블리 이름을 변경하는 비호환 릴리스이다.

## 소비 프로젝트 변경

| 0.3.0 | 0.4.0 |
| --- | --- |
| `using UniTest;` | `using BlackThunder.UniTest;` |
| `UniTest.Model` 등 정규화된 형식명 | `BlackThunder.UniTest.Model` 등 새 형식명 |
| `UniTest.dll` | `BlackThunder.UniTest.dll` |
| Lab, CompactLab, CompositeLab, Node와 ILab의 `ID` | 같은 형식의 `Id` |
| `Lab<TModel>.DefaultID` | `Lab<TModel>.DefaultId` |
| Run의 이름 지정 인자 `executionIDs` | `executionIds` |
| `BlackThunder.UniTest` 버전 `[0.3.0]` | 동일한 패키지 ID의 `[0.4.0]` |

이전 `ILab<TModel>` 구현도 `ID` 속성을 `Id`로 변경한다. 이전 네임스페이스나 이름을 유지하는 호환 래퍼는 제공하지 않는다. 소비 프로젝트를 새 패키지로 다시 빌드하고, 리플렉션·형식 이름 문자열·DLL 로딩 설정에 이전 이름을 보관했다면 함께 갱신한다.

패키지 ID와 `netstandard2.1` 대상은 유지한다. DLL 직접 참조 시 기존 DLL을 교체하고, NuGet 사용 시 검증된 새 패키지를 로컬 피드에 추가한 뒤 정확한 버전을 지정한다. 기존 버전의 패키지를 덮어쓰지 않는다.

## 실행 데이터

Lab의 식별자 문자열, `/`로 구분하는 재실행 경로, JSON 필드와 노드 연결 구조는 유지한다. `Id`는 코드 식별자의 변경이며 저장된 Lab ID 값을 바꾸지 않는다. 예외의 정규화된 형식명처럼 타입에서 얻는 진단 문자열에는 새 네임스페이스가 나타난다.

0.3.0에서 도입된 JSON 리포트와 현재 Lab의 Assert 이후 적용되는 `ToUncontinuable` 동작도 유지한다. 0.2.0의 XML API에서 이전하는 방법은 [사용법](04-Usage.md)의 JSON 리포트 절을 참고한다.

## 저장소 개발과 샘플

개발 SDK는 .NET 10, 언어 버전은 C# 9이다. 솔루션은 `BlackThunder.UniTest.sln`, 샘플 프로젝트는 `samples/BlackThunder.UniTest.Samples.csproj`로 바뀌었다. 테스트와 샘플 프로젝트의 디렉터리·어셈블리·네임스페이스도 `BlackThunder.UniTest` 계층을 사용한다.

샘플에 명령 인자를 주면 한 번 실행한 뒤 성공은 `0`, 실패·취소·잘못된 명령·예외는 `1`로 종료한다. 인자가 없으면 대화형 프롬프트를 사용한다. 리포트는 실행 파일 옆의 `SampleOutput`에 저장하며 외부 창을 자동으로 열지 않는다.

다중 상태 샘플은 충전 중 면허 없는 이용자의 탑승 요청도 충전을 먼저 중단한다는 기존 객체 동작을 기대 모델에 반영한다. 라이브러리의 실행·취소 알고리즘과 샘플 객체의 동작은 바꾸지 않는다.
