using UnityEngine;

public class ShadowSweepController : MonoBehaviour
{
    public ClockJamSolarClock clock;
    public Transform sombra;

    [Header("Calibracion")]
    public Transform posicionOriginal;
    public Vector2 puntaLocal = new Vector2(0f, 1f);
    public float anguloOffset = 0f;

    [Header("Barrido")]
    public Transform[] referencias = new Transform[0];
    public AnimationCurve curvaBarrido = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private void OnEnable()
    {
        if (clock != null) clock.OnTimeChanged += HandleTick;
        Refresh();
    }

    private void OnDisable()
    {
        if (clock != null) clock.OnTimeChanged -= HandleTick;
    }

    private void Start() => Refresh();

    public void HandleTick(float t) => Apply(t);

    public void Refresh() => Apply(0f);

    public void CapturarPunta()
    {
        if (sombra == null || posicionOriginal == null) return;
        puntaLocal = sombra.InverseTransformPoint(posicionOriginal.position);
    }

    public Vector3 TargetAt(float u)
    {
        int n = referencias != null ? referencias.Length : 0;
        if (n == 0) return sombra != null ? sombra.position : Vector3.zero;
        if (n == 1) return Pos(referencias[0]);

        float f = Mathf.Clamp01(u) * (n - 1);
        int i = Mathf.Clamp(Mathf.FloorToInt(f), 0, n - 2);
        return Vector3.Lerp(Pos(referencias[i]), Pos(referencias[i + 1]), f - i);
    }

    public Vector3 Origin() => sombra != null ? sombra.position : Vector3.zero;

    private void Apply(float t)
    {
        if (sombra == null) return;
        sombra.localScale = Vector3.one;

        int n = referencias != null ? referencias.Length : 0;
        if (n == 0) return;

        Vector3 origin = Origin();
        Vector3 d = TargetAt(curvaBarrido.Evaluate(Mathf.Clamp01(t))) - origin;
        if (d.sqrMagnitude < 1e-8f) return;

        bool useTip = posicionOriginal != null && puntaLocal.sqrMagnitude > 1e-10f;
        float targetAngle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        float tipAngle = useTip ? Mathf.Atan2(puntaLocal.y, puntaLocal.x) * Mathf.Rad2Deg : 0f;
        sombra.rotation = Quaternion.Euler(0f, 0f, targetAngle - tipAngle + anguloOffset);
    }

    private static Vector3 Pos(Transform t) => t != null ? t.position : Vector3.zero;
}
