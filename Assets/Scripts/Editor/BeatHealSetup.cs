using UnityEngine;
using UnityEditor;

public class BeatHealSetup : EditorWindow
{
    int   instrumentCount = 5;
    float radius          = 1.8f;
    float heightOffset    = 1.0f;
    float arcDegrees      = 150f;

    static readonly Color[] InstrumentColors =
    {
        new Color(0.8f, 0.2f, 0.2f),
        new Color(0.2f, 0.4f, 0.9f),
        new Color(0.2f, 0.8f, 0.3f),
        new Color(0.9f, 0.8f, 0.1f),
        new Color(0.7f, 0.2f, 0.9f),
        new Color(0.1f, 0.8f, 0.8f),
        new Color(0.9f, 0.5f, 0.1f),
    };

    [MenuItem("BeatHeal/Setup Scene")]
    public static void ShowWindow() => GetWindow<BeatHealSetup>("BeatHeal Setup");

    void OnGUI()
    {
        GUILayout.Label("악기 배치 설정", EditorStyles.boldLabel);
        instrumentCount = EditorGUILayout.IntSlider("악기 개수",     instrumentCount, 3, 7);
        radius          = EditorGUILayout.Slider("반원 반지름 (m)", radius,          0.8f, 3f);
        heightOffset    = EditorGUILayout.Slider("높이 (m)",        heightOffset,    0.5f, 2.0f);
        arcDegrees      = EditorGUILayout.Slider("호 각도",          arcDegrees,      90f,  180f);
        GUILayout.Space(10);
        if (GUILayout.Button("악기 배치 생성")) CreateInstruments();
        if (GUILayout.Button("무대 바닥 생성")) CreateStage();
        if (GUILayout.Button("조명 세팅"))     CreateLights();
    }

    void CreateInstruments()
    {
        // XR Origin 위치를 기준점으로 사용
        var xrOrigin = GameObject.Find("XR Origin (XR Rig)")
                    ?? GameObject.Find("XR Origin")
                    ?? GameObject.Find("XRRig")
                    ?? GameObject.Find("Camera Offset");

        Vector3 center = xrOrigin != null ? xrOrigin.transform.position : Vector3.zero;

        if (xrOrigin == null)
            Debug.LogWarning("[BeatHeal] XR Origin을 찾지 못했습니다. 월드 원점(0,0,0) 기준으로 배치합니다.");
        else
            Debug.Log($"[BeatHeal] XR Origin 발견: {xrOrigin.name} @ {center}");

        var existing = GameObject.Find("Instruments");
        if (existing != null) DestroyImmediate(existing);

        var parent = new GameObject("Instruments");
        Undo.RegisterCreatedObjectUndo(parent, "Create Instruments");

        for (int i = 0; i < instrumentCount; i++)
        {
            float t     = instrumentCount == 1 ? 0.5f : (float)i / (instrumentCount - 1);
            float angle = Mathf.Lerp(-arcDegrees / 2f, arcDegrees / 2f, t);
            float rad   = angle * Mathf.Deg2Rad;
            // XR Origin 기준 반원 배치
            Vector3 pos = center + new Vector3(Mathf.Sin(rad) * radius, heightOffset, Mathf.Cos(rad) * radius);

            Color col = InstrumentColors[i % InstrumentColors.Length];

            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = $"Instrument_{i}";
            obj.transform.position   = pos;
            obj.transform.localScale = new Vector3(0.28f, 0.04f, 0.28f);
            obj.transform.SetParent(parent.transform);

            // 머티리얼
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = col;
            obj.GetComponent<Renderer>().material = mat;

            // 트리거 콜라이더
            var col3d = obj.GetComponent<CapsuleCollider>();
            col3d.isTrigger = true;

            // 리지드바디
            var rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;

            // InstrumentPanel
            var panel = obj.AddComponent<InstrumentPanel>();
            panel.instrumentIndex = i;
            panel.idleColor = col;
            panel.litColor  = Color.Lerp(col, Color.white, 0.7f);

            // 악기 전용 포인트 라이트 (악기 바로 위)
            var lightObj = new GameObject($"Instrument_{i}_Light");
            lightObj.transform.position = pos + Vector3.up * 0.3f;
            lightObj.transform.SetParent(obj.transform);
            var pointLight = lightObj.AddComponent<Light>();
            pointLight.type      = LightType.Point;
            pointLight.color     = col;
            pointLight.intensity = 1.5f;
            pointLight.range     = 0.8f;
            Undo.RegisterCreatedObjectUndo(lightObj, "Create Instrument Light");

            Undo.RegisterCreatedObjectUndo(obj, "Create Instrument");
        }

        Debug.Log($"악기 {instrumentCount}개 생성 완료!");
        Selection.activeGameObject = parent;
    }

    void CreateLights()
    {
        var dir = GameObject.Find("Directional Light");
        if (dir != null)
        {
            var l = dir.GetComponent<Light>();
            l.color     = new Color(1f, 0.78f, 0.58f);
            l.intensity = 0.5f;
        }

        var ep = GameObject.Find("StagePointLight");
        if (ep != null) DestroyImmediate(ep);

        var pl = new GameObject("StagePointLight");
        pl.transform.position = new Vector3(0f, 3f, 1f);
        var lt = pl.AddComponent<Light>();
        lt.type      = LightType.Point;
        lt.color     = Color.white;
        lt.intensity = 3f;
        lt.range     = 8f;

        Undo.RegisterCreatedObjectUndo(pl, "Create Light");
        Debug.Log("조명 세팅 완료!");
    }

    void CreateStage()
    {
        var ex = GameObject.Find("Stage");
        if (ex != null) DestroyImmediate(ex);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name             = "Stage";
        floor.transform.localScale = new Vector3(2f, 1f, 2f);

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = new Color(0.10f, 0.10f, 0.15f);
        floor.GetComponent<Renderer>().material = mat;

        Undo.RegisterCreatedObjectUndo(floor, "Create Stage");
        Debug.Log("무대 바닥 생성 완료!");
    }
}
