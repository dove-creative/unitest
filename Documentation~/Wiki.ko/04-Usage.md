# 목차

1. 시작 전에 확인할 것
2. 가장 짧은 시작 흐름
3. 클래스와 메서드 선택 기준
4. 튜토리얼 1. 단일 상태 객체 테스트하기
5. 튜토리얼 2. 다중 상태 테스트 조합하기
6. 실행과 결과 읽기

---

이 문서는 Uni Test의 구현 구조를 다시 설명하는 문서가 아니라, 상태-동작 표와 Uni Test 구성 요소를 실제 테스트 코드로 옮기는 사용 흐름을 다룬다.

따라서 설명 순서는 내부 클래스의 상세 구현보다, 테스트 작성자가 먼저 준비해야 할 표와 모델, 가장 짧은 실행 흐름, 단일 상태와 다중 상태 예제 순서를 따른다.

## 1. 시작 전에 확인할 것

### 1-1. `UNITEST` 심볼이 정의되어 있어야 한다

`UniTest` 어셈블리는 `UNITEST` define constraint를 기준으로 컴파일된다. 이 심볼이 없으면 `Project<TModel>`, `Model`, `Lab<TModel>` 같은 핵심 타입을 사용할 수 없다.

테스트 어셈블리에서는 보통 `UNITY_INCLUDE_TESTS`도 함께 필요하다. 예를 들어 Unity Test Runner에서 실행되는 테스트라면 테스트 asmdef가 `UniTest`, `nunit.framework.dll`을 참조하고, 필요한 define constraint가 현재 프로젝트 설정과 맞는지 확인해야 한다.

즉 "테스트 코드를 작성했는데 타입을 찾지 못한다"면, 먼저 asmdef 참조와 define constraint를 확인하는 편이 좋다.

### 1-2. 상태-동작 표를 먼저 만든다

Uni Test는 테스트를 직접 한 줄씩 나열하는 도구가 아니라, 현재 상태에서 가능한 `Lab`을 생성하고 이어지는 상태를 자동으로 확장하는 도구이다. 따라서 코드보다 먼저 아래 두 가지를 정리해야 한다.

1. 객체가 가질 수 있는 상태
2. 각 상태에서 실행할 수 있는 동작과 예상 결과

이 표가 없으면 `CreateLabs`가 단순한 조건문 묶음으로 커지기 쉽다. 반대로 상태-동작 표가 있으면 각 분기를 그대로 `Lab` 생성 규칙으로 옮길 수 있다.

여기서 상태는 현재 시스템이 어떤 동작을 어떻게 처리할지 결정하는 조건의 분류이다. `대기 중`, `실행 중`, `종료됨`처럼 같은 동작의 결과를 바꾸는 분류는 상태이다. 단순히 리스트가 비어 있거나 특정 필드가 null인 것처럼 데이터 모양만 설명하는 조건은 상태가 아니다.

동작은 시스템 외부에서 호출하거나 관찰할 수 있는 의미 있는 행동이다. `시작하기`, `중지하기`, `저장하기`처럼 결과를 만드는 행동은 동작이지만, 단순 helper나 내부 계산 함수는 상태-동작 표의 동작으로 올리지 않는다.

### 1-3. Model에는 실제 상태와 예상 상태를 함께 둔다

Uni Test에서 `Model`은 테스트 대상 객체만 담는 곳이 아니다. 실제 객체인 `Subject`, 예상 상태를 나타내는 mock 필드, 테스트 진행에 필요한 asset을 함께 보관한다.

```csharp
public class Model : UniTest.Model
{
    public SingleStatedKickboard Kickboard
    {
        get => (SingleStatedKickboard)Subject;
        set => Subject = value;
    }

    public Rider rider;
    public bool isDisposed;

    public Rider TargetedRider;
}
```

여기서 `Kickboard`는 실제 테스트 대상이고, `rider`와 `isDisposed`는 테스트가 기대하는 상태이다. `Asserter`는 이 두 세계가 같은지 확인한다.

### 1-4. 실제 예시 코드 위치

이 문서의 예시는 `Samples~/Unity/Scripts` 아래의 실제 테스트 코드를 기준으로 한다.

