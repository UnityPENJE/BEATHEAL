using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 번외(보너스) 스테이지 — 비트세이버식 리듬 모드.
// 기존 5개 악기를 "레인"으로 재사용한다. 노트가 악기 뒤쪽 멀리서 천천히 다가오고,
// 노트가 악기(타격선)에 도달하는 순간 그 악기를 치면 타이밍 판정.
// 입력은 기존 InstrumentPanel.OnTouched(드럼스틱/맨손 공용)를 그대로 사용한다.
public class RhythmStageManager : MonoBehaviour
{
    [Header("악기 레인 (Instrument_0 ~ N)")]
    public InstrumentPanel[] instruments;

    [Header("참조")]
    public GameManager game;

    [Header("사용할 레인 (instrumentIndex). 2·4번 = 인덱스 1,3)")]
    public int[] laneWhitelist = { 1, 3 };  // 5개 중 2번·4번만 사용, 나머지는 숨김

    [Header("노트 흐름 설정 (난이도 상향)")]
    public float approachDistance = 6f;   // 노트가 생성되는 거리 (악기 뒤로 m)
    public float travelTime = 1.1f;       // 다가오는 데 걸리는 시간 (작을수록 빠름/어려움)
    public float spawnInterval = 0.45f;   // 노트 간 간격 (초) — 작을수록 노트 많음
    public int noteCount = 50;            // 스테이지 총 노트 수
    public float noteThickness = 0.12f;   // 노트 두께 (악기 디스크보다 살짝 두껍게 = 잘 보이도록)

    [Header("타격 판정 (타격선까지 거리, m)")]
    public float hitWindow = 0.28f;       // 이 안에서 치면 성공
    public float perfectWindow = 0.09f;   // 이 안이면 정타(보너스)

    [Header("체력 변화량 (리듬 모드, 최대 100)")]
    public int goodHeal = 4;              // GOOD 성공 시 회복
    public int perfectHeal = 7;           // PERFECT 성공 시 회복
    public int wrongDamage = 8;           // 빗맞춤(타이밍 X) 감소
    public int missDamage = 10;           // 놓침(통과) 감소

    private readonly List<RhythmNote> _active = new List<RhythmNote>();
    private Transform _player;
    private bool _running;
    private int _spawned;
    private int _resolved;                 // 처리된(맞추거나 놓친) 노트 수

    // 보너스 스테이지 진입 준비 — 악기 활성화 + 입력 구독
    public void Init(GameManager g)
    {
        game = g;
        EnsureInstruments();

        _player = Camera.main != null ? Camera.main.transform : null;

        // 화이트리스트(2·4번)만 활성화, 나머지 레인은 숨김 → 시야에 집중
        for (int i = 0; i < instruments.Length; i++)
        {
            var inst = instruments[i];
            if (inst == null) continue;

            bool use = IsAllowedLane(inst.instrumentIndex);
            inst.gameObject.SetActive(use);
            if (!use) continue;

            inst.LightOff();
            inst.StopFakeFlicker();
            inst.OnTouched -= OnInstrumentTouched;
            inst.OnTouched += OnInstrumentTouched;
        }
    }

    bool IsAllowedLane(int lane)
    {
        if (laneWhitelist == null || laneWhitelist.Length == 0) return true;
        foreach (int l in laneWhitelist) if (l == lane) return true;
        return false;
    }

    // 사용 가능한(화이트리스트 + 존재하는) 악기 인덱스 목록
    List<int> AllowedInstrumentIndices()
    {
        var result = new List<int>();
        for (int i = 0; i < instruments.Length; i++)
            if (instruments[i] != null && instruments[i].gameObject.activeSelf
                && IsAllowedLane(instruments[i].instrumentIndex))
                result.Add(i);
        return result;
    }

    void EnsureInstruments()
    {
        bool needFind = instruments == null || instruments.Length == 0;
        if (!needFind)
            foreach (var i in instruments)
                if (i == null) { needFind = true; break; }

        if (needFind)
            instruments = Object.FindObjectsOfType<InstrumentPanel>();

        System.Array.Sort(instruments, (a, b) =>
            (a == null ? int.MaxValue : a.instrumentIndex)
            .CompareTo(b == null ? int.MaxValue : b.instrumentIndex));
    }

    public void StartStage()
    {
        StopAllCoroutines();
        _active.Clear();
        _spawned = 0;
        _resolved = 0;
        _running = true;
        game?.uiManager?.ShowBanner("🎵 리듬 모드!\n노트가 도착할 때 그 악기를 치세요", 2f);
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(1.5f); // 배너 읽을 시간

        while (_running && _spawned < noteCount)
        {
            var lanes = AllowedInstrumentIndices();
            if (lanes.Count == 0) break; // 사용할 레인이 없음
            int lane = lanes[Random.Range(0, lanes.Count)];
            SpawnNote(lane);
            _spawned++;
            yield return new WaitForSeconds(spawnInterval);
        }

        // 마지막 노트가 처리될 때까지 대기 후 종료
        while (_running && _resolved < _spawned)
            yield return null;

        if (_running) EndStage();
    }

