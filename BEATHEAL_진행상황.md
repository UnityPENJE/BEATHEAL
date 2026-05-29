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

## 태그 설정 필요 (아직 미완료)

`InstrumentPanel.cs`의 `OnTriggerEnter`에서 아래 태그를 감지합니다.
Unity 에디터에서 직접 태그를 추가해야 합니다:

- `XRController` — XR 컨트롤러 오브젝트에 적용
- `Hand` — Hand Tracking 사용 시 적용

---

## 다음 작업 — Phase 3: 핵심 게임 로직

아래 스크립트들을 `Assets/Scripts/` 에 생성해야 합니다:

### 작성 필요한 스크립트

| 파일명 | 역할 |
|--------|------|
| `SequenceManager.cs` | 시퀀스 생성, 터치 판정, HP, 점수/콤보 |
| `GameManager.cs` | 게임 상태 관리 (시작/진행/종료) |
| `UIManager.cs` | HUD 업데이트 (HP, 점수, 콤보, 라운드) |

### SequenceManager 설계 요약
- 라운드마다 시퀀스 길이 +1
- 악기 점등 → 플레이어 터치 판정 (InstrumentPanel.OnTouched 구독)
- 정답: 다음 순서로 진행 / 오답: HP -1
- HP 3개, 0이 되면 게임 오버
- 점수: 정답 시 +100 × 콤보배율
- 특수 라운드: 페이크 악기(깜빡임), 역재생 라운드

---

## 평가 항목 진행률

| 번호 | 평가 항목 | 상태 |
|------|----------|------|
| 1 | 오류 없이 실행 | 🔲 |
| 2 | 시작과 종료 | 🔲 |
| 3 | 막힘 방지 장치 | 🔲 |
| 4 | 이동/진행 방식 | ✅ 제자리 고정 |
| 5 | 상호작용 1 — 악기 터치 판정 | 🔲 (InstrumentPanel 준비됨) |
| 6 | 상호작용 2 — 관객 반응 시스템 | 🔲 |
| 7 | 상호작용 3 — 악기 발광 시퀀스 | 🔲 |
| 8 | 피드백 1 — 햅틱 + 색상 | 🔲 |
| 9 | 피드백 2 — 관객 함성 | 🔲 |
| 10 | 과업 1 — 페이크 악기 | 🔲 |
| 11 | 과업 2 — 역재생 라운드 | 🔲 |
| 12 | UI 1 — 인게임 HUD | 🔲 |
| 13 | UI 2 — 결과 화면 | 🔲 |

---

## 새 채팅 시작 시 전달할 내용

```
이 파일(BEATHEAL_진행상황.md)을 참고해서 BEAT HEAL VR 재활 게임 개발을 이어서 진행해줘.
현재 Phase 2까지 완료됐고, Phase 3 핵심 게임 로직(SequenceManager, GameManager, UIManager)부터 시작해야 해.
프로젝트 경로: D:\SWJ30810\BEATHEAL
```
