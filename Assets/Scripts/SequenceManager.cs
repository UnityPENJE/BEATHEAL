using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 시퀀스 생성 / 점등 / 터치 판정 / 페이크·역재생 기믹
public class SequenceManager : MonoBehaviour
{
    [Header("악기 목록 (Instrument_0 ~ N)")]
    public InstrumentPanel[] instruments;

    [Header("연출 설정")]
    public float lightOnDuration = 0.6f;   // 각 악기 점등 시간
    public float gapDuration = 0.3f;        // 점등 사이 간격
    public int reverseRoundInterval = 3;    // N라운드마다 역재생
    public int fakeRoundInterval = 2;       // N라운드마다 페이크 악기 등장

    private GameManager _game;
    private readonly List<int> _sequence = new List<int>();
    private int _expectIndex;       // 플레이어가 맞춰야 할 시퀀스 위치
    private bool _reverse;
    private bool _acceptingInput;
    private float _lastLightOnTime;
    private int _activePool;        // 난이도에 따라 실제 사용하는 악기 수
    private InstrumentPanel _currentFake;  // 이번 라운드 페이크 악기
    private bool _guideMode;        // true면 칠 노트를 점등해 안내(쉬움 전용), false면 순수 암기

    public void Init(GameManager game)
    {
        _game = game;
        EnsureInstruments();
        _activePool = instruments.Length;  // ApplyDifficulty에서 덮어씀
        foreach (var inst in instruments)
        {
            if (inst == null) continue;
            inst.gameObject.SetActive(true); // 이전 게임에서 숨겼을 수 있어 복구
            inst.OnTouched -= HandleTouch;
            inst.OnTouched += HandleTouch;
            inst.LightOff();
        }
        _sequence.Clear();
    }

    // 난이도 적용: 사용하는 악기 수(노트 풀)와 점등 속도 조절
    public void ApplyDifficulty(GameManager.Difficulty diff)
    {
        switch (diff)
        {
            case GameManager.Difficulty.Easy:
                _activePool = Mathf.Min(3, instruments.Length);
                lightOnDuration = 0.85f; gapDuration = 0.45f;
                _guideMode = true;   // 쉬움: 칠 노트를 점등해 안내
                break;
            case GameManager.Difficulty.Normal:
                _activePool = Mathf.Min(5, instruments.Length);
                lightOnDuration = 0.6f;  gapDuration = 0.3f;
                _guideMode = false;  // 보통: 순수 암기
                break;
            case GameManager.Difficulty.Hard:
                _activePool = instruments.Length;
                lightOnDuration = 0.4f;  gapDuration = 0.18f;
                _guideMode = false;  // 어려움: 순수 암기
                break;
            case GameManager.Difficulty.UltraHard:
                _activePool = instruments.Length;
                lightOnDuration = 0.32f; gapDuration = 0.14f;  // 가장 빠름
                _guideMode = false;  // 초 하드: 순수 암기 + 날아오는 공
                break;
        }

        // 사용하지 않는 악기는 숨김 (선택지 줄이기)
        for (int i = 0; i < instruments.Length; i++)
            if (instruments[i] != null)
                instruments[i].gameObject.SetActive(i < _activePool);
    }

    // 배열이 비었거나 깨진 참조(null)가 있으면 씬에서 자동 검색.
    // 또한 instruments[i].instrumentIndex == i 가 되도록 정렬해 판정 일관성 보장.
    void EnsureInstruments()
    {
        bool needFind = instruments == null || instruments.Length == 0;
        if (!needFind)
            foreach (var i in instruments)
                if (i == null) { needFind = true; break; }

        if (needFind)
        {
            instruments = Object.FindObjectsOfType<InstrumentPanel>();
            Debug.LogWarning($"[BeatHeal] instruments 배열이 비었거나 누락되어 자동 검색했습니다. 개수={instruments.Length}");
        }

        // instrumentIndex 기준 정렬 (배열 인덱스와 일치시켜 정답 판정 오류 방지)
        System.Array.Sort(instruments, (a, b) =>
            (a == null ? int.MaxValue : a.instrumentIndex)
            .CompareTo(b == null ? int.MaxValue : b.instrumentIndex));
    }

    public void StartRound(int round)
    {
        StopAllCoroutines();
        _reverse = (round % reverseRoundInterval == 0);
        _sequence.Add(Random.Range(0, Mathf.Max(1, _activePool))); // 라운드마다 +1 (난이도 풀 내에서)
        StartCoroutine(PlaySequence(round));
    }

    IEnumerator PlaySequence(int round)
    {
        _acceptingInput = false;
        foreach (var inst in instruments)
            if (inst != null) { inst.LightOff(); inst.StopFakeFlicker(); }
        _currentFake = null;

        // 기믹 안내 배너 (확실하게 구분되도록) — 읽을 시간을 준 뒤 보여주기 시작
        bool useFake = (round % fakeRoundInterval == 0);
        if (_reverse && useFake)
            _game.uiManager?.ShowBanner("🔄 역재생 + ⚠ 가짜 악기!\n반대 순서로, 깜빡이는 건 치지 마세요", 2f);
        else if (_reverse)
            _game.uiManager?.ShowBanner("🔄 역재생 라운드!\n보여준 순서의 반대로 치세요", 2f);
        else if (useFake)
            _game.uiManager?.ShowBanner("⚠ 가짜 악기 등장!\n깜빡이는 악기는 치지 마세요", 2f);

        if (_reverse || useFake)
            yield return new WaitForSeconds(2f); // 기믹 안내를 읽을 시간

        // 페이크 악기 깜빡임 (과업 1) — 입력 단계까지 계속 깜빡여 혼란 유발
        if (useFake)
        {
            _currentFake = PickFakeInstrument();
            if (_currentFake != null) _currentFake.StartFakeFlicker();
        }

        // 보여주기 안내
        _game.uiManager?.ShowBanner("👀 잘 보세요 (보여주기)", 1.2f);
        yield return new WaitForSeconds(0.9f);

        // 악기 발광 시퀀스 — 보여주기 단계는 흰색 (상호작용 3)
        foreach (int idx in _sequence)
        {
            var inst = instruments[idx];
            if (inst == null) continue;
            inst.ShowDemo();
            yield return new WaitForSeconds(lightOnDuration);
            inst.LightOff();
            yield return new WaitForSeconds(gapDuration);
        }

        // === 보여주기 끝 → 치는 단계로 전환 신호 ===
        yield return InputCue();

        // 입력 받기 시작 (페이크는 계속 깜빡이는 상태 유지)
        _expectIndex = 0;
        _acceptingInput = true;
        _lastLightOnTime = Time.time;

        // 가이드 모드(쉬움)면 현재 맞춰야 할 악기를 고유색으로 점등
        HighlightExpected();
    }

