using UnityEngine;

// '초 하드' 모드 진행(Playing) 중 일정 간격으로 보너스 공을 플레이어에게 날린다.
// 좌우로 흩어서 스폰해 팔 뻗기(상지 재활)를 유도한다. (다른 난이도/리듬에서는 비활성)
public class BonusBallSpawner : MonoBehaviour
{
    [Header("참조")]
    public GameManager game;

    [Header("설정")]
    public float interval = 3.5f;     // 스폰 간격(초)
    public float travelTime = 1.6f;   // 도달까지 걸리는 시간
    public float reach = 0.5f;        // 플레이어 앞 도달 거리(m)
    public float spread = 0.5f;       // 좌우 분산(m) — 팔 뻗기 유도
    public float ballScale = 0.18f;

    float _timer;
    Camera _cam;

    void Update()
    {
        // 보너스 공은 '초 하드' 모드에서만 등장
        if (game == null || game.CurrentState != GameManager.State.Playing || !game.BonusBallsEnabled)
            return;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _timer = interval;
            Spawn();
        }
    }

    void Spawn()
    {
        var camT = _cam.transform;
        Vector3 right = camT.right;
        float side = Random.Range(-spread, spread);

        // 도달 지점: 플레이어 가슴 앞, 좌우로 살짝
        Vector3 target = camT.position + camT.forward * reach + right * side + Vector3.down * 0.2f;
        // 시작 지점: 정면 멀리 위쪽
        Vector3 start = camT.position + camT.forward * 3f + right * side * 1.5f + Vector3.up * 0.4f;

        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "BonusBall";
        ball.transform.position = start;
        ball.transform.localScale = Vector3.one * ballScale;

        var col = ball.GetComponent<SphereCollider>();
        col.isTrigger = true;

        var rb = ball.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Color c = new Color(1f, 0.7f, 0.1f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = c;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", c * 2f);
        ball.GetComponent<Renderer>().material = mat;

        float speed = Vector3.Distance(start, target) / Mathf.Max(0.1f, travelTime);
        var bb = ball.AddComponent<BonusBall>();
        bb.Init(game, target, speed);

        Object.Destroy(ball, travelTime + 3f); // 안전망
    }
}
