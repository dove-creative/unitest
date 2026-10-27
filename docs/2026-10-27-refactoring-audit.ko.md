# UniTest 리팩토링 사전 검증 리포트

- 검증일: 2026-10-27
- 검증 기준: 커밋 `76ab64a` — `Add runtime execution modes and stable UniTest packaging`
- 대상 버전: `0.2.0`
- 범위: 실행 엔진, 경로 재실행, 취소·동시성 처리, XML 리포트, 실행 도구, 관련 문서 및 기존 테스트
- 최초 작업 성격: 코드 검토와 독립 프로브를 통한 검증. 최초 검증에서는 제품 코드와 기존 테스트를 수정하지 않았습니다.

후속 수정: 2026-10-27에 F1을 예외 전파 방식으로, F2를 신호 대기 방식으로, F3을 중단 시 작업자 종료 대기 방식으로 수정했습니다. F2 수정에 포함된 완료 통지 정리로 R1도 해결했습니다. 이어서 XML 리포트를 독립적인 JSON 스냅샷으로 전환해 F4·F5·F7과 R2를 해결했습니다. F6은 사용자와 취소 우선 정책을 확인한 뒤 결함 분류에서 제외했으며 코드는 유지합니다. 최초 검증 결과와 후속 수정·정책 확인 결과는 각 절에서 구분해 기록했습니다.