| 예시             | 파일                                                                                                                                                                                                                                                                                                                 | 문서에서 보는 내용                             |
| -------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------- |
| 실행 진입점         | [Manager.cs](../../Samples~/Unity/Scripts/Manager.cs)                                                                                                                                                                                                                                              | `Run`, `RunContinuously`, replay 실행 방식 |
| 단일 상태 모델       | [SingleStateTest/Model.cs](../../Samples~/Unity/Scripts/SingleStateTest/Model.cs)                                                                                                                                                                                                                  | `Subject`, 예상 상태, asset 구성             |
| 단일 상태 프로젝트     | [SingleStateTest/Project.cs](../../Samples~/Unity/Scripts/SingleStateTest/Project.cs)                                                                                                                                                                                                              | `CreateLabs`, 상태별 Lab 생성               |
| 다중 상태 모델       | [MultiStateTest/Model.cs](../../Samples~/Unity/Scripts/MultiStateTest/Model.cs)                                                                                                                                                                                                                    | 배터리, 충전, 탑승 상태의 예상값 구성                 |
| 다중 상태 진입점      | [MultiStateTest/Project.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project.cs)                                                                                                                                                                                                                | `MainTestCase`와 계층형 생성기 진입점            |
| 다중 상태 생성기      | [Project-Charge.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Charge.cs), [Project-Battery.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Battery.cs), [Project-Kickboard.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Kickboard.cs) | 상태 계층별 Lab 조합                          |
| 실제 동작 template | [GetTemplates.cs](../../Samples~/Unity/Scripts/MultiStateTest/GetTemplates.cs)                                                                                                                                                                                                                     | `MainTestCase`별 실제 Act 정의              |
| 테스트 대상 객체      | [SingleStatedKickboard.cs](../../Samples~/Unity/Scripts/Kickboard/SingleStatedKickboard.cs), [MultiStatedKickboard.cs](../../Samples~/Unity/Scripts/Kickboard/MultiStatedKickboard.cs)                                                                                             | 테스트 대상의 실제 상태 전환                       |

---

## 2. 가장 짧은 시작 흐름

가장 단순한 사용 흐름은 아래 여섯 단계이다.

1. 테스트 대상 상태를 표현할 `Model`을 만든다.
2. `Project<Model>`을 상속한 테스트 프로젝트를 만든다.
3. `CreateLabs(Model model)`에서 현재 상태에 맞는 `Lab` 목록을 반환한다.
4. 각 `Lab`에 Arrange, Act, Assert를 넣는다.
5. `Run(...)` 또는 `Execute(...)`로 테스트를 실행한다.
6. XML 리포트나 실패한 실행 이력을 읽는다.

```csharp
using UniTest;

public class Project : UniTest.Project<Model>
{
    public override IEnumerable<ILab<Model>> CreateLabs(Model model)
    {
        if (model.Kickboard == null)
        {
            yield return new CompactLab<Model>("Ignite")
            {
                Actor = m =>
                {
                    m.Kickboard = new SingleStatedKickboard();
                    m.rider = null;
                    m.isDisposed = false;
                },
                Asserter = Check
            }.Build();

            yield break;
        }

        // 현재 상태에 따라 실행 가능한 Lab을 이어서 반환한다.
    }

    private void Check(Model model)
    {
        Assert.AreEqual(model.isDisposed, model.Kickboard.IsDisposed);
        Assert.AreSame(model.rider, model.Kickboard.Rider);
    }
}
```

실행은 Unity 안에서 아래처럼 시작할 수 있다.

```csharp
await new SingleState.Project()
    .Run(
        Configuration.ProjectPath,
        "SingleState",
        depth: 5,
        printResult: true);
```

이 흐름만으로도 `Ignite`에서 시작해, 현재 상태에서 가능한 모든 후속 테스트를 일정 깊이까지 자동으로 전개할 수 있다.

---

## 3. 클래스와 메서드 선택 기준

Uni Test 코드는 처음 보면 타입이 많아 보이지만, 사용자가 직접 잡아야 하는 축은 많지 않다.

| 대상 | 쓰는 상황 | 역할 |
| --- | --- | --- |
| `Model` | 테스트 대상과 예상 상태를 함께 들고 가야 할 때 | `Subject`, mock 상태, 실행 보조 데이터를 저장 |
| `Project<TModel>` | 테스트 전체의 진입점을 만들 때 | 현재 `Model`에서 가능한 `Lab` 목록을 생성 |
| `Lab<TModel>` | metadata, 예상 예외, 확장 조합이 필요할 때 | Arrange, Act, Assert를 직접 정의 |
| `CompactLab<TModel>` | 단순한 AAA만 빠르게 작성할 때 | `Lab<TModel>`을 간단한 delegate 형태로 구성 |
| `TestCase` | 다중 상태 테스트에서 어떤 동작을 만들지 전달할 때 | 하위/상위 테스트 생성기 사이의 선택 조건 전달 |
| `Merge(...)` | 현재 Lab에 실제 동작 template을 합칠 때 | 하나의 Lab 안에 상태 조건과 실제 Act를 결합 |
| `Extend(...)` | 기존 Lab 위에 상태 검증 계층을 덧붙일 때 | `CompositeLab`을 만들어 Arrange/Assert 계층을 추가 |
| `Run(...)` | Unity 실행 중 결과 XML까지 뽑고 싶을 때 | 전체 실행과 XML export를 함께 처리 |
| `RunContinuously(...)` | 모든 조합 대신 하나의 지속 가능한 경로를 길게 보고 싶을 때 | 임의 경로를 선택해 긴 연속 테스트 수행 |
| `Execute(ids)` | 실패한 경로를 다시 실행하고 싶을 때 | 지정된 실행 이력만 재현 |

