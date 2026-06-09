using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;

public class BeatHealSetup : EditorWindow
{
    int   instrumentCount = 5;
    float radius          = 1.25f;  // 노트 간격 축소
    float heightOffset    = 1.0f;
    float arcDegrees      = 95f;     // 호 각도 축소 → 악기들이 더 가까이 모임

    static readonly Color[] InstrumentColors =
    {
        new Color(0.8f, 0.2f, 0.2f),
        new Color(0.2f, 0.4f, 0.9f),
        new Color(0.2f, 0.8f, 0.3f),
        new Color(0.9f, 0.8f, 0.1f),
        new Color(0.7f, 0.2f, 0.9f),
        new Color(0.1f, 0.8f, 0.8f),
        new Color(0.9f, 0.5f, 0.1f),
    };

    [MenuItem("BeatHeal/Setup Scene")]
    public static void ShowWindow() => GetWindow<BeatHealSetup>("BeatHeal Setup");

    void OnGUI()
    {
        GUILayout.Label("악기 배치 설정", EditorStyles.boldLabel);
        instrumentCount = EditorGUILayout.IntSlider("악기 개수",     instrumentCount, 3, 7);
        radius          = EditorGUILayout.Slider("반원 반지름 (m)", radius,          0.8f, 3f);
        heightOffset    = EditorGUILayout.Slider("높이 (m)",        heightOffset,    0.5f, 2.0f);
        arcDegrees      = EditorGUILayout.Slider("호 각도",          arcDegrees,      90f,  180f);
        GUILayout.Space(10);
        if (GUILayout.Button("악기 배치 생성")) CreateInstruments();
        if (GUILayout.Button("무대 바닥 생성")) CreateStage();
        if (GUILayout.Button("조명 세팅"))     CreateLights();
        GUILayout.Space(10);
        GUILayout.Label("게임 시스템", EditorStyles.boldLabel);
        if (GUILayout.Button("① 게임 시스템(매니저) 세팅")) CreateManagers();
        if (GUILayout.Button("② UI 세팅"))                 CreateUI();
        EditorGUILayout.Space(2);
        if (GUILayout.Button("①+② 한 번에 (매니저 + UI)")) CreateGameSystem();
        GUILayout.Space(10);
        GUILayout.Label("상호작용", EditorStyles.boldLabel);
        if (GUILayout.Button("컨트롤러에 드럼스틱 부착")) CreateControllerSticks();
        if (GUILayout.Button("핸드 트래킹(VR 손) 세팅")) SetupHandTracking();
        GUILayout.Space(10);
        GUILayout.Label("번외 스테이지", EditorStyles.boldLabel);
        if (GUILayout.Button("보너스 스테이지(리듬) 세팅")) SetupRhythmStage();
    }

