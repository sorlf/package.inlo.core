# INLO Core Full Manual Test Guide

이 문서는 패키지를 처음 받은 사람이 INLO Core의 DataTable, Events, Pooling, Editor Window를 실제 Unity Editor에서 순서대로 체험하기 위한 수동 테스트 가이드입니다.

현재 저장소에서 어떤 테스트가 실제로 실행 가능한지는 [Manual Test Status](MANUAL_TEST_STATUS.md)를 먼저 확인하세요.

## 0. 테스트 전 정리

이 저장소에는 과거 테스트 중 만들어진 Assets 잔여물이 있을 수 있습니다. 메뉴에 `Game > DataTables`, `INLO > Test` 같은 항목이 보이면 패키지 본체가 아니라 Assets 테스트 스크립트가 만든 메뉴입니다.

현재 저장소에는 일부 수동 테스트 asset이 남아 있고, 일부 C# 스크립트는 제거되어 Missing Script가 발생할 수 있습니다. Unity에서 다음 폴더나 asset이 비어 있거나 테스트 잔여물로 보이면 Project 창에서 삭제하세요.

| 삭제 후보 | 이유 |
| --- | --- |
| `Assets/EventChannelTest` | 이전 EventChannel 수동 테스트 코드 |
| `Assets/GameStateTest` | 이전 GameState/EventChannel 테스트 코드 |
| `Assets/DamageTakenTest` | 이전 테스트 폴더 |
| `Assets/GeneratedEvents` | 이전 Event Channel Creator 생성 결과 |
| `Assets/GameEvents` | 이전 테스트 EventChannel asset |
| `Assets/Stater` | 이전 DataTable 테스트 코드와 asset |
| `Assets/INLO/Pool` | 이전 Pool 테스트 코드와 PoolDatabase |
| `Assets/INLO/DataTable` | 이전 DataTableDatabase |
| `Assets/Prefabs/Bullet_Normal.prefab` | 이전 Pool 테스트 prefab |
| `Assets/INLOCoreManualTest/Data/ManualMonsterTable.asset` | 현재 `ManualMonsterTable` C# 타입이 없어 깨진 참조가 될 수 있음 |
| `Assets/Prefabs/TEST.unity` | 현재 `ManualDataTableReader` 등 제거된 타입 참조가 남아 있을 수 있음 |

중요: `.meta`, `.prefab`, `.unity`는 파일 탐색기에서 직접 지우지 말고 Unity Project 창에서 삭제하세요.

## 1. 준비된 테스트 스크립트

새 테스트 코드는 `Assets/INLOCoreManualTest/Scripts`에 있습니다.

| 파일 | 역할 |
| --- | --- |
| `ManualDamageEventData.cs` | EventChannel payload |
| `ManualDamageEventChannelSO.cs` | typed EventChannel asset 타입 |
| `ManualDamageEventRaiser.cs` | Context Menu로 event 발행 |
| `ManualDamageEventLogger.cs` | event 수신 후 Console 출력 |
| `ManualPoolProjectile.cs` | Pool에서 재사용할 projectile 예제 |
| `ManualPoolSpawner.cs` | PoolKey로 projectile spawn |
| `Demo/UiGameplayController.cs` | UI/EventChannel 수동 테스트용 컨트롤러 |
| `Demo/UiDamageNotificationPanel.cs` | UI/EventChannel 수동 테스트용 표시 패널 |

현재 저장소에는 DataTable 수동 테스트용 `ManualMonsterRow.cs`, `ManualMonsterTable.cs`, `ManualDataTableReader.cs`가 없습니다. DataTable Importer 수동 테스트를 다시 살리려면 별도 승인된 계획으로 해당 타입을 복구하거나 새 샘플 row/table을 생성해야 합니다.

## 2. Google Sheet 샘플 표

Google Sheet 첫 행에 아래 헤더를 넣고, 두 번째 행부터 데이터를 붙여넣으세요.

```csv
id,key,displayName,hp,attack,moveSpeed,isBoss
1001,slime,Slime,30,5,2.5,false
1002,goblin,Goblin,55,12,3.2,false
1003,skeleton,Skeleton,70,16,2.8,false
2001,orc_warrior,Orc Warrior,140,28,2.4,false
9001,dragon_boss,Dragon Boss,1200,90,1.8,true
```

Google Sheet에서 `File > Share > Publish to web`를 열고, 테스트할 sheet를 `CSV` 형식으로 게시합니다. 생성된 `https://...output=csv` URL을 복사합니다.

## 3. DataTable Importer 테스트

현재 이 저장소만으로는 DataTable 수동 테스트를 완료할 수 없습니다. 필요한 `ManualMonsterRow`, `ManualMonsterTable`, `ManualDataTableReader` 타입이 없습니다.

복구 후 테스트 기준은 다음과 같습니다.

1. Unity Project 창에서 `Assets/INLOCoreManualTest` 아래에 `Data` 폴더를 만듭니다.
2. `DataTableAsset<TRow>`를 상속한 Monster Table asset을 만듭니다.
3. `Tools > INLO > DataTable > Importer`를 엽니다.
4. 왼쪽 목록에서 대상 table asset을 선택합니다.
5. `Source Type`을 `GoogleSheets`로 선택합니다.
6. `Sheet URL`에 Google Published CSV URL을 붙여넣습니다.
7. `Source 설정 저장`을 누릅니다.
8. `선택 테이블 검증`을 누릅니다.
9. Preview 탭에서 5개 row가 보이는지 확인합니다.
10. Validation Errors가 0인지 확인합니다.
11. `선택 테이블 가져오기`를 누릅니다.
12. `Selected Table`의 Row Count가 5가 되었는지 확인합니다.
13. `DB 생성/갱신`을 누릅니다.
14. `Assets/INLO/DataTable/DataTableDatabase.asset`이 생성되고 Registered Tables가 1인지 확인합니다.

