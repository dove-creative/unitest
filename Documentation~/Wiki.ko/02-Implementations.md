# 목차

1. Uni Test 구조

2. Lab의 동작
	2.1 Lab의 동작
	2.2 Compact Lab의 동작
	2.3 Composite Lab의 동작
	2.4 Merge를 통한 Lab의 확장

3. Node의 동작
	3.1 Node의 동작
	3.2 Xml 리포트 생성

4. Project의 동작
	4.1 Project 동작 파이프라인
	4.2 Test Case의 이해

5. 테스트 설계
	5.1 단일 상태를 가지는 시스템의 테스트 설계
	5.2 다수의 독립된 상태를 가지는 시스템의 테스트 설계

---
## 1. Uni Test 구조

Uni Test는 아래와 같은 구조를 가진다.

<img src="../Resources/02-image1.png" alt="UniTest structure diagram" width="655">

구성 요소

**Model: 테스트 대상 단위 객체 인터페이스**
- `Subject`: 실제 테스트가 이루어지는 대상 객체
- `Metadata Group`: 각 Lab에서 지정한 메타데이터 저장소
- `Model Metadata`: Model의 테스트 진행 정보 (지속가능 여부, 남은 테스트
  횟수 등)

**Subject Metadata: 각 Lab에서 지정한 메타데이터**
- `Metadata`: 해당 Lab에서 지정한 메타데이터 (object)
- `Expected Exception Type`: 해당 Lab이 예상한 Act 수행 시 반환될 예외
  타입
- `Model Metadata`: 해당 Lab이 예상한 모델의 실행 상태 정보

**ILab: Lab 공통 인터페이스**
- `ID`: Lab 식별자
- `Execute(IModel model)`: model에 대해 테스트 수행

**Lab: 단일 AAA 테스트 단위**
- `SetMetadata(IModel) => object`: Arrange 직전 Model에 저장할 Metadata 반환
- `Arranger(IModel, SubjectMetadata)`: 테스트 초기 상태 설정
- `Act(IModel, SubjectMetadata)`: 테스트 동작 수행
- `Assert(IModel, SubjectMetadata)`: 테스트 결과 검증

**Composite Lab: 복수 상태 통합 테스트**
- `CompositeLab(Lab original, Lab extension)`: original Lab을 바탕으로 extension Lab 확장
- `Extend(Lab extension)`: 현재 조합 구조에서 새로운 Lab 추가

**Compact Lab: Lab 빌더** (단일 상태 테스트에서 SubjectMetadata를 사용하지 않을 경우, 각 AAA 함수를 IModel만으로 구성하는 간결한 Lab 빌더)

**Node: 테스트 실행 단위** (하나의 테스트에 대응하는 Model과 Lab 쌍)
- `ID`: Node 식별자 (Lab ID와 동일)
- `Model`, `Lab`: 해당 테스트에 해당하는 Model과 Lab 객체
- `Before`, `Afters`: 이전/이후 테스트에 해당하는 연결된 Node들
- `Execute()`: 저장된 Lab을 자신의 Model에 대해 테스트 실행

**Project: 테스트 전체 실행 컨트롤러**
- `Execute(int depth)`: 지정한 깊이까지 가능한 모든 테스트 실행
- `Terminate()`: 테스트 실행 중단
- `CreateLabs(IModel) => IEnumerable<ILab>` : 입력된 model에 대해 가능한 Labs 생성 (템플릿 메서드)

**Test Designer: 다음 테스트 생성기**
- `Execute()`: 현재 Model에 대해 가능한 다음 단계 Nodes 생성

**Test Executor: 테스트 실행기**
- `Execute()`: 현재 Node.Execute를 실행하여 테스트 진행

**TestCase: 테스트 실행 정보**
- `TestCases`: 테스트의 단계별 실행 정보

---

## 2. Lab의 동작

### 2-1 Lab의 동작

Lab은 Model에 대해 실제 테스트를 수행하는 실행 단위 객체이다.

Lab은 Model과 철저히 분리된 불변 객체로, 내부에 어떤 테스트 대상 데이터도 보유하지 않는다.이러한 구조는 Lab의 재사용성과 병렬 처리 안전성을 확보하며, 동일한 Lab이 다양한 테스트 환경에서 반복적으로 활용될 수 있도록 한다.

Lab은 아래와 같이 작업을 수행하는 메서드를 가진다.

#### 2-1.1. 생성과 초기화

Lab은 아래의 인자를 가지고 준비 상태를 마친다.

1.  `ID`: Lab 식별자

2.  AAA 과정 수행 인자
    - `SetMetadata(IModel) => object` : 테스트 직전 Model에 저장할 메타데이터 생성
    - `Arranger(IModel, SubjectMetadata)`: Model을 초기 상태로 설정
    - `Act(IModel, SubjectMetadata)`: 테스트 동작 수행
    - `Assert(IModel, SubjectMetadata)`: 테스트 결과 검증

3.  Subject Metadata
    - `ExpectedExceptionType`: Act 수행 시 예상되는 예외 타입
    - `ToUncontinuable`: true일 경우, 해당 테스트를 마지막으로 종료
    - `RemainingExecutionCount`: 잔여 테스트 수행 횟수

#### 2-1.2. Do Arrange

Arrange 단계에서 Model의 초기 상태를 구성한다.

1.  Lab의 Subject Metadata를 복사하여 새로운 Subject Metadata 생성 (이 시점부터 해당 메타데이터는 Model의 소유로 간주)
2.  SetMetadata를 호출하여 테스트용 상태 정보를 생성하고, 복사된 메타데이터에 저장
3.  완성된 SubjectMetadata를 Model.MetadataGroup에 저장
4.  Arranger 대리자를 실행하여 입력 Model에 대해 실제 Arrange 과정 수행

#### 2-1.3. Do Act

Actor 대리자를 수행하여 실제 Act 과정을 수행한다. 이때 예상되는 반환 예외 타입을 기반으로, 예외가 발생해야 하는데 발생하지 않은 경우와 예외가 발생하지 않아야 하는데 발생한 경우 각각 실패 예외를 반환하도록 한다.

#### 2-1.4. Do Assert

Asserter 대리자를 수행하여 실제 Assert 과정을 수행한다. 이때 Asserter는 테스트 결과가 예상과 다를 경우 실패 예외를 반환한다.

Lab이 독립적으로 실행되는 경우, 해당 Lab의 Execute 메서드에서 DoArrange, DoAct, DoAssert 메서드를 순서대로 호출하여 테스트를 진행한다. 만약 Lab이 CompositeLab의 일부로 포함된 경우, CompositeLab이 Lab의 해당 메서드들을 순차적으로 호출한다.

Lab.Execute는 아래의 순서로 전체 테스트를 수행한다.

