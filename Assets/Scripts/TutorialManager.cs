using System.Collections;
using UnityEngine;

// 단계별 튜토리얼: 기본 터치 → 시퀀스 → 가짜 악기 → 역재생
public class TutorialManager : MonoBehaviour
{
    [Header("참조")]
    public InstrumentPanel[] instruments;
    public UIManager uiManager;

    private int _lastHit = -1;
    private bool _hitReceived;
    private bool _running;

    public bool IsRunning => _running;

    public void StartTutorial()
    {
        if (_running) return;

        // 악기 확보 + 정렬 (instruments[i].instrumentIndex == i)
        if (instruments == null || instruments.Length == 0)
            instruments = Object.FindObjectsOfType<InstrumentPanel>();
        System.Array.Sort(instruments, (a, b) =>
            (a == null ? int.MaxValue : a.instrumentIndex).CompareTo(b == null ? int.MaxValue : b.instrumentIndex));

        foreach (var i in instruments)
        {
            if (i == null) continue;
            i.gameObject.SetActive(true);
            i.OnTouched -= OnHit;
            i.OnTouched += OnHit;
            i.LightOff();
            i.StopFakeFlicker();
        }

        _running = true;
        StartCoroutine(Run());
    }

    public void StopTutorial()
    {
        StopAllCoroutines();
        _running = false;
        foreach (var i in instruments)
        {
            if (i == null) continue;
            i.OnTouched -= OnHit;
            i.LightOff();
            i.StopFakeFlicker();
        }
    }

    void OnHit(InstrumentPanel p)
    {
        _lastHit = p.instrumentIndex;
        _hitReceived = true;
    }

    // 지정한 악기를 칠 때까지 대기. 틀리면 안내하고 다시 기다림.
    IEnumerator WaitForHit(int expected)
    {
        _hitReceived = false;
        while (true)
        {
            if (_hitReceived)
            {
                _hitReceived = false;
                if (_lastHit == expected)
                    yield break;

                // 틀린 악기 → 잠깐 빨강 표시 후 다시 기대 악기 점등
                if (_lastHit >= 0 && _lastHit < instruments.Length && instruments[_lastHit] != null)
                {
                    instruments[_lastHit].ShowWrong();
                    yield return new WaitForSeconds(0.3f);
                    instruments[_lastHit].LightOff();
                }
                if (instruments[expected] != null) instruments[expected].LightOn();
            }
            yield return null;
        }
    }

    IEnumerator Flash(int idx)
    {
        if (instruments[idx] == null) yield break;
        instruments[idx].ShowDemo();   // 보여주기 = 흰색
        yield return new WaitForSeconds(0.55f);
        instruments[idx].LightOff();
        yield return new WaitForSeconds(0.25f);
    }

    void LightOffAll()
    {
        foreach (var i in instruments)
            if (i != null) { i.LightOff(); i.StopFakeFlicker(); }
    }

    IEnumerator Run()
    {
        // 1단계 — 기본 터치
        uiManager?.ShowTutorial("1/4. 빛나는 악기를 드럼스틱으로 쳐보세요!");
        yield return new WaitForSeconds(1.5f);
        instruments[0].LightOn();
        yield return WaitForHit(0);
        instruments[0].ShowCorrect();
        yield return new WaitForSeconds(0.8f);
        LightOffAll();

        // 2단계 — 시퀀스
        uiManager?.ShowTutorial("2/4. 이번엔 순서대로! 빛난 순서를 기억해 그대로 치세요.");
        yield return new WaitForSeconds(2f);
        yield return Flash(0);
        yield return Flash(1);
        instruments[0].LightOn(); yield return WaitForHit(0); instruments[0].ShowCorrect();
        instruments[1].LightOn(); yield return WaitForHit(1); instruments[1].ShowCorrect();
        yield return new WaitForSeconds(0.8f);
        LightOffAll();

        // 3단계 — 가짜 악기
        uiManager?.ShowTutorial("3/4. ⚠ 가짜 악기! 주황색으로 깜빡이는 악기는 함정이에요. 켜진 악기만 치세요.");
        yield return new WaitForSeconds(2.5f);
        if (instruments.Length > 2) instruments[2].StartFakeFlicker(); // 가짜
        instruments[0].LightOn();                                      // 진짜
        yield return WaitForHit(0);
        if (instruments.Length > 2) instruments[2].StopFakeFlicker();
        instruments[0].ShowCorrect();
        yield return new WaitForSeconds(0.8f);
        LightOffAll();

        // 4단계 — 역재생
        uiManager?.ShowTutorial("4/4. 🔄 역재생! 빛난 순서의 '반대'로 치세요. (예: 0→1 로 빛나면 1→0)");
        yield return new WaitForSeconds(3f);
        yield return Flash(0);
        yield return Flash(1);
        instruments[1].LightOn(); yield return WaitForHit(1); instruments[1].ShowCorrect(); // 반대로!
        instruments[0].LightOn(); yield return WaitForHit(0); instruments[0].ShowCorrect();
        yield return new WaitForSeconds(0.8f);
        LightOffAll();

        // 완료
        uiManager?.ShowTutorial("튜토리얼 완료! 이제 난이도를 골라 게임을 시작하세요.");
        yield return new WaitForSeconds(2.5f);

        _running = false;
        foreach (var i in instruments)
            if (i != null) i.OnTouched -= OnHit;

        uiManager?.EndTutorial(); // 타이틀로 복귀
    }
}