    // 비트세이버식 리듬 모드를 세팅: RhythmStageManager 생성/연결 + 타이틀에 진입 버튼 추가.
    // '게임 시스템 + UI 세팅'을 먼저 실행한 뒤 사용해야 한다.
    void SetupRhythmStage()
    {
        var sys = GameObject.Find("GameSystem");
        if (sys == null)
        {
            Debug.LogWarning("[BeatHeal] GameSystem이 없습니다. 먼저 '게임 시스템 + UI 세팅'을 실행하세요.");
            return;
        }

        var game = sys.GetComponent<GameManager>();
        var ui   = sys.GetComponent<UIManager>();

        var rhythm = sys.GetComponent<RhythmStageManager>();
        if (rhythm == null) rhythm = Undo.AddComponent<RhythmStageManager>(sys);

        // 악기 레인 연결
        var instParent = GameObject.Find("Instruments");
        if (instParent != null)
            rhythm.instruments = instParent.GetComponentsInChildren<InstrumentPanel>(true);
        else
            Debug.LogWarning("[BeatHeal] Instruments를 찾지 못했습니다. 먼저 '악기 배치 생성'을 실행하세요.");

        rhythm.game = game;
        if (game != null) game.rhythmManager = rhythm;

        // 타이틀 패널에 '리듬 모드' 진입 버튼 추가
        var titlePanel = GameObject.Find("TitlePanel");
        if (titlePanel != null && ui != null)
        {
            var existing = titlePanel.transform.Find("RhythmButton");
            if (existing != null) DestroyImmediate(existing.gameObject);

            var rhythmBtn = MakeButton("RhythmButton", titlePanel.transform, "🎵 리듬 모드 (번외)",
                                       new Vector2(0, -410), new Color(0.9f, 0.3f, 0.6f));
            UnityEventTools.AddPersistentListener(rhythmBtn.onClick, ui.StartBonusStage);
        }
        else
        {
            Debug.LogWarning("[BeatHeal] TitlePanel을 찾지 못했습니다. '게임 시스템 + UI 세팅'을 먼저 실행하세요. " +
                             "(매니저는 연결됐으니 UIManager.StartBonusStage를 다른 버튼에 연결해도 됩니다.)");
        }

        Selection.activeGameObject = sys;
        Debug.Log("[BeatHeal] 보너스 리듬 스테이지 세팅 완료! 타이틀의 '🎵 리듬 모드' 버튼으로 시작합니다.");
    }

    // 컨트롤러 대신(또는 병행해서) 맨손으로 악기를 칠 수 있도록 핸드 트래킹을 세팅한다.
    // XR Origin에 HandPokeDriver를 붙여, 손가락 끝에 "Hand" 태그 트리거 콜라이더가 따라가게 한다.
    void SetupHandTracking()
    {
        EnsureTag("Hand");

        var xrOrigin = GameObject.Find("XR Origin (XR Rig)")
                    ?? GameObject.Find("XR Origin")
                    ?? GameObject.Find("XRRig");

        if (xrOrigin == null)
        {
            Debug.LogWarning("[BeatHeal] XR Origin을 찾지 못했습니다. 씬에 'XR Origin (XR Rig)'이 있는지 확인하세요.");
            return;
        }

        var driver = xrOrigin.GetComponent<HandPokeDriver>();
        if (driver == null)
        {
            driver = Undo.AddComponent<HandPokeDriver>(xrOrigin);
            Debug.Log($"[BeatHeal] HandPokeDriver를 '{xrOrigin.name}'에 부착했습니다.");
        }
        else
        {
            Debug.Log($"[BeatHeal] '{xrOrigin.name}'에 이미 HandPokeDriver가 있습니다.");
        }

        Selection.activeGameObject = xrOrigin;
        Debug.Log("[BeatHeal] 핸드 트래킹 세팅 완료! OpenXR 설정에서 'Hand Tracking Subsystem' 기능을 활성화했는지 확인하세요. " +
                  "(Project Settings > XR Plug-in Management > OpenXR > 각 플랫폼 탭에서 Hand Tracking 체크)");
    }

    // 양손 컨트롤러에 드럼스틱(콜라이더 + XRController 태그)을 부착해 악기를 칠 수 있게 함
    void CreateControllerSticks()
    {
        EnsureTag("XRController");

        // 이름에 "controller"가 들어간 트랜스폼을 컨트롤러로 간주
        var controllers = new System.Collections.Generic.List<Transform>();
        foreach (var t in GameObject.FindObjectsOfType<Transform>())
        {
            string n = t.name.ToLower();
            if (n.Contains("controller") && !n.Contains("xr origin") && !n.Contains("interaction manager"))
                controllers.Add(t);
        }

        if (controllers.Count == 0)
        {
            Debug.LogWarning("[BeatHeal] 컨트롤러를 찾지 못했습니다. XR Rig의 Left/Right Controller가 씬에 있는지 확인하세요. " +
                             "수동으로 컨트롤러에 드럼스틱을 붙이려면 이 함수의 AttachStick 로직을 참고하세요.");
            return;
        }

        int count = 0;
        foreach (var c in controllers)
        {
            AttachStick(c);
            count++;
        }
        Debug.Log($"[BeatHeal] 드럼스틱 {count}개 부착 완료! (대상: {string.Join(", ", controllers.ConvertAll(x => x.name))})");
    }