실전에서는 아래처럼 생각하면 편하다.

- 먼저 `Model`과 `Project`를 만든다.
- 단순 테스트는 `CompactLab`으로 시작한다.
- 예상 예외, metadata, 조합이 필요해지면 `Lab`으로 넓힌다.
- 다중 상태에서는 `TestCase`, `Merge`, `Extend`로 테스트 생성기를 나눈다.

---

## 4. 튜토리얼 1. 단일 상태 객체 테스트하기

첫 번째 단계는 `SingleStateTest`처럼 하나의 상태 축을 가진 객체를 테스트하는 것이다. 여기서는 킥보드가 `Idle`, `Mounted`, `Disposed` 중 어떤 상태인지 판단하고, 그 상태에서 가능한 동작만 `Lab`으로 만든다.

실제 코드는 [SingleStateTest/Model.cs](../../Samples~/Unity/Scripts/SingleStateTest/Model.cs)와 [SingleStateTest/Project.cs](../../Samples~/Unity/Scripts/SingleStateTest/Project.cs)에 있다. 테스트 대상의 원래 동작은 [SingleStatedKickboard.cs](../../Samples~/Unity/Scripts/Kickboard/SingleStatedKickboard.cs)를 기준으로 확인할 수 있다.

아래 표는 `SingleStateTest.Project`가 실제로 코드로 옮기는 상태-동작 표이다. 표기 규칙은 [[00-Unit-Test-Guideline]]의 상태-동작 표와 같다.

| State    | -   | Mount              | <                  | <    | <                  | <            | Ride               | Dismount | Dispose                    |
| -------- | --- | ------------------ | ------------------ | ---- | ------------------ | ------------ | ------------------ | -------- | -------------------------- |
|          | -   | Licensed           | Targeted           | Same | Not Licensed       | Null         |                    |          |                            |
|          | -   |                    |                    |      |                    |              |                    |          |                            |
| Idle     | -   | **Mounted**        | **Mounted**        | X    | _InvalidOperation_ | `<Dismount>` | _InvalidOperation_ | -        | **Disposed**               |
| Mounted  | -   | _InvalidOperation_ | _InvalidOperation_ | -    | _InvalidOperation_ | ^            | `[Ride]`           | **Idle** | `<Dismount>` → `<Dispose>` |
| Disposed | -   | _ObjectDisposed_   | _ObjectDisposed_   | X    | _ObjectDisposed_   | ^            | _ObjectDisposed_   | -        | -                          |

`Targeted` 열은 현재 rider가 `TargetedRider`가 아닐 때의 경로이고, 같은 rider를 다시 전달하는 경로는 `Same` 열로 읽는다. 표의 각 칸은 `CreateLabs` 안에서 하나 이상의 `Lab`으로 변환된다. 예를 들어 `Idle` 행의 `Mount` `Licensed`는 성공 Lab이고, `Not Licensed`는 예상 예외를 검증한 뒤 더 이상 경로를 확장하지 않는 Lab이다.

### 4-1. 현재 상태를 코드로 판별한다

`CreateLabs`는 현재 `Model`을 보고 후속 테스트를 만든다. 따라서 먼저 현재 상태를 안정적으로 판별하는 함수가 필요하다.

```csharp
enum KickboardState
{
    Idle,
    Mounted,
    Disposed,
}

KickboardState GetState(Model model)
{
    if (model.Kickboard.IsDisposed)
        return KickboardState.Disposed;

    if (model.Kickboard.Rider != null)
        return KickboardState.Mounted;

    return KickboardState.Idle;
}
```

상태 판별을 한 곳에 모아두면, 이후 `Mount`, `Ride`, `Dismount`, `Dispose` 테스트를 같은 기준으로 나눌 수 있다.

### 4-2. 시작 상태는 별도로 만든다

처음에는 테스트 대상 객체가 없다. 이때는 `Ignite` Lab을 만들어 객체를 생성하고, 예상 상태를 초기화한다.

```csharp
if (model.Kickboard == null)
{
    labs.Add(new("Ignite")
    {
        Actor = m =>
        {
            m.Kickboard = new();

            m.rider = null;
            m.isDisposed = false;
            m.TargetedRider = new(true, "Targeted Rider");
        },
        Asserter = Check
    });

    return labs.Select(l => l.Build());
}
```

이 Lab이 테스트 그래프의 실제 출발점이 된다. 이후 생성된 Node들은 이 `Ignite` 실행 이력을 다시 재생한 뒤, 다음 Lab을 붙여 독립적인 후속 상태를 만든다.

