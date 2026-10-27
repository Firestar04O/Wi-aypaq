using UnityEngine;

public class ClockJamWrathController : MonoBehaviour
{
    public ClockJamPuzzleManager puzzleManager;
    public ClockJamSolarClock solarClock;

    public float warningDuration = 3.5f;
    public float respawnTime = 15f;

    private void OnEnable()
    {
        if (solarClock != null) solarClock.OnHourChanged += HandleHour;
        if (puzzleManager != null) puzzleManager.OnPuzzleComplete += HandlePuzzleComplete;
    }

    private void OnDisable()
    {
        if (solarClock != null) solarClock.OnHourChanged -= HandleHour;
        if (puzzleManager != null) puzzleManager.OnPuzzleComplete -= HandlePuzzleComplete;
        if (puzzleManager != null) puzzleManager.InterruptAllPendingAttacks();
    }

    private void HandleHour(float hour)
    {
        if (puzzleManager == null || puzzleManager.IsComplete) return;
        ClockJamPiece target = puzzleManager.GetRandomTargetablePiece();
        if (target != null) target.StartIntiAttack(warningDuration, respawnTime);
    }

    private void HandlePuzzleComplete()
    {
        if (puzzleManager != null) puzzleManager.InterruptAllPendingAttacks();
    }
}