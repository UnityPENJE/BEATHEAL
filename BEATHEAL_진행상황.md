# BEAT HEAL — 개발 진행 상황

> 이 파일을 새 채팅 시작 시 첨부하면 이전 작업 내용을 이어서 진행할 수 있습니다.

---

## 프로젝트 기본 정보

| 항목 | 내용 |
|------|------|
| 프로젝트 경로 | `D:\SWJ30810\BEATHEAL` |
| Unity 버전 | 2022.3 LTS (URP) |
| XR 패키지 | XR Interaction Toolkit 3.4.1 |
| 테스트 방식 | XR Device Simulator (PC) |

---

## 📌 다음 작업 계획 (계획만 확정, 아직 미구현)

### 1. 상호작용 2 — 관객 반응 강화 (조명 + 함성 방식)
> 관객 오브젝트는 추가하지 않고, **무대 조명 변화 + 함성 사운드**를 확실하게 만든다.
- 현재 `UIManager.OnAudienceReact`의 무대 조명 세기 변화가 잘 안 보임 → 변화 폭/속도를 키우고 색도 바꾼다
- 콤보 구간별로 조명·함성이 단계적으로 고조되게 (예: 콤보 0~2 / 3~5 / 6+ 단계)
- 함성 AudioSource에 클립 연결 + 콤보에 따라 볼륨/피치 상승
- 무대 조명 레퍼런스(`stageLight`)가 실제로 연결됐는지 점검 필요

### 2. 과업 — 목표 달성형 미션 시스템 (평가 10·11번 충족)
> 게임 진행 중 **목표가 HUD에 표시되고, 달성하면 완료 처리**되는 미션 시스템.
- 미션 예시: "콤보 5 달성", "라운드 5 도달", "가짜 악기 3회 회피", "역재생 라운드 1회 성공"
- 여러 미션을 순차/동시 제공하고 달성 시 알림(배너) + 완료 체크
- 신규 스크립트 `MissionManager.cs` 예정, HUD에 미션 텍스트 영역 추가
- 과업1 = 미션 시스템 자체, 과업2 = 두 번째 미션(또는 미션 누적 보상) 등으로 매핑

---

## 완료된 작업

### ✅ Phase 1 — Unity 프로젝트 세팅
- URP 프로젝트 생성 완료
- XR Interaction Toolkit 3.4.1 설치 완료
- XR Device Simulator import 완료
- Starter Assets import 완료

### ✅ Phase 2 — 게임 씬 구성
- 악기 오브젝트 배치 완료 (납작한 실린더 5개, 색상 구분)
- 무대 바닥 생성 완료
- 조명 세팅 완료
- 각 악기에 개별 포인트 라이트 부착 완료

### ✅ Phase 3 — 핵심 게임 로직 (스크립트 작성 완료)
- `GameManager.cs` — 상태(Title/Countdown/Playing/GameOver), 난이도(Easy/Normal/Hard), HP·점수·콤보·라운드, 시작/종료/재시작
- `SequenceManager.cs` — 시퀀스 생성(+1), 발광 시퀀스, 터치 판정, 페이크 악기, 역재생 라운드, 햅틱
  - 배열 자동 복구(`EnsureInstruments`) + `instrumentIndex` 정렬로 판정 일관성 보장
  - **난이도별 노트 풀/속도/가이드** (`ApplyDifficulty`):
    - 쉬움: 악기 3개·느림·**가이드 ON**(칠 노트 점등 안내)
    - 보통: 악기 5개·**순수 암기**(입력 중 노트 안 켜짐)
    - 어려움: 악기 전부·빠름·**순수 암기**
  - 입력 단계엔 노트를 켜지 않아 외운 대로 쳐야 함 (가이드 모드 제외)
  - 반응속도 기록을 SequenceManager로 이전 (점등 의존 제거)