### 4-3. 상태별로 가능한 동작만 추가한다

예를 들어 `Mount`는 현재 상태에 따라 기대 결과가 달라진다. `Idle` 상태에서는 면허가 있는 사용자가 탑승할 수 있고, 면허가 없는 사용자는 예외를 발생시켜야 한다.

```csharp
case KickboardState.Idle:
    labs.Add(new("Mount_Licensed")
    {
        Arranger = m => m.rider = new Rider(true),
        Actor = m => m.Kickboard.Mount(m.rider),
        Asserter = Check
    });

    labs.Add(new("Mount_NotLicensed")
    {
        Actor = m => Assert.Throws<InvalidOperationException>(
            () => m.Kickboard.Mount(new Rider(false))),
        ToUncontinuable = true
    });
    break;
```

`ToUncontinuable`은 이 테스트 이후 같은 경로를 더 확장하지 않겠다는 의미이다. 실패를 기대하는 동작이나 의미상 종료된 경로에 붙이면, 불필요한 후속 테스트 생성을 줄일 수 있다.

### 4-4. 상태 변화는 mock을 먼저 바꾸고 검증한다

`Dismount`처럼 상태가 바뀌는 동작에서는 `Arranger`에서 예상 상태를 먼저 바꾸고, `Actor`에서 실제 동작을 실행한 뒤, `Asserter`에서 둘을 비교한다.

```csharp
labs.Add(new("Dismount")
{
    Arranger = model => model.rider = null,
    Actor = model => model.Kickboard.Dismount(),
    Asserter = Check
});
```

이 패턴은 Uni Test에서 가장 자주 쓰는 형태이다.

1. 예상 상태를 먼저 갱신한다.
2. 실제 객체에 동작을 실행한다.
3. 예상 상태와 실제 상태가 같은지 확인한다.

### 4-5. 종료 직후의 후속 검증은 횟수를 제한한다

객체가 `Dispose`된 뒤에도 몇 가지 동작은 확인할 수 있다. 다만 종료된 상태에서 무한히 테스트를 확장할 필요는 없으므로 `RemainingExecutionCount`를 사용한다.

```csharp
labs.Add(new("Dispose")
{
    Arranger = model =>
    {
        model.isDisposed = true;
        model.rider = null;
    },
    Actor = model => model.Kickboard.Dispose(),
    Asserter = Check,
    RemainingExecutionCount = 2
});
```

이렇게 하면 dispose 이후에도 필요한 만큼만 후속 검증을 이어가고, 그 뒤에는 경로 확장을 멈출 수 있다.

---

## 5. 튜토리얼 2. 다중 상태 테스트 조합하기

두 번째 단계는 `MultiStateTest`처럼 여러 독립 상태를 가진 객체를 테스트하는 것이다. 예를 들어 킥보드에는 탑승 상태뿐 아니라 배터리 상태와 충전 상태도 있다. 이때 모든 조합을 한 함수에서 직접 쓰면 코드가 급격히 복잡해진다.

Uni Test에서는 이 문제를 테스트 생성기를 계층적으로 나누어 해결한다.

실제 코드는 아래 파일들로 나뉘어 있다.

| 역할 | 파일 |
| --- | --- |
| 모델과 예상 상태 | [MultiStateTest/Model.cs](../../Samples~/Unity/Scripts/MultiStateTest/Model.cs) |
| 전체 테스트 진입점 | [MultiStateTest/Project.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project.cs) |
| 충전 상태 생성기 | [Project-Charge.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Charge.cs) |
| 배터리 상태 생성기 | [Project-Battery.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Battery.cs) |
| 기본 킥보드 상태 생성기 | [Project-Kickboard.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Kickboard.cs) |
| 실제 동작 template | [GetTemplates.cs](../../Samples~/Unity/Scripts/MultiStateTest/GetTemplates.cs) |
| 테스트 대상 객체 | [MultiStatedKickboard.cs](../../Samples~/Unity/Scripts/Kickboard/MultiStatedKickboard.cs) |

다중 상태 예제의 기반 표는 `00-Unit-Test-Guideline`의 상태-동작 표 형식을 그대로 따른다. 다만 Usage 문서에서는 표를 한곳에 몰아두지 않고, 각 표가 다루는 층위와 그 표를 코드로 옮긴 테스트 생성기를 나란히 둔다.

설계 순서는 기본 상태에서 확장 상태로 내려간다. 먼저 킥보드의 기본 탑승 상태를 만들고, 그 위에 배터리 조건을 추가하고, 마지막으로 충전 상태가 전체 동작을 한 번 더 제어하도록 구체화한다.

표를 코드로 읽을 때는 아래 대응을 먼저 잡으면 된다.

