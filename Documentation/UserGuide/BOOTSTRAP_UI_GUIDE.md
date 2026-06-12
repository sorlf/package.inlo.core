# Bootstrap & UI 상세 설정 매뉴얼

이 가이드는 `com.inlo.core` 패키지의 **Bootstrap(초기화 및 씬 전환)**과 **UI Scene 연동** 기능을 당장 여러분의 프로젝트 씬에 얹어서 작동하게 만들기 위한 친절한 설명서입니다.

---

## 1. AppBootstrap 초기화 구축하기

`AppBootstrap`은 게임이 켜질 때 전역 매니저나 서비스들을 순서대로 초기화해 주는 핵심 컨트롤러입니다.

### 🛠️ 인스펙터 세팅법 (따라 하기)
1. **첫 진입 씬 생성**: 프로젝트의 첫 진입점 역할을 할 빈 씬(예: `BootstrapScene`)을 생성합니다.
2. **오브젝트 배치**: 씬 안에 빈 게임오브젝트를 만들고 이름을 `Bootstrap`으로 지정합니다.
3. **컴포넌트 추가**: 생성한 오브젝트에 **`AppBootstrap`** 컴포넌트를 붙집니다.
4. **초기화 순서 등록**:
   - 기획 데이터나 풀링 등을 먼저 로드해야 한다면, 초기화 로직이 담긴 컴포넌트(예: `PoolRuntimeInitializer` 등 `IBootstrapInitializer`를 구현한 컴포넌트)를 붙입니다.
   - `AppBootstrap` 인스펙터의 **Initializers** 리스트에 이 컴포넌트들을 원하는 순서대로 드래그 앤 드롭하여 등록합니다.
5. **실행**: 게임이 시작되면 등록된 순서대로 코루틴 초기화가 진행되고, 모두 정상 완료되면 상태가 `Ready`로 전환됩니다.

### 💻 스크립트에서 완료 이벤트 구독하기
다른 시스템(예: 로비 진입, 로그인 UI 활성화 등)이 Bootstrap 완료 시점에 맞춰 구동되게 하려면 아래처럼 이벤트를 사용하세요.
```csharp
using INLO.Core.Bootstrap;
using UnityEngine;

public class GameStartHandler : MonoBehaviour
{
    private void Start()
    {
        AppBootstrap bootstrap = AppBootstrap.Instance;
        if (bootstrap == null) return;

        // 이미 초기화가 완료된 상태라면 즉시 실행
        if (bootstrap.InitializationState == BootstrapInitializationState.Ready)
        {
            OnGameReady();
        }
        else
        {
            // 아직 대기 중이라면 이벤트를 구독하여 대기
            bootstrap.InitializationCompleted += OnInitializationCompleted;
        }
    }

    private void OnInitializationCompleted(BootstrapInitializationState state)
    {
        if (state == BootstrapInitializationState.Ready)
        {
            OnGameReady();
        }
        else
        {
            Debug.LogError("Bootstrap 초기화에 실패했습니다.");
        }
    }

    private void OnGameReady()
    {
        Debug.Log("게임 준비 완료! 로그인 화면으로 진입합니다.");
        // 이벤트 해제
        if (AppBootstrap.Instance != null)
        {
            AppBootstrap.Instance.InitializationCompleted -= OnInitializationCompleted;
        }
    }
}
```

---

## 2. 씬 전환 시 UI 씬 자동 Additive 로딩 세팅

게임 씬(예: `LobbyScene`)이 열릴 때 UI가 담긴 씬(예: `UIScene_Lobby`)을 자동으로 중첩하여 띄우고 싶다면 다음 단계를 따르세요.

### 1단계. 씬 로더 및 UI 씬 로더 결합
1. 위에서 만든 `Bootstrap` 오브젝트에 **`SceneLoader`**와 **`UiSceneLoader`** 컴포넌트를 둘 다 추가합니다.
2. `UiSceneLoader`는 씬이 전환될 때 자동으로 동작하여 매핑된 UI 씬을 Additive(중첩) 방식으로 불러오고, 불필요해진 이전 UI 씬을 해제해 주는 편리한 씬 전환 부품(`ISceneTransitionStep`)입니다.

