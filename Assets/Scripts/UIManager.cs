using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 타이틀 / HUD / 카운트다운 / 결과 화면 + 관객 반응 연출
public class UIManager : MonoBehaviour
{
    [Header("참조")]
    public GameManager game;

    [Header("패널")]
    public GameObject titlePanel;
    public GameObject hudPanel;
    public GameObject resultPanel;

    [Header("HUD 텍스트")]
    public Text hpText;
    public Text scoreText;
    public Text comboText;
    public Text roundText;
    public Text countdownText;

    [Header("결과 텍스트")]
    public Text resultText;

    [Header("기믹 안내 배너")]
    public Text bannerText;               // 역재생/가짜 악기 등 안내

    [Header("튜토리얼")]
    public TutorialManager tutorial;
    public GameObject tutorialPanel;
    public Text tutorialText;

    [Header("관객 반응")]
    public AudioSource cheerAudio;        // 함성 (콤보에 따라 볼륨 변화)
    public Light stageLight;              // 콤보 시 밝아지는 무대 조명
    public ParticleSystem cheerParticle;  // 콤보 파티클

    private Coroutine _bannerCo;

    void OnEnable()
    {
        if (game != null) game.OnStatsChanged += RefreshHUD;
    }

    void OnDisable()
    {
        if (game != null) game.OnStatsChanged -= RefreshHUD;
    }

    public void ShowTitle()
    {
        SetActive(titlePanel, true);
        SetActive(hudPanel, false);
        SetActive(resultPanel, false);
        SetActive(tutorialPanel, false);
        SetActive(countdownText?.gameObject, false);
    }

    // 튜토리얼 버튼이 호출
    public void OnTutorialButton()
    {
        if (tutorial == null) return;
        SetActive(titlePanel, false);
        SetActive(hudPanel, false);
        SetActive(resultPanel, false);
        SetActive(tutorialPanel, true);
        tutorial.StartTutorial();
    }

    // TutorialManager가 단계별 안내 텍스트 갱신에 사용
    public void ShowTutorial(string msg)
    {
        SetActive(tutorialPanel, true);
        if (tutorialText != null) tutorialText.text = msg;
    }

    // 튜토리얼 종료 → 타이틀 복귀
    public void EndTutorial()
    {
        SetActive(tutorialPanel, false);
        ShowTitle();
    }

    // 난이도 버튼이 호출 (0=쉬움, 1=보통, 2=어려움)
    public void StartWithDifficulty(int diffIndex)
    {
        SetActive(titlePanel, false);
        SetActive(hudPanel, true);
        SetActive(resultPanel, false);
        game?.StartGame((GameManager.Difficulty)diffIndex);
    }

    // 하위호환: 기존 단일 시작 버튼 (보통 난이도)
    public void OnStartButton() => StartWithDifficulty(1);

    // 결과 화면 → 타이틀로 돌아가 난이도 재선택
    public void OnRestartButton()
    {
        SetActive(resultPanel, false);
        game?.ShowTitle();
    }

    public void ShowCountdown(int n)
    {
        SetActive(countdownText?.gameObject, true);
        if (countdownText != null) countdownText.text = n.ToString();
    }

    public void HideCountdown()
    {
        SetActive(countdownText?.gameObject, false);
    }

    // 기믹 안내 배너를 잠시 표시
    public void ShowBanner(string msg, float duration)
    {
        if (bannerText == null) return;
        if (_bannerCo != null) StopCoroutine(_bannerCo);
        _bannerCo = StartCoroutine(BannerRoutine(msg, duration));
    }

    IEnumerator BannerRoutine(string msg, float duration)
    {
        bannerText.gameObject.SetActive(true);
        bannerText.text = msg;
        yield return new WaitForSeconds(duration);
        bannerText.gameObject.SetActive(false);
        _bannerCo = null;
    }

    void RefreshHUD()
    {
        if (game == null) return;
        if (hpText != null)    hpText.text    = "HP: " + new string('@', Mathf.Max(0, game.HP)) + "   (" + game.HP + ")";
        if (scoreText != null) scoreText.text = "SCORE: " + game.Score;
        if (comboText != null) comboText.text = game.Combo > 1 ? "COMBO x" + game.Combo : "";
        if (roundText != null) roundText.text = "ROUND " + game.Round + "  ·  " + DiffName(game.CurrentDifficulty);
    }

    // 관객 반응 시스템 + 피드백 2 (함성 볼륨/조명 변화)
    public void OnAudienceReact(int combo)
    {
        float t = Mathf.Clamp01(combo / 10f);
        if (cheerAudio != null)
        {
            cheerAudio.volume = Mathf.Lerp(0.2f, 1f, t);
            if (!cheerAudio.isPlaying) cheerAudio.Play();
        }
        if (stageLight != null)
            stageLight.intensity = Mathf.Lerp(2f, 6f, t);
        if (cheerParticle != null && combo >= 3)
            cheerParticle.Play();
    }

    public void ShowResult()
    {
        SetActive(hudPanel, false);
        SetActive(resultPanel, true);
        if (resultText != null)
        {
            resultText.text =
                "GAME OVER\n\n" +
                $"최종 점수: {GameData.FinalScore}\n" +
                $"최장 콤보: {GameData.MaxCombo}\n" +
                $"정확도: {GameData.Accuracy:F1}%\n" +
                $"평균 반응속도: {GameData.AvgReactionMs:F0} ms\n" +
                $"팔 뻗기 범위: {GameData.MaxReach:F2} m";
        }
    }

    static string DiffName(GameManager.Difficulty d)
    {
        switch (d)
        {
            case GameManager.Difficulty.Easy:   return "쉬움";
            case GameManager.Difficulty.Hard:   return "어려움";
            default:                            return "보통";
        }
    }

    static void SetActive(GameObject go, bool on)
    {
        if (go != null) go.SetActive(on);
    }
}