| 표의 위치 | 코드에서 대응되는 것 |
| --------- | -------------------- |
| 가장 위쪽 기능 열 | `TestCase`의 첫 번째 값. 예를 들어 `Mount`는 `MainTestCase.Mount`이다. |
| `<`로 묶인 하위 열 | `TestCase`의 두 번째 값. 예를 들어 `Mount` 아래의 `Licensed`는 문자열 `"Licensed"`이다. |
| 왼쪽 상태 행 | 현재 `Model`에서 판별한 상태이다. 예를 들어 `Idle`, `Mounted`, `Available`, `Charging` 같은 값이다. |
| 칸의 내용 | 그 상태와 동작 조건이 만났을 때 생성할 `Lab`이다. |
| `[base]` | 더 기본이 되는 다음 생성기로 넘긴다는 뜻이다. 코드에서는 보통 `Extend(CreateLabs_...)`로 연결된다. |
| `[Ride]`, `<Dispose>` 같은 실제 동작 | `GetTemplates`가 만드는 실제 `Actor` Lab과 연결된다. |

`GetTemplates`는 상태를 판단하는 함수가 아니라, 실제 동작을 실행하는 template을 만드는 함수이다. `CreateLabs_Kickboard`, `CreateLabs_Battery`, `CreateLabs_Charge`는 "지금 어떤 상태인가"와 "이 칸에서 어떤 결과를 기대하는가"를 정하고, `GetTemplates`는 마지막에 실제 `Actor`를 붙인다.

예를 들어 `Mount` / `Licensed` 경로는 아래처럼 이어진다.

1. `Operations` 표의 `Mount` 열은 `MainTestCase.Mount`가 된다.
2. `Kickboard` 표에서 `Mount` 아래의 `Licensed` 열은 `"Licensed"`가 된다.
3. `CreateLabs_Kickboard`의 `Licensed()`는 현재 상태 행을 보고 예상 결과를 만든다.
4. `GetTemplates(tc)`는 같은 `TestCase`를 받아 `Mount_Licensed` template을 만든다.
5. `Merge(GetTemplates(tc))`가 예상 상태 Lab과 실제 호출 Lab을 하나로 합친다.

즉 표를 읽을 때 `Licensed` 같은 이름은 실제 함수 이름이라기보다, `GetTemplates`의 실제 동작 template까지 이어지는 경로 이름으로 보면 된다.

### 5-1. Kickboard 표와 CreateLabs_Kickboard

`Kickboard` 표는 가장 상위에 있는 기본 탑승 상태를 다룬다. 이 표의 코드 층위는 [Project-Kickboard.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Kickboard.cs)의 `CreateLabs_Kickboard`이다.

| Kickboard | -   | Mount              | <    | <                  | <            | Ride               | Dismount | Dispose                    |
| --------- | --- | ------------------ | ---- | ------------------ | ------------ | ------------------ | -------- | -------------------------- |
|           | -   | Licensed           | Same | Not Licensed       | Null         |                    |          |                            |
|           | -   |                    |      |                    |              |                    |          |                            |
| Idle      | -   | **Mounted**        | X    | _InvalidOperation_ | `<Dismount>` | _InvalidOperation_ | -        | **Disposed**               |
| Mounted   | -   | _InvalidOperation_ | -    | _InvalidOperation_ | ^            | `[Ride]`           | **Idle** | `<Dismount>` → `<Dispose>` |
| Disposed  | -   | _ObjectDisposed_   | X    | _ObjectDisposed_   | ^            | _ObjectDisposed_   | -        | -                          |

`Licensed`는 별도의 사용자 동작이 아니라, `Mount` 열 아래에 있는 하위 조건이다. 따라서 아래 코드에서 `MainTestCase.Mount`는 표의 `Mount` 열을 고르고, `"Licensed"`는 그 아래의 `Licensed` 하위 열을 고른다.

```csharp
if (testCase.Confineable(0, out var _tc, MainTestCase.Mount)) // 표의 Mount 열
{
    if (_tc.Confineable(1, out tc, "Licensed")) // Mount 아래 Licensed 열
        Licensed(); // Licensed 열의 Idle / Mounted / Disposed 칸을 만든다.

    if (_tc.Confineable(1, out tc, "Same") && model.rider != null)
        Same();

    if (_tc.Confineable(1, out tc, "NotLicensed"))
        NotLicensed();

    if (_tc.Confineable(1, out tc, "Null"))
        Null();
}
```

`Licensed()` 안에서는 왼쪽 상태 행을 다시 확인한다. 예를 들어 현재 상태가 `Idle`이면 표의 `Idle` 행과 `Mount` / `Licensed` 열이 만나는 칸은 `**Mounted**`이다. 그래서 예상 rider를 먼저 채운 뒤, 실제 `Mount_Licensed` template과 합친다.

