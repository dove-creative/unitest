# UniTest

[English](README.md)

UniTest는 모델 기반 테스트 프레임워크이다. 모델의 각 상태에서 가능한 동작과 기대 결과를 정의하면, UniTest가 동작을 조합해 경로를 탐색하고 검증한다.

| 구현 | 실행 환경 | 패키지 | 안내 |
| --- | --- | --- | --- |
| C# | .NET Standard 2.1 호환 런타임 | `BlackThunder.UniTest` | [설치와 사용법](csharp/README.ko.md) |
| TypeScript | Node.js 22 이상과 ES2022 브라우저 | `@blackthunder/unitest` | [설치와 사용법](typescript/README.ko.md) |

## 주요 기능

- Arrange·Act·Assert로 상태 전환을 탐색한다.
- 독립된 상태 축의 테스트를 조합한다.
- Lab ID로 실패 경로를 다시 실행하거나 분리된 디버그 노드를 복원한다.
- seed로 연속 경로를 선택하고 결과를 JSON으로 저장한다.

각 구현의 빌드·테스트·패키징은 독립적으로 실행한다. 두 구현은 [경로 선택 벡터](contracts/selection.json)와 JSON 리포트 형식을 공유한다.

## 문서

- [테스트 방법론](docs/Wiki.ko/00.%20Unit%20Test%20Guideline.md)
- [프레임워크 개요](docs/Wiki.ko/01.%20Overview.md)
- [기여와 검증](CONTRIBUTING.md)
- [변경 이력](CHANGELOG.md)
- [MIT 라이선스](LICENSE.md)