## 4. DataTable Runtime 조회 테스트

현재 이 저장소만으로는 DataTable Runtime 조회 수동 테스트를 완료할 수 없습니다. `ManualDataTableReader` 타입이 없습니다.

복구 후 테스트 기준은 다음과 같습니다.

1. 빈 GameObject를 만들고 DataTable reader 컴포넌트를 붙입니다.
2. Table 필드에 import된 `DataTableAsset<TRow>` asset을 연결합니다.
3. 조회 id를 `1001`로 둡니다.
4. Context Menu 또는 테스트 버튼으로 row 조회를 실행합니다.
5. Console에 `Slime | HP 30 | Attack 5` 형태의 로그가 찍히면 통과입니다.

## 5. Events 테스트

1. Project 창에서 `Create > INLO > Manual Test > Events > Damage Event Channel`을 선택합니다.
2. asset 이름을 `ManualDamageEventChannel`로 둡니다.
3. 빈 GameObject `Manual Event Raiser`를 만들고 `ManualDamageEventRaiser`를 붙입니다.
4. 빈 GameObject `Manual Event Logger`를 만들고 `ManualDamageEventLogger`를 붙입니다.
5. 두 컴포넌트의 `Channel` 필드에 `ManualDamageEventChannel`을 연결합니다.
6. `ManualDamageEventRaiser`의 Context Menu에서 `Raise Damage Event`를 실행합니다.
7. Console에 `1001 damaged by 10` 형태의 로그가 찍히면 통과입니다.
8. `Tools > INLO > Events > Event System Manager`를 엽니다.
9. Browser 탭에서 생성한 channel이 보이는지 확인합니다.
10. Graph 탭에서 channel 관계가 표시되는지 확인합니다.
11. Audit 탭에서 audit 결과가 뜨는지 확인합니다.

## 6. Pooling 테스트

1. 빈 GameObject를 만들고 이름을 `Manual Projectile Prefab Source`로 지정합니다.
2. `ManualPoolProjectile` 컴포넌트를 붙입니다.
3. Project 창으로 드래그해서 prefab을 만듭니다.
4. Scene의 원본 GameObject는 삭제하거나 비활성화합니다.
5. Project 창에서 `Create > INLO > Pooling > Pool Database`를 선택합니다.
6. asset 이름을 `ManualPoolDatabase`로 둡니다.
7. Inspector에서 Entries를 1개 추가합니다.
8. Entry 값을 다음처럼 설정합니다.

| Field | Value |
| --- | --- |
| Pool Key | `Enemies/Slime` |
| Prefab | 방금 만든 projectile prefab |
| Preload Count | `3` |
| Max Count | `12` |
| Overflow Policy | `Expand` |

9. 빈 GameObject `Manual Pool Bootstrapper`를 만들고 `PoolBootstrapper`를 붙입니다.
10. `Database` 필드에 `ManualPoolDatabase`를 연결합니다.
11. 빈 GameObject `Manual Pool Spawner`를 만들고 `ManualPoolSpawner`를 붙입니다.
12. `Pool Key`를 `Enemies/Slime`로 둡니다.
13. Play Mode를 실행합니다.
14. projectile이 반복 생성되고 사라지면 통과입니다.
15. `Tools > INLO > Pooling > Pool System Manager`를 엽니다.
16. Debug 탭에서 active/inactive count가 변하는지 확인합니다.
17. Browser 탭에서 database와 entry가 보이는지 확인합니다.
18. Validation 탭에서 validation 결과를 확인합니다.

## 7. 전체 회귀 체크리스트

| Area | 확인 |
| --- | --- |
| DataTable Importer | Google Published CSV Validate/Import 성공 |
| DataTable Importer | xlsx source 선택과 Sheet Load가 여전히 동작 |
| DataTable Database | `DB 생성/갱신`으로 database 생성/갱신 |
| DataTable Runtime | DataTable reader 샘플 복구 후 row 조회 |
| Events Runtime | raiser/logger가 event 송수신 |
| Events Editor | Event System Manager의 Browser, Graph, Audit 탭 열림 |
| Pool Runtime | PoolBootstrapper 등록 후 PoolKey spawn |
| Pool Editor | Pool System Manager의 Debug, Browser, Validation 탭 열림 |
| Menus | 이전 `Game > DataTables`, `INLO > Test` 테스트 메뉴가 사라짐 |

## 8. 실패 시 빠른 원인

| 증상 | 확인할 것 |
| --- | --- |
| Import 버튼 비활성 | DataTableAsset 선택 여부, URL이 `https://`인지 확인 |
| Validation에서 id 누락 | Google Sheet 첫 행에 `id`가 있는지 확인 |
| Row Count가 0 | Published URL이 CSV로 게시되었는지 확인 |
| Event 로그 없음 | Raiser와 Logger에 같은 channel asset을 연결했는지 확인 |
| Pool spawn 실패 | PoolDatabase entry의 Pool Key와 Spawner Pool Key가 같은지 확인 |
| Pool Debug에 변화 없음 | Play Mode에서 PoolBootstrapper가 있는지 확인 |