```csharp
void Licensed()
{
    if (state == State.Idle)
    {
        labs.AddRange(new Lab<Model>
        {
            ID = "idle",
            Arranger = (m, md) => m.rider = (Rider)md.Metadata,
            Asserter = Check
        }.Merge(GetTemplates(tc))); // GetTemplates(tc) -> Mount_Licensed
    }
    else if (state == State.Mounted)
    {
        labs.AddRange(new Lab<Model>("mounted",
            expectedExceptionType: typeof(InvalidOperationException),
            toUncontinuable: true)
            .Merge(GetTemplates(tc)));
    }
    else if (state == State.Disposed)
    {
        labs.AddRange(new Lab<Model>("disposed",
            asserter: Check,
            expectedExceptionType: typeof(ObjectDisposedException),
            toUncontinuable: true)
            .Merge(GetTemplates(tc)));
    }
}
```

즉 `Licensed()`는 실제 탑승을 실행하는 함수가 아니다. 표의 `Licensed` 열에서 현재 상태별로 어떤 `Lab`을 만들지 정하고, 실제 호출은 `GetTemplates(tc)`가 만든 `Mount_Licensed`가 담당한다.

### 5-2. Battery 표와 CreateLabs_Battery

`Battery` 표는 기본 킥보드 동작 위에 배터리 조건을 추가한다. 이 표의 코드 층위는 [Project-Battery.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Battery.cs)의 `CreateLabs_Battery`이다.

| Battery    | -   | Check         | <              | Mount (override) | Ride (override)            |
| ---------- | --- | ------------- | -------------- | ---------------- | -------------------------- |
|            | -   | Battery > 10% | Battery <= 10% |                  |                            |
|            | -   |               |                |                  |                            |
| Available  | -   | -             | **Discharged** | `[base]`         | `[base]` → `[Use Battery]` |
| Discharged | -   | **Available** | -              | -                | -                          |

여기서 `[base]`는 기본 킥보드 상태 층위인 `CreateLabs_Kickboard`로 넘긴다는 뜻이다. 반대로 `Discharged`에서 `Mount`나 `Ride`가 `-`인 경우에는 기본 킥보드 상태까지 내려가지 않고, 실제 동작 template만 실행해 상태가 유지되는지 확인한다.

```csharp
if (testCase.Confineable(0, out var _tc, MainTestCase.Mount)) // Battery 표의 Mount (override) 열
{
    if (!model.Kickboard.Available) // Discharged 행
    {
        labs.AddRange(new Lab<Model>("discharged", asserter: Check)
            .Merge(GetTemplates(_tc))); // 표의 '-' 칸: base로 내려가지 않고 Mount template만 실행한다.
    }
    else // Available 행
    {
        labs.AddRange(new Lab<Model>("available", asserter: Check)
            .Extend(CreateLabs_Kickboard(model, _tc))); // 표의 [base] 칸
    }
}
```

`Battery` 층위는 자신이 판단할 수 있는 조건만 처리한다. 배터리가 충분한 경우에는 기본 킥보드의 `Mount`, `Ride`, `Dismount`, `Dispose` 판단이 필요하므로 다음 생성기로 넘긴다.

### 5-3. Charge State 표와 CreateLabs_Charge

`Charge State` 표는 배터리 조건 위에 충전 상태를 추가한다. 이 표의 코드 층위는 [Project-Charge.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Charge.cs)의 `CreateLabs_Charge`이다.

| Charge State | -   | Charge                      | Do Charge     | Stop Charging    | Mount (override)                          | Ride (override)    | Dispose (override)           |
| ------------ | --- | --------------------------- | ------------- | ---------------- | ----------------------------------------- | ------------------ | ---------------------------- |
|              | -   |                             |               |                  |                                           |                    |                              |
| Not Charging | -   | `<Dismount>` → **Charging** | X             | -                | `[base]`                                  | `[base]`           | `[base]`                     |
| Charging     | -   | -                           | `[Do Charge]` | **Not Charging** | `<Stop Charging>` → `[base if available]` | _InvalidOperation_ | `<Stop Charging>` → `[base]` |
| Disposed     | -   | _ObjectDisposed_            | X             | -                | `[base]`                                  | _ObjectDisposed_   | -                            |

여기서 `[base]`는 다음 상태 층위인 `CreateLabs_Battery`로 넘긴다는 뜻이다. 예를 들어 `Ride (override)` 열에서 `Charging` 행은 `_InvalidOperation_`이므로 이 층위에서 바로 예외 Lab을 만들고, `Not Charging` 행은 `[base]`이므로 배터리 층위로 내려보낸다.

```csharp
if (testCase.Confineable(0, out tc, MainTestCase.Ride)) // Charge State 표의 Ride (override) 열
    Ride();

void Ride()
{
    if (model.charging) // Charging 행: _InvalidOperation_
    {
        labs.AddRange(new Lab<Model>("charging",
            expectedExceptionType: typeof(InvalidOperationException),
            toUncontinuable: true)
            .Merge(GetTemplates(tc)));
    }
    else // Not Charging 행: [base]
    {
        labs.AddRange(new Lab<Model>("notCharging")
            .Extend(CreateLabs_Battery(model, tc)));
    }
}
```

