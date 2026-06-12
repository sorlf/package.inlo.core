# INLO Core DataTable Technical Guide

이 문서는 INLO Core DataTable의 기술 기준과 사용 규칙을 설명합니다.

DataTable의 주 사용 흐름은 Unity Editor에서 xlsx 또는 Google Published CSV를 가져와 `DataTableAsset<TRow>`에 저장하는 것입니다. 이때 C# Row 타입이 스키마 역할을 합니다.

런타임 `CsvDataTableParser`는 별도의 보조 API입니다. 이 API는 2행 타입 선언 CSV를 직접 파싱해 메모리 `DataTable` 모델을 만들 때 사용합니다. Editor Importer와 CSV 형식이 다르므로 두 흐름을 섞지 않습니다.

결정 배경은 [ADR-0001](../Architecture/ADR-0001-datatable-primary-workflow.md)을 봅니다. Pool 연동 경계는 [ADR-0002](../Architecture/ADR-0002-pool-datatable-boundary.md)를 봅니다.

## 작업 원칙

INLO Core 작업은 루트 `AGENTS.md`를 단일 AI 협업 규칙 원천으로 봅니다. AI 작업자는 [AI Collaboration Guide](../AI_COLLABORATION.md)도 함께 읽습니다.

- 코드 변경 전에는 계획을 먼저 세우고 사람의 승인을 받습니다.
- 승인된 범위를 벗어나면 다시 계획하고 승인받습니다.
- 패키지 내부 규칙 문서를 먼저 읽고 그 기준을 따릅니다.
- Unity나 기존 패키지가 이미 제공하는 기능을 임의로 재구현하지 않습니다.
- Agent는 코드, 로직, 리팩터링, 검증을 담당합니다.
- 사람은 씬 구성, 프리팹 배치, 인스펙터 세팅, 에셋 임포트를 담당합니다.
- 씬, 프리팹, `.meta` 파일은 직접 편집하지 않습니다.

## 지원 상태

| 영역 | 상태 | 설명 |
| --- | --- | --- |
| Editor xlsx import | Stable | 첫 행 헤더, 두 번째 행부터 데이터, C# Row 필드 매핑 |
| Editor Google Published CSV import | Stable | 게시된 CSV URL 하나가 시트 하나에 대응 |
| `DataTableAsset<TRow>` | Stable | ScriptableObject 기반 런타임 조회 자산 |
| Runtime `CsvDataTableParser` | Secondary | 2행 타입 선언 CSV를 직접 파싱하는 보조 API |
| DataTable C# Schema preparation | Controlled | Importer 안에서 Preview 후 신규 파일만 생성 |
| Runtime Google request | Prohibited | 빌드 안정성 때문에 금지 |

## 책임 경계

DataTable은 데이터 시스템입니다.

DataTable은 다음을 담당합니다.

- Editor에서 xlsx 또는 Google Published CSV를 읽습니다.
- C# Row 타입과 원본 헤더를 비교해 검증합니다.
- 원본 row를 `DataTableAsset<TRow>`의 직렬화된 row 목록으로 매핑합니다.
- 런타임에서 id 기반 row 조회를 제공합니다.
- 별도 보조 API로 타입 행이 있는 CSV 텍스트를 메모리 `DataTable`로 파싱합니다.

DataTable은 다음을 담당하지 않습니다.

- PoolDatabase asset 생성 또는 갱신
- PoolManager 초기화
- 런타임 Google Spreadsheet 요청
- Addressables 자동 연결
- EventChannel 구조 변경
- UI Toolkit 대규모 레이아웃 리디자인

DataTable과 Pool은 runtime과 Editor tooling 모두에서 서로 독립적이어야 합니다.

## Primary Workflow: Editor Importer

게임 데이터의 기본 경로는 Editor Importer입니다.

```text
xlsx or Google Published CSV
-> Tools > INLO > DataTable > Importer
-> Prepare Changes
-> Review Errors and Diff
-> Apply Prepared Changes
-> DataTableAsset<TRow>
-> Runtime lookup
```

원본 표 형식은 다음과 같습니다.

```csv
id,key,displayName,hp,attack,moveSpeed,isBoss
1001,slime,Slime,30,5,2.5,false
1002,goblin,Goblin,55,12,3.2,false
```

행의 의미는 고정입니다.