0. Model에 실험 기록 추가 및 Model.MetadataGroup 초기화
1. Do Arrange 실행, 오류 반환 시 테스트 중지 및 Model의 후속 테스트 금지
2. Do Act 실행, 오류 반환 시 테스트 중지 및 Model의 후속 테스트 금지
3. Do Assert 실행, 오류 반환 시 테스트 중지 및 Model의 후속 테스트 금지
4. Model.Metadata에 저장된 테스트 지속성/남은 테스트 횟수를 Model에 반영 후 테스트 종료

### 2-2. Compact Lab의 동작

Compact Lab은 Subject Metadata 없이도 Lab을 구성할 수 있도록 하는 빌더 객체이다. 이는 단일 상태 테스트처럼 메타데이터를 사용하지 않는 환경에서, Model만을 이용해 간단하게 테스트를 정의하고 실행할 수 있도록 설계되었다.

Compact Lab은 아래와 같은 인자를 사용하며, Build() 메서드 호출 시 일반 Lab을 생성한다.

| Compact Lab               | Build | Lab                                       |
| ------------------------- | ----- | ----------------------------------------- |
| ID                        | →     | ID                                        |
| 해당 없음                     | →     | SetMetadata (\_ => null)                  |
| Arranger (IModel)         | →     | Arranger (IModel, SubjectMetadata = null) |
| Actor (IModel)            | →     | Actor (IModel, SubjectMetadata = null)    |
| Asserter (IModel)         | →     | Asserter (IModel, SubjectMetadata = null) |
| 해당 없음 (Assert.Throws로 대체) | →     | expectedExceptionType (null)              |
| ToUncontinuable           | →     | ToUncontinuable                           |
| RemainingExecutionCount   | →     | RemainingExecutionCount                   |

특징

- IModel만을 기반으로 AAA 구조 구현
- 내부적으로 SubjectMetadata는 항상 null로 전달되어 처리됨
- ExpectedExceptionType 대신 Assert에 Assert.Throws를 정의함으로써 예외
  처리 구현

### 2-3. Composite Lab의 동작

Composite Lab은 여러 개의 Lab을 순차적으로 연결하여 하나의 테스트 흐름을 구성하는 객체이다. Lab은 각각 독립적인 상태 단위를 테스트하며, Composite Lab은 이들을 적절한 순서로 호출하여 전체 시스템의 테스트를 수행한다.

Composite Lab은 아래의 두 메서드를 가진다.

- `Extend(Lab extension)`: 입력 Lab을 기존 테스트 체인의 최하위로 추가하여 테스트 연장
- `Execute`: 등록된 Lab들을 올바른 순서로 실행하여 전체 테스트 수행 

Composite Lab의 Execute는 아래와 같은 순서로 동작한다.

#### 2-3.0. 사전 처리

- Model에 테스트 실행 기록을 추가하고, MetadataGroup을 초기화한다.

#### 2-3.1. Do Arrange (상위 → 하위 순서)

- 각 Lab의 DoArrange를 상위 Lab부터 하위 Lab까지의 순서로 실행한다. 원본 상태를 먼저 초기화한 뒤 확장 상태를 설정해야 올바른 초기화가 이루어질 수 있다.
- 예외가 발생하면 테스트를 중단하고, 해당 Model에 대해 후속 테스트를 금지한다.

#### 2-3.2. Do Act (최상위 Lab만 실행)

- 최상위 Lab의 DoAct만 수행한다. 하나의 테스트 흐름에서는 하나의 Act만 수행하는 것이 원칙이기 때문이며, 중복 실행은 의도하지 않은 부작용을 유발할 수 있기 때문이다.
- 예외가 발생하면 테스트를 중단하고, 해당 Model에 대해 후속 테스트를 금지한다.

#### 2-3.3. 유효성 판단

- 모든 Lab의 SubjectMetadata.ToUncontinuable 값을 검사하여, 하나라도 true인 경우 테스트를 즉시 중지한다. 지속 불가능한 상태가 하나라도 발생했다면, Model 자체가 신뢰할 수 없는 상태로 전환되었을 수 있기 때문이다.

#### 2-3.4. Do Assert (하위 → 상위 순서)

- 각 Lab의 DoAssert를 하위 Lab부터 상위 Lab까지의 역순으로 실행한다. 확장 상태의 유효성을 먼저 검증한 뒤, 그 위에 쌓인 원본 상태를 검증해야 문제 발생 시 정확한 책임 추적이 가능하기 때문이다.

- 예외가 발생하면 테스트를 중단하고, 해당 Model에 대해 후속 테스트를 금지한다.

#### 2-3.5. 테스트 상태 반영

- 최상위 Lab이 보유한 ModelMetadata에 따라 Model의 테스트 지속 여부, 남은 테스트 횟수 등을 업데이트한 후 테스트를 종료한다.

### 2.4 Merge를 통한 Lab의 확장

Lab은 테스트 유형에 따라 다양한 방식으로 확장/파생될 수 있다. 예를 들어 ‘수치를 증가시키는 Act'를 테스트하려는 경우, 아래와 같이 여러 세부케이스가 존재할 수 있다.

- 수치가 1만큼 증가
- 수치가 10만큼 증가
- 수치가 기본인자만큼 증가
- 수치기 0만큼 증가
- 수치가 음수만큼 증가 등

이처럼 다양한 상황을 모두 개별 Lab으로 구현하는 것은 비효율적이므로, 먼저 기본적인 상황에서의 AAA 로직을 정의하고, 상황에 따라 각 요소를 수정하여 Lab을 확장하는 방식을 사용하면 다양한 상황의 테스트를 효율적으로 생성할 수 있다.

Merge를 통하여 Lab을 확장하는 예시는 아래와 같다.

1. 기본 Lab 정의

```csharp
var template = new Lab<Model>("Increase")
{
	Arranger = (model, metadata) =>
		model.value = (int)metadata.Metadata.value,
	
	Actor = (model, metadata) =>
		model.Increase((int)metadata.Metadata.value),
	   
	Asserter = (model, metadata) =>
		Assert.AreEqual(model.value, model.Subject.value)
 };
```

2. 기본 Lab을 기반으로 개별 Labs 정의

```csharp
yield return new Lab<Model>("1")
{
	SetMetadata = _ => 1,
}.Merge(template);

yield return new Lab<Model>("10")
{
	SetMetadata = _ => 10,
}.Merge(template);

yield return new Lab<Model>("default")
{
	Arranger = (model, _) => model.value = model.defaultValue,
	Actor = (model, _) => model.Increase()
}.Merge(template, useArranger: false, useActor: false);

yield return new Lab<Model>("0")
{
	SetMetadata = _ => 0,
}.Merge(template);

yield return new Lab<Model>("negative")
{
	SetMetadata = _ => -1,
	Arranger = (_, metadata) =>
	{
		metadata.ToUncontinuable = true;
		metadata.ExpectedExceptionType = typeof(ArgumentOutOfRangeException);
	},
}.Merge(template, useArranger: false, useAsserter: false);
```


이는 각각 아래의 Lab들을 생성한 효과를 가진다.