    // 보여주기 → 입력 전환을 명확히 알리는 신호 (초록 펄스 + 배너)
    IEnumerator InputCue()
    {
        _game.uiManager?.ShowBanner("🥁 당신 차례! 본 순서대로 치세요", 1.6f);

        for (int i = 0; i < _activePool && i < instruments.Length; i++)
            if (instruments[i] != null && instruments[i] != _currentFake)
                instruments[i].ShowReady();

        yield return new WaitForSeconds(0.45f);

        for (int i = 0; i < _activePool && i < instruments.Length; i++)
            if (instruments[i] != null && instruments[i] != _currentFake)
                instruments[i].LightOff();

        yield return new WaitForSeconds(0.15f);
    }

    InstrumentPanel PickFakeInstrument()
    {
        // 시퀀스에 없는 악기 하나를 페이크로 (난이도 풀 내에서)
        for (int attempt = 0; attempt < 10; attempt++)
        {
            int i = Random.Range(0, Mathf.Max(1, _activePool));
            if (!_sequence.Contains(i)) return instruments[i];
        }
        return null;
    }

    // 현재 맞춰야 할 악기를 살짝 점등해 안내 (가이드 모드/쉬움에서만)
    void HighlightExpected()
    {
        if (!_acceptingInput || !_guideMode) return;
        int seqPos = _reverse ? _sequence.Count - 1 - _expectIndex : _expectIndex;
        if (seqPos < 0 || seqPos >= _sequence.Count) return;
        var inst = instruments[_sequence[seqPos]];
        if (inst != null) inst.LightOn();
    }

    void HandleTouch(InstrumentPanel panel)
    {
        if (!_acceptingInput || _game == null) return;

        float reaction = Time.time - _lastLightOnTime;
        _lastLightOnTime = Time.time;

        int seqPos = _reverse ? _sequence.Count - 1 - _expectIndex : _expectIndex;
        int correctIdx = _sequence[seqPos];

        if (panel.instrumentIndex == correctIdx && !panel.isFake)
        {
            // 정답 — 반응속도 기록 (점등 → 터치까지가 아니라 입력 가능 시점부터)
            GameData.TotalReactionTime += reaction;
            GameData.ReactionCount++;

            panel.ShowCorrect();
            TriggerHaptic(0.3f, 0.1f);  // 피드백: 햅틱 (약)

            int before = _game.Score;
            _game.OnCorrect(reaction);
            int gained = _game.Score - before;

            // 직관적 피드백: 파티클 + 점수 팝업 + 효과음(노트 성공)
            FeedbackFX.Burst(panel.transform.position, panel.correctColor);
            FeedbackFX.Popup(panel.transform.position, "+" + gained, panel.correctColor);
            SfxPlayer.Instance?.PlaySuccess();

            _expectIndex++;

            if (_expectIndex >= _sequence.Count)
            {
                // 시퀀스 완주
                _acceptingInput = false;
                StartCoroutine(EndRoundAfter(0.5f));
            }
            else
            {
                StartCoroutine(RelightAfter(0.2f));
            }
        }
        else
        {
            // 오답
            panel.ShowWrong();
            TriggerHaptic(0.8f, 0.25f);  // 피드백: 햅틱 (강)
            SfxPlayer.Instance?.PlayFail();  // 노트 실패음
            _game.OnWrong();
            StartCoroutine(RelightAfter(0.4f));
        }
    }

    IEnumerator RelightAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (var inst in instruments) if (inst != null) inst.LightOff();
        // 페이크는 계속 깜빡이게 유지
        if (_currentFake != null) _currentFake.StartFakeFlicker();
        HighlightExpected();
    }

    IEnumerator EndRoundAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (var inst in instruments)
            if (inst != null) { inst.LightOff(); inst.StopFakeFlicker(); }
        _currentFake = null;
        _game.OnRoundComplete();
    }

    public void StopAll()
    {
        StopAllCoroutines();
        _acceptingInput = false;
        foreach (var inst in instruments)
        {
            if (inst == null) continue;
            inst.StopFakeFlicker();
            inst.LightOff();
        }
    }

    // 햅틱 피드백 (XR 컨트롤러 진동)
    void TriggerHaptic(float amplitude, float duration)
    {
#if ENABLE_VR || UNITY_XR
        var devices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
            UnityEngine.XR.InputDeviceCharacteristics.Controller, devices);
        foreach (var d in devices)
        {
            if (d.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                d.SendHapticImpulse(0u, amplitude, duration);
        }
#endif
    }
}