| Row | Meaning |
| --- | --- |
| 1 | Column name matching C# Row fields |
| 2+ | Data |

첫 행의 컬럼명은 Row 타입의 직렬화 대상 필드명과 일치해야 합니다. 비교는 대소문자를 무시합니다.

Row 타입 예:

```csharp
using INLO.Core.DataTable;
using System;

[Serializable]
public sealed class MonsterRow : IDataTableRow
{
    public string id;
    public string key;
    public string displayName;
    public int hp;
    public int attack;
    public float moveSpeed;
    public bool isBoss;

    public string Id => id;
}
```

Table asset 예:

```csharp
using INLO.Core.DataTable;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/DataTable/Monster Table")]
public sealed class MonsterTable : DataTableAsset<MonsterRow>
{
}
```

## Editor Importer 타입 규칙

Editor Importer는 Row 타입의 필드 타입을 기준으로 값을 변환합니다.

지원 타입:

- `string`
- `int`
- `long`
- `float`
- `double`
- `bool`
- `enum`
- Nullable value type

`bool`은 다음 값을 허용합니다.

| True | False |
| --- | --- |
| `true` | `false` |
| `1` | `0` |
| `yes` | `no` |
| `y` | `n` |
| `on` | `off` |

숫자 파싱은 `CultureInfo.InvariantCulture` 기준입니다.

필드 규칙:

- public 필드는 기본적으로 import 대상입니다.
- private 필드는 `[SerializeField]`가 있을 때만 import 대상입니다.
- `[DataTableIgnore]` 필드는 import 대상에서 제외됩니다.
- `[DataTableOptional]` 필드는 원본 컬럼이 없어도 허용합니다.
- `[DataTableRequired]` 필드는 빈 값을 오류로 처리합니다.
- 값 타입 필드는 기본적으로 필수입니다.
- `string`과 nullable value type은 기본적으로 선택 값입니다.
- 일반 문자열 값의 앞뒤 공백은 보존합니다.
- `id`는 앞뒤 공백을 제거한 값을 사용하며, 제거 후 중복도 오류입니다.
- 빈 string 셀은 `string.Empty`, 빈 nullable 셀은 `null`로 가져옵니다.

원본 컬럼 규칙:

- `id` 컬럼은 필수입니다.
- `#` 또는 `//`로 시작하는 컬럼은 무시합니다.
- `id` 값이 `#` 또는 `//`로 시작하는 row는 무시합니다.
- 알 수 없는 컬럼은 오류입니다.
- 필수 필드에 대응하는 컬럼이 없으면 오류입니다.

## Google Published CSV Import

Google Spreadsheet는 원본 편집 도구입니다. Import는 Unity Editor에서만 수행합니다.

권장 흐름:

1. In Google Sheets, use `File > Share > Publish to web`.
2. Choose the target sheet and publish it as CSV.
3. Copy the generated `https://...output=csv` URL.
4. In Unity, open `Tools > INLO > DataTable > Importer`.
5. Select a `DataTableAsset`.
6. Set `Source Type` to `GoogleSheets`.
7. Paste the published CSV URL into `Sheet URL`.
8. Click `Save Source Config`.
9. Click `Google CSV 변경 준비`, review errors and Diff, then apply the prepared changes.
10. Use Database Preview and apply only the tables that should be registered.

Rules and limits:

- The first CSV row must contain column names that match the row C# fields.
- Data rows start on the second CSV row, the same as the xlsx importer.
- `id` is required.
- One published CSV URL maps to one sheet.
- HTTPS Google Published CSV URLs만 허용합니다.
- Google API, OAuth, private sheet access, and sheet tab listing are not used.
- 요청은 Editor 비동기로 실행되며 취소, 30초 timeout, 10 MB 제한을 적용합니다.
- Runtime Google requests are prohibited.

## xlsx 지원 범위

xlsx Reader는 Excel 계산 엔진이 아니라 DataTable용 제한 Reader입니다.

지원:

- 일반 문자열과 shared/inline string
- 일반 정수·실수·bool 원시 값
- 여러 worksheet 선택

명시적으로 차단:

- 수식 셀
- 병합 셀
- 오류 셀
- 외부 링크
- 날짜·시간 등 표시 형식에 의존하는 숫자

