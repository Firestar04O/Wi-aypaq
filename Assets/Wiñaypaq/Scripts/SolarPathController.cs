using UnityEngine;

public class SolarPathController : MonoBehaviour
{
    public ClockJamSolarClock clock;
    public Transform sun;
    public Transform[] waypoints = new Transform[0];
    public AnimationCurve curvaTiempo = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    public Vector3 offset = Vector3.zero;

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

    private void Apply(float t)
    {
        if (sun == null) return;
        int n = waypoints != null ? waypoints.Length : 0;
        if (n == 0) return;

        if (n == 1)
        {
            if (waypoints[0] != null) sun.position = waypoints[0].position + offset;
            return;
        }

        float u = Mathf.Clamp01(curvaTiempo.Evaluate(Mathf.Clamp01(t))) * (n - 1);
        int i = Mathf.Clamp(Mathf.FloorToInt(u), 0, n - 2);
        float f = u - i;

        sun.position = CatmullRom(
            At(i - 1),
            At(i),
            At(i + 1),
            At(i + 2),
            f) + offset;
    }

    private Transform At(int index)
    {
        if (waypoints == null) return null;
        if (index < 0 || index >= waypoints.Length) return null;
        return waypoints[index];
    }

    private static Vector3 CatmullRom(Transform p0, Transform p1, Transform p2, Transform p3, float t)
    {
        Vector3 a = Pos(p1);
        Vector3 b = Pos(p2);
        Vector3 before = p0 != null ? Pos(p0) : a;
        Vector3 after = p3 != null ? Pos(p3) : b;

        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * ((2f * a)
                       + (-before + b) * t
                       + (2f * before - 5f * a + 4f * b - after) * t2
                       + (-before + 3f * a - 3f * b + after) * t3);
    }

    private static Vector3 Pos(Transform t) => t != null ? t.position : Vector3.zero;
}