    void AttachStick(Transform controller)
    {
        // 중복 방지
        var existing = controller.Find("DrumStick");
        if (existing != null) DestroyImmediate(existing.gameObject);

        var stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stick.name = "DrumStick";
        stick.transform.SetParent(controller, false);
        stick.transform.localPosition = new Vector3(0f, 0f, 0.18f);  // 컨트롤러 앞으로
        stick.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Z축 방향으로 눕힘
        stick.transform.localScale = new Vector3(0.02f, 0.18f, 0.02f); // 가늘고 길게 (약 0.36m)
        stick.tag = "XRController";

        // 트리거 콜라이더 (악기와 겹치면 InstrumentPanel.OnTriggerEnter 발생)
        var col = stick.GetComponent<CapsuleCollider>();
        col.isTrigger = true;

        Undo.RegisterCreatedObjectUndo(stick, "Create DrumStick");
    }

    // 태그가 없으면 ProjectSettings에 추가
    static void EnsureTag(string tag)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;

        var so = new SerializedObject(assets[0]);
        var tagsProp = so.FindProperty("tags");
        for (int i = 0; i < tagsProp.arraySize; i++)
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        so.ApplyModifiedProperties();
        Debug.Log($"[BeatHeal] 태그 '{tag}' 추가됨.");
    }

    // 매니저 + UI를 한 번에 세팅 (편의용 — 내부적으로 둘을 순서대로 호출)
    void CreateGameSystem()
    {
        CreateManagers();
        CreateUI();
    }

    // === ① 게임 시스템(매니저) 세팅 — 매니저 컴포넌트 생성 + 참조/효과음/악기 연결 ===
    void CreateManagers()
    {
        var old = GameObject.Find("GameSystem");
        if (old != null) DestroyImmediate(old);

        // --- 매니저 오브젝트 ---
        var sys = new GameObject("GameSystem");
        var game = sys.AddComponent<GameManager>();
        var seq  = sys.AddComponent<SequenceManager>();
        var ui   = sys.AddComponent<UIManager>();
        var tut  = sys.AddComponent<TutorialManager>();
        var spawner = sys.AddComponent<BonusBallSpawner>();
        var rhythm = sys.AddComponent<RhythmStageManager>();
        var sfx = sys.AddComponent<SfxPlayer>();   // 효과음 재생기 (AudioSource 자동 추가)
        // 효과음 클립 자동 연결 (Casual Game Sounds 팩 — 파형 분석으로 매칭)
        const string sfxDir = "Assets/Casual Game Sounds U6/CasualGameSounds/";
        sfx.hitSuccess = LoadClip(sfxDir + "DM-CGS-30.wav"); // 상승음 차임 = 성공
        sfx.hitFail    = LoadClip(sfxDir + "DM-CGS-02.wav"); // 하강음 = 실패
        sfx.ballHit    = LoadClip(sfxDir + "DM-CGS-44.wav"); // 짧은 팝 = 공 타격

        game.sequenceManager = seq;
        game.uiManager = ui;
        game.rhythmManager = rhythm;
        ui.game = game;
        ui.tutorial = tut;
        tut.uiManager = ui;
        spawner.game = game;
        rhythm.game = game;

        // 악기 배열 연결
        var instParent = GameObject.Find("Instruments");
        if (instParent != null)
        {
            seq.instruments = instParent.GetComponentsInChildren<InstrumentPanel>(true);
            tut.instruments = seq.instruments;
            rhythm.instruments = seq.instruments;   // 리듬 모드도 같은 악기 레인 사용
        }
        else
            Debug.LogWarning("[BeatHeal] Instruments를 찾지 못했습니다. 먼저 '악기 배치 생성'을 실행하세요.");

        // 무대 조명 연결 (관객 반응용)
        var stageLight = GameObject.Find("StagePointLight");
        if (stageLight != null) ui.stageLight = stageLight.GetComponent<Light>();

        Undo.RegisterCreatedObjectUndo(sys, "Create GameSystem");
        Selection.activeGameObject = sys;
        Debug.Log("[BeatHeal] ① 게임 시스템(매니저) 세팅 완료! 이어서 'UI 세팅'을 실행하세요.");
    }

    // === ② UI 세팅 — Canvas/패널/버튼 생성 + UIManager 참조 연결 (매니저가 먼저 있어야 함) ===
    void CreateUI()
    {
        var sys = GameObject.Find("GameSystem");
        if (sys == null)
        {
            Debug.LogWarning("[BeatHeal] GameSystem이 없습니다. 먼저 '① 게임 시스템(매니저) 세팅'을 실행하세요.");
            return;
        }

        var game = sys.GetComponent<GameManager>();
        var ui   = sys.GetComponent<UIManager>();
        var tut  = sys.GetComponent<TutorialManager>();
        if (game == null || ui == null || tut == null)
        {
            Debug.LogWarning("[BeatHeal] GameSystem에 매니저 컴포넌트가 없습니다. '① 게임 시스템(매니저) 세팅'을 다시 실행하세요.");
            return;
        }

        var oldCanvas = GameObject.Find("GameCanvas");
        if (oldCanvas != null) DestroyImmediate(oldCanvas);

        var instParent = GameObject.Find("Instruments");

        // --- Canvas (World Space, VR에서 보이도록) ---
        var canvasObj = new GameObject("GameCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        var center = instParent != null ? instParent.transform.position : Vector3.zero;
        canvasObj.transform.position = center + new Vector3(0f, 1.6f, 1.5f);
        // 플레이어(+Z 방향을 봄)를 향하도록 180° 회전 → 글자가 정방향으로 보임
        canvasObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        canvasObj.transform.localScale = Vector3.one * 0.003f;
        var crt = canvasObj.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(800, 980); // 난이도 4종+튜토리얼+리듬 버튼이 들어가도록 세로 확장

        // World Space 캔버스는 이벤트 카메라가 있어야 버튼 클릭(레이캐스트)이 동작함
        canvas.worldCamera = Camera.main ?? GameObject.FindObjectOfType<Camera>();

        // XR 컨트롤러 레이로 UI를 누르려면 TrackedDeviceGraphicRaycaster가 필요 (마우스도 같이 동작)
        AddXRComponent(canvasObj, "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster");

        // EventSystem 세팅
        var existingES = GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        GameObject esObj = existingES != null ? existingES.gameObject : null;
        if (esObj == null)
        {
            esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }

        // XR UI Input Module 우선 사용 (없으면 StandaloneInputModule 폴백)
        var xrModule = AddXRComponent(esObj, "UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule");
        if (xrModule != null)
        {
            // XR 모듈을 쓰면 StandaloneInputModule은 충돌하므로 제거
            var legacy = esObj.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (legacy != null) DestroyImmediate(legacy);
        }
        else if (esObj.GetComponent<UnityEngine.EventSystems.BaseInputModule>() == null)
        {
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.LogWarning("[BeatHeal] XRUIInputModule을 찾지 못해 StandaloneInputModule(마우스 전용)로 폴백했습니다.");
        }

        // --- 타이틀 패널 (난이도 선택) ---
        var title = MakePanel("TitlePanel", canvasObj.transform);
        MakeText("Title", title.transform, "BEAT HEAL", 60, new Vector2(0, 300));
        MakeText("Subtitle", title.transform, "난이도 선택", 32, new Vector2(0, 210));

        var easyBtn   = MakeButton("EasyButton",   title.transform, "쉬움 (악기 3개)",      new Vector2(0, 115),
                                   new Color(0.2f, 0.7f, 0.3f));
        var normalBtn = MakeButton("NormalButton", title.transform, "보통 (악기 5개)",      new Vector2(0, 20),
                                   new Color(0.2f, 0.5f, 0.9f));
        var hardBtn   = MakeButton("HardButton",   title.transform, "어려움 (악기 전부)",    new Vector2(0, -75),
                                   new Color(0.85f, 0.3f, 0.2f));
        var ultraBtn  = MakeButton("UltraHardButton", title.transform, "초 하드 (공 날아옴!)", new Vector2(0, -170),
                                   new Color(0.6f, 0.1f, 0.15f));

        UnityEventTools.AddIntPersistentListener(easyBtn.onClick,   ui.StartWithDifficulty, 0);
        UnityEventTools.AddIntPersistentListener(normalBtn.onClick, ui.StartWithDifficulty, 1);
        UnityEventTools.AddIntPersistentListener(hardBtn.onClick,   ui.StartWithDifficulty, 2);
        UnityEventTools.AddIntPersistentListener(ultraBtn.onClick,  ui.StartWithDifficulty, 3);

        // 튜토리얼 버튼
        var tutBtn = MakeButton("TutorialButton", title.transform, "튜토리얼", new Vector2(0, -265),
                                new Color(0.5f, 0.4f, 0.7f));
        UnityEventTools.AddPersistentListener(tutBtn.onClick, ui.OnTutorialButton);

        // 리듬 모드(번외) 진입 버튼 — 재실행 시 사라지지 않도록 여기서 함께 생성
        var rhythmBtn = MakeButton("RhythmButton", title.transform, "🎵 리듬 모드 (번외)", new Vector2(0, -360),
                                   new Color(0.9f, 0.3f, 0.6f));
        UnityEventTools.AddPersistentListener(rhythmBtn.onClick, ui.StartBonusStage);

        // --- HP 전용 패널 (HUD와 분리, 좌상단 강조 배치) ---
        var hpPanel = MakePanel("HpPanel", canvasObj.transform, transparent: true);
        // 사이먼(소량 HP)용: HP 개수만큼 하트 아이콘 (하나씩 사라짐)
        ui.hpHearts = MakeHearts("HpHearts", hpPanel.transform, new Vector2(-200, 330), game.maxHP);
        // 리듬(대량 HP)용: fill 게이지 바 (평소엔 숨김, 리듬 모드에서 표시)
        var hpBar = MakeHPBar("HPBar", hpPanel.transform, new Vector2(-200, 330), new Vector2(460, 64));
        ui.hpBarRoot = hpBar.root;
        ui.hpFill = hpBar.fill;
        ui.hpText = hpBar.label;
        ui.hpPanel = hpPanel;
        hpPanel.SetActive(false);

        // --- HUD 패널 ---
        var hud = MakePanel("HudPanel", canvasObj.transform, transparent: true);
        ui.scoreText     = MakeText("Score", hud.transform, "SCORE: 0", 36, new Vector2(250, 250));
        ui.comboText     = MakeText("Combo", hud.transform, "",         48, new Vector2(0, 0));
        ui.roundText     = MakeText("Round", hud.transform, "ROUND 1",  36, new Vector2(0, 250));
        ui.countdownText = MakeText("Countdown", hud.transform, "3",    120, new Vector2(0, -100));
        // 기믹 안내 배너
        ui.bannerText    = MakeText("Banner", hud.transform, "", 40, new Vector2(0, 150));
        ui.bannerText.color = new Color(1f, 0.9f, 0.2f);
        ui.bannerText.gameObject.SetActive(false);
        // 스테이지 목표 진행도 (HUD 좌측 상단)
        ui.goalText = MakeText("Goal", hud.transform, "", 26, new Vector2(-250, 130));
        ui.goalText.alignment = TextAnchor.UpperLeft;
        var goalRt = ui.goalText.GetComponent<RectTransform>();
        goalRt.sizeDelta = new Vector2(440, 140);
        hud.SetActive(false);

        // --- 튜토리얼 패널 ---
        var tutPanel = MakePanel("TutorialPanel", canvasObj.transform);
        ui.tutorialText = MakeText("TutorialText", tutPanel.transform, "", 34, new Vector2(0, 220));
        MakeText("TutorialHint", tutPanel.transform, "(드럼스틱으로 악기를 치세요)", 26, new Vector2(0, -260));
        var tutSkipBtn = MakeButton("SkipButton", tutPanel.transform, "건너뛰기", new Vector2(0, -330),
                                    new Color(0.4f, 0.4f, 0.4f));
        UnityEventTools.AddPersistentListener(tutSkipBtn.onClick, tut.StopTutorial);
        UnityEventTools.AddPersistentListener(tutSkipBtn.onClick, ui.EndTutorial);
        tutPanel.SetActive(false);
        ui.tutorialPanel = tutPanel;

        // --- 결과 패널 ---
        var result = MakePanel("ResultPanel", canvasObj.transform);
        ui.resultText = MakeText("Result", result.transform, "GAME OVER", 36, new Vector2(0, 80));
        var restartBtn = MakeButton("RestartButton", result.transform, "다시 하기", new Vector2(0, -200));
        UnityEventTools.AddPersistentListener(restartBtn.onClick, ui.OnRestartButton);
        result.SetActive(false);

        ui.titlePanel  = title;
        ui.hudPanel    = hud;
        ui.resultPanel = result;

        Undo.RegisterCreatedObjectUndo(canvasObj, "Create GameCanvas");
        Selection.activeGameObject = canvasObj;
        Debug.Log("[BeatHeal] ② UI 세팅 완료! (난이도/리듬 버튼·HP·목표·결과 화면 생성, 참조 자동 연결됨)");
    }

    // XRI 타입을 리플렉션으로 찾아 컴포넌트로 추가 (없으면 null + 경고). 컴파일 의존성 회피용.
    Component AddXRComponent(GameObject go, string fullTypeName)
    {
        var type = System.Type.GetType(fullTypeName + ", Unity.XR.Interaction.Toolkit");
        if (type == null)
        {
            // 어셈블리 한정 없이 전체 로드된 어셈블리에서 탐색
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(fullTypeName);
                if (type != null) break;
            }
        }
        if (type == null)
        {
            Debug.LogWarning($"[BeatHeal] {fullTypeName} 타입을 찾지 못했습니다 (XR Interaction Toolkit 미설치?).");
            return null;
        }
        var existing = go.GetComponent(type);
        return existing != null ? existing : go.AddComponent(type);
    }

    static AudioClip LoadClip(string path)
    {
        var c = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (c == null) Debug.LogWarning("[BeatHeal] 오디오 클립을 찾지 못했습니다: " + path);
        return c;
    }

    GameObject MakePanel(string name, Transform parent, bool transparent = false)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = transparent ? new Color(0, 0, 0, 0f) : new Color(0, 0, 0, 0.7f);
        img.raycastTarget = false;
        return go;
    }

    Text MakeText(string name, Transform parent, string content, int size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
              ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(700, 200);
        rt.anchoredPosition = pos;
        return t;
    }

    // 외곽 프레임 + 배경 + 채워지는 Fill 이미지 + ♥ 아이콘 + 외곽선 숫자 라벨로 구성된
    // 가독성 높은 HP 게이지 바를 만든다.
    (Image fill, Text label, GameObject root) MakeHPBar(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rrt = root.GetComponent<RectTransform>();
        rrt.sizeDelta = size;
        rrt.anchoredPosition = pos;

        // 외곽 프레임 (밝은 테두리 → 어두운 배경 위에서 바가 또렷이 보임)
        var frameGo = new GameObject("Frame", typeof(RectTransform));
        frameGo.transform.SetParent(root.transform, false);
        var frame = frameGo.AddComponent<Image>();
        frame.sprite = uiSprite;
        frame.type = Image.Type.Sliced;
        frame.color = new Color(0.95f, 0.95f, 1f, 0.9f);
        Stretch(frameGo.GetComponent<RectTransform>(), new Vector2(-4, -4), new Vector2(4, 4));

        // 배경 (어두운 박스)
        var bgGo = new GameObject("BG", typeof(RectTransform));
        bgGo.transform.SetParent(root.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.sprite = uiSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);
        Stretch(bgGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        // 채워지는 바 (가로 Filled)
        var fillGo = new GameObject("Fill", typeof(RectTransform));
        fillGo.transform.SetParent(root.transform, false);
        var fill = fillGo.AddComponent<Image>();
        fill.sprite = uiSprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        fill.color = new Color(0.2f, 0.9f, 0.35f);
        Stretch(fillGo.GetComponent<RectTransform>(), new Vector2(6, 6), new Vector2(-6, -6));

        // ♥ 아이콘 (바 왼쪽 바깥)
        var heart = MakeText(name + "_Heart", root.transform, "♥", 44, Vector2.zero);
        heart.color = new Color(1f, 0.35f, 0.4f);
        AddOutline(heart, 2f);
        var hrt = heart.GetComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(60, 60);
        hrt.anchorMin = hrt.anchorMax = new Vector2(0f, 0.5f);
        hrt.pivot = new Vector2(1f, 0.5f);
        hrt.anchoredPosition = new Vector2(-8, 0);

        // 숫자 라벨 (바 위 중앙, 굵게 + 외곽선으로 가독성 확보)
        var label = MakeText(name + "_Label", root.transform, "HP", 30, Vector2.zero);
        label.fontStyle = FontStyle.Bold;
        AddOutline(label, 2f);
        Stretch(label.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        return (fill, label, root);
    }

    // HP 개수만큼 하트 아이콘을 가로로 나열해 만든다 (HP가 줄면 UIManager가 하나씩 끔).
    Image[] MakeHearts(string name, Transform parent, Vector2 pos, int count)
    {
        count = Mathf.Max(1, count);
        var heartSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rrt = root.GetComponent<RectTransform>();
        rrt.anchoredPosition = pos;
        rrt.sizeDelta = new Vector2(count * 90f, 90f);

        const float spacing = 84f;
        float startX = -(count - 1) * spacing / 2f;

        var hearts = new Image[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Heart_" + i, typeof(RectTransform));
            go.transform.SetParent(root.transform, false);
            var img = go.AddComponent<Image>();
            img.sprite = heartSprite;        // 원형 노브 스프라이트를 빨간 HP 구슬로 사용
            img.color = new Color(1f, 0.3f, 0.35f, 1f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(72, 72);
            rt.anchoredPosition = new Vector2(startX + i * spacing, 0);

            // 어두운 배경에서도 또렷하게 (그림자)
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.6f);
            sh.effectDistance = new Vector2(2f, -2f);

            hearts[i] = img;
        }
        return hearts;
    }

    // 텍스트에 검은 외곽선 + 그림자를 추가해 어떤 배경에서도 잘 읽히게 한다.
    static void AddOutline(Text t, float dist)
    {
        var outline = t.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(dist, -dist);
        var shadow = t.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(dist, -dist);
    }

    // RectTransform을 부모에 꽉 채우되 여백(offset) 적용
    static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    Button MakeButton(string name, Transform parent, string label, Vector2 pos, Color? color = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color ?? new Color(0.2f, 0.5f, 0.9f, 1f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(360, 90);
        rt.anchoredPosition = pos;
        MakeText(name + "_Label", go.transform, label, 32, Vector2.zero);
        return btn;
    }

    void CreateInstruments()
    {
        // XR Origin 위치를 기준점으로 사용
        var xrOrigin = GameObject.Find("XR Origin (XR Rig)")
                    ?? GameObject.Find("XR Origin")
                    ?? GameObject.Find("XRRig")
                    ?? GameObject.Find("Camera Offset");

        Vector3 center = xrOrigin != null ? xrOrigin.transform.position : Vector3.zero;

        if (xrOrigin == null)
            Debug.LogWarning("[BeatHeal] XR Origin을 찾지 못했습니다. 월드 원점(0,0,0) 기준으로 배치합니다.");
        else
            Debug.Log($"[BeatHeal] XR Origin 발견: {xrOrigin.name} @ {center}");

        var existing = GameObject.Find("Instruments");
        if (existing != null) DestroyImmediate(existing);

        var parent = new GameObject("Instruments");
        Undo.RegisterCreatedObjectUndo(parent, "Create Instruments");

        for (int i = 0; i < instrumentCount; i++)
        {
            float t     = instrumentCount == 1 ? 0.5f : (float)i / (instrumentCount - 1);
            float angle = Mathf.Lerp(-arcDegrees / 2f, arcDegrees / 2f, t);
            float rad   = angle * Mathf.Deg2Rad;
            // XR Origin 기준 반원 배치
            Vector3 pos = center + new Vector3(Mathf.Sin(rad) * radius, heightOffset, Mathf.Cos(rad) * radius);

            Color col = InstrumentColors[i % InstrumentColors.Length];

            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = $"Instrument_{i}";
            obj.transform.position   = pos;
            obj.transform.localScale = new Vector3(0.28f, 0.04f, 0.28f);
            obj.transform.SetParent(parent.transform);

            // 머티리얼
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = col;
            obj.GetComponent<Renderer>().material = mat;

            // 트리거 콜라이더
            var col3d = obj.GetComponent<CapsuleCollider>();
            col3d.isTrigger = true;

            // 리지드바디
            var rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;

            // InstrumentPanel
            var panel = obj.AddComponent<InstrumentPanel>();
            panel.instrumentIndex = i;
            panel.idleColor = col;
            // 고유색을 유지하되 살짝만 밝게 → 발광 효과로 충분히 도드라짐
            panel.litColor  = Color.Lerp(col, Color.white, 0.3f);

            // 악기 전용 포인트 라이트 (악기 바로 위)
            var lightObj = new GameObject($"Instrument_{i}_Light");
            lightObj.transform.position = pos + Vector3.up * 0.3f;
            lightObj.transform.SetParent(obj.transform);
            var pointLight = lightObj.AddComponent<Light>();
            pointLight.type      = LightType.Point;
            pointLight.color     = col;
            pointLight.intensity = 1.5f;
            pointLight.range     = 0.8f;
            Undo.RegisterCreatedObjectUndo(lightObj, "Create Instrument Light");

            Undo.RegisterCreatedObjectUndo(obj, "Create Instrument");
        }

        Debug.Log($"악기 {instrumentCount}개 생성 완료!");
        Selection.activeGameObject = parent;
    }

    void CreateLights()
    {
        var dir = GameObject.Find("Directional Light");
        if (dir != null)
        {
            var l = dir.GetComponent<Light>();
            l.color     = new Color(1f, 0.78f, 0.58f);
            l.intensity = 0.5f;
        }

        var ep = GameObject.Find("StagePointLight");
        if (ep != null) DestroyImmediate(ep);

        var pl = new GameObject("StagePointLight");
        pl.transform.position = new Vector3(0f, 3f, 1f);
        var lt = pl.AddComponent<Light>();
        lt.type      = LightType.Point;
        lt.color     = Color.white;
        lt.intensity = 3f;
        lt.range     = 8f;

        Undo.RegisterCreatedObjectUndo(pl, "Create Light");
        Debug.Log("조명 세팅 완료!");
    }

    void CreateStage()
    {
        var ex = GameObject.Find("Stage");
        if (ex != null) DestroyImmediate(ex);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name             = "Stage";
        floor.transform.localScale = new Vector3(2f, 1f, 2f);

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = new Color(0.10f, 0.10f, 0.15f);
        floor.GetComponent<Renderer>().material = mat;

        Undo.RegisterCreatedObjectUndo(floor, "Create Stage");
        Debug.Log("무대 바닥 생성 완료!");
    }
}