`Confineable`은 해당 조건으로 테스트를 만들 수 있는지 확인한다. 이 층위에서 직접 다루지 않는 흐름은 `ConfineableExcept`로 모아서 다음 단계인 `CreateLabs_Battery`로 내려보낸다.

### 5-4. Operations 표와 전체 진입점

`Operations` 표는 지금까지 만든 상태 층위들이 사용자 동작에 어떻게 연결되는지 보여준다. 이 표의 코드 층위는 [MultiStateTest/Project.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project.cs)의 `MainTestCase`와 `CreateLabs`이다.

| Operations   | -   | Mount | Ride | Dismount | Charge | Do Charge | Stop Charging | Dispose |
| ------------ | --- | ----- | ---- | -------- | ------ | --------- | ------------- | ------- |
|              | -   |       |      |          |        |           |               |         |
| Kickboard    | -   | Mount | Ride | Dismount | X      | X         | X             | Dispose |
| Battery      | -   | Mount | Ride | -        | X      | X         | X             | Dispose |
| Charge State | -   | Mount | Ride | -        | Charge | Do Charge | Stop Charging | Dispose |

다중 상태 테스트에서는 각 계층이 "지금 어떤 동작을 만들고 있는가"를 알아야 한다. 이를 위해 `Operations` 표의 열을 `MainTestCase`로 정의한다. 예를 들어 사용자가 보는 `Mount` 열은 코드에서 `MainTestCase.Mount`가 되고, `Kickboard` 표의 `Licensed` 같은 하위 조건은 그 다음 index의 `"Licensed"` 값이 된다.

```csharp
public enum MainTestCase
{
    Create,
    Mount,
    Ride,
    Dismount,
    Charge,
    DoCharge,
    StopCharging,
    Dispose,
}
```

이 값은 `TestCase`에 담겨 하위 생성기와 상위 생성기 사이를 이동한다. 설계 설명은 `Kickboard`에서 `Charge State` 방향으로 내려가지만, 실제 실행 진입점은 가장 바깥에서 동작을 제어하는 `CreateLabs_Charge`로 먼저 들어간다.

```csharp
public override IEnumerable<ILab<Model>> CreateLabs(Model model)
{
    if (model.Subject == null)
    {
        foreach (var lab in CreateLabs_Charge(model, new(MainTestCase.Create)))
            yield return lab;

        yield break;
    }

    foreach (MainTestCase testCase in Enum.GetValues(typeof(MainTestCase)))
    {
        if (testCase == MainTestCase.Create)
            continue;

        foreach (var lab in CreateLabs_Charge(model, new(testCase)))
            yield return lab;
    }
}
```

이 구조의 핵심은 `CreateLabs`가 전체 목록을 다 알 필요가 없다는 점이다. 각 상태 계층은 자신이 맡은 조건만 판단하고, 나머지는 다음 생성기로 넘긴다.

### 5-5. 실제 동작은 template으로 분리한다

다중 상태에서는 상태 조건과 실제 동작을 분리하는 편이 좋다. [GetTemplates.cs](../../Samples~/Unity/Scripts/MultiStateTest/GetTemplates.cs)는 실제 `Actor`를 가진 기본 Lab을 만든다. 앞에서 본 `Mount` / `Licensed` 경로가 끝까지 내려오면, 여기에서 `Mount_Licensed` template으로 바뀐다.

```csharp
if (testCase.Confineable(0, MainTestCase.Mount))
{
    if (testCase.Confineable(1, "Licensed")) // Kickboard 표의 Mount / Licensed 열
        yield return new()
        {
            ID = "Mount_Licensed",
            SetMetadata = _ => new Rider(true),
            Actor = (m, md) => m.Kickboard.Mount((Rider)md.Metadata)
        };
}
```

상태 생성기는 이 template을 가져와 현재 상태의 조건과 합친다. 이렇게 하면 실제 동작 정의가 여러 상태 분기 안에 흩어지는 것을 줄일 수 있다.

### 5-6. Merge와 Extend를 구분해서 사용한다

`Merge`는 현재 Lab과 동작 template을 하나의 Lab으로 합칠 때 사용한다.

```csharp
labs.AddRange(new Lab<Model>("charging", arranger: Check)
    .Merge(GetTemplates(tc)));
```

이 경우 `charging` 상태에서 template의 `Actor`를 실행하고, 현재 Lab의 검증도 같은 Lab 안에서 함께 수행한다.

반대로 `Extend`는 이미 만들어진 다른 Lab 위에 현재 상태 계층을 덧붙일 때 사용한다.

