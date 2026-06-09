using UnityEngine;

// 타격 시 시각 피드백을 코드만으로 생성한다 (프리팹 불필요). 효과음은 SfxPlayer가 담당.
//  - Burst : 타격 지점에서 파티클 폭발
//  - Popup : "+점수" 같은 텍스트가 떠오르며 사라짐
public static class FeedbackFX
{
    static Material _particleMat;

    public static void Burst(Vector3 pos, Color color, int count = 18, float size = 0.05f)
    {
        var go = new GameObject("HitBurst");
        go.transform.position = pos;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = 1.6f;
        main.startSize = size;
        main.startColor = color;
        main.gravityModifier = 0.4f;
        main.maxParticles = count;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission; em.enabled = false;       // 수동 Emit
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.03f;

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.material = ParticleMaterial();

        ps.Emit(count);
        Object.Destroy(go, 1.0f);
    }

    public static void Popup(Vector3 pos, string text, Color color)
    {
        var go = new GameObject("ScorePopup");
        go.transform.position = pos + Vector3.up * 0.12f;

        var tm = go.AddComponent<TextMesh>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            tm.font = font;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.material = font.material;
        }
        tm.text = text;
        tm.fontSize = 64;
        tm.characterSize = 0.012f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;

        go.AddComponent<PopupAnim>();
    }

    static Material ParticleMaterial()
    {
        if (_particleMat == null)
        {
            var sh = Shader.Find("Sprites/Default")
                  ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Standard");
            _particleMat = new Material(sh);
        }
        return _particleMat;
    }
}

// 점수 팝업 텍스트: 위로 떠오르며 카메라를 향하고 서서히 사라진 뒤 자기 자신을 제거.
class PopupAnim : MonoBehaviour
{
    float _life = 0.8f;
    float _t;
    TextMesh _tm;
    Color _c0;

    void Start()
    {
        _tm = GetComponent<TextMesh>();
        if (_tm != null) _c0 = _tm.color;
    }

    void Update()
    {
        _t += Time.deltaTime;
        transform.position += Vector3.up * Time.deltaTime * 0.5f;

        var cam = Camera.main;
        if (cam != null)
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);

        if (_tm != null)
        {
            var c = _c0;
            c.a = Mathf.Clamp01(1f - _t / _life);
            _tm.color = c;
        }

        if (_t >= _life) Destroy(gameObject);
    }
}
