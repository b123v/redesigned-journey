using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class NewtonsCradleBalls : MonoBehaviour
{
    [Header("Параметры")]
    public int   ballCount    = 5;
    public float ballRadius   = 0.25f;
    public float stringLength = 2.0f;
    public float damping      = 0.0008f;

    [Range(1, 4)]     public int   liftBallCount = 1;
    [Range(15f, 75f)] public float liftAngle     = 45f;

    [Header("Материалы")]
    public Material ballMaterial;
    public Material stringMaterial;

    private GameObject[]   ballObjects;
    private LineRenderer[] stringsLeft;
    private LineRenderer[] stringsRight;

    private float[] angles;
    private float[] angularVelocities;

    private const float kDepthMargin = 0.05f;

    private float dia;
    private float startX;
    private float pivotY;
    private float frameDepth;

    void Start()
    {
        dia        = ballRadius * 2f;
        startX     = -dia * (ballCount - 1) * 0.5f;
        pivotY     = stringLength + ballRadius * 3f;
        frameDepth = dia + kDepthMargin * 2f;

        angles            = new float[ballCount];
        angularVelocities = new float[ballCount];

        FindOrSpawnBalls();
        LiftBalls(liftBallCount);
        CreateStringRenderers();
    }

    void FindOrSpawnBalls()
    {
        ballObjects = new GameObject[ballCount];

        for (int i = 0; i < ballCount; i++)
        {
            Transform found = transform.Find("Ball_" + i);
            if (found != null)
            {
                ballObjects[i] = found.gameObject;
                Transform sl = transform.Find("StaticStringL_" + i);
                Transform sr = transform.Find("StaticStringR_" + i);
                if (sl != null) Destroy(sl.gameObject);
                if (sr != null) Destroy(sr.gameObject);
            }
            else
            {
                float x     = startX + i * dia;
                float ballY = pivotY - stringLength;
                var   ball  = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name   = "Ball_" + i;
                ball.transform.SetParent(transform);
                ball.transform.localPosition = new Vector3(x, ballY, 0f);
                ball.transform.localScale    = Vector3.one * dia;
                Destroy(ball.GetComponent<Collider>());
                Destroy(ball.GetComponent<Rigidbody>());

                var rend = ball.GetComponent<Renderer>();
                if (ballMaterial != null)
                {
                    rend.material = ballMaterial;
                }
                else
                {
                    var mat = new Material(Shader.Find(LitShaderName()));
                    mat.color = new Color(0.82f, 0.86f, 0.92f);
                    mat.SetFloat("_Metallic",   0.95f);
                    mat.SetFloat("_Smoothness", 0.98f);
                    rend.material = mat;
                }

                ballObjects[i] = ball;
            }

            angles[i]            = 0f;
            angularVelocities[i] = 0f;
        }
    }

    void LiftBalls(int count)
    {
        int n = Mathf.Clamp(count, 1, ballCount - 1);
        for (int i = 0; i < ballCount; i++)
        {
            angles[i]            = 0f;
            angularVelocities[i] = 0f;
        }
        for (int i = 0; i < n; i++)
            angles[i] = -liftAngle * Mathf.Deg2Rad;
        ApplyPositions();
    }

    void ApplyPositions()
    {
        for (int i = 0; i < ballCount; i++)
        {
            float x  = startX + i * dia;
            float bx = x  + Mathf.Sin(angles[i]) * stringLength;
            float by = pivotY - Mathf.Cos(angles[i]) * stringLength;
            ballObjects[i].transform.localPosition = new Vector3(bx, by, 0f);
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        float g  = Physics.gravity.magnitude;

        for (int i = 0; i < ballCount; i++)
        {
            float alpha = -(g / stringLength) * Mathf.Sin(angles[i]);
            angularVelocities[i] += alpha * dt;
            angularVelocities[i] *= (1f - damping);
            angles[i]            += angularVelocities[i] * dt;
        }

        ResolveCollisions();

        for (int i = 0; i < ballCount; i++)
        {
            float x  = startX + i * dia;
            float bx = x  + Mathf.Sin(angles[i]) * stringLength;
            float by = pivotY - Mathf.Cos(angles[i]) * stringLength;
            ballObjects[i].transform.localPosition = new Vector3(bx, by, 0f);
        }

        UpdateStrings();
    }

    void ResolveCollisions()
    {
        float contactThreshold = dia + 0.002f;

        for (int iter = 0; iter < 8; iter++)
        {
            for (int i = 0; i < ballCount - 1; i++)
            {
                float xA = startX + i * dia + Mathf.Sin(angles[i]) * stringLength;
                float xB = startX + (i + 1) * dia + Mathf.Sin(angles[i + 1]) * stringLength;

                float dist     = xB - xA;
                float vA       = angularVelocities[i]     * stringLength;
                float vB       = angularVelocities[i + 1] * stringLength;
                float approach = vA - vB;

                if (dist <= contactThreshold && approach > 0f)
                {
                    angularVelocities[i]     = vB / stringLength;
                    angularVelocities[i + 1] = vA / stringLength;
                }

                if (dist < dia)
                {
                    float overlap = dia - dist;
                    angles[i]     -= (overlap * 0.5f) / stringLength;
                    angles[i + 1] += (overlap * 0.5f) / stringLength;
                }
            }
        }
    }

    void CreateStringRenderers()
    {
        stringsLeft  = new LineRenderer[ballCount];
        stringsRight = new LineRenderer[ballCount];
        for (int i = 0; i < ballCount; i++)
        {
            stringsLeft[i]  = MakeString(ballObjects[i], "StringL_" + i);
            stringsRight[i] = MakeString(ballObjects[i], "StringR_" + i);
        }
    }

    LineRenderer MakeString(GameObject parent, string goName)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent.transform);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount     = 2;
        lr.useWorldSpace     = true;
        lr.startWidth        = 0.015f;
        lr.endWidth          = 0.015f;
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        if (stringMaterial != null)
        {
            lr.material = stringMaterial;
        }
        else
        {
            var mat = new Material(Shader.Find(LitShaderName()));
            mat.color = new Color(0.75f, 0.75f, 0.78f);
            lr.material = mat;
        }
        return lr;
    }

    void UpdateStrings()
    {
        if (stringsLeft == null) return;
        float hd = frameDepth * 0.5f;

        for (int i = 0; i < ballCount; i++)
        {
            float   x      = startX + i * dia;
            Vector3 ballWP = ballObjects[i].transform.position;
            Vector3 pivotL = transform.TransformPoint(new Vector3(x, pivotY, -hd));
            Vector3 pivotR = transform.TransformPoint(new Vector3(x, pivotY,  hd));
            stringsLeft[i].SetPosition(0, pivotL);
            stringsLeft[i].SetPosition(1, ballWP);
            stringsRight[i].SetPosition(0, pivotR);
            stringsRight[i].SetPosition(1, ballWP);
        }
    }

    static string LitShaderName()
    {
        var rp = GraphicsSettings.currentRenderPipeline;
        if (rp == null) return "Standard";
        string t = rp.GetType().FullName;
        if (t.Contains("Universal"))      return "Universal Render Pipeline/Lit";
        if (t.Contains("HighDefinition")) return "HDRP/Lit";
        return "Standard";
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.spaceKey.wasPressedThisFrame)  LiftBalls(liftBallCount);
        if (keyboard.digit1Key.wasPressedThisFrame) LiftBalls(1);
        if (keyboard.digit2Key.wasPressedThisFrame) LiftBalls(2);
        if (keyboard.digit3Key.wasPressedThisFrame) LiftBalls(3);
        if (keyboard.digit4Key.wasPressedThisFrame) LiftBalls(4);
    }
}
