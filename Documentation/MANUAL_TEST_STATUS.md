# INLO Core Manual Test Status

이 문서는 현재 저장소에서 실제로 실행 가능한 수동 테스트와 복구가 필요한 수동 테스트를 분리합니다.

`FULL_PACKAGE_MANUAL_TEST_GUIDE.md`는 전체 흐름의 역사와 절차를 담고 있지만, 현재 DataTable 샘플 일부가 제거되어 그대로 전체 통과를 기대하면 안 됩니다.

## 현재 결론

| 영역 | 상태 | 이유 |
| --- | --- | --- |
| Events manual test | Runnable | `ManualDamageEvent*` 스크립트가 존재함 |
| Pooling manual test | Runnable with setup | `ManualPool*` 스크립트가 존재하고 prefab/database setup 필요 |
| UI/Event demo | Runnable with scene setup | `Demo/Ui*` 스크립트가 존재함 |
| DataTable import manual test | Needs Restoration | `ManualMonsterRow`, `ManualMonsterTable`, `ManualDataTableReader`가 없음 |
| Full package walkthrough | Blocked by sample gap | DataTable sample 복구 전에는 전체 guide 그대로 통과 불가 |

## 실행 가능한 테스트

### Events

1. `ManualDamageEventChannel` asset을 준비합니다.
2. `ManualDamageEventRaiser`와 `ManualDamageEventLogger`를 빈 GameObject에 붙입니다.
3. 두 컴포넌트에 같은 channel asset을 연결합니다.
4. `ManualDamageEventRaiser`의 Context Menu에서 event를 발행합니다.
5. Console에 damage log가 찍히면 통과입니다.
6. `Tools > INLO > Events > Event System Manager`의 Browser/Graph/Audit 탭에서 channel이 보이는지 확인합니다.

### Pooling

1. `ManualPoolProjectile`을 붙인 prefab을 만듭니다.
2. `PoolDatabase` asset을 만들고 prefab과 `PoolKey`를 등록합니다.
3. `PoolBootstrapper`에 database를 연결합니다.
4. `ManualPoolSpawner`에 같은 `PoolKey`를 설정합니다.
5. Play Mode에서 spawn/release가 반복되면 통과입니다.
6. `Tools > INLO > Pooling > Pool System Manager`의 Debug/Browser/Validation 탭을 확인합니다.

## 복구가 필요한 테스트

### DataTable import

현재 누락된 타입:

- `ManualMonsterRow.cs`
- `ManualMonsterTable.cs`
- `ManualDataTableReader.cs`

복구 방법은 별도 승인된 계획에서 처리해야 합니다.

복구 기준:

1. `ManualMonsterRow`는 `IDataTableRow`를 구현합니다.
2. `ManualMonsterTable`은 `DataTableAsset<ManualMonsterRow>`를 상속합니다.
3. `ManualDataTableReader`는 table asset과 id를 받아 row를 조회하고 Console에 출력합니다.
4. `FULL_PACKAGE_MANUAL_TEST_GUIDE.md`의 Google Sheet 샘플을 import할 수 있어야 합니다.
5. Missing Script가 남지 않아야 합니다.

## 사람이 Unity에서 처리할 일

파일 탐색기로 직접 지우지 말고 Unity Project 창에서 처리합니다.

- 깨진 `ManualMonsterTable.asset` 확인 또는 삭제
- `TEST.unity`에 Missing Script가 있는지 확인
- 더 이상 쓰지 않는 manual test prefab/asset 정리
- DataTable 샘플을 복구할지 완전히 제거할지 결정

## 문서 갱신 규칙

- DataTable 샘플이 복구되면 이 문서의 상태를 `Runnable`로 바꿉니다.
- 수동 테스트 절차가 바뀌면 `FULL_PACKAGE_MANUAL_TEST_GUIDE.md`와 같이 갱신합니다.
- Missing Script가 발견되면 이 문서에 먼저 기록합니다.