    void SpawnNote(int lane)
    {
        if (lane < 0 || lane >= instruments.Length || instruments[lane] == null) return;

        var inst = instruments[lane];
        Vector3 target = inst.transform.position; // 타격선 = 악기 위치

        // 플레이어 → 악기 방향(수평)으로 더 멀리 = 악기 뒤쪽에서 다가오게
        Vector3 playerPos = _player != null ? _player.position : (target + Vector3.back);
        Vector3 away = target - playerPos;
        away.y = 0f;
        if (away.sqrMagnitude < 0.001f) away = Vector3.forward;
        away.Normalize();

        Vector3 spawn = target + away * approachDistance;

        // 치는 악기와 같은 실린더 모양으로 (디스크형). 두께만 살짝 키워 잘 보이게 함
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = $"RhythmNote_{_spawned}_lane{lane}";

        // 물리 충돌은 쓰지 않음 (판정은 악기 터치로) — 콜라이더 제거
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        go.GetComponent<Renderer>().material = mat;

        // 악기의 크기/회전을 그대로 따와 같은 모양으로 보이게 (두께만 noteThickness로)
        Vector3 scale = inst.transform.localScale;
        scale.y = Mathf.Max(scale.y, noteThickness * 0.5f); // 실린더 y=높이의 절반
        Quaternion rot = inst.transform.rotation;

        Color color = inst.idleColor;
        var note = go.AddComponent<RhythmNote>();
        note.Init(this, lane, spawn, target, travelTime, color, scale, rot);
        _active.Add(note);

        // 목표 악기를 살짝 켜서 어느 레인인지 미리 안내
        inst.ShowReady();
    }

    // 악기를 쳤을 때 — 해당 레인에서 타격선에 가장 가까운 노트를 찾아 판정
    void OnInstrumentTouched(InstrumentPanel panel)
    {
        if (!_running || panel == null) return;

        RhythmNote best = null;
        float bestDist = float.MaxValue;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var n = _active[i];
            if (n == null) { _active.RemoveAt(i); continue; }
            if (n.Hit || n.lane != panel.instrumentIndex) continue;
            float d = n.DistanceToHitLine;
            if (d < bestDist) { bestDist = d; best = n; }
        }

        if (best != null && bestDist <= hitWindow)
        {
            // 성공 — 점수/콤보 + 체력 회복 (정타면 더)
            bool perfect = bestDist <= perfectWindow;
            panel.ShowCorrect();
            game?.OnCorrect(0f);                       // 점수 + 콤보 + 관객 반응
            game?.RhythmHeal(perfect ? perfectHeal : goodHeal);  // 체력 회복
            game?.uiManager?.ShowBanner(perfect ? "PERFECT!" : "GOOD", 0.5f);

            _active.Remove(best);
            best.MarkHit();
            _resolved++;
            StartCoroutine(LightOffAfter(panel, 0.15f));
        }
        else
        {
            // 빈 타격 / 타이밍 안 맞음 — 체력 감소 (RhythmDamage가 0이면 종료 처리)
            panel.ShowWrong();
            game?.RhythmDamage(wrongDamage);
            StartCoroutine(LightOffAfter(panel, 0.2f));
        }
    }

    // 노트가 타격선을 지나쳐 놓친 경우 — 체력 감소(가장 큼)
    public void OnNoteMissed(RhythmNote note)
    {
        if (!_running) return;
        _active.Remove(note);
        _resolved++;
        if (instruments != null && note.lane >= 0 && note.lane < instruments.Length
            && instruments[note.lane] != null)
            instruments[note.lane].LightOff();
        game?.RhythmDamage(missDamage);
    }

    IEnumerator LightOffAfter(InstrumentPanel panel, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (panel != null) panel.LightOff();
    }

    void EndStage()
    {
        if (!_running) return;
        _running = false;
        StopAllCoroutines();

        foreach (var n in _active)
            if (n != null) Destroy(n.gameObject);
        _active.Clear();

        foreach (var inst in instruments)
            if (inst != null) inst.LightOff();

        // 결과 화면 표시 (게임오버든 완주든 결과로 마무리)
        game?.ForceGameOver();
    }

    public void StopAll()
    {
        _running = false;
        StopAllCoroutines();
        foreach (var n in _active)
            if (n != null) Destroy(n.gameObject);
        _active.Clear();
        if (instruments != null)
            foreach (var inst in instruments)
                if (inst != null) { inst.OnTouched -= OnInstrumentTouched; inst.LightOff(); }
    }
}