```csharp
yield return new Lab<Model>("1")
{
	SetMetadata = _ => 1,
	
	Arranger = (model, metadata) =>
		model.value = (int)metadata.Metadata.value,
	Actor = (model, metadata) =>
		model.Increase((int)metadata.Metadata.value),
	Asserter = (model, metadata) =>
		Assert.AreEqual(model.value, model.Subject.value);
};

yield return new Lab<Model>("10")
{
	SetMetadata = _ => 10,
	
	Arranger = (model, metadata) =>
	     model.value = (int)metadata.Metadata.value,
	Actor = (model, metadata) =>
	     model.Increase((int)metadata.Metadata.value),
	Asserter = (model, metadata) =>
	     Assert.AreEqual(model.value, model.Subject.value);
};

yield return new Lab<Model>("default")
{
	Arranger = (model, _) =>
		model.value = model.defaultValue,
	Actor = (model, _) =>
		model.Increase(),
	Asserter = (model, metadata) =>
		Assert.AreEqual(model.value, model.Subject.value)
};

yield return new Lab<Model>("0")
{
	SetMetadata = _ => 0,
	
	Arranger = (model, metadata) =>
		model.value = (int)metadata.Metadata.value,
	Actor = (model, metadata) =>
		model.Increase((int)metadata.Metadata.value),
	Asserter = (model, metadata) =>
		Assert.AreEqual(model.value, model.Subject.value);
};

yield return new Lab<Model>("negative")
{
	SetMetadata = _ => -1,
	Arranger = (_, metadata) =>
	{
		metadata.ToUncontinuable = true;
		metadata.ExpectedExceptionType = typeof(ArgumentOutOfRangeException);
	},
	Actor = (model, metadata) => model.Increase((int)metadata.Metadata.value)
};
```

Composite Lab을 사용한 Lab의 결합과 Merge를 사용한 Lab의 결합은 아래와 같은 차이점이 존재한다.

| 구분 | Composite Lab | Merge |
|---|---|---|
| 목적 | 하나의 Act에 대한 여러 상태 단위 테스트를 조합하여 하나의 통합 테스트 구성 | 하나의 상태 단위에서 기본 테스트를 기반으로 여러 파생 테스트 생성 |
| 실행 순서 | 하위 → 상위 Lab 순차 실행; DoArrange (상위 → 하위); DoAct (최상위 1회); DoAssert (하위 → 상위) | 템플릿 Lab 실행 → 파생 Lab 실행; 템플릿 Arrange → 파생 Arrange; 템플릿 Act → 파생 Act; 템플릿 Assert → 파생 Assert |
| 상위 Lab의 선택적 실행 | 불가: 모든 Lab이 고정 순서로 실행 | 가능: 파생 Lab 생성 시 불필요한 단계 건너뛰기 가능 |
| 상태 단위 리포트 | 지원: 실패 Lab이 어느 단계에 속하는지 리포트에 표시됨 | 미지원: 템플릿/파생 중 어느 단계에서 실패가 발생했는지 리포트에 표시되지 않음 |

---

## 3. Node의 동작

### 3-1. Node의 동작

Node는 하나의 테스트 실행 단계를 나타내는 객체로, Model과 Lab의 쌍으로 구성된다.
Node는 테스트 흐름의 단일 단계를 표현하며, 테스트의 선후 관계를 나타내는 Before, Afters 필드를 통해 테스트 전이 흐름을 구성할 수 있다.

Node는 아래와 같이 작업을 수행하는 메서드를 가진다.

#### 3-1.1. 생성과 초기화

Node는 아래의 두 방법으로 생성할 수 있다.

1.  Node(Lab lab)
	- 테스트의 첫 번째 실행 단계를 생성할 때 사용된다.
	- 새로운 Model을 생성하며, 전달받은 lab을 실행 대상 Lab으로 설정한다.

2. Node(Node before, Lab lab)
	- 테스트의 두 번째 이후 단계를 생성할 때 사용된다.
	- 새로운 Model을 생성하며, 전달받은 lab을 실행 대상 Lab으로 설정한다.
	- before Node를 현재 Node의 Before 필드에 저장하고, 자신을 before.Lab.Afters 목록에 추가하여 노드 간 연결을 형성한다.
	- 이어서 자신의 이전 Node들을 재귀적으로 탐색하여 해당 Node들의 Lab을 순차적으로 저장하고, 이 Lab들을 현재 Node의 Model에 순서대로 실행함으로써 Model의 상태를 입력된 Lab이 요구하는 초기 상태와 동일하게 맞춘다.

#### 3-1.2. 테스트 수행

Node는 Execute 메서드를 통해 저장된 Lab을 해당 Model에 실행함으로써 테스트를 수행한다. 테스트 수행 도중 테스트 실패 또는 예외가 발생한 경우, Node는 해당 예외를 자신의 Exception 필드에 저장하며, Model.Continuable 값을 false로 설정하여 이후 테스트가 수행되지 않도록 차단한다. 또한 테스트 과정에서 예외가 발생하지 않았더라도, 외부에서 SetExternalException 메서드를 호출하면 사용자 중단, 테스트 환경 오류 등 외부 요인에 의한 강제 중단 예외를 수동으로 주입할 수도 있다.

### 3-2. Xml 리포트 생성

Node는 자신의 상태, 테스트 결과, 후속 Node의 연결 관계 등 테스트 실행 흐름에 대한 정보를 Xml 형식으로 보관한다. 이 리포트는 사용자가 테스트 전개 과정을 추적하고, 어떤 테스트가 어떤 맥락에서 실패했는지를 확인할 수 있도록 한다.

Node는 아래의 Xml 정보를 가진다.

- Inner Text: 테스트 진행 정보
- Root Node: 테스트의 시작 지점이며, Model과 Lab을 가지지 않음
- Waiting For Execution: 테스트가 아직 실행되지 않은 상태
- Success/Failed: 진행된 테스트의 성공/실패 여부
- Report: 실패 예외 정보
- Model: 현재 Node가 보유한 테스트 대상 모델 객체 정보
- History: 현재까지 Model에 수행된 Lab들의 ID 목록
- Continuable: 이후 테스트를 수행할 수 있는 상태인지 여부
- Error: XML 리포트 생성 중 오류가 발생한 경우 해당 예외 정보
- Child Node: 이후 진행된 Node의 Xml 파일

사용자는 아래 확장 메서드를 사용하여 실험 정보를 선택적으로 가져올 수 있다.

- Count(Node) => int : 해당 Node 이후 생성된 Node의 수
- All Succeed(Node) => bool : 해당 Node 이후 모든 테스트가 성공했는지의 여부
- Get Failed Nodes(Node) => XmlNode : 해당 Node 이후 실패한 모든 테스트 Node의 Xml 정보 반환

---

## 4.Project의 동작

### 4-1. Project 동작 파이프라인

