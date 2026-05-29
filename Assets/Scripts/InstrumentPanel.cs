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
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public Color fakeFlickerColor = Color.white;

    public event Action<InstrumentPanel> OnTouched;

    private Renderer _renderer;
    private bool _isLit;
    private bool _acceptInput;
    private float _lightOnTime;

    // 페이크 깜빡임
    private bool _isFakeFlickering;
    private float _flickerTimer;
    private const float FlickerInterval = 0.15f;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        SetColor(idleColor);
    }

    void Update()
    {
        if (_isFakeFlickering)
        {
            _flickerTimer -= Time.deltaTime;
            if (_flickerTimer <= 0f)
            {
                _flickerTimer = FlickerInterval;
                _renderer.material.color =
                    _renderer.material.color == idleColor ? fakeFlickerColor : idleColor;
            }
        }
    }

    public void LightOn()
    {
        _isLit = true;
        _acceptInput = true;
        _lightOnTime = Time.time;
        SetColor(litColor);
    }

    public void LightOff()
    {
        _isLit = false;
        _acceptInput = false;
        SetColor(idleColor);
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
    }

    public void ShowCorrect()
    {
        _acceptInput = false;
        SetColor(correctColor);
    }

    public void ShowWrong()
    {
        _acceptInput = false;
        SetColor(wrongColor);
    }

    void OnTriggerEnter(Collider other)
    {
        // XR 컨트롤러 또는 Hand 태그 확인
        if (!other.CompareTag("XRController") && !other.CompareTag("Hand"))
            return;

        // 반응속도 기록 (올바른 악기일 때만)
        if (_acceptInput && _isLit)
        {
            float reactionTime = Time.time - _lightOnTime;
            GameData.TotalReactionTime += reactionTime;
            GameData.ReactionCount++;
        }

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
}