- `UIManager.cs` — 타이틀(난이도 3버튼+튜토리얼)/HUD/카운트다운/결과 화면, 기믹 안내 배너, 관객 반응
- `InstrumentPanel.cs` — 점등 연출: **발광 + 크기 팝업 + 라이트 강화**, 가짜 악기는 주황색 발광 깜빡임으로 구분
- `TutorialManager.cs` — **단계별 튜토리얼** (기본 터치 → 시퀀스 → 가짜 악기 → 역재생)
- `BeatHealSetup.cs` — **"게임 시스템 + UI 세팅"** 버튼 (매니저·Canvas·난이도/튜토리얼 버튼·배너·XRI UI·참조 자동 연결)

### 노트 간격 / 기믹 구분 (요청 반영)
- 악기 배치 기본값 축소: radius 1.25, arcDegrees 95 → 악기들이 더 가까이 모임
- 가짜 악기: 입력 단계까지 **주황빨강으로 계속 깜빡임** + "⚠ 가짜 악기" 배너
- 역재생: "🔄 역재생 라운드" 배너

### 보여주기 / 치는 단계 구분 (요청 반영)
- **보여주기 단계**: 악기가 **흰색**으로 점등 ("👀 잘 보세요" 배너)
- **전환 신호**: 보여주기가 끝나면 활성 악기가 **초록색으로 한 번 펄스** + "🥁 당신 차례!" 배너
- **치는 단계**: 보통/어려움은 어두움(암기), 쉬움은 고유색 안내
- 색상값은 `InstrumentPanel`의 `demoColor`(흰), `readyColor`(초록)로 조정 가능

---

## 생성된 스크립트 파일 목록

### `Assets/Scripts/GameData.cs`
- 씬 간 결과 데이터 전달용 **정적 클래스**
- 보관 데이터: FinalScore, MaxCombo, TotalTouches, CorrectTouches, TotalReactionTime, MaxReach
- 계산 프로퍼티: Accuracy(정확도%), AvgReactionMs(평균반응속도)

### `Assets/Scripts/InstrumentPanel.cs`
- 악기 하나에 붙는 컴포넌트
- 주요 기능:
  - `LightOn()` / `LightOff()` — 점등/소등
  - `ShowCorrect()` / `ShowWrong()` — 정답/오답 색상
  - `StartFakeFlicker()` / `StopFakeFlicker()` — 페이크 깜빡임
  - `OnTriggerEnter` — XR 컨트롤러 태그(`XRController` 또는 `Hand`) 감지 시 반응속도/팔뻗기 기록 후 `OnTouched` 이벤트 발생
- Inspector 설정값: `instrumentIndex`, `isFake`, `idleColor`, `litColor`, `correctColor`, `wrongColor`

### `Assets/Scripts/Editor/BeatHealSetup.cs`
- Unity 에디터 전용 씬 자동 세팅 툴
- 메뉴: `BeatHeal → Setup Scene`
- 버튼:
  - **악기 배치 생성** — XR Origin 위치 기준 반원 배치, 색깔 실린더 + 포인트 라이트 자동 생성
  - **무대 바닥 생성** — 어두운 Plane 생성
  - **조명 세팅** — Directional Light 조정 + 무대 포인트 라이트 생성

---

## 현재 씬 Hierarchy 구조

```
Scene
├── XR Origin (XR Rig)         ← XR Device Simulator 포함
├── Directional Light          ← 따뜻한 색, intensity 0.5
├── StagePointLight            ← 무대 중앙 위 포인트 라이트
├── Stage                      ← 어두운 바닥 Plane
└── Instruments
    ├── Instrument_0            ← 빨강 실린더 + 빨강 포인트 라이트 (자식)
    ├── Instrument_1            ← 파랑
    ├── Instrument_2            ← 초록
    ├── Instrument_3            ← 노랑
    └── Instrument_4            ← 보라
```

> 각 Instrument에는 `InstrumentPanel`, `CapsuleCollider(isTrigger=true)`, `Rigidbody(isKinematic)` 컴포넌트가 붙어 있습니다.

