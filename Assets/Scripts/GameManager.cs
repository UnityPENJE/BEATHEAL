using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// 게임 전체 상태 관리 (시작 / 진행 / 종료)
public class GameManager : MonoBehaviour
{
    public enum State { Title, Countdown, Playing, GameOver }
    public enum Difficulty { Easy, Normal, Hard, UltraHard }

    [Header("참조")]
    public SequenceManager sequenceManager;
    public UIManager uiManager;
    public RhythmStageManager rhythmManager;   // 번외 리듬 스테이지 (선택)

    [Header("설정")]
    public int maxHP = 3;
    public int rhythmMaxHP = 100;          // 리듬 모드 전용 최대 체력
    public float countdownSeconds = 3f;
    public int bonusBallScore = 200;       // 보너스 공 1개 쳐낼 때 점수

    [Header("스테이지 목표 — 라운드 채우면 클리어 (Easy/Normal/Hard/UltraHard)")]
    public int[] roundGoals = { 5, 8, 10, 12 };

    public bool RhythmMode { get; private set; }  // true면 보너스 공 스폰 안 함
    // 보너스 공은 '초 하드' 모드에서만 등장
    public bool BonusBallsEnabled => !RhythmMode && CurrentDifficulty == Difficulty.UltraHard;

    public State CurrentState { get; private set; } = State.Title;
    public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Normal;
    public int HP { get; private set; }
    public int CurrentMaxHP { get; private set; } = 3;  // 현재 모드의 최대 HP (사이먼=maxHP, 리듬=rhythmMaxHP)
    public int Score { get; private set; }
    public int Combo { get; private set; }
    public int Round { get; private set; }
    public bool StageCleared { get; private set; }      // 목표 라운드 달성 여부

    // 현재 난이도의 목표 라운드 수
    public int RoundGoal
    {
        get
        {
            int i = (int)CurrentDifficulty;
            return (roundGoals != null && i < roundGoals.Length) ? roundGoals[i] : 8;
        }
    }

    public event Action OnStatsChanged;

    void Start()
    {
        ShowTitle();
    }

    public void ShowTitle()
    {
        CurrentState = State.Title;
        uiManager?.ShowTitle();
    }

    // 난이도 버튼 → 게임 시작
    public void StartGame(Difficulty diff)
    {
        CurrentDifficulty = diff;
        RhythmMode = false;
        StageCleared = false;
        GameData.Reset();
        CurrentMaxHP = maxHP;
        HP = maxHP;
        Score = 0;
        Combo = 0;
        Round = 0;
        OnStatsChanged?.Invoke();
        uiManager?.ShowGoal($"목표: 라운드 {RoundGoal} 클리어\n진행: 0 / {RoundGoal}");

        sequenceManager.Init(this);
        sequenceManager.ApplyDifficulty(diff);
        StartCoroutine(CountdownThenPlay());
    }

    // === 번외 스테이지: 비트세이버식 리듬 모드 ===
    public void StartBonusStage()
    {
        if (rhythmManager == null)
        {
            Debug.LogWarning("[BeatHeal] rhythmManager가 연결되지 않았습니다. BeatHeal > Setup Scene > '보너스 스테이지(리듬) 세팅'을 실행하세요.");
            return;
        }
        CurrentDifficulty = Difficulty.Normal;
        RhythmMode = true;
        StageCleared = false;
        GameData.Reset();
        CurrentMaxHP = rhythmMaxHP;
        HP = rhythmMaxHP;
        Score = 0;
        Combo = 0;
        Round = 0;
        OnStatsChanged?.Invoke();

        rhythmManager.Init(this);
        StartCoroutine(CountdownThenRhythm());
    }

    // 리듬 모드: 제대로 치면 체력 회복
    public void RhythmHeal(int amount)
    {
        HP = Mathf.Min(CurrentMaxHP, HP + amount);
        OnStatsChanged?.Invoke();
    }

