# User Guide Router (초보자용 사용자 가이드)

이 문서는 `com.inlo.core` 패키지를 처음 다루며, **"아키텍처고 설계고 잘 모르겠고 당장 실무 프로젝트에 연동해서 상사에게 완료 보고를 해야 하는 분"**들을 위한 초간단 5분 퀵스타트 라우터 매뉴얼입니다.

---

## 5분 만에 세팅 끝내기 (3단계 퀵스타트)

### Step 1. 앱 시작 진입점(AppBootstrap) 세팅
1. 유니티 프로젝트에 빈 씬을 하나 만들고 이름을 `BootstrapScene`이라고 지으세요.
2. 씬 안에 빈 GameObject를 만들고 이름을 `Bootstrap`으로 바꿉니다.
3. 이 GameObject에 `AppBootstrap` 컴포넌트를 붙이세요. (상사가 시키는 초기화 요소가 없다면 그냥 이대로 두셔도 `Ready` 상태가 됩니다.)

### Step 2. 씬 전환 시 UI 씬 자동 연동 (Scene ↔ UI Scene 바인딩)
1. Step 1의 `Bootstrap` 오브젝트에 `SceneLoader` 컴포넌트와 `UiSceneLoader` 컴포넌트를 나란히 붙입니다.
2. 유니티 프로젝트 창에서 우클릭 ➔ `Create > INLO > Core > UI > Scene UI Binding Table`을 눌러 바인딩 설정 자산을 만드세요.
3. 생성된 테이블 인스펙터에서 `Add Entry`를 눌러 예를 들어 **GameScene**이 열릴 때 **UIScene_Gameplay**가 Additive(중첩)로 자동 로드되도록 씬 매핑 정보를 작성합니다.
4. `UiSceneLoader` 컴포넌트의 `Bindings` 필드에 방금 만든 바인딩 테이블을 할당합니다.

### Step 3. 기획용 데이터 테이블 임포트하기 (DataTable)
1. 데이터 테이블을 가져오기 전에, 유니티 프로젝트(Assets) 창에서 다음 두 폴더를 **반드시 직접 미리 생성**해 두어야 합니다. (만들지 않으면 임포트 시 에러가 납니다!)
   - `Assets/INLO/DataTable/Scripts`
   - `Assets/INLO/DataTable/ScriptableObjects`
2. 엑셀(`.xlsx`) 파일이나 구글 스프레드시트 게시 CSV URL을 준비합니다.
3. 유니티 상단 메뉴에서 `Tools > INLO > DataTable > Importer`를 클릭해 임포터 창을 엽니다.
4. 임포터 화면 가이드에 따라 원본 파일을 로드하고 `Prepare Changes` ➔ `Apply Prepared Changes` 순으로 적용하면 데이터 조회를 위한 ScriptableObject 자산(`MonsterTable.asset` 등)이 자동으로 생성됩니다.

---

## 세부 모듈별 설정 매뉴얼

* 씬 로딩과 화면 크기(Safe Area) 및 UI 레이어 관리의 더 자세한 실무 가이드는 **[Bootstrap & UI 상세 설정 매뉴얼](BOOTSTRAP_UI_GUIDE.md)**을 참조하세요.
* 데이터테이블 설정 방법과 이벤트 채널, 오브젝트 풀링(Pooling) 매니저 사용법은 **[DataTable, Events, Pooling 사용 가이드](DATATABLE_EVENTS_POOLING.md)**를 참조하세요.

---

## ⚠️ 초보 실무자 주의사항

1. **파일 인코딩 규약**: 스크립트나 마크다운 문서를 직접 생성하거나 수정할 때는 반드시 인코딩을 **UTF-8 without BOM**으로 지정하고, 줄끝(Line Ending)을 **LF**로 고정해야 합니다. (CRLF나 BOM이 포함되어 있으면 Git 커밋 및 검증 단계에서 반려 처리됩니다.)
2. **에디터 메타 데이터**: 유니티 내부에서 문서 폴더(`UserGuide`)와 마크다운 파일들을 생성한 경우, 에디터에서 해당 문서들을 정상적으로 인식하도록 생성 직후 유니티 창을 열어 `.meta` 파일이 생성되도록 유도하십시오. (에이전트는 씬/프리팹 및 `.meta` 직접 수정을 수행하지 않고 사람에게 위임합니다.)

---

## 🤖 에이전트 문서 자동화 지침 (Rule #9)

이 가이드는 패키지의 수명 주기 동안 항상 최신의 연동 방식을 반영해야 합니다. 
프로젝트에 참여하는 모든 AI 에이전트(AI 작업자)는 **Bootstrap, SceneLoader, DataTable, Events, Pooling 등 핵심 C# 코드를 변경할 때마다 관련된 이 UserGuide 문서도 무조건 동시 갱신할 의무가 규정되어 있습니다.** 

상사가 시킨 일로 인해 C# 소스 코드가 고쳐지면, 이 가이드 역시 **자동으로 최신화**되므로 안심하고 참고하십시오!