---

## 상호작용 방식 — 드럼스틱 / VR 맨손

컨트롤러에 **드럼스틱(가는 실린더 + 트리거 콜라이더 + `XRController` 태그)**을 달아
악기를 쳐서 상호작용함. `BeatHeal → Setup Scene → 컨트롤러에 드럼스틱 부착` 버튼이:
- `XRController` 태그를 자동 생성 (Project Settings 수정)
- 이름에 "controller"가 포함된 트랜스폼(양손 컨트롤러)에 `DrumStick` 자식 생성
- 빠른 스윙 대응용 키네마틱 Rigidbody 포함

### ✅ VR 핸드 트래킹 (맨손 입력) 추가됨
- `HandPokeDriver.cs` — XR Hands(`XRHandSubsystem`)로 양손 검지·중지 끝에
  `Hand` 태그 트리거 콜라이더를 따라가게 함 → **컨트롤러 없이 맨손으로 악기 타격**
- `[RuntimeInitializeOnLoadMethod]`로 Play 시 XR Origin의 Camera Offset 아래에 **자동 설치** (버튼 불필요)
- `BeatHeal → Setup Scene → 핸드 트래킹(VR 손) 세팅` 버튼으로 수동 부착도 가능
- `Hand` 태그는 이미 프로젝트에 존재. OpenXR Hand Tracking Subsystem 기능은 활성화됨
- ⚠ 핸드 트래킹은 **실제 Quest 헤드셋에서만** 동작 (XR Device Simulator로는 테스트 불가)

> 드럼스틱(컨트롤러)과 맨손은 둘 다 `InstrumentPanel.OnTriggerEnter`의 `XRController`/`Hand` 태그를 통해 동작 — 병행 가능.

---

## 번외 스테이지 — 비트세이버식 리듬 모드 (신규)

기존 5개 악기를 **레인**으로 재사용해, 노트가 뒤에서 다가오면 타이밍 맞게 치는 모드.
- `RhythmNote.cs` — 악기 뒤 먼 곳에서 생성돼 악기(타격선)로 다가오는 노트. 악기와 **같은 실린더 모양/색**. 통과하면 놓침 처리
- `RhythmStageManager.cs` — 노트 스폰/판정. 악기 터치(드럼스틱·맨손 공용)로 판정:
  - `hitWindow` 안 = 성공, `perfectWindow` 안 = PERFECT
  - **사용 레인 화이트리스트**: 기본 2·4번(인덱스 1,3)만, 나머지 숨김
  - 기본값: travelTime 1.1s, spawnInterval 0.45s, noteCount 50 (난이도 상향됨)
- **리듬 전용 100 HP 시스템**:
  - 성공 시 회복(GOOD +4 / PERFECT +7), 빗맞춤 −8, 놓침 −10, 0이면 게임오버
  - `GameManager.RhythmHeal/RhythmDamage`, `CurrentMaxHP`로 사이먼(3HP)과 분리
- 진입: 타이틀의 **🎵 리듬 모드 (번외)** 버튼 (`BeatHeal → Setup Scene → 보너스 스테이지(리듬) 세팅`으로 생성)

---

## UI — HP 게이지 바 (이미지화)

- 기존 텍스트 하트(`@@@`) → **이미지 게이지 바**로 교체 (`UIManager.hpFill`, Image Type=Filled)
- HP 비율에 따라 색 변화(빨강→노랑→초록) + `HP 64 / 100` 숫자 라벨
- 사이먼·리듬 공통. `BeatHealSetup.MakeHPBar()`가 배경+Fill+라벨 3겹으로 생성

---

## VR 세팅 주의사항 (트러블슈팅 기록)

- **XR Device Simulator는 실기 테스트 시 비활성화** 필요 — 켜져 있으면 Quest 트래킹과 충돌해
  "카메라가 땅에 박히고 화면이 머리 따라 도는" 증상 발생. 씬에서 `XR Device Simulator` 오브젝트를 꺼둠
