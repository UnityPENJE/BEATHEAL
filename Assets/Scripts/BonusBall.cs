using UnityEngine;

// 플레이어에게 날아오는 보너스 공. 손(맨손) 또는 드럼스틱(컨트롤러)으로 쳐내면 추가 점수.
// 쳐내지 못하고 목표 지점(플레이어 앞)에 도달하면 페널티 없이 사라진다 (순수 +α 요소).
public class BonusBall : MonoBehaviour
{
    GameManager _game;
    Vector3 _target;
    float _speed;
    bool _consumed;

    public void Init(GameManager game, Vector3 target, float speed)
    {
        _game = game;
        _target = target;
        _speed = speed;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, _target, _speed * Time.deltaTime);
        transform.Rotate(Vector3.up * 180f * Time.deltaTime, Space.Self);

        if (Vector3.Distance(transform.position, _target) < 0.05f)
            Destroy(gameObject); // 놓침 — 페널티 없음
    }

    void OnTriggerEnter(Collider other)
    {
        if (_consumed) return;
        if (!other.CompareTag("Hand") && !other.CompareTag("XRController")) return;

        _consumed = true;
        _game?.OnBonusBallHit(transform.position);
        Destroy(gameObject);
    }
}
