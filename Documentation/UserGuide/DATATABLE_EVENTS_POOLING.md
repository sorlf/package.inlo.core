# DataTable, Events, Pooling 사용 가이드

이 문서는 게임 내 기획 데이터 테이블을 파싱하고, 이벤트 채널을 설계하며, 오브젝트 풀링을 관리하는 에디터 툴의 위치와 실무 사용 방법을 정리한 안내서입니다.

---

## 1. 기획 데이터 테이블 가져오기 (DataTable)

기획자가 작성한 엑셀 파일(`.xlsx`) 또는 구글 스프레드시트 데이터를 게임 안에서 쓸 수 있는 ScriptableObject 자산(`DataTableAsset`)으로 만드는 방법입니다.

### ⚠️ 중요: 시작하기 전에 폴더 만들기
임포터 툴을 사용하기 전에, 유니티 **Project 창의 Assets 폴더 하위**에 다음 경로를 직접 만드셔야 합니다. (폴더가 없으면 코드 생성 및 임포트 단계에서 에러가 발생합니다.)
*   `Assets/INLO/DataTable/Scripts`
*   `Assets/INLO/DataTable/ScriptableObjects`

### 1단계. 엑셀/구글 시트 포맷 맞추기
*   **첫 번째 행**: C# Row 스크립트의 필드명과 일치해야 합니다. (대소문자 구분 없음, 예: `id`, `name`, `hp`, `speed`)
*   **두 번째 행부터**: 실제 데이터입니다.
*   **필수 컬럼**: 고유 식별자인 `id` 컬럼(문자열 형식)은 반드시 존재해야 합니다.
*   **주석 처리**: `#` 이나 `//` 로 시작하는 컬럼 또는 id의 행은 파싱하지 않고 무시합니다.

### 2단계. 임포터 툴 실행 및 신규 생성
1.  유니티 메뉴에서 `Tools > INLO > DataTable > Importer`를 클릭합니다.
2.  새로운 데이터를 추가해야 한다면 **Create New DataTable** 영역을 이용해 엑셀 파일 또는 구글 시트 게시 URL을 등록합니다.
3.  자동으로 Row C# 스크립트(예: `MonsterRow.cs`)와 Table 스크립트(`MonsterTable.cs`), 그리고 에셋 파일(`MonsterTable.asset`)이 생성됩니다.

### 3단계. 기존 데이터 테이블 갱신하기
1.  임포터 윈도우에서 갱신할 데이터 테이블 에셋을 선택합니다.
2.  `Prepare Changes` (변경 준비) 버튼을 누릅니다. 에러가 없는지, 데이터의 차이점(Diff)이 무엇인지 검토합니다.
3.  문제가 없다면 `Apply Prepared Changes` (변경 적용)를 누르면 에셋에 최신 데이터가 반영됩니다.

### 💻 스크립트에서 데이터 테이블 런타임 조회하기
임포트한 데이터 테이블 에셋(`MonsterTable.asset` 등)을 실제 C# 스크립트 상에서 꺼내어 쓰는 방법입니다.
```csharp
using INLO.Core.DataTable;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    // 인스펙터 상에 임포트된 MonsterTable.asset을 할당합니다.
    [SerializeField] private MonsterTable monsterTable;

    private void Start()
    {
        if (monsterTable == null) return;

        // 1. 특정 ID를 가진 몬스터 데이터 하나 조회하기
        if (monsterTable.TryGet("1001", out MonsterRow monster))
        {
            Debug.Log($"[Monster] 이름: {monster.Name}, 체력: {monster.Hp}, 속도: {monster.Speed}");
        }
        else
        {
            Debug.LogWarning("ID '1001'번에 해당하는 몬스터 데이터를 찾을 수 없습니다.");
        }

        // 2. 전체 몬스터 데이터 순회(Iteration)하기
        foreach (MonsterRow row in monsterTable.Rows)
        {
            Debug.Log($"스캔된 몬스터 ID: {row.Id}, 이름: {row.Name}");
        }
    }
}
```

---

## 2. SO 기반 이벤트 시스템 사용하기 (Events)

이벤트 채널은 클래스 간의 직접적인 참조 관계를 끊고, 시스템 간에 "사건이 일어났음"을 전파할 때 사용합니다.

*   **메뉴 위치**: `Tools > INLO > Events > Event System Manager`
*   **사용 예시**: `GoldService` ➔ `GoldChangedChannel` ➔ `CurrencyUI`
*   **주의 사항**: EventChannel은 데이터를 들고 있는 "상태 저장소"가 아닙니다. 단지 사건을 알리는 통로입니다. 상태(값)의 원본은 서비스나 매니저가 소유해야 합니다.

---

## 3. 오브젝트 풀링 설정하기 (Pooling)

총알, 이펙트, 몬스터처럼 빈번하게 생성(Instantiate)되고 소멸(Destroy)되는 게임오브젝트를 관리하여 모바일 기기에서의 성능 저하(GC 할당)를 막는 기능입니다.

*   **메뉴 위치**: `Tools > INLO > Pooling > Pool System Manager`
*   **사용법**:
    1.  풀 매니저 창을 엽니다.
    2.  풀링할 프리팹을 등록하고, 최초 생성 개수(Initial Size)와 최대 크기(Max Size)를 설정합니다.
    3.  설정된 정보는 `PoolDatabase` 에셋에 저장되며, 런타임 시작 시 자동으로 풀링 인스턴스들이 생성됩니다.
    4.  코드에서는 `PoolManager.Instance.Spawn(...)` 및 `PoolManager.Instance.Despawn(...)` 형태로 인스턴스를 가져오거나 보관 풀로 되돌려 사용합니다.