Peoject는 각 Node의 Execute 메서드를 호출하여 테스트를 수행하고, 이후 Node.Model의 상태에 따라 후속 Node들을 생성하여 연속적인 테스트 흐름을 구성하는 핵심 객체이다.

Project는 아래의 구조로 이루어진다.

<img src="../Resources/02-image2.png" alt="UniTest project structure diagram" width="697">

구성 요소

- `Root Node`: 테스트 시작점이 되는 Node로, 최초 Idle Node에 해당
- `Idle Nodes`: 테스트 주기의 첫 번째 단계(Design)를 대기 중인 Node들의 집합
- `Prepared Nodes`: 테스트 주기의 두 번째 단계(Execute)를 대기 중인 Node들의 집합

메서드

- `Execute`: 전체 테스트 과정 실행
- `Create Labs` (템플릿 메서드): Design 과정 중 주어진 Node.Model에 대해 실행 가능한 Labs 생성

Project가 동작하는 과정은 아래와 같다.

#### 4-1.0. Root Node 생성

Root Node는 모든 테스트 Node의 시작점이 되는 Node로, Model과 Lab을 갖지 않는다. Root Node는 Project.Execute 과정이 실행되기 전 Idle Nodes에 저장된다.

#### 4-1.1. 후속 Node 생성

Project.Execute의 첫 번째 과정으로, 현재 Idle Nodes에 있는 모든 Node들에 대해 후속 Node들을 생성하는 과정이다. 이때 하나의 Node에 대한 후속 Node 생성은 Test Designer라는 객체가 맡는다.

Test Designer는 Project.CreateLabs 메서드를 사용하여 입력 Node에 대해 후속 테스트를 생성하고, 이를 기반으로 후속 Node들을 생성한다. 이렇게 생성되는 후속 Node들은 원본 Node에 있던 Model의 복사본(재현본)과 Lab을 가지게 된다. 이후 생성된 후속 Node들은 Project.Prepared Nodes에 저장된다.

Model 복사는 새로운 Model을 생성한 후, 이전 Lab들을 순차적으로 재실행하여 상태를 복원하는 방식으로 이루어지기 때문에, 테스트에 따라서는 많은 자원을 소모할 수 있다. 이에, Test Designer의 작업은 비동기로 진행된다.

#### 4-1.2. Node의 테스트 수행

Project.Execute의 두 번째 과정으로, 현재 Prepared Nodes에 있는 모든 Node들에 대해 Execute 메서드를 호출하여 실제 테스트를 수행하는 과정이다. 이때 하나의 Node에 대한 테스트의 수행은 Test Executor라는 객체가 맡으며, Node.Execute의 실행을 비동기 환경에서 수행하여 전체 테스트의 흐름이 중단되지 않도록 한다.

테스트가 완료된 Node는, 이어서 테스트가 가능한 상태라면 Project.Idle Nodes에 저장되고, 그렇지 않은 상태라면 Project에 저장되지 않는다. Node가 Project에 저장되지 않더라도 사용자는 Root Node에서 트리 형태로 이어지는 Node 체인을 통해 완료된 Node에 접근할 수 있다.

#### 4-1.3. 테스트 종료

Project.Execute는 아래와 같은 조건들을 통해 중지될 수 있다.

1.  모든 Node가 사전에 설정한 테스트 깊이를 충족
2.  테스트 제한 시간 초과
3.  테스트 중 예외 발생
4.  사용자가 직접 테스트 중지 등

테스트가 중지되면 Project는 Root Node를 반환하며, 이를 통해 사용자가 전체 테스트의 진행 과정을 확인할 수 있다.

### 4.2 Test Case의 이해

#### 4-2.1. Test Case의 확장

앞서 ‘2.4 Actor DTO를 사용한 Lab의 확장’에서 살펴본 것처럼, 테스트는 그 유형에 따라 다양한 방식으로 확장 및 파생될 수 있다. 예를 들어 ‘수치를 증가시키는 Act’를 테스트하려는 경우, 다음과 같은 세부 케이스가 존재할 수 있다.

- 수치를 1만큼 증가
- 수치를 10만큼 증가
- 수치를 기본인자만큼 증가
- 수치를 0 혹은 음수만큼 증가 등

여러 독립된 상태를 가지는 객체의 경우, 하위 상태를 담당하는 부분이 상위 단계의 세부적인 테스트 상황들을 모두 고려하여 테스트를 작성하게 된다면 그 구조가 매우 복잡해질 수 있다. 이를 해결하기 위해 UniTest에서는 다음과 같은 전략을 사용한다.

- 하위 상태에서는 보편적 명령만을 정의한다.
- 실제 테스트를 구성하는 단계에서는, 각 세부 케이스에 해당하는 테스트를 확장하여 구성한다.

Test Case는 이러한 테스트 명령을 구체화 단계별로 순차적으로 저장하는 객체이다. 예를 들어, '수치를 1만큼 증가'라는 명령은 다음과 같은 두 단계로 나뉘어 저장된다.

- 1단계: '수치 증가' (명령 유형)
- 2단계: '1만큼 증가' (구체적 실행 조건)

만약 상위 객체가 첫 단계인 ‘수치 증가’만 작성하였다면, 구성 부분에서는 해당 단계에 해당하는 모든 케이스로 테스트를 확장하여 구성한다. 만약 Test Case에 첫 번째와 두 번째 항목이 모두 작성되어 있다면, 구성 단계에서는 해당하는 테스트 한 건만 작성한다.

#### 4-2.2. Test Case의 배타적 정의

State Table을 활용하여 테스트를 구성했을 경우, Model에 적용할 수 있는 테스트는 Model이 가질 수 있는 모든 독립 상태 조합에 기반하여 작성되기 때문에 하위 상태에서는 자신과 무관한 테스트 명령이 입력될 수 있다. 예를 들어, 상위 상태로 ‘탑승 상태’를, 하위 상태로 ‘충전 상태’를 가지는 킥보드 객체의 경우, ‘킥보드 하차’라는 행동은 ‘충전 상태’에 아무런 의미를 가지지 않거나, 관심 대상이 아닐 수 있다.

이처럼 하위 상태는 입력된 Test Case가 자신과 무관할 경우, 자신의 Arranger / Asserter를 설정하지 않음으로써 자신의 단계에 해당하는 테스트를 pass할 수 있다.

Test Case는 입력된 정의가 자신에게 해당하는지 판단하고, 필요한 경우 보다 정밀하게 구체화된 Test Case를 제공하기 위해 다음과 같은 메서드들을 구현한다.

- `Confineable(int index, out confined, object\[\] definitions) => bool`
	주어진 단계(index)의 항목이 definitions에 포함되는 경우 true를 반환하며, 해당 조건을 만족하는 구체화된 TestCase를 confined로 반환

- `ConfineableExcept(int index, out confined, object\[\] definitions) => bool`
	주어진 단계(index)의 항목이 definitions에 포함되지 않는 경우 true 반환하며, 동시에 해당 조건을 제외한 구체화된 TestCase를 confined로 반환