JSON 전환 검증: 솔루션 빌드 성공(기존 미사용 필드 경고 1개), 단위 테스트 561개와 재귀 테스트 8개, 총 **569개 통과**. 신규 JSON 회귀 테스트 14개에는 특수 문자 ID, iterator 예외 뒤 자식 보존, 깊이 120 경로, 모델 설명 실패, 반복 복원·후속 실행, 두 실행 모드의 분기 결과, 실제 UTF-8 파일 재읽기를 포함했습니다. API 이전 방법은 [사용 문서](Wiki.ko/04-Usage.md#6-5-json-리포트-읽기)에 기록했습니다.

기타 항목 정리 검증: `ToUncontinuable`의 Assert 생략을 수정하고, 진단 문자열·오류 메시지·미사용 필드·CI 설정을 정리했습니다. CI와 동일한 명령으로 로컬 Windows에서 복원 및 Release 빌드를 수행해 **경고 0개·오류 0개**, 단위 테스트 583개와 재귀 테스트 8개, 총 **591개 통과**를 확인했습니다. 신규 회귀 테스트 22개는 수정 전 21개가 실패했고 수정 후 모두 통과했습니다. CI YAML 구문과 실행 조건을 로컬에서 점검했으며, 실제 GitHub Windows·Ubuntu 작업은 아직 실행하지 않았습니다.

## 1. 평가 요약

제시된 평가의 핵심 결함 1~5번은 최초 검증 기준 코드에서 모두 재현되었습니다. 당시 기존 테스트 529개가 통과하더라도 잘못된 경로가 성공으로 판정되거나, 실행 함수 반환 후 결과가 바뀌거나, XML에서 결과 일부가 사라질 수 있었습니다.

추가로 **실행 중 취소 요청 후 발생한 예외보다 취소 판정이 우선하는 동작**과 **XML 선언 및 파일 인코딩이 불일치하는 문제**를 재현했습니다. 전자는 처음에 F6 결함으로 분류했으나, 이후 취소된 실행의 결과를 채택하지 않는 정책을 확인하여 의도된 동작으로 재분류했습니다. 리팩토링의 첫 목표는 실행 결과의 의미와 완료 시점을 명확히 하고, 채택한 결과를 손실 없이 저장하도록 만드는 것입니다.

상태와 동작에서 실행 경로를 생성하는 모델, `Lab`의 Arrange–Act–Assert 구성, `Merge`·`Extend`를 통한 조합, 경로 재실행과 디버깅 기능은 유지할 만한 설계 자산입니다. 한·영 문서와 고정 버전·소스 커밋·SHA-256 기록 정책도 갖춰져 있습니다. 다만 문서에 적힌 정책과 실제 배포 이력 전체의 준수 여부는 구분해야 하며, 이번 검증은 배포 이력 전체를 감사한 것은 아닙니다.

## 2. 검증 환경과 방법

| 항목 | 내용 |
|---|---|
| 운영 환경 | Windows, 일반 콘솔 실행 |
| .NET SDK | `9.0.318` |
| 프로브 런타임 | `.NET 9.0.20` |
| 논리 프로세서 | 24개 |
| 프로브의 SynchronizationContext | 없음 (`null`) |
| 라이브러리 대상 | `netstandard2.1` |
| 재빌드 | 성공, 경고 1개, 오류 0개 |
| 단위 테스트 | 521개 통과 |
| 재귀 테스트 | 8개 통과 |

저장소 루트에서 다음 명령으로 재빌드와 테스트를 확인했습니다.

```powershell
dotnet build UniTest.sln --no-restore --nologo -m:1 -t:Rebuild -v:minimal
dotnet test UniTest.sln --no-build --no-restore --nologo -m:1 --logger 'console;verbosity=minimal'
```

샌드박스 안의 첫 테스트 실행은 testhost 연결 시간 초과로 중단됐습니다. 같은 테스트를 샌드박스 밖에서 실행했을 때 529개가 모두 통과했습니다. 빌드 경고는 `TestExecutorTest.cs:18`의 사용하지 않는 `_createLabs` 필드에 대한 `CS0169`입니다.

독립 콘솔 프로브는 빌드된 라이브러리를 직접 참조합니다. 조기 반환 검증에는 동기화 게이트를 사용해 다른 Actor가 실행 중인 상태를 유지했습니다. CPU 측정은 다른 프로브 작업이 끝난 뒤 수행했습니다.

이 문서의 코드 행 번호는 검증 기준 커밋을 기준으로 합니다.

## 3. 재현 결과와 후속 판정

F1~F5는 기존 평가의 번호를 유지했습니다. F7은 추가로 확인한 결함입니다. F6은 최초 분류 번호를 유지하되, 취소 우선 정책에 따른 의도된 동작으로 재분류했습니다.

| ID | 문제 | 재현 결과 |
|---|---|---|
| F1 | 존재하지 않는 재실행 ID가 성공으로 판정됨 | `Nope`, `Increment/Nope` 모두 `AllSucceed=true`, `cancelled=false`. 후자는 `Run`도 `true`를 반환하고 실패 리포트를 남기지 않음 |
| F2 | 조정 루프의 바쁜 대기 | 1초 대기 동안 CPU 시간 5.47~7.39초 사용. 두 실행 모드에서 재현 |
| F3 | 작업자 종료 전 실행 함수 반환 | 반환 시 `Executing`이던 노드가 이후 `Cancelled`로 바뀌고, 실행 횟수와 XML도 변경됨 |
| F4 | Lab ID가 XML 요소 이름에 직접 사용됨 | `Charge 50%`, `a/b` 모두 `XmlException`과 설계 실패 발생 |
| F5 | iterator 예외 처리 중 XML 자식 유실 | 실제 트리에는 성공한 자식 1개가 있지만 XML 자식 요소는 0개 |
| F6 | 취소 우선 정책 확인 — 결함에서 제외 | 실행 중 취소 요청 후 Actor가 예외를 던지면 `Cancelled`, `AllSucceed=true`. 의도된 동작으로 유지 |
| F7 | 저장 XML의 인코딩 불일치 | 선언은 UTF-16, 실제 바이트는 UTF-8. 저장 직후 `XmlDocument.Load` 실패 |

### F1. 잘못된 재실행 경로가 성공으로 판정됨

**수정 완료 — 2026-10-27:** `Execute(ids)`가 일반 예외를 삼키고 루트를 반환하던 처리를 `throw;`로 바꿨습니다. 존재하지 않는 ID는 `ArgumentException`으로 호출자에게 전달되며, 조회·경로 구성 중 발생한 다른 일반 예외도 전파됩니다. 기존 취소 처리와 Lab 실행 실패의 Node 기록은 유지합니다. `Run(..., executionIDs)`는 기존 예외 처리에서 오류를 로그에 남기고 `false`를 반환합니다.

첫 ID 및 중간 ID 누락, `stopOnFailure` 설정, 즉시·지연 Lab 생성 예외, `Run` 반환값을 검증하는 회귀 테스트 8개는 수정 전에 모두 실패했고 수정 후 통과했습니다. 전체 테스트는 단위 테스트 529개와 재귀 테스트 8개, 총 **537개 통과**입니다. 아래 설명과 수치는 수정 전 재현 기록입니다.

`CreateLab`은 ID를 찾지 못하면 예외를 던집니다. 그러나 `Execute(string ids)`의 바깥쪽 예외 처리에서는 로그만 남기고 기존 루트 노드를 반환합니다. 오류가 결과 트리에 기록되지 않으므로 `AllSucceed`는 성공으로 판단합니다.

| 입력 경로 | 실행된 노드 수 | AllSucceed | cancelled |
|---|---:|---|---|
| `Increment` | 1 | `true` | `false` |
| `Nope` | 0 | `true` | `false` |
| `Increment/Nope` | 1 | `true` | `false` |

`Run(..., "Increment/Nope", printResult: false)`도 `true`를 반환했습니다. Lab 이름 변경이나 잘못된 실패 경로 입력이 성공으로 오인될 수 있습니다.

원인 위치: [Project.cs](../src/UniTest/Project/Project.cs) 190, 215, 358행.

수정 시에는 존재하지 않는 ID뿐 아니라 재실행 경로 구성 중 발생하는 오류도 결과에 남겨야 합니다. 잘못된 입력과 테스트 대상의 실패를 어떻게 구분할지는 결과 모델에서 명시하는 것이 좋습니다.

### F2. 조정 루프가 바쁜 대기로 CPU를 소비함

**수정 완료 — 2026-10-27:** `Execute(depth)`의 반복 `Task.Yield()`를 `SemaphoreSlim.WaitAsync(ct)`로 변경했습니다. 새 실행·설계 노드의 등록과 실제 작업자 Task 완료 시 신호를 보내며, 중복 알림은 하나로 합칩니다. 루프는 상태를 확인하기 전에 이전 알림을 소비하고, 확인 중 발생한 새 알림은 다음 대기에 남겨 놓습니다. 작업자 자체의 `Inline` 스케줄링용 `Task.Yield()`는 유지했습니다.

회귀 테스트 9개로 유휴 대기, 실제 Task 완료 시점, 설계 중 준비된 Lab 실행, 반복 분기 생성, 빈 프로젝트 종료, 대기 중 취소를 검증했습니다. 바쁜 대기 및 조기 완료 판정 테스트 2개는 수정 전 실패를 확인했습니다. 기존 작업자 테스트의 완료 큐 검증은 완료 처리의 책임이 조정 코드로 이동함에 따라 조정 코드의 테스트로 옮겼습니다. 전체 테스트는 단위 테스트 538개와 재귀 테스트 8개, 총 **546개 통과**입니다.

워밍업 후 동일한 1초 대기 조건으로 세 번 측정한 CPU 시간은 다음과 같습니다. 실제 경과 시간은 약 1.00~1.01초였습니다.

| 조건 | 1차 CPU 시간 | 2차 CPU 시간 | 3차 CPU 시간 |
|---|---:|---:|---:|
| 기준선: `Task.Run` 안에서 1초 대기 | 0초 | 0.015625초 | 0초 |
| `ExecutionMode.ThreadPool` | 0.015625초 | 0초 | 0.015625초 |
| `ExecutionMode.Inline` | 0초 | 0초 | 0.015625초 |

측정값은 프로세스 CPU 시간의 해상도에 영향을 받습니다. 0초는 CPU를 전혀 사용하지 않았다는 의미가 아니라 측정 해상도 안에서 증가가 관찰되지 않았다는 의미입니다. [후속 측정 소스](../artifacts/f2-scheduling-probe/Program.cs)와 [측정 결과](../artifacts/f2-scheduling-probe/results.json)를 로컬 `artifacts`에 남겼습니다.

F2 수정 당시에는 F3의 작업 종료 대기가 별도 과제로 남아 있었습니다. 이후 F3 수정으로 실행 중인 작업자 종료를 기다리게 했으며, 완료 알림과 세마포어 정리는 같은 잠금으로 계속 보호합니다. 아래는 F2 수정 전 재현 기록입니다.

`Execute(int depth)`는 작업 완료 신호를 기다리지 않고 `await Task.Yield()`로 반복합니다. `Task.Yield()`는 반복 실행 기회를 양보하지만, 처리할 일이 생길 때까지 대기하는 신호 장치가 아닙니다.

Actor에서 `Thread.Sleep(1000)`을 실행한 결과는 다음과 같습니다.

| 조건 | 1차 CPU 시간 | 2차 CPU 시간 |
|---|---:|---:|
| 기준선: `Task.Run` 안에서 1초 대기 | 0.046875초 | 0초 |
| `ExecutionMode.ThreadPool` | 6.5625초 | 6.921875초 |
| `ExecutionMode.Inline` | 7.390625초 | 5.46875초 |

실제 경과 시간은 각 측정에서 약 1.00~1.01초였습니다. CPU 시간은 프로세스 내 여러 스레드의 사용 시간을 합산하므로 경과 시간보다 클 수 있습니다. 이 수치는 해당 환경의 측정값이며 일반적인 성능 보장값은 아닙니다.

원인 위치: [Project.cs](../src/UniTest/Project/Project.cs) 146행.

작업 등록·완료·취소에 반응하는 신호 기반 조정으로 바꾸는 것이 적절합니다. 구체적인 수단은 Task 완료 대기, `SemaphoreSlim`, 채널 등의 선택과 함께 기존 대상 프레임워크와 의존성을 검토해야 합니다.

### F3. 실패나 취소 시 작업자가 끝나기 전에 반환함

**수정 완료 — 2026-10-27:** `Execute(depth)`가 `finally`에서 추적 중인 모든 설계·실행 Task를 `Task.WhenAll`로 기다린 뒤 결과를 반환하도록 변경했습니다. 이 종료 대기에는 취소 토큰을 적용하지 않습니다. 모든 작업자가 끝난 뒤 각 Task의 예외를 확인하고 추적 및 대기 큐를 정리하며, 마지막으로 취소 등록과 세마포어를 해제합니다. 작업 등록 루프에서도 취소와 중단 대상 실패를 확인해 새 작업 등록을 멈춥니다.

Runner는 제한 초과 후 작업자가 끝나기를 기다리는 동안 같은 취소 요청과 로그를 반복하지 않도록 변경했습니다. 기존 순차 재실행은 사용자 콜백을 직접 완료하고, 연속 실행은 설계 작업과 해당 실행 묶음을 이미 기다리므로 이번 작업자 종료 대기는 깊이 기반 실행에 추가했습니다.

두 모드의 실행 작업자·설계 작업자 취소, 실패에 따른 중단, 종료 중 늦게 발생한 설계자 예외, Runner 시간 제한을 검증하는 회귀 테스트 9개를 추가했습니다. 모두 수정 전 조기 반환을 검출했고 수정 후 통과했습니다. 반환 후 실행 횟수와 XML이 더 이상 작업자에 의해 변경되지 않는 것도 확인했습니다. 전체 테스트는 단위 테스트 547개와 재귀 테스트 8개, 총 **555개 통과**입니다.

시간 제한은 강제 종료 시각이 아니라 취소 요청 시점입니다. 실행 중인 동기 `CreateLabs`·Actor 등의 콜백이 오래 걸리면 반환도 그만큼 늦어지고, 콜백이 끝나지 않으면 계속 기다립니다. F3에서는 취소 판정 규칙을 변경하지 않았습니다. 이후 F6에서 실행 중 취소된 테스트는 결과를 채택하지 않는 정책을 확인했습니다. 아래는 수정 전 재현 기록입니다.

프로브는 `slow` Actor를 게이트에서 대기시키고, 다른 Actor가 실패하도록 구성했습니다. 두 실행 모드에서 모두 `slow`를 해제하기 전에 `Execute`가 반환했습니다.

| 관찰 항목 | 반환 직후 | 게이트 해제 및 작업 종료 후 |
|---|---|---|
| `slow.Status` | `Executing` | `Cancelled` |
| `ProcessCount` | 1 | 2 |
| XML | 반환 시점 내용 | 이후 변경됨 |

호출자가 받은 결과가 아직 변경 중이므로 집계, 실패 리포트 추출, XML 저장의 시점에 따라 관찰 결과가 달라질 수 있습니다.

원인 위치: [Project.cs](../src/UniTest/Project/Project.cs) 123~165행, [TestExecutor.cs](../src/UniTest/Project/TestExecutor.cs) 60행.

실행 중단 시 새 작업 등록을 멈추고, 이미 시작한 모든 작업의 종료와 예외를 관찰한 뒤 결과를 반환해야 합니다. 현재 Actor는 동기 콜백이므로 종료를 기다리는 정책과 시간 제한의 의미도 함께 정해야 합니다. 취소 요청만으로 실행 중인 사용자 콜백이 강제 종료되지는 않습니다.

### F4. Lab ID와 XML 요소 이름이 결합되어 있음

**수정 완료 — 2026-10-27:** `Node.Report`와 실행 중 XML 갱신을 제거하고 ID를 JSON의 `labId` 값으로 저장합니다. 공백·한글·따옴표·기호 ID의 실행 및 재실행을 검증했습니다. `/`는 경로 구분자이므로 Node 생성 시 `ArgumentException`으로 거부하며, 잘못된 노드를 붙이거나 조상 Lab을 재실행하기 전에 검증합니다. 아래는 수정 전 재현 기록입니다.

`Node.SetXmlNode`가 ID를 그대로 `XmlDocument.CreateElement`에 전달합니다. 공백이나 `/`가 포함된 ID는 노드 생성 중 XML 예외를 발생시키며, 사용자에게는 `Test Designer failed`라는 오류로 전달됩니다.

원인 위치: [Node.cs](../src/UniTest/Node.cs) 85, 89행.

리포트에서는 고정 요소 이름과 ID 속성을 사용하거나 이름을 인코딩할 수 있습니다. 별도로 `/`는 실행 이력 구분자이므로, XML 문제를 해결한 뒤에도 ID 검증 또는 경로 표현 변경이 필요합니다. 현재 문자열 경로 API를 유지한다면 `/`를 포함한 ID를 명시적으로 거부하는 방식이 단순합니다.

### F5. iterator 예외 처리에서 XML 자식이 삭제됨

**수정 완료 — 2026-10-27:** 리포트를 실제 노드 트리에서 생성하므로 부모의 실패 기록이 기존 자식을 삭제하지 않습니다. 두 실행 모드에서 iterator가 첫 Lab 이후 예외를 던져도 전체 JSON에 부모의 `Failure`와 자식의 `Success`가 함께 남는 것을 확인했습니다. 실패 전용 리포트는 별도로 실패와 그 조상만 선택합니다. 아래는 수정 전 재현 기록입니다.

`CreateLabs`가 첫 Lab을 `yield return`한 뒤 예외를 던지도록 구성했습니다. 실행 후 실제 트리에는 `Success`인 자식 1개가 남았지만 XML에는 자식 요소가 없었고, 그 자식의 `Report.ParentNode`도 `null`이었습니다.

열거 중 예외를 처리하면서 부모 노드의 `SetExternalException`을 호출하고, `UpdateXml`에서 `Report.InnerText`를 덮어쓰는 것이 직접적인 원인입니다. `InnerText` 설정은 기존 자식 노드를 대체합니다. 이는 [공식 API 설명](https://learn.microsoft.com/en-us/dotnet/api/system.xml.xmlnode.innertext?view=net-9.0#remarks)과도 일치합니다.

원인 위치: [TestDesigner.cs](../src/UniTest/Project/TestDesigner.cs) 79~99행, [Node.cs](../src/UniTest/Node.cs) 240행.

지연 실행되는 iterator의 예외가 메서드 호출 시점의 첫 `try`에 잡히지 않는 것은 정상 동작입니다. 수정 대상은 열거 과정의 실패 기록과, 기존 자식을 보존하는 리포트 갱신 방식입니다.

### F6. 실행 중 취소 우선 판정 — 결함에서 제외

**정책 확인 — 2026-10-27:** 사용자는 실행 중 취소된 테스트의 결과를 채택하지 않고 `Cancelled`로 처리하는 것이 자연스럽다고 판단했습니다. 따라서 아래 재현은 의도된 동작이며, F6을 이유로 실행 상태나 `Run()`의 반환 규칙을 변경하지 않습니다. 최초 평가는 실패 우선 정책을 전제로 결함으로 분류한 것이므로 이를 정정합니다.

Actor 실행 중 외부 취소를 요청한 뒤 Actor가 예외를 던지도록 했습니다. `Node.Execute`는 해당 예외를 실패로 처리하지만, 이어지는 `ct.ThrowIfCancellationRequested()`가 취소 예외를 발생시킵니다. 이후 `SetCancellationException`이 기존 실패 상태와 예외를 덮어씁니다.

최종 관찰 결과는 `Status=Cancelled`, `AllSucceed=true`, `cancelled=true`였습니다. 결과 노드에는 실제 Actor 실패 대신 취소 예외가 남았습니다.

관련 위치: [TestExecutor.cs](../src/UniTest/Project/TestExecutor.cs), [NodeExtensions.cs](../src/UniTest/Tools/NodeExtensions.cs).

취소된 실행에서 발생한 원래 예외를 별도로 보존하는 것은 선택적인 진단 개선이며, 이번 필수 수정 대상에는 포함하지 않습니다. 실행 완료 후 취소가 도착하는 경계 상황은 위 프로브와 별개이며, 이 재분류가 해당 경계 동작까지 검증했다는 뜻은 아닙니다.

### F7. XML 선언과 실제 파일 인코딩이 다름

**수정 완료 — 2026-10-27:** `ExportXml`을 `ExportJson`으로 교체하고 `System.Text.Json`이 만든 UTF-8 바이트를 BOM 없이 직접 저장합니다. 한글을 포함한 실제 파일을 UTF-8로 디코딩하고 JSON으로 다시 읽는 회귀 테스트를 통과했습니다. 아래는 수정 전 재현 기록입니다.

`ExportXml`은 `StringWriter`를 통해 XML 문자열을 만든 뒤 UTF-8 파일로 저장합니다. 프로브에서 생성한 파일은 다음 상태였습니다.

- XML 선언: `<?xml version="1.0" encoding="utf-16"?>`
- 파일 선두 바이트: `EF BB BF 3C 3F 78 6D 6C` — UTF-8 BOM 및 XML 시작 문자
- 다시 읽기: `XmlException: There is no Unicode byte order mark. Cannot switch to Unicode.`

원인 위치: [ProjectRunner.cs](../src/UniTest/Tools/ProjectRunner.cs) 129~141행.

인코딩을 지정한 파일 스트림에 XML을 직접 쓰는 등 선언과 실제 바이트를 일치시켜야 합니다. 회귀 검증은 문자열 파싱뿐 아니라 실제 저장 파일을 `XmlDocument.Load`로 다시 읽는 과정까지 포함해야 합니다.

## 4. 코드상 확인한 동시성 위험

다음 두 항목은 최초 코드 검토에서 확인한 위험입니다. 최초 검토에서는 각 위험 자체로 인한 별도의 손상이나 예외 유실을 재현하지 않았습니다. 이후 R1은 회귀 테스트로 조기 반환을 재현하고 수정했으며, R2는 JSON 전환으로 공유 XML 변경 경로를 제거했습니다.

### R1. 완료 큐와 Task 완료 시점의 불일치

**수정 완료 — 2026-10-27:** 작업자 내부의 완료 큐 등록을 제거하고 조정 코드의 `TrackTask`에서 실제 Task 종료 후 완료 큐 등록과 신호 발송을 수행하도록 변경했습니다. 작업 실행 전에 취소된 Task도 완료 통지 대상입니다. 중복된 실행 중 작업자 집합도 제거하고 Task 사전으로 추적을 통일했습니다. 제어 가능한 SynchronizationContext를 사용한 회귀 테스트에서 수정 전에는 작업자 Task 종료 전에 조정 함수가 반환했으며, 수정 후에는 실제 Task 종료를 기다리는 것을 확인했습니다. 여기서의 정상 완료 추적과 F3의 실패·취소 시 전체 작업 종료 대기는 별도입니다. 아래는 수정 전 코드 검토 기록입니다.

작업자는 `ExecuteCore` 내부에서 완료 큐에 자신을 넣습니다. 이를 감싼 바깥쪽 비동기 `Execute` Task는 그 시점에 아직 완료되지 않았을 수 있습니다.

`ObserveTask`는 Task 추적 항목을 먼저 삭제한 뒤, `IsCompleted`가 아니면 돌아갑니다. 따라서 이후 완료되거나 실패하는 Task의 예외 관찰을 놓칠 수 있습니다. 완료 통지와 실제 Task 종료를 하나의 완료 처리 흐름으로 묶어야 합니다.

관련 위치: [Project.cs](../src/UniTest/Project/Project.cs) 309~324행, [TestDesigner.cs](../src/UniTest/Project/TestDesigner.cs) 103행, [TestExecutor.cs](../src/UniTest/Project/TestExecutor.cs) 87행.

### R2. 공유 XML 문서의 동시 변경

**수정 완료 — 2026-10-27:** 작업자들은 공유 문서를 변경하지 않고 노드별 진단값만 기록합니다. 작업자 종료 후 `GetReport()`가 읽기 전용 스냅샷을 만들고 이를 직렬화합니다. 두 실행 모드에서 364개 노드가 모두 보존되고 반환 후 JSON이 안정적인 것을 검증했습니다. 실행 중의 임의 리포트 조회나 외부 호출자의 동시 트리 수정까지 지원하는 변경은 아닙니다. 아래는 수정 전 코드 검토 기록입니다.

여러 작업자가 같은 `XmlDocument`에 대해 요소 생성, 자식 추가, 속성 추가, 본문 갱신을 수행합니다. 작업 큐에 적용된 잠금은 XML 변경 전체를 보호하지 않습니다.

이 문제는 ThreadPool 모드에만 한정할 수 없습니다. 이번 일반 콘솔 프로브에서는 `Inline`도 여러 Actor가 동시에 실행되는 상황을 만들었습니다. 실행 중에는 결과 데이터만 수집하고, 모든 작업이 끝난 뒤 XML로 직렬화하면 이 공유 변경 경로를 줄일 수 있습니다.

관련 위치: [Node.cs](../src/UniTest/Node.cs) 80~96, 236~268행.

## 5. 평가문에서 보정할 사항

| 기존 설명 | 확인 및 보정 |
|---|---|
| `ExportXml`은 항상 클립보드에 복사한다 | 함수 자체의 기본값은 `copyToClipboard=false`입니다. `RunAsync`가 실제 리포트 출력 시 이를 `true`로 전달합니다. 성공하고 `printResult=false`이면 리포트 출력도 하지 않습니다. |
| 창 열기를 옵트인으로 바꿔야 한다 | 창 열기는 이미 기본값 `false`인 `printResult`로 선택됩니다. 파일 저장과 클립보드·창 열기의 제어를 분리하는 개선은 유효합니다. |
| 사용자 코드의 스레드 안전성은 ThreadPool의 전제다 | 일반 콘솔 환경의 `Inline`에도 적용됩니다. `Inline`은 직렬 실행 모드와 동의어가 아닙니다. |
| Lab에는 외부 부수 효과가 없어야 한다 | 반복 실행을 격리·초기화할 수 있고 같은 경로를 재현할 수 있어야 한다는 설명이 더 정확합니다. 외부 시스템 사용 자체가 반드시 금지되는 것은 아닙니다. |
| 상태 중복 제거가 없으므로 추가해야 한다 | 기능이 없는 것은 맞지만, 상태 동등성 및 이력 의존성을 정의하지 않고 합치면 테스트 의미가 달라질 수 있습니다. 별도 설계 과제입니다. |

관련 위치: [ProjectRunner.cs](../src/UniTest/Tools/ProjectRunner.cs) 66, 76, 120행, [Usage 문서](Wiki.ko/04-Usage.md) 1-5절.

노드별 경로 재실행 비용도 재현했습니다. 분기 2개, 깊이 10에서 루트를 제외한 노드는 **2,046개**, Actor 호출은 **18,434회**, 즉 노드 수의 약 **9.01배**였습니다. 이 비용은 현재의 독립 상태 재구성 방식에서 발생합니다. 상태 복제나 캐시를 도입하려면 격리와 재현성에 대한 계약을 먼저 정해야 합니다.

## 6. 기타 확인 사항

- **수정 완료:** 제네릭 형식명만 출력하던 `Node.ToString()`을 `Node '{ID}' (Depth={Depth}, Status={Status})`로 변경했습니다. 모델의 `ToString()`은 호출하지 않습니다.
- **수정 완료:** `TestExecutor`의 중복 실행 오류 메시지를 `Test Executor cannot be executed multiple times.`로 바로잡았습니다.
- **수정 완료:** `ToUncontinuable=true`일 때 현재 Assert를 생략하던 조기 반환을 제거했습니다. 일반 Lab·CompactLab·CompositeLab은 현재 검증을 마친 뒤 후속 실행만 막습니다. CompositeLab은 기존 역순 Assert를 유지하고 어느 구성 Lab에서든 설정한 플래그를 마지막에 반영합니다. Assert 도중 설정한 플래그도 반영하며, 예상 예외 이후 검증과 실제 Assert 실패 보고도 보존합니다. 이전 생략 동작에 의존한 테스트에 영향을 주므로 양언어 Usage·Implementations 및 CHANGELOG에 동작 변경을 기록했습니다.
- **수정 완료:** 사용하지 않는 `TestExecutorTest._createLabs` 필드를 제거해 기존 빌드 경고를 없앴습니다.
- **설정 추가 완료:** [.github/workflows/ci.yml](../.github/workflows/ci.yml)에 push·pull request·수동 실행을 위한 Windows·Ubuntu CI를 추가했습니다. .NET SDK 9.0.x로 복원·Release 빌드·전체 테스트를 실행하고 운영체제별 TRX 결과를 7일 보관합니다. 샘플은 빌드에 포함하며 대화형 실행·패키지 배포는 하지 않습니다. 실제 GitHub 실행 결과는 저장소에 반영된 뒤 확인해야 합니다.
- **정책 확인 — 코드 유지:** 이미 취소된 토큰을 `Execute`에 전달하면 자식이 없는 루트가 반환되며 `AllSucceed=true`, `cancelled=false`입니다. 이는 반환된 노드 중 실패한 노드와 `Cancelled` 노드가 없다는 집계로 해석하며, 프로젝트 전체의 취소 여부를 반환하는 계약으로 바꾸지 않습니다. 별도로 `Project.Cancel()`을 먼저 호출한 뒤 `Run`을 호출한 프로브에서는 재실행 방지 때문에 `false`가 반환됐으므로 두 경우를 혼동하면 안 됩니다.

관련 파일: [Node.cs](../src/UniTest/Node.cs), [TestExecutor.cs](../src/UniTest/Project/TestExecutor.cs), [Lab.cs](../src/UniTest/Lab/Lab.cs), [TestExecutorTest.cs](../tests/UniTest.Test.UnitTest/TestExecutorTest.cs).

## 7. 권장 리팩토링 순서와 완료 기준

| 순서 | 작업 | 완료 기준 |
|---|---|---|
| 1 | 결과 판정 규칙 정립 | 잘못된 경로가 성공으로 처리되지 않음. 실행 중 취소 우선 정책과 실행 전 취소의 기존 노드 집계 의미는 유지 |
| 2 | 작업 수명과 완료 처리 통합 | 반환 전에 시작한 작업의 종료·예외를 모두 관찰하고, 반환 후 상태·횟수·리포트가 변경되지 않음 |
| 3 | 조정 루프를 신호 기반으로 변경 | 유휴 대기에서 지속적인 CPU 소비가 발생하지 않고, 작업 등록·완료·취소에 정상 반응함 |
| 4 | 결과 데이터와 저장 형식 분리 | ID 및 상태가 자식 노드와 독립적으로 저장되고, 오류를 기록해도 기존 결과가 유지되며, 저장한 JSON을 다시 읽을 수 있음 |
| 5 | 실행 도구의 부수 효과와 문서 정리 | 파일 저장, 클립보드 복사, 창 열기를 각각 제어할 수 있고, 병렬 실행·경로 재실행·후속 실행 제어의 의미가 문서에 명시됨 |
| 6 | 탐색 비용 개선 검토 | 상태 동등성과 이력 의존성을 정의한 뒤 복제·캐시·중복 제거의 적용 여부를 결정함 |

결함 수정 대상은 F1~F5와 F7이며, F6은 기존 취소 우선 동작을 유지합니다. 특히 종료 시점 검증은 짧은 지연 시간에 의존하기보다 동기화 게이트를 사용해야 합니다. CPU 문제는 환경에 민감한 절대 임계값 테스트와, 작업 완료 전 조정 루프가 실제로 대기하는지 확인하는 검증을 구분하는 편이 좋습니다.

기존 테스트 중에는 취소 시 노드가 `Cancelled`가 되는 현재 동작을 직접 검증하는 항목도 있습니다. F6의 정책 확인에 따라 이 기대값은 유지합니다. 최초 529개 테스트 통과만으로 다른 결함의 완료 기준까지 충족된 것은 아니며, 후속 수정의 검증 결과는 각 절에 기록했습니다.

## 8. 재현 자료

검증 당시 생성한 로컬 자료는 다음과 같습니다.

- [프로브 소스](../artifacts/refactor-audit/Program.cs)
- [프로브 프로젝트](../artifacts/refactor-audit/Audit.csproj)
- [프로브 복원 설정](../artifacts/refactor-audit/NuGet.Config)
- [전체 측정 결과 JSON](../artifacts/refactor-audit/output/results.json)
- [인코딩 오류 재현 XML](../artifacts/refactor-audit/output/encoding.xml)
- [F2 후속 CPU 측정 소스](../artifacts/f2-scheduling-probe/Program.cs)
- [F2 후속 CPU 측정 결과](../artifacts/f2-scheduling-probe/results.json)

`artifacts/`는 Git에서 제외되므로 위 자료는 현재 로컬 작업 공간에만 존재하며, 이 문서만으로 다른 체크아웃에 함께 전달되지는 않습니다. 주요 재현 조건과 결과는 본문에 기록했습니다.

로컬 프로브 파일이 있는 상태에서는 저장소 루트에서 다음 명령으로 실행할 수 있습니다.

```powershell
dotnet build src/UniTest/UniTest.csproj --no-restore --nologo -m:1
dotnet restore artifacts/refactor-audit/Audit.csproj --configfile artifacts/refactor-audit/NuGet.Config --nologo
dotnet build artifacts/refactor-audit/Audit.csproj --no-restore --nologo -m:1
dotnet artifacts/refactor-audit/bin/Debug/net9.0/Audit.dll artifacts/refactor-audit/output
```

프로브는 현재 빌드된 라이브러리를 참조하므로 리팩토링 후 실행 결과는 본 리포트와 달라질 수 있습니다.

F2 후속 측정은 다음 명령으로 실행할 수 있습니다. F1 수정으로 기존 전체 프로브의 잘못된 경로 재실행은 이제 예외를 던지므로, CPU 측정에는 별도 후속 프로브를 사용합니다.

```powershell
dotnet restore artifacts/f2-scheduling-probe/F2Probe.csproj --configfile artifacts/f2-scheduling-probe/NuGet.Config --nologo
dotnet run --project artifacts/f2-scheduling-probe/F2Probe.csproj --no-restore
```
