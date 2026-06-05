using UnityEngine;

// 비트세이버식 리듬 모드의 날아오는 노트 하나.
// 악기 뒤쪽 먼 곳(spawnPos)에서 생성되어 해당 악기 위치(targetPos = 타격선)로 일정 시간에 걸쳐 다가온다.
// 타격선에 도달할 때까지 플레이어가 해당 악기를 치면 판정 성공(매니저가 처리), 못 치면 통과 = 놓침.
public class RhythmNote : MonoBehaviour
{
    public int lane;                    // 어느 악기 레인인지 (instrumentIndex)
    public bool Hit { get; private set; }

    private RhythmStageManager _mgr;
    private Vector3 _spawnPos;
    private Vector3 _targetPos;
    private float _travelTime;
    private float _t;                   // 0=생성지점, 1=타격선
    private Renderer _renderer;

    public void Init(RhythmStageManager mgr, int lane, Vector3 spawnPos, Vector3 targetPos,
                     float travelTime, Color color, Vector3 scale, Quaternion rotation)
    {
        _mgr = mgr;
        this.lane = lane;
        _spawnPos = spawnPos;
        _targetPos = targetPos;
        _travelTime = Mathf.Max(0.01f, travelTime);

        transform.position = spawnPos;
        transform.rotation = rotation;
        transform.localScale = scale;

        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _renderer.material.color = color;
            _renderer.material.EnableKeyword("_EMISSION");
            _renderer.material.SetColor("_EmissionColor", color * 2.5f);
        }
    }

    void Update()
    {
        _t += Time.deltaTime / _travelTime;
        transform.position = Vector3.Lerp(_spawnPos, _targetPos, _t);

        // 타격선을 지나치면 놓침 처리
        if (_t >= 1f && !Hit)
        {
            _mgr?.OnNoteMissed(this);
            Destroy(gameObject);
        }
    }

    // 현재 노트가 타격선에서 얼마나 떨어져 있는지 (작을수록 정타에 가까움)
    public float DistanceToHitLine => Vector3.Distance(transform.position, _targetPos);

    public void MarkHit()
    {
        Hit = true;
        Destroy(gameObject);
    }
}