    // 리듬 모드: 틀리거나 놓치면 체력 감소 (+콤보 초기화), 0이면 종료
    public void RhythmDamage(int amount)
    {
        Combo = 0;
        HP = Mathf.Max(0, HP - amount);
        OnStatsChanged?.Invoke();
        if (HP <= 0) GameOver();
    }

    System.Collections.IEnumerator CountdownThenRhythm()
    {
        CurrentState = State.Countdown;
        float t = countdownSeconds;
        while (t > 0f)
        {
            uiManager?.ShowCountdown(Mathf.CeilToInt(t));
            t -= Time.deltaTime;
            yield return null;
        }
        uiManager?.HideCountdown();
        CurrentState = State.Playing;
        Round = 1;
        OnStatsChanged?.Invoke();
        rhythmManager.StartStage();
    }

    // RhythmStageManager가 스테이지 종료 시 결과 화면을 띄우기 위해 호출
    public void ForceGameOver() => GameOver();

    // 막힘 방지 장치: 라운드 시작 전 카운트다운
    System.Collections.IEnumerator CountdownThenPlay()
    {
        CurrentState = State.Countdown;
        float t = countdownSeconds;
        while (t > 0f)
        {
            uiManager?.ShowCountdown(Mathf.CeilToInt(t));
            t -= Time.deltaTime;
            yield return null;
        }
        uiManager?.HideCountdown();
        CurrentState = State.Playing;
        Round++;
        OnStatsChanged?.Invoke();
        uiManager?.ShowGoal($"목표: 라운드 {RoundGoal} 클리어\n진행: {Round - 1} / {RoundGoal}");
        sequenceManager.StartRound(Round);
    }

    // 정답 터치
    public void OnCorrect(float reactionTime)
    {
        Combo++;
        if (Combo > GameData.MaxCombo) GameData.MaxCombo = Combo;
        int gained = 100 * Mathf.Max(1, Combo);
        Score += gained;
        GameData.CorrectTouches++;
        OnStatsChanged?.Invoke();
        uiManager?.OnAudienceReact(Combo); // 무대 조명 연출 (보조)
    }

    // 보너스 공을 손/드럼스틱으로 쳐냈을 때 (상호작용 — 쳐내기)
    public void OnBonusBallHit(Vector3 pos)
    {
        Score += bonusBallScore;
        OnStatsChanged?.Invoke();

        // 직관적 피드백: 파티클 + 점수 팝업 + 효과음
        FeedbackFX.Burst(pos, new Color(1f, 0.85f, 0.2f));
        FeedbackFX.Popup(pos, "+" + bonusBallScore, new Color(1f, 0.9f, 0.3f));
        SfxPlayer.Instance?.PlayBall();
    }

    // 오답 터치
    public void OnWrong()
    {
        Combo = 0;
        HP--;
        OnStatsChanged?.Invoke();
        if (HP <= 0)
            GameOver();
    }

    // 한 라운드(시퀀스) 완주 → 목표 라운드 달성 시 클리어, 아니면 다음 라운드
    public void OnRoundComplete()
    {
        uiManager?.ShowGoal($"목표: 라운드 {RoundGoal} 클리어\n진행: {Round} / {RoundGoal}");

        if (Round >= RoundGoal)
        {
            StageClear();
            return;
        }
        StartCoroutine(CountdownThenPlay());
    }

    // 목표 라운드를 모두 채워 스테이지 클리어
    void StageClear()
    {
        StageCleared = true;
        uiManager?.ShowBanner("🏆 STAGE CLEAR!", 4f);
        GameOver();
    }

    public void GameOver()
    {
        CurrentState = State.GameOver;
        sequenceManager?.StopAll();
        if (rhythmManager != null) rhythmManager.StopAll();

        GameData.FinalScore = Score;
        uiManager?.ShowResult();
    }

    // 결과 화면 → 같은 난이도로 즉시 재시작 (막힘 방지)
    public void Restart()
    {
        StopAllCoroutines();
        StartGame(CurrentDifficulty);
    }
}