### 2단계. Scene UI 바인딩 테이블 생성 및 맵핑
1. 유니티 프로젝트 창에서 빈 곳을 우클릭하고 `Create > INLO > Core > UI > Scene UI Binding Table`을 클릭하여 매핑 에셋(ScriptableObject)을 생성합니다. (이름은 `SceneUiBindings` 권장)
2. 생성된 에셋의 인스펙터 창을 보면 매핑 목록이 있습니다. `Add Entry`를 누릅니다.
   - **Game Scene Name**: 불러올 게임 월드 씬 이름 (예: `LobbyScene`)
   - **UI Scene Name**: 함께 띄울 UI 씬 이름 (예: `UIScene_Lobby`)
   - **Load Mode**: `Additive`로 설정합니다.
   - **Unload Previous Ui**: 체크해두면 새로운 UI가 뜰 때 이전 UI 씬을 메모리에서 자동으로 내려줍니다.
3. 작성한 바인딩 테이블 에셋을 씬의 `UiSceneLoader` 컴포넌트에 있는 **Bindings** 필드에 드래그하여 할당합니다.

### 3단계. 코드에서 씬 전환 호출하기
스크립트 상에서 다음 코드를 호출하면 지정된 씬으로 전환되며, 2단계에서 설정한 UI 씬도 함께 화면에 나타납니다.
```csharp
using INLO.Core.Bootstrap;
using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    public void GoToLobby()
    {
        // SceneLoader 인스턴스를 가져와 호출합니다.
        SceneLoader sceneLoader = AppBootstrap.Instance.GetComponent<SceneLoader>();
        if (sceneLoader == null) return;

        // LoadScene은 씬 로딩 요청이 성공적으로 접수되면 true를 반환합니다.
        // 이미 씬 전환 중이라면 false가 반환되며 중복 요청을 거부합니다.
        bool accepted = sceneLoader.LoadScene("LobbyScene");
        if (accepted)
        {
            Debug.Log("로비 씬 로딩 시작...");
        }
    }
}
```

---

## 3. UI 유틸리티 즉시 연동하기

패키지에 들어있는 UI 유틸리티를 프로젝트에 얹어 화면 크기와 레이어를 바로 관리하는 방법입니다.

### 📱 Safe Area 해상도 대응 (SafeAreaFitter)
노치 디자인이 있는 모바일 디바이스(아이폰, 최신 갤럭시 등)에서 UI가 가려지지 않게 처리하는 방법입니다.
1. UI 씬의 Canvas 바로 밑에 최상단 패널 오브젝트(예: `SafeAreaRoot`)를 만듭니다. (Anchor Preset을 Stretch-Stretch로 채우는 것을 권장)
2. 이 오브젝트에 **`SafeAreaFitter`** 컴포넌트를 붙이기만 하면 끝입니다. 실행 시 기기의 Safe Area 영역을 계산하여 자동으로 자식 UI들의 여백을 조절합니다.

### 🗂️ UI 레이어 관리 (UiRoot & UiLayerRegistry)
HUD, 팝업, 토스트 알림의 렌더링 순서(Layer)를 관리하는 법입니다.
1. UI 씬의 Canvas 하위에 `HUD`, `Popup`, `Toast` 등의 자식 오브젝트들을 만듭니다.
2. UI Canvas가 위치한 오브젝트에 `UiRoot` 컴포넌트를 추가합니다.
3. `UiLayerRegistry` 컴포넌트를 붙인 뒤, 인스펙터 상에서 미리 만들어 둔 `HUD`, `Popup`, `Toast` 오브젝트들을 각각의 레이어 슬롯에 매칭해 줍니다.
4. 코드에서 팝업을 열거나 토스트를 띄울 때 해당 슬롯 정보를 읽어가므로, 레이어 꼬임 현상이 방지됩니다.
   - 예: `PopupService.Instance.Open(...)` 이나 `ToastService.Instance.Show(...)` 호출 시 지정된 레이어 하위에 정렬 생성됩니다.