또한, Test Case는 자신을 구체화할 수 있는 아래의 메서드들을 구현한다.

- `Append(object definition, bool include)`: 다음 단계를 definition을 include(포함/제외)하도록 구체화
- `Confine(int index, object definition)`: 특정 단계의 항목을 해당 정의로 고정
- `Include(int index, object[] definitions)`: 특정 단계의 항목이 입력된 목록 전부를 포함하도록 구체화
- `Exclude(int index, object[] definitions)`: 특정 단계의 항목이 입력된 목록 전부를 포함하지 않도록 구체화

---
## 5. 테스트 설계

### 5-1. 단일 상태를 가지는 시스템의 테스트 설계

단일 상태 기반 시스템의 테스트는 다음과 같은 단계로 설계한다.

#### 5-1.1. 상태 - 동작 표 정의

먼저 객체의 단일 상태 - 동작 표를 작성하여 객체가 가질 수 있는 모든 상태와 동작을 정리한다. 이 표를 기반으로 테스트를 설계하면 객체의 모든 유스케이스에 해당하는 테스트를 체계적으로 설계할 수 있다.

예를 들어, 전동 킥보드의 상태 - 동작 표는 아래와 같다.

상태

- Idle: 이용자 대기 상태
- Mounted: 이용자가 이용 중인 상태
- Disposed: 삭제된 상태

동작

- Mount: 이용자의 이용 시작
	- Licensed: 면허가 있는 이용자의 이용 시작
	- Same: 이미 이용 중인 이용자의 재이용 시도
	- Not Licensed: 면허가 없는 이용자의 이용 시도
	- Null: null 이용자의 이용 시도
- Ride: 이용자의 실제 킥보드 주행
- Dismount: 이용자의 이용 종료
- Dispose: 킥보드 삭제


| Kickboard | -   | Mount              | <    | <                  | <            | Ride               | Dismount | Dispose                    |
| --------- | --- | ------------------ | ---- | ------------------ | ------------ | ------------------ | -------- | -------------------------- |
|           | -   | Licensed           | Same | Not Licensed       | Null         |                    |          |                            |
|           | -   |                    |      |                    |              |                    |          |                            |
| Idle      | -   | **Mounted**        | X    | _InvalidOperation_ | `<Dismount>` | _InvalidOperation_ | -        | **Disposed**               |
| Mounted   | -   | _InvalidOperation_ | -    | _InvalidOperation_ | ^            | `[Ride]`           | **Idle** | `<Dismount>` → `<Dispose>` |
| Disposed  | -   | _ObjectDisposed_   | X    | _ObjectDisposed_   | ^            | _ObjectDisposed_   | -        | -                          |

#### 5-1.2. Model 정의

테스트에서 사용할 모델 클래스는 UniTest.Model을 상속받아 정의한다. 이 모델은 실제 테스트 대상인 Subject와 예상 상태를 담는 Mock 객체, 그리고 실험에 필요한 외부 리소스(Assets)를 포함한다.

아래는 전동 킥보드 객체를 테스트하기 위한 Model 클래스의 예시이다.

```csharp
public class Model : UniTest.Model
{
    // Subject
    public SingleStatedKickboard Kickboard
    {
        get => (SingleStatedKickboard)Subject;
        set => Subject = value;
    }

    // Mock
    public Rider rider;
    public bool isDisposed;

    // Assets
    public Rider TargetedRider;
    public int RideCount = 0;
    public void OnRide() => RideCount++;

    // Content
    public override string ToString()
    {
        var sb = new StringBuilder();
        
        sb.Append($"Rider : {Kickboard.Rider?.Name ?? "None"}, ");
        sb.Append($"Disposed : {Kickboard.IsDisposed} | ");
        sb.AppendLine(base.ToString());

        return sb.ToString();
    }
}
```

구성 요소

- `Subject`: 실제 테스트 대상 객체. `base.Subject`는 object 타입이므로, 테스트에서 사용하기 적합한 도메인 타입으로 형변환하여 사용한다.
- `Mock`: 예상 상태를 외부에서 정의함으로써, 객체 내부의 상태를 간접적으로 검증할 수 있도록 한다. 이를 통해 private 멤버에 접근하지 않고도 테스트의 정확성을 확보할 수 있다.
- `Assets`: 실험에 사용할 객체들이다.
	- `Target Rider`: 동일한 `Rider`가 재탑승하는 상황을 테스트하기 위해 사용한다.
	- `OnRide`: `Kickboard.OnRide` 대리자가 정상적으로 호출되었는지를 확인하기 위해 사용한다.
- `Content`: 기타 실험에 필요한 메서드이다. `ToString`의 경우, `Model`을 `Node`의 Xml 파일에 기록할 때 사용할 문자열을 보기 편하게 제공하는 역할을 한다.

#### 5-1.3. Project 정의

UniTest.Project 객체를 상속하여 실제 테스트를 수행하는 객체를 작성한다.

여기서는 본적적인 테스트 작성에 앞서, 테스트의 원활한 진행을 위해 아래의 두 항목을 작성하였다.

- `KickboardState`/`GetState`: 킥보드가 현재 상태–동작 표에서 어떤 상태에 속하는지를 판별하는 열거형과 메서드이다. Model을 입력받아 킥보드의 상태를 반환한다.
- `Check`: 킥보드의 실제 상태를 Mock 상태와 비교하여 일치 여부를 확인하는 메서드이다. 주로 Assert 단계에서 호출되며, 예상한 상태와 실제 상태가 일치하는지를 검증하는 역할을 수행한다.

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
    else
        return KickboardState.Idle;
}

