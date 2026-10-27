using System.Collections;
using UnityEngine;

public class ClockJamLevelController : MonoBehaviour
{
    public ClockJamSolarClock clock;
    public ClockJamWrathController wrath;
    public ClockJamPuzzleManager puzzleManager;
    public GameObject figura;
    public GameObject piezasRoot;
    public GameObject costurasRoot;
    public bool debugLog = true;
    public float repairDelay = 0.8f;
    public float repairPopDuration = 0.35f;
    public float restartDelay = 2f;

    private Coroutine _repair;
    private Vector3 _figuraBaseScale = Vector3.one;

    private void OnEnable()
    {
        if (clock != null) clock.OnDayComplete += HandleDayComplete;
        if (puzzleManager != null) puzzleManager.OnPuzzleComplete += HandlePuzzleComplete;
    }

    private void OnDisable()
    {
        if (clock != null) clock.OnDayComplete -= HandleDayComplete;
        if (puzzleManager != null) puzzleManager.OnPuzzleComplete -= HandlePuzzleComplete;
    }

    private void Start()
    {
        if (figura != null)
        {
            _figuraBaseScale = figura.transform.localScale;
            figura.SetActive(false);
        }
        if (piezasRoot != null) piezasRoot.SetActive(true);
        if (costurasRoot != null) costurasRoot.SetActive(true);
    }

    private void HandlePuzzleComplete()
    {
        if (debugLog) Debug.Log("¡El textil está completo: victoria!");
        clock?.Stop();
        if (wrath != null) wrath.enabled = false;
        if (_repair != null) StopCoroutine(_repair);
        _repair = StartCoroutine(RepairRoutine());
    }

    private void HandleDayComplete()
    {
        if (debugLog) Debug.Log("El día terminó. Reiniciando gameplay…");
        StartCoroutine(RestartRoutine());
    }

    private IEnumerator RepairRoutine()
    {
        yield return new WaitForSeconds(repairDelay);

        if (piezasRoot != null) piezasRoot.SetActive(false);
        if (costurasRoot != null) costurasRoot.SetActive(false);

        if (figura != null)
        {
            figura.SetActive(true);
            Transform ft = figura.transform;
            Vector3 target = _figuraBaseScale;
            ft.localScale = target * 0.96f;

            float elapsed = 0f;
            while (elapsed < repairPopDuration)
            {
                elapsed += Time.deltaTime;
                float k = repairPopDuration > 0f ? Mathf.Clamp01(elapsed / repairPopDuration) : 1f;
                k = 1f - (1f - k) * (1f - k);
                ft.localScale = Vector3.Lerp(target * 0.96f, target, k);
                yield return null;
            }
            ft.localScale = target;
        }

        _repair = null;
    }

    private IEnumerator RestartRoutine()
    {
        yield return new WaitForSeconds(restartDelay);

        if (_repair != null)
        {
            StopCoroutine(_repair);
            _repair = null;
        }
        if (figura != null) figura.SetActive(false);
        if (piezasRoot != null) piezasRoot.SetActive(true);
        if (costurasRoot != null) costurasRoot.SetActive(true);

        puzzleManager?.ResetPuzzle();
        clock?.ResetClock();
        if (wrath != null) wrath.enabled = true;

        if (debugLog) Debug.Log("Nuevo día: piezas devueltas a su origen.");
    }
}