- Quest Link 사용 시 PC의 **활성 OpenXR 런타임을 Meta**로 설정해야 VR 모드로 진입(아니면 평면 모니터로 보임)

---

## 다음 작업 — Unity 에디터에서 씬 연결 (대부분 자동)

스크립트는 모두 작성됨. `BeatHeal → Setup Scene` 창에서 버튼만 순서대로 누르면 됨:

1. `악기 배치 생성` → `무대 바닥 생성` → `조명 세팅`
2. **`게임 시스템 + UI 세팅`** — 매니저 + Canvas + 난이도 버튼 + 참조 자동 연결
3. **`컨트롤러에 드럼스틱 부착`** — 태그 자동 생성 + 양손에 스틱 부착
4. (선택) UIManager의 `cheerAudio`, `cheerParticle`에 에셋 연결 (없어도 동작)
5. ▶ Play → 난이도 선택 → 카운트다운 → 드럼스틱으로 악기 치기

> World Space Canvas라 VR/시뮬레이터 카메라 정면(악기 위쪽)에 표시됨.
> 캔버스는 플레이어를 향해 180° 회전됨, `worldCamera` 자동 연결됨.

### UI 입력 처리 (자동 세팅에 포함됨)
- 캔버스에 `TrackedDeviceGraphicRaycaster`(XRI) 자동 추가 → XR 컨트롤러 레이로 버튼 클릭 가능
- EventSystem에 `XRUIInputModule`(XRI) 자동 추가 (마우스 입력도 함께 처리)
- XRI 타입을 못 찾으면 `StandaloneInputModule`(마우스 전용)로 자동 폴백
- ※ XR 컨트롤러 레이가 동작하려면 XR Rig에 Ray Interactor가 UI 상호작용 가능하도록 세팅돼 있어야 함

---

## 평가 항목 진행률

| 번호 | 평가 항목 | 상태 |
|------|----------|------|
| 1 | 오류 없이 실행 | 🔲 에디터 연결 후 검증 필요 |
| 2 | 시작과 종료 | ✅ 시작버튼 / HP 0 → GameOver |
| 3 | 막힘 방지 장치 | ✅ 라운드 전 카운트다운 + 결과화면 재시작 |
| 4 | 이동/진행 방식 | ✅ 제자리 고정 |
| 5 | 상호작용 1 — 악기 터치 판정 | ✅ InstrumentPanel + SequenceManager |
| 6 | 상호작용 2 — 관객 반응 시스템 | ✅ OnAudienceReact (조명/함성) |
| 7 | 상호작용 3 — 악기 발광 시퀀스 | ✅ PlaySequence |
| 8 | 피드백 1 — 햅틱 + 색상 | ✅ TriggerHaptic + ShowCorrect/Wrong |
| 9 | 피드백 2 — 관객 함성 | ✅ cheerAudio 볼륨 변화 (에셋 연결 시) |
| 10 | 과업 1 — 페이크 악기 | ✅ StartFakeFlicker (fakeRoundInterval) |
| 11 | 과업 2 — 역재생 라운드 | ✅ _reverse (reverseRoundInterval) |
| 12 | UI 1 — 인게임 HUD | ✅ HUD 패널 (HP/점수/콤보/라운드) |
| 13 | UI 2 — 결과 화면 | ✅ ResultPanel (점수/콤보/정확도/반응속도/팔뻗기) |

---

## 새 채팅 시작 시 전달할 내용

```
이 파일(BEATHEAL_진행상황.md)을 참고해서 BEAT HEAL VR 재활 게임 개발을 이어서 진행해줘.
현재 Phase 2까지 완료됐고, Phase 3 핵심 게임 로직(SequenceManager, GameManager, UIManager)부터 시작해야 해.
프로젝트 경로: D:\SWJ30810\BEATHEAL
```