private void Check(Model model)
{
    Assert.IsNotNull(model, "Kickboard is Null");
    Assert.AreEqual(model.isDisposed, model.Kickboard.IsDisposed, "Dispose state mismatched");
    Assert.AreSame(model.rider, model.Kickboard.Rider, "Rider mismatched");
}
```

사전 준비가 완료되었으면, CreateLabs 템플릿 메서드를 구현하여 주어진 Model에 대해 어떤 테스트 케이스들을 생성할지를 정의한다. 본 구현 예시에서는 Model.Subject가 null인지 여부에 따라 테스트 흐름을 나누어 설계하였다.

Model.Subject가 null인 경우, 즉 킥보드가 아직 생성되지 않은 경우에는, 킥보드를 생성하고 실험에 필요한 Mock 데이터를 설정한 뒤 그 유효성을 확인하는 테스트를 작성하였다.

```csharp
protected override IEnumerable<ILab<Model>> CreateLabs(Model model)
{
    var labs = new List<CompactLab<Model>>();

    // Ignite
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

    // Model이 Null이 아닐 때의 처리
    // ...
}
```

킥보드가 생성되지 않은 경우, Actor 단계에서 킥보드를 초기화하고 관련 상태를 설정한다. 이후 Check를 통해 생성된 객체의 상태가 예상 상태와 일치하는지를 검증한다.

Model.Subject가 이미 존재하는 경우에는 아래와 같이 테스트를 작성하였다.

1. 킥보드가 수행할 수 있는 모든 행동에 대해 테스트 그룹을 설계한다.
2. 각 실험 그룹에 대해 킥보드가 현재 상태에서 실행할 수 있는 테스트를 설계한다.
3. 설계한 모든 테스트를 반환한다.

```csharp
protected override IEnumerable<ILab<Model>> CreateLabs(Model model)
{
    var labs = new List<CompactLab<Model>>();

    // Model이 Null일 때의 처리
    // ...
    
    // Mount
    switch (GetState(model))
    {
        case KickboardState.Idle:
            labs.Add(new("Mount_Licensed")
            {
                Arranger = m => m.rider = new Rider(true),
                Actor = m => m.Kickboard.Mount(m.rider),
                Asserter = Check
            });
            labs.Add(new("Mount_Targeted")
            {
                Arranger = m => m.rider = m.TargetedRider,
                Actor = m => m.Kickboard.Mount(m.rider),
                Asserter = Check
            });
            labs.Add(new("Mount_NotLicensed")
            {
                Actor = m => Assert.Throws<InvalidOperationException>(() => m.Kickboard.Mount(new Rider(false))),
                ToUncontinuable = true
            });
            labs.Add(new("Mount_Null")
            {
                Arranger = m => m.rider = null,
                Actor = modml => modml.Kickboard.Mount(null),
                Asserter = Check
            });
            break;

        case KickboardState.Mounted:
          // ...

        case KickboardState.Disposed:
          // ...

    }

    // Ride
    // ...

    // Dismount
    // ...

    // Dispose
    // ...

    return labs.Select(l => l.Build());
}
```

- Mount 동작을 테스트하는 테스트의 경우 사용자가 면허가 있는 경우, Targeted Rider인 경우, 면허가 없는 경우, null인 경우를 각각 테스트하며, 예외 발생 여부와 상태 일치를 검증한다.
- 필요 시 Compact Lab에 존재하는 ToUncontinuable = true 플래그를 통해 테스트 실패 시 후속 테스트 생성을 중단하도록 지정한다.
- 모든 테스트의 설계가 완료되었을 경우, 테스트들을 Lab의 형식으로 변환하여 반환한다.

#### 5-1.4. Project 실행

Project.Execute 메서드를 호출하면 작성한 테스트를 비동기로 테스트를 수행할 수 있다. 이때 테스트의 진행 깊이를 인자로 지정하여 각 Node가 후속 테스트를 몇 단계까지 진행할지를 설정할 수 있다.

또한, Project.Run 확장 메서드를 이용하면 다음과 같은 기능을 추가로 사용할 수 있다.

- 테스트 시간 제한: 입력한 시간이 초과되면 테스트를 종료한다.
	이를 통해 무한 반복이나 과도한 시간 소모를 방지할 수 있다.

- 실패 시 실패한 테스트만 반환: 테스트 수행 중 실패한 경우, 전체 실행 결과 중 실패한 테스트만 필터링하여 반환한다.
	이를 통해 실패 원인 분석을 보다 빠르게 진행할 수 있다.

- Xml 출력: 테스트 결과를 XML 형식으로 지정된 경로에 저장한다.
	이를 외부 시각화 도구로 불러오면 테스트 흐름과 결과를 시각적으로 확인할 수 있다.

### 5-2. 다수의 독립된 상태를 가지는 시스템의 테스트 설계

다수의 독립된 상태 기반 시스템의 테스트는 다음과 같은 단계로 설계한다.

#### 5-2.1. 상태 - 동작 표 정의

먼저 객체의 상태별 상태 - 동작 표와 Operations 표를 작성하여 객체가 가질 수 있는 모든 상태와 동작을 정리한다. 예를 들어, 아래는 전동 킥보드 시스템이 Kickboard, Battery, Charge라는 세 개의 독립 상태를 가질 때의 상태–동작 표이다.

상태

- Kickboard: 킥보드의 최상위 상태
	- Idle: 이용자 대기 상태
	- Mounted: 이용자가 이용 중인 상태
	- Disposed: 삭제된 상태

- Battery: 킥보드의 전력 상태
	- Avaliable: 이용 가능 상태 (배터리가 10% 이상)
	- Discharged: 이용 불가능 상태 (배터리가 10% 미만)
	- Disposed: 킥보드가 삭제된 상태

- Charge: 킥보드의 충전 상태
	- Not Charging: 충전 중이 아닌 상태
	- Charging: 충전 중인 상태
	- Disposed: 킥보드가 삭제된 상태

동작

- Mount: 이용자의 이용 시작
- Ride: 이용자의 실제 킥보드 주행
- Dismount: 이용자의 이용 종료
- Charge: 킥보드 충전 시작
- Do Charge: 충전기의 킥보드 충전
- Stop Charging: 킥보드 충전 종료
- Dispose: 킥보드 삭제

#### Kickboard

| Kickboard | -   | Mount              | <    | <                  | <            | Ride               | Dismount | Dispose                    |
| --------- | --- | ------------------ | ---- | ------------------ | ------------ | ------------------ | -------- | -------------------------- |
|           | -   | Licensed           | Same | Not Licensed       | Null         |                    |          |                            |
|           | -   |                    |      |                    |              |                    |          |                            |
| Idle      | -   | **Mounted**        | X    | _InvalidOperation_ | `<Dismount>` | _InvalidOperation_ | -        | **Disposed**               |
| Mounted   | -   | _InvalidOperation_ | -    | _InvalidOperation_ | ^            | `[Ride]`           | **Idle** | `<Dismount>` → `<Dispose>` |
| Disposed  | -   | _ObjectDisposed_   | X    | _ObjectDisposed_   | ^            | _ObjectDisposed_   | -        | -                          |

#### Battery

| Battery    | -   | Check         | <              | Mount (override) | Ride (override)            |
| ---------- | --- | ------------- | -------------- | ---------------- | -------------------------- |
|            | -   | Battery > 10% | Battery <= 10% |                  |                            |
|            | -   |               |                |                  |                            |
| Available  | -   | -             | **Discharged** | `[base]`         | `[base]` → `[Use Battery]` |
| Discharged | -   | **Available** | -              | -                | -                          |

#### Charge State

| Charge State | -   | Charge                      | Do Charge     | Stop Charging    | Mount (override)                          | Ride (override)    | Dispose (override)           |
| ------------ | --- | --------------------------- | ------------- | ---------------- | ----------------------------------------- | ------------------ | ---------------------------- |
|              | -   |                             |               |                  |                                           |                    |                              |
| Not Charging | -   | `<Dismount>` → **Charging** | X             | -                | `[base]`                                  | `[base]`           | `[base]`                     |
| Charging     | -   | -                           | `[Do Charge]` | **Not Charging** | `<Stop Charging>` → `[base if available]` | _InvalidOperation_ | `<Stop Charging>` → `[base]` |
| Disposed     | -   | _ObjectDisposed_            | X             | -                | `[base]`                                  | _ObjectDisposed_   | -                            |

### Operations

| Operations | -   | Mount | Ride | Dismount | Charge | Do Charge | Stop Charging | Dispose |
| ---------- | --- | ----- | ---- | -------- | ------ | --------- | ------------- | ------- |
|            | -   |       |      |          |        |           |               |         |
| Kickboard  | -   | Mount | Ride | Dismount | X      | X         | X             | Dispose |
| Battery    | -   | Mount | Ride | -        | X      | X         | X             | Dispose |
| Charge State | -   | Mount | Ride | -        | Charge | Do Charge | Stop Charging | Dispose |


#### 5-2.2. Model 정의

테스트에서 사용할 모델은 앞서 단일 상태 기반 시스템의 테스트에서 정의했던 것과 같이 UniTest.Model을 상속받아 정의한다. 모델은 실제 테스트 대상인 Subject와 예상 상태를 담는 Mock 객체, 그리고 실험에 필요한 외부 리소스(Assets)를 포함한다.

아래는 전동 킥보드 객체를 테스트하기 위한 Model 클래스의 예시이다.

```csharp
public class Model : UniTest.Model
{
    // Subject
    public MultiStatedKickboard Kickboard
    {
        get => (MultiStatedKickboard)Subject;
        set => Subject = value;
    }