```csharp
labs.AddRange(new Lab<Model>("available", asserter: Check)
    .Extend(CreateLabs_Kickboard(model, tc)));
```

`Extend` 결과는 `CompositeLab`이다. 실행 순서는 아래처럼 이해하면 된다.

1. 기존 Lab의 Arrange를 실행한다.
2. 확장 Lab의 Arrange를 실행한다.
3. 기존 Lab의 Act를 실행한다.
4. 확장 Lab의 Assert를 실행한다.
5. 기존 Lab의 Assert를 실행한다.

즉 실제 동작은 한 곳에서만 실행하고, 주변 상태 계층은 준비와 검증을 덧붙인다.

---

## 6. 실행과 결과 읽기

### 6-1. 전체 경로 실행

가장 일반적인 실행은 `Run(...)`이다. 지정한 깊이까지 가능한 모든 경로를 전개하고, `printResult`가 켜져 있으면 XML 리포트를 저장하고 연다.

```csharp
await new SingleState.Project()
    .Run(
        Configuration.ProjectPath,
        "SingleState",
        depth: 5,
        processLimit: -1,
        timeLimit: -1,
        printResult: true);
```

`processLimit`은 실행한 Node 수 제한이고, `timeLimit`은 초 단위 시간 제한이다. 둘 다 `-1`이면 제한하지 않는다.

### 6-2. 실패한 경로만 다시 실행

실패한 실행 이력을 알고 있다면 문자열로 다시 실행할 수 있다. 실행 이력은 `/`로 구분된 Lab ID의 나열이다.

```csharp
const string History = "Ignite/Mount_Null/Mount_Targeted";

await new SingleState.Project()
    .Run(
        Configuration.ProjectPath,
        "SingleStateReplay",
        History,
        printResult: true);
```

이 방식은 전체 조합을 다시 돌리지 않고, 문제가 발생한 경로만 빠르게 재현할 때 사용한다.

### 6-3. 긴 연속 테스트

모든 조합을 전부 실행하면 테스트 수가 급격히 늘 수 있다. 이때는 `RunContinuously(...)`로 지속 가능한 경로 하나를 선택해 길게 실행할 수 있다.

```csharp
await new MultiState.Project()
    .RunContinuously(
        Configuration.ProjectPath,
        "MultiState",
        depth: 50,
        processLimit: -1,
        timeLimit: -1,
        printResult: true);
```

이 방식은 전체 조합의 완전성보다, 긴 실행 흐름 안에서 상태가 무너지지 않는지 확인하고 싶을 때 적합하다.

### 6-4. 코드 레벨에서 다시 디버그하기

XML 리포트만으로 원인을 찾기 어렵다면, 실행 결과에서 원하는 Node를 복원해 IDE 디버깅용으로 다시 실행할 수 있다.

```csharp
var root = await new SingleState.Project()
    .Execute("Ignite/Mount_Null/Mount_Targeted");

var restored = root.GetLastNode().DetachAndRestore();
restored.Execute();
```

`DetachAndRestore()`는 원본 Node 그래프를 바꾸지 않는 분리된 Node를 만든다. 이후 `restored.Execute()`에 중단점을 걸면 같은 실행 이력을 재현한 상태에서 문제 지점을 다시 볼 수 있다.

### 6-5. XML 리포트 읽기

`Run(...)` 계열은 내부적으로 Node의 XML report를 export한다. 성공한 경우에는 전체 report를, 실패한 경우에는 실패 Node만 남긴 report를 저장한다.

리포트에서 특히 볼 부분은 아래 세 가지이다.

- Node 이름: 어떤 Lab이 실행되었는지 나타낸다.
- `History`: 해당 Node까지 도달한 실행 이력이다.
- `Model`: 그 시점의 테스트 대상과 예상 상태를 읽기 위한 문자열이다.

따라서 `Model.ToString()`을 보기 좋게 작성해 두는 것이 중요하다. XML 리포트는 결국 실패한 경로에서 "어떤 상태였고, 어떤 Lab을 실행했는가"를 읽기 위한 도구이기 때문이다.

---

정리하면 Uni Test 사용 흐름은 아래와 같다.

1. 상태-동작 표를 먼저 만든다.
2. `Model`에 실제 객체와 예상 상태를 함께 둔다.
3. `Project.CreateLabs`에서 현재 상태에 맞는 `Lab`을 생성한다.
4. 단일 상태는 상태 분기와 `CompactLab`으로 시작한다.
5. 다중 상태는 `TestCase`, `Merge`, `Extend`로 생성기를 나눈다.
6. `Run`, `RunContinuously`, `Execute(ids)` 중 목적에 맞는 실행 방식을 선택한다.

앞의 문서가 Uni Test의 구조와 구현 방식을 설명한다면, 이 문서는 그 구조를 실제 테스트 코드로 옮기는 첫 번째 사용 흐름을 담당한다.
