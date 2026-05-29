# BEAT HEAL — 개발 진행 계획

> VR 기반 상지·인지 재활 게임 | Unity XR Interaction Toolkit + XR Device Simulator

---

## 프로젝트 개요

| 항목 | 내용 |
|------|------|
| 게임 제목 | BEAT HEAL (가안) |
| 장르 | VR 인터랙션 / 재활 훈련 게임 |
| 플랫폼 | Unity (XR Device Simulator로 개발) |
| 재활 목적 | 상지 재활 + 인지 재활 |
| 진행 방식 | 무한 생존 + 점수 |

---

## Phase 1 — Unity 프로젝트 세팅

- [ ] 프로젝트 생성 (URP)
- [ ] XR Interaction Toolkit 설치 (Package Manager)
- [ ] Samples → XR Device Simulator import
- [ ] XR Origin + 카메라 세팅

---

## Phase 2 — 게임 씬 구성

- [ ] 공연장 기본 환경 배치
- [ ] 악기 오브젝트 배치 (플레이어 정면 반원형)
- [ ] 조명 기본 세팅
- [ ] 파티클 이펙트 기본 세팅

---

## Phase 3 — 핵심 게임 로직

- [ ] 시퀀스 생성 시스템 (라운드마다 +1개)
- [ ] 악기 점등 → 터치 판정 (Direct Interactor)
- [ ] 정답 / 오답 판정
- [ ] HP 시스템 (오답 3회 → 종료)
- [ ] 점수 / 콤보 시스템

---

## Phase 4 — 피드백 시스템

- [ ] 햅틱 피드백 (정답/오답 진동 패턴 구분)
- [ ] 3D 오디오 (악기 위치에서 소리 발생)
- [ ] 파티클 이펙트 (터치 성공 시 발광)
- [ ] 관객 반응 시스템 (콤보 → 함성/조명 변화)

---

## Phase 5 — UI

- [ ] 시작 화면 (타이틀, 시작 버튼, 난이도 선택, 조작 안내)
- [ ] 인게임 HUD (HP 하트, 점수, 콤보, 라운드)
- [ ] 결과 화면
  - 최종 점수
  - 최장 콤보
  - 정확도 (정답수 / 전체 터치수 × 100)
  - 평균 반응속도 (점등 ~ 터치 시간, ms)
  - 팔 뻗기 범위 (컨트롤러 position 최대값)

---

## Phase 6 — 기믹

- [ ] 페이크 악기 (점등 안 된 악기가 깜빡여 혼란 유발)
- [ ] 역재생 라운드 (시퀀스를 반대 순서로 터치)

---

## Phase 7 — 마무리

- [ ] 전체 테스트 및 버그 수정
- [ ] 최적화

---

## 평가 항목 체크리스트

| 번호 | 평가 항목 | 구현 내용 | 완료 |
|------|----------|----------|------|
| 1 | 오류 없이 실행 | 전체 테스트 | [ ] |
| 2 | 시작과 종료 | 시작 버튼 / HP 0 → 종료 | [ ] |
| 3 | 막힘 방지 장치 | 라운드 시작 전 카운트다운 | [ ] |
| 4 | 이동/진행 방식 | 제자리 고정, 무한 생존 | [ ] |
| 5 | 상호작용 1 | 악기 터치 판정 | [ ] |
| 6 | 상호작용 2 | 관객 반응 시스템 | [ ] |
| 7 | 상호작용 3 | 악기 발광 시퀀스 | [ ] |
| 8 | 피드백 1 | 햅틱 + 정답/오답 색상 | [ ] |
| 9 | 피드백 2 | 관객 함성 볼륨 변화 | [ ] |
| 10 | 과업 1 | 페이크 악기 | [ ] |
| 11 | 과업 2 | 시퀀스 역재생 라운드 | [ ] |
| 12 | 안내/상태 UI 1 | 인게임 HUD | [ ] |
| 13 | 안내/상태 UI 2 | 결과 화면 (반응속도 등) | [ ] |

---

## 핵심 기술 메모

```csharp
// 반응속도 측정
float lightOnTime;
float reactionTime = Time.time - lightOnTime; // ms 변환: * 1000

// 팔 뻗기 범위
float maxReach = Vector3.Distance(headPosition, controllerPosition);

// 결과값 씬 간 전달
PlayerPrefs.SetFloat("ReactionTime", reactionTime);
PlayerPrefs.SetFloat("MaxReach", maxReach);
PlayerPrefs.SetInt("FinalScore", score);
```

---

## 개발 환경

| 항목 | 내용 |
|------|------|
| Unity 버전 | 2022.3 LTS 권장 |
| 렌더 파이프라인 | URP |
| XR 패키지 | XR Interaction Toolkit 2.5↑ |
| 테스트 방식 | XR Device Simulator (PC) |
| 실제 기기 | 학교 VR 기기 (추후 연동) |