    // Mock
    public Rider rider;
    public int battery;
    public bool charging;
    public bool isDisposed;

    // Assets
    public Rider TargetedRider;
    public int RideCount = 0;

    public void OnRide() => RideCount++;

    public class Charger : IObservable<object>
    {
        List<IObserver<object>> observers = new();
        public IDisposable Subscribe(IObserver<object> observer)
        {
            observers.Add(observer);
            return new Token(() => observers.Remove(observer));
        }

        public void DoCharge() => observers.ForEach(o => o.OnNext(null));

        class Token : IDisposable
        {
            Action onDispose;

            public Token(Action onDispose) => this.onDispose = onDispose;
            public void Dispose()
            {
                onDispose?.Invoke();
                onDispose = null;
            }
        }
    }

    public Charger TargetCharger;

    // Content

    public override string ToString() { ... }
}
```

구성 요소

- `Subject`: 실제 테스트 대상 객체
- `Mock`: 킥보드의 예상 상태 데이터
- `Assets`: 실험에 사용할 객체
	- `Target Rider`: 동일한 `Rider`가 재탑승하는 상황을 테스트하기 위해 사용
	- `OnRide`: `Kickboard.OnRide` 대리자가 정상적으로 호출되었는지를 확인하기 위해 사용
	- `Target Charger`: 킥보드 충전 객체
- `Content`: 기타 실험에 필요한 메서드

#### 5-2.3. Project 정의

UniTest.Project 객체를 상속하여 실제 테스트를 수행하는 객체를 작성한다. 이때 테스트는 시스템이 가지는 상위 상태부터 작성해야 하며, 상위 상태에 해당하는 테스트가 하위 상태에 해당하는테스트에 의존하지 않도록 작성해야 한다.

여기서는 본적적인 테스트 작성에 앞서, 테스트의 원활한 진행을 위해 아래의 세 항목을 작성하였다.

- MainTestCase
	Project의 최상위에 존재하며, 킥보드가 수행할 수 있는 행동의 최상위 분류를 저장하고 있는 객체이다.
	이 값을 바탕으로 TestCase를 설계할 수 있다.

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

- Check
	Project의 최상위에 존재하며, 킥보드의 실제 상태를 Mock 상태와 비교하여 일치 여부를 확인하는 메서드이다.
	주로 Assert 단계에서 호출되며, 예상한 상태와 실제 상태가 일치하는지를 검증하는 역할을 수행한다.

```csharp
void Check(Model model, SubjectMetadata _ = default)
{
    Assert.IsNotNull(model, "Kickboard is Null");
    
    Assert.AreEqual(model.isDisposed, model.Kickboard.IsDisposed, "Dispose state mismatched");
    Assert.AreSame(model.rider, model.Kickboard.Rider, "Rider mismatched");
    Assert.AreEqual(model.charging, model.Kickboard.Charging, "Charging State Mismatched");
    Assert.AreEqual(model.battery, model.Kickboard.Battery, "Battery Mismatched");

    if (model.isDisposed) return;

    Assert.AreEqual(model.battery > 10, model.Kickboard.Available, "Available State Mismatched");
}
```

- GetActors(TestCase) => ActorDTOs
	TestCase를 입력하면 해당 TestCase에 해당하는 Actor DTO의 목록을 반환하는 함수이다.
	이를 이용하면 프로젝트의 모든 부분에서 모든 구체화 정도를 가지는 TestCase에 대해 실제 테스트 케이스를 구현할 수 있다.

```csharp
IEnumerable<ActorDTO<Model>> GetActors(TestCase testCase)
{
    if (testCase.Confineable(0, MainTestCase.Create)) { ... }
    if (testCase.Confineable(0, MainTestCase.Mount))
    {
        if (testCase.Confineable(1, "Licensed"))
            yield return new()
            {
                ID = "Mount_Licensed",
                SetArrangeData = _ => new Rider(true),
                Actor = (m, md) => m.Kickboard.Mount((Rider)md.Metadata)
            };

        if (testCase.Confineable(1, "Same"))
            yield return new()
            {
                ID = "Mount_Same",
                SetArrangeData = m => m.rider,
                Actor = (m, _) => m.Kickboard.Mount(m.rider)
            };

        if (testCase.Confineable(1, "NotLicensed"))
            yield return new()
            {
                ID = "Mount_NotLicensed",
                SetArrangeData = _ => new Rider(false),
                Actor = (m, md) => m.Kickboard.Mount((Rider)md.Metadata)
            };

        if (testCase.Confineable(1, "Null"))
            yield return new()
            {
                ID = "Mount_Null",
                SetArrangeData = _ => null
                Actor = (m, _) => m.Kickboard.Mount(null)
            };

        if (testCase.Confineable(1, "Targeted"))
            yield return new()
            {
                ID = "Mount_Targeted",
                SetArrangeData = m => m.TargetedRider,
                Actor = (m, _) => m.Kickboard.Mount(m.TargetedRider)
            };
    }

    if (testCase.Confineable(0, MainTestCase.Ride)) { ... }
    if (testCase.Confineable(0, MainTestCase.Dismount)) { ... }
    if (testCase.Confineable(0, MainTestCase.Charge)) { ... }
    if (testCase.Confineable(0, MainTestCase.DoCharge)) { ... }
    if (testCase.Confineable(0, MainTestCase.StopCharging)) { ... }
    if (testCase.Confineable(0, MainTestCase.Dispose)) { ... }

}
```

##### 5-2.3.1. Kickboard 테스트 정의

사전 준비가 완료되었으면, 가장 먼저 최상위 상태인 Kickboard에 대한 테스트를 작성한다. 
테테스트를 작성하기에 앞서, 해당 상태에 적용될 내부 상태 판별용 enum과 이를 반환하는 함수를 정의한다.

```csharp
enum State
{
    Ignite,
    Idle,
    Mounted,
    Disposed,
}