지원하지 않는 셀을 원시 값으로 추측해 가져오지 않고 원본 셀 위치와 함께 오류로 보고합니다.

## Prepare / Diff / Apply

`Prepare`는 asset을 수정하지 않고 전체 Row 생성, 값 변환, 필수 값, ID 중복과 기존 asset 대비
Diff를 계산합니다. Source, Sheet, 대상 asset이 바뀌면 이전 Plan은 폐기됩니다.

`Apply`는 가장 최근에 성공한 Plan만 반영하며 Undo를 지원합니다. `Import All`은 모든 대상의
Prepare가 성공한 뒤에만 한 번에 적용하고, 적용 중 예외가 발생하면 기존 상태를 복구합니다.

## C# Schema 준비

Importer의 C# Schema 기능은 prepared grid의 전체 열을 분석해 보수적인 타입 후보를 제안합니다.
사용자는 기존 `Assets/...` 폴더, namespace, 소속 assembly와 생성 코드 전체를 확인해야 합니다.

- 기존 C# 파일은 덮어쓰지 않습니다.
- 존재하지 않는 폴더와 `Packages/`에는 생성하지 않습니다.
- 첫 데이터 행의 문자열을 사용자 정의 타입으로 추측하지 않습니다.

### 자동 생성 출력 폴더 준비

`Create New DataTable`은 Row/Table C# 스크립트와 Table ScriptableObject asset을 서로 다른 폴더에 생성합니다.

기본 경로:

| 생성 결과 | 기본 경로 |
| --- | --- |
| `MonsterRow.cs`, `MonsterTable.cs` | `Assets/INLO/DataTable/Scripts` |
| `MonsterTable.asset` | `Assets/INLO/DataTable/ScriptableObjects` |

자동 생성기는 폴더를 만들지 않습니다. 생성 전에 Unity Project 창에서 위 두 폴더를 직접 만들어야 합니다.
다른 경로를 사용할 때도 대상 폴더를 먼저 만든 뒤 생성해야 합니다. 폴더가 없으면 생성 계획 검증이 실패합니다.

생성 건별 경로를 바꾸려면:

1. `Tools > INLO > DataTable > Importer`를 엽니다.
2. Google Published CSV 또는 xlsx 소스를 준비합니다.
3. `Create New DataTable`의 `Advanced Settings`를 펼칩니다.
4. `Script Folder`와 `ScriptableObject Folder`를 각각 입력하거나 선택합니다.
5. 생성 상세에서 Row, Table, Asset 경로를 확인한 뒤 생성합니다.

프로젝트의 기본 경로 자체를 바꾸려면 다음 파일의 상수를 수정합니다.

```text
Packages/com.inlo.core/Editor/DataTable/Generation/DataTableCreationWorkflow.cs
```

```csharp
public const string DefaultScriptOutputFolder =
    "Assets/INLO/DataTable/Scripts";

public const string DefaultScriptableObjectOutputFolder =
    "Assets/INLO/DataTable/ScriptableObjects";
```

이 상수는 Importer를 열었을 때 표시되는 기본값입니다. UI에서 생성 건별로 바꾼 값은 프로젝트 기본 설정으로 저장되지 않습니다.
기본 경로 상수를 바꾼 경우에도 새 경로의 폴더는 Unity Project 창에서 미리 생성해야 합니다.

## Runtime DataTableAsset API

Imported data is normally read through `DataTableAsset<TRow>`.

```csharp
if (monsterTable.TryGet("1001", out MonsterRow monster))
{
    Debug.Log(monster.displayName);
}
```

`BuildCache()` is called lazily by lookup APIs. If duplicate or empty ids exist, lookup throws `DataTableException`.

## Secondary Workflow: Runtime CsvDataTableParser

`CsvDataTableParser` is a lower-level runtime parser. It is not the same format as Editor Importer.

Use it when code needs to parse CSV text directly into the runtime `DataTable` model.

```csv
id,key,name,hp,speed,isBoss
int,string,string,int,float,bool
1001,goblin,Goblin,50,3.5,false
1002,dragon_boss,Dragon Boss,500,1.25,yes
```

행의 의미는 고정입니다.

| Row | Meaning |
| --- | --- |
| 1 | Column name |
| 2 | Column type |
| 3+ | Data |

지원 타입:

