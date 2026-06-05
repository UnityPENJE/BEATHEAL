using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using Unity.XR.CoreUtils;

// XR Hands(핸드 트래킹)를 사용해 양손 손가락 끝에 트리거 콜라이더를 따라가게 한다.
// 콜라이더에는 "Hand" 태그가 붙으므로, 맨손으로 악기(InstrumentPanel)를 칠 수 있다.
// 기존 컨트롤러+드럼스틱 방식을 대체/병행하는 VR 손 입력 방식.
//
// 사용법: XR Origin (XR Rig) 오브젝트에 이 컴포넌트를 붙인다.
//   - 조인트 포즈는 XR Origin(트래킹 원점) 로컬 공간 기준이므로, 콜라이더를
//     이 컴포넌트의 transform 하위에 두고 localPosition으로 갱신한다.
[DisallowMultipleComponent]
public class HandPokeDriver : MonoBehaviour
{
    [Header("팁 콜라이더 설정")]
    [Tooltip("악기에 닿는 손가락 끝 콜라이더 반지름(m)")]
    public float tipRadius = 0.012f;

    [Tooltip("검지 끝뿐 아니라 중지 끝에도 콜라이더를 둘지 여부")]
    public bool includeMiddleFinger = true;

    [Tooltip("손 추적이 끊겼을 때 콜라이더를 비활성화")]
    public bool hideWhenUntracked = true;

    [Tooltip("디버그용: 손가락 끝 콜라이더를 작은 구체로 표시")]
    public bool showDebugSpheres = false;

    // 콜라이더를 둘 손가락 끝 조인트 목록
    static readonly XRHandJointID[] TipJoints =
    {
        XRHandJointID.IndexTip,
        XRHandJointID.MiddleTip,
    };

    class FingerTip
    {
        public XRHandJointID jointId;
        public Transform transform;
        public GameObject go;
    }

    readonly List<FingerTip> _leftTips = new List<FingerTip>();
    readonly List<FingerTip> _rightTips = new List<FingerTip>();

    XRHandSubsystem _subsystem;
    static readonly List<XRHandSubsystem> s_Subsystems = new List<XRHandSubsystem>();

    // Play 시작 시 씬에 HandPokeDriver가 없으면 XR Origin의 Camera Offset 아래에 자동 설치한다.
    // (에디터 버튼을 누르거나 씬을 수정하지 않아도 VR 손 입력이 동작하게 함)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInstall()
    {
        if (FindObjectOfType<HandPokeDriver>() != null)
            return;

        var origin = FindObjectOfType<XROrigin>();
        Transform parent = null;
        if (origin != null)
            parent = origin.CameraFloorOffsetObject != null
                ? origin.CameraFloorOffsetObject.transform
                : origin.transform;

        var go = new GameObject("HandPokeDriver (auto)");
        if (parent != null)
            go.transform.SetParent(parent, false);
        go.AddComponent<HandPokeDriver>();
        Debug.Log("[HandPokeDriver] VR 손 입력 자동 설치됨" +
                  (parent != null ? $" (부모: {parent.name})" : " (XR Origin을 찾지 못해 월드 루트에 설치)"));
    }

    // 손 조인트 포즈는 XR Origin의 트래킹 공간(Camera Offset) 기준이므로 그 공간에 팁을 둔다.
    Transform _trackingSpace;

    void Start()
    {
        EnsureHandTag();

        var origin = FindObjectOfType<XROrigin>();
        if (origin != null && origin.CameraFloorOffsetObject != null)
            _trackingSpace = origin.CameraFloorOffsetObject.transform;
        else if (origin != null)
            _trackingSpace = origin.transform;
        else
            _trackingSpace = transform; // 폴백

        CreateTips(Handedness.Left, _leftTips);
        CreateTips(Handedness.Right, _rightTips);
    }

    void CreateTips(Handedness handedness, List<FingerTip> list)
    {
        int count = includeMiddleFinger ? 2 : 1;
        for (int i = 0; i < count; i++)
        {
            var jointId = TipJoints[i];
            var go = showDebugSpheres
                ? GameObject.CreatePrimitive(PrimitiveType.Sphere)
                : new GameObject();
            go.name = $"HandTip_{handedness}_{jointId}";
            go.transform.SetParent(_trackingSpace, false);
            go.transform.localScale = Vector3.one * (tipRadius * 2f);

            // 디버그 구체일 경우 기본 콜라이더 제거 후 트리거 콜라이더 재구성
            var existingCol = go.GetComponent<Collider>();
            if (existingCol != null) Destroy(existingCol);

            var sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = showDebugSpheres ? 0.5f : tipRadius; // 디버그 구체는 스케일이 적용됨

            // 트리거가 다른 트리거(악기)와 충돌 이벤트를 일으키려면 한쪽에 Rigidbody 필요
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            go.tag = "Hand";
            go.SetActive(false);

            list.Add(new FingerTip { jointId = jointId, transform = go.transform, go = go });
        }
    }

    void Update()
    {
        if (_subsystem == null || !_subsystem.running)
            TryAcquireSubsystem();

        if (_subsystem == null || !_subsystem.running)
        {
            if (hideWhenUntracked) SetActiveAll(false);
            return;
        }

        UpdateHand(_subsystem.leftHand, _leftTips);
        UpdateHand(_subsystem.rightHand, _rightTips);
    }

    void TryAcquireSubsystem()
    {
        SubsystemManager.GetSubsystems(s_Subsystems);
        for (int i = 0; i < s_Subsystems.Count; i++)
        {
            if (s_Subsystems[i].running)
            {
                _subsystem = s_Subsystems[i];
                return;
            }
        }
    }

    void UpdateHand(XRHand hand, List<FingerTip> tips)
    {
        bool tracked = hand.isTracked;
        for (int i = 0; i < tips.Count; i++)
        {
            var tip = tips[i];
            if (tracked && hand.GetJoint(tip.jointId).TryGetPose(out Pose pose))
            {
                tip.transform.localPosition = pose.position;
                tip.transform.localRotation = pose.rotation;
                if (!tip.go.activeSelf) tip.go.SetActive(true);
            }
            else if (hideWhenUntracked && tip.go.activeSelf)
            {
                tip.go.SetActive(false);
            }
        }
    }

    void SetActiveAll(bool active)
    {
        for (int i = 0; i < _leftTips.Count; i++)
            if (_leftTips[i].go.activeSelf != active) _leftTips[i].go.SetActive(active);
        for (int i = 0; i < _rightTips.Count; i++)
            if (_rightTips[i].go.activeSelf != active) _rightTips[i].go.SetActive(active);
    }

    // "Hand" 태그가 없으면 런타임에서는 추가할 수 없으므로 경고만 출력.
    // 에디터에서는 BeatHealSetup이 태그를 보장한다.
    void EnsureHandTag()
    {
        try { gameObject.CompareTag("Hand"); }
        catch
        {
            Debug.LogError("[HandPokeDriver] 'Hand' 태그가 프로젝트에 없습니다. " +
                           "BeatHeal > Setup Scene > '핸드 트래킹(VR 손) 세팅'을 먼저 실행하세요.");
        }
    }
}