State GetState(Model model)
{
    if (model.Kickboard == null)
        return State.Ignite;
    if (model.isDisposed)
        return State.Disposed;
    if (model.rider == null)
        return State.Idle;
    else
        return State.Mounted;
}
```

먼저 TestCase가 Create인 경우를 처리한다. 이후에는 GetActors 함수를 활용하여 해당 케이스를 다양한 실행 방식으로 확장한다.

```csharp
IEnumerable<Lab<Model>> CreateLabs_Kickboard(Model model, TestCase testCase)
{
    if (testCase.Count == 0)
        throw new TestCaseAbsentException(nameof(testCase));

    TestCase tc;

    var state = GetState(model);

    if (testCase.Confineable(0, out tc, MainTestCase.Create))
        return new Lab<Model>()
        {
            ID = "ignite",
            Arranger = (m, md) =>
            {
                m.TargetedRider = new Rider(true, "Targeted Rider");
                m.TargetCharger = new Model.Charger();
                m.isDisposed = false;
                m.battery = (int)md.Metadata;
            },
            Asserter = Check
        }.Extend(GetActors(tc));

    // testCase가 Create가 아닐 때의 처리
    // ...
    }
}
```

Create가 아닌 경우에는 다음과 같이 각 MainTestCase에 따라 조건을 나누어 테스트를 정의한다. 이때 다중 조건 조합을 유연하게 처리하기 위해, switch가 아닌 if 분기를 사용하였다.

```csharp
IEnumerable<Lab<Model>> CreateLabs_Kickboard(Model model, TestCase testCase)
{
    // testCase가 Create일 때의 처리
    // ...

    var labs = new List<Lab<Model>>();

    if (testCase.Confineable(0, out var _tc, MainTestCase.Mount))
    {
        if (_tc.Confineable(1, out tc, "Licensed"))
            Licensed();

        if (_tc.Confineable(1, out tc, "Same") && model.rider != null)
            Same();

        if (_tc.Confineable(1, out tc, "NotLicensed"))
            NotLicensed();

        if (_tc.Confineable(1, out tc, "Null"))
            Null();
    }

    if (testCase.Confineable(0, out tc, MainTestCase.Ride)) Ride();
    if (testCase.Confineable(0, out tc, MainTestCase.Dismount)) Dismount();
    if (testCase.Confineable(0, out tc, MainTestCase.Dispose)) Dispose();

    return labs;

    // 내부 함수 정의
    // ...
```

이후 각 분기 조건에 대응하는 내부 함수들을 AAA 패턴을 기반으로 테스트를 구성하며, 상태 – 동작 표에 따라 예외 발생 여부 및 테스트 지속 가능성도 함께 정의한다.

```csharp
IEnumerable<Lab<Model>> CreateLabs_Kickboard(Model model, TestCase testCase)
{
    // testCase 분기
    // ...

    return labs;

    void Licensed()
    {
        if (state == State.Idle)
        {
            labs.AddRange(new Lab<Model>
            {
                ID = "idle",
                Arranger = (m, md) => m.rider = (Rider)md.Metadata,
                Asserter = Check
            }.Extend(GetActors(tc)));
        }
        else if (state == State.Mounted)
        {
            labs.AddRange(new Lab<Model>("mounted",
                expectedExceptionType: typeof(InvalidOperationException),
                toUncontinuable: true)
                .Extend(GetActors(tc)));
        }
        else if (state == State.Disposed)
        {
            labs.AddRange(new Lab<Model>("disposed",
                asserter: Check,
                expectedExceptionType: typeof(ObjectDisposedException),
                toUncontinuable: true)
                .Extend(GetActors(tc)));
        }
        else
            throw new InvalidTestException(state, model);
    }

    void Same() { ... }
    void NotLicensed() { ... }
    void Null() { ... }
    void Ride() { ... }
    void Dismount() { ... }
    void Dispose() { ... }
}
```

##### 5-2.3.2. Battery/Charge 테스트 정의

Battery와 Charge 상태는 Kickboard 상태와 달리 (Disposed 상태를 제외하면) 두 가지 상태만 가지므로, 별도의 enum 정의 없이 조건 분기만으로 테스트를 구성하였다.

테스트를 구성한 이후에는 필요에 따라 앞에서 정의한 CreateLabs_Kickboard 함수를 호출하여 테스트를 상위 상태로 연장하였으며, 반대로 상위 상태로 테스트가 전달되지 않고 해당 단계에서 종료되어야 하는 경우에는 GetActors 함수를 호출하여 현재 단계에서 테스트 구성을 완료하였다.

Battery 상태와 직접적으로 관련 없는, Mount와 Ride를 제외한 동작의 경우, ConfineableExcept를 활용하여 자신의 단계에서 테스트를 구성하지 않고 상위 단계에서 테스트를 구상할 수 있도록
설계하였다.

```csharp
IEnumerable<Lab<Model>> CreateLabs_Kickboard(Model model, TestCase testCase)
{
    // testCase 분기
    // ...

    return labs;

    void Licensed()
    {
        if (state == State.Idle)
        {
            labs.AddRange(new Lab<Model>
            {
                ID = "idle",
                Arranger = (m, md) => m.rider = (Rider)md.Metadata,
                Asserter = Check
            }.Extend(GetActors(tc)));
        }
        else if (state == State.Mounted)
        {
            labs.AddRange(new Lab<Model>("mounted",
                expectedExceptionType: typeof(InvalidOperationException),
                toUncontinuable: true)
                .Extend(GetActors(tc)));
        }
        else if (state == State.Disposed)
        {
            labs.AddRange(new Lab<Model>("disposed",
                asserter: Check,
                expectedExceptionType: typeof(ObjectDisposedException),
                toUncontinuable: true)
                .Extend(GetActors(tc)));
        }
        else
            throw new InvalidTestException(state, model);
    }

    void Same() { ... }
    void NotLicensed() { ... }
    void Null() { ... }
    void Ride() { ... }
    void Dismount() { ... }
    void Dispose() { ... }
}
```

##### 5-2.3.3. Project 진입점 정의

마지막으로, 템플릿 메서드인 CreateLabs를 정의하여 프로젝트의 진입점을 구성한다. 이 함수는 테스트 실행 시 가장 먼저 호출되는 함수로, 최하위 상태의 테스트 함수(CreateLabs_Charge)를 시작점으로 호출함으로써 내부적으로 전체 테스트 계층이 아래에서부터 순차적으로 수행되도록 한다.

```csharp
protected override IEnumerable<ILab<Model>> CreateLabs(Model model)
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