| Type | Alias |
| --- | --- |
| `string` | `str` |
| `int` | `integer` |
| `long` | |
| `float` | |
| `double` | |
| `bool` | `boolean` |

`CsvDataTableParser`는 아직 다음 타입을 지원하지 않습니다.

- `Vector2`
- `Vector3`
- `Color`
- `Enum`
- `AssetReference`
- Addressables key 전용 타입

예:

```csharp
using INLO.Core.DataTable;

DataTable table = CsvDataTableParser.Parse(csvText, "MonsterTable");

DataTableRow goblin = table.GetRow(1001);
string name = goblin.GetString("name");
int hp = goblin.GetInt("hp");
bool isBoss = goblin.GetBool("isBoss");
```

존재하지 않을 수 있는 row는 `TryGetRow`를 사용합니다.

```csharp
if (table.TryGetRow(1001, out DataTableRow row))
{
    string key = row.GetString("key");
}
```

`key` 컬럼이 있는 테이블은 `TryGetRowByKey`를 사용할 수 있습니다.

```csharp
if (table.TryGetRowByKey("goblin", out DataTableRow row))
{
    int id = row.Id;
}
```

CSV reader는 단순 `Split(',')`을 사용하지 않습니다. 따옴표 문자열, 쉼표가 포함된 문자열, escaped quote, CRLF/LF 줄바꿈을 처리해야 합니다.

```csv
id,key,desc
int,string,string
1001,goblin,"fast, small enemy"
1002,dragon,"boss says ""hello"""
```

## 오류 메시지 규칙

데이터 오류는 사람이 원본 시트를 고칠 수 있게 작성합니다.

좋은 메시지는 다음 정보를 포함해야 합니다.

- 테이블 이름 또는 asset 이름
- row number
- column name 또는 field name
- expected type
- actual value

예시:

```text
DataTable parse failed. Row 5, Column 'hp': expected int but got 'abc'.
DataTable import failed. Row 5, Field 'hp': expected int but got 'abc'.
DataTable schema error. Column name is empty at index 3.
```

AI 도구는 `Parse failed`, `Invalid value`, `Error`처럼 원인을 알 수 없는 메시지를 새로 추가하지 않습니다.

## 금지 사항

이번 DataTable 구조에서 다음 작업은 금지합니다.

- DataTable과 Pool을 runtime 또는 Editor tooling에서 연결하기
- DataTable에서 PoolDatabase를 생성 또는 갱신하기
- Google Spreadsheet를 게임 실행 중 요청하기
- 일반 데이터 테이블을 Pool 전용 구조로 오염시키기
- EventChannel 시스템을 DataTable 작업 중 변경하기
- R3, UniRx, MessagePipe 등을 DataTable 작업 명목으로 도입하기
- UI Toolkit Import Window를 다른 기능 작업에 끼워 넣기

## 다음 작업 후보

다음 단계에서 다룰 수 있는 작업은 다음과 같습니다.

- DataTable import 로그와 검증 리포트 강화
- DataTable Preview Window 개선
- Google Published CSV Import Settings 명확화
- Pool Validation Report Window
- Pool / Events Editor Window UI Toolkit 레이아웃 통일

DataTable Importer Window의 UI Toolkit 기준은 `Documentation/EditorUI/README.md`와 `Editor/DataTable/Windows/UXML`, `Editor/DataTable/Windows/USS`에 정리되어 있습니다.

## AI 작업 체크리스트

AI 도구가 DataTable 코드를 수정하기 전에는 다음을 확인합니다.

1. 작업 계획을 먼저 작성했는가?
2. 사람이 계획을 승인했는가?
3. `Packages/com.inlo.core/README.md`와 관련 문서를 읽었는가?
4. DataTable과 Pool이 서로 참조하지 않는가?
5. Runtime 코드가 Editor 네임스페이스를 참조하지 않는가?
6. PoolManager, PoolDatabase 책임을 바꾸지 않는가?
7. EventChannel 구조를 건드리지 않는가?
8. Unity나 기존 패키지 기능을 재구현하고 있지 않은가?
9. 매 프레임 경로에 할당을 추가하지 않는가?
10. 씬, 프리팹, `.meta` 파일을 직접 편집하지 않는가?

위 항목 중 하나라도 애매하면 구현을 멈추고 계획으로 되돌아갑니다.
