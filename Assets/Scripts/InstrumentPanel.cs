using System;
using UnityEngine;

public class InstrumentPanel : MonoBehaviour
{
    [Header("설정")]
    public int instrumentIndex;
    public bool isFake; // 페이크 악기 여부

    [Header("색상")]
    public Color idleColor = Color.gray;
    public Color litColor = Color.yellow;
    public Color demoColor = Color.white;                    // 보여주기(시범) 단계 색
    public Color readyColor = new Color(0.1f, 1f, 0.4f);     // "당신 차례" 신호 색 (초록)
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public Color fakeFlickerColor = new Color(1f, 0.25f, 0f); // 가짜 악기 경고색 (주황빨강)

    [Header("점등 연출")]
    public float litScale = 1.4f;          // 켜질 때 커지는 배율
    public float emissionStrength = 3.5f;  // 발광 세기
    public float litLightIntensity = 6f;   // 점등 시 포인트 라이트 세기
    public float scaleLerpSpeed = 12f;     // 크기 보간 속도

    public event Action<InstrumentPanel> OnTouched;

    private Renderer _renderer;
    private Light _attachedLight;
    private float _baseLightIntensity;
    private Vector3 _baseScale;
    private Vector3 _targetScale;

    private bool _isLit;
    private bool _acceptInput;
    private float _lightOnTime;

    // 페이크 깜빡임
    private bool _isFakeFlickering;
    private bool _flickerOn;
    private float _flickerTimer;
    private const float FlickerInterval = 0.13f;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _attachedLight = GetComponentInChildren<Light>();
        if (_attachedLight != null) _baseLightIntensity = _attachedLight.intensity;
        _baseScale = transform.localScale;
        _targetScale = _baseScale;
        SetColor(idleColor);
        SetEmission(idleColor, false);
    }

    void Update()
    {
        // 크기 부드럽게 보간 (팝업 효과)
        transform.localScale = Vector3.Lerp(
            transform.localScale, _targetScale, Time.deltaTime * scaleLerpSpeed);

        if (_isFakeFlickering)
        {
            _flickerTimer -= Time.deltaTime;
            if (_flickerTimer <= 0f)
            {
                _flickerTimer = FlickerInterval;
                _flickerOn = !_flickerOn;
                if (_flickerOn) { SetColor(fakeFlickerColor); SetEmission(fakeFlickerColor, true); }
                else           { SetColor(idleColor);        SetEmission(idleColor, false); }
            }
        }
    }

    public void LightOn()
    {
        _isLit = true;
        _acceptInput = true;
        _lightOnTime = Time.time;
        SetColor(litColor);
        SetEmission(litColor, true);     // 발광
        SetLight(litColor, litLightIntensity);
        _targetScale = _baseScale * litScale;  // 커지기
    }

    public void LightOff()
    {
        _isLit = false;
        _acceptInput = false;
        SetColor(idleColor);
        SetEmission(idleColor, false);
        SetLight(idleColor, _baseLightIntensity);
        _targetScale = _baseScale;
    }

    // 보여주기(시범) 단계 점등 — 흰색, 입력 받지 않음
    public void ShowDemo()
    {
        SetColor(demoColor);
        SetEmission(demoColor, true);
        SetLight(demoColor, litLightIntensity);
        _targetScale = _baseScale * litScale;
    }

    // "당신 차례" 입력 시작 신호 — 초록 펄스
    public void ShowReady()
    {
        SetColor(readyColor);
        SetEmission(readyColor, true);
        SetLight(readyColor, litLightIntensity);
        _targetScale = _baseScale * litScale;
    }

    public void StartFakeFlicker()
    {
        _isFakeFlickering = true;
        _flickerTimer = FlickerInterval;
    }

    public void StopFakeFlicker()
    {
        _isFakeFlickering = false;
        SetColor(idleColor);
        SetEmission(idleColor, false);
    }

    public void ShowCorrect()
    {
        _acceptInput = false;
        SetColor(correctColor);
        SetEmission(correctColor, true);
        SetLight(correctColor, litLightIntensity);
        _targetScale = _baseScale * litScale;
    }

    public void ShowWrong()
    {
        _acceptInput = false;
        SetColor(wrongColor);
        SetEmission(wrongColor, true);
        SetLight(wrongColor, litLightIntensity);
        _targetScale = _baseScale;
    }

    void OnTriggerEnter(Collider other)
    {
        // XR 컨트롤러 또는 Hand 태그 확인
        if (!other.CompareTag("XRController") && !other.CompareTag("Hand"))
            return;

        // 반응속도는 SequenceManager에서 기록 (입력 단계엔 노트가 꺼져 있으므로)

        // 팔 뻗기 범위 기록
        var head = Camera.main?.transform;
        if (head != null)
        {
            float reach = Vector3.Distance(head.position, other.transform.position);
            if (reach > GameData.MaxReach) GameData.MaxReach = reach;
        }

        GameData.TotalTouches++;
        OnTouched?.Invoke(this);
    }

    void SetColor(Color c)
    {
        if (_renderer != null)
            _renderer.material.color = c;
    }

    // 머티리얼 발광 (URP Lit / Standard 모두 _EmissionColor 사용)
    void SetEmission(Color c, bool on)
    {
        if (_renderer == null) return;
        var mat = _renderer.material;
        if (on)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", c * emissionStrength);
        }
        else
        {
            mat.SetColor("_EmissionColor", Color.black);
            mat.DisableKeyword("_EMISSION");
        }
    }

    // 악기 전용 포인트 라이트 색/세기 조절
    void SetLight(Color c, float intensity)
    {
        if (_attachedLight == null) return;
        _attachedLight.color = c;
        _attachedLight.intensity = intensity;
    }
}
