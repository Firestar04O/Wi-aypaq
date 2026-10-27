using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TapestrySeam
{
    public ClockJamPiece pieceA;
    public ClockJamPiece pieceB;
    public GameObject seamObject;
}

public class ClockJamPuzzleManager : MonoBehaviour
{
    public bool debugLog = true;
    public TapestrySeam[] seams = new TapestrySeam[0];
    public event System.Action OnPuzzleComplete;

    private readonly List<ClockJamPiece> _pieces = new List<ClockJamPiece>();
    private bool _completed;

    public IReadOnlyList<ClockJamPiece> Pieces => _pieces;
    public int TotalCount => _pieces.Count;
    public int PlacedCount { get; private set; }
    public bool IsComplete => _completed;

    public void RegisterPiece(ClockJamPiece piece)
    {
        if (piece == null || _pieces.Contains(piece)) return;
        _pieces.Add(piece);
        piece.puzzleManager = this;
        piece.OnPlaced += HandlePiecePlaced;
    }

    public void UnregisterPiece(ClockJamPiece piece)
    {
        if (piece == null || !_pieces.Remove(piece)) return;
        piece.OnPlaced -= HandlePiecePlaced;
    }

    private void HandlePiecePlaced(ClockJamPiece piece)
    {
        RecalculatePlacedCount();
        RefreshSeams();
        if (!_completed && TotalCount > 0 && PlacedCount >= TotalCount)
            CompleteLevel();
    }

    private void RefreshSeams()
    {
        foreach (TapestrySeam seam in seams)
        {
            if (seam.seamObject == null) continue;
            bool show = seam.pieceA != null && seam.pieceB != null
                        && seam.pieceA.IsPlaced && seam.pieceB.IsPlaced;
            if (seam.seamObject.activeSelf != show) seam.seamObject.SetActive(show);
        }
    }

    public void CompleteLevel()
    {
        if (_completed) return;
        _completed = true;
        if (debugLog) Debug.Log("¡El reloj ancestral ha sido completado con éxito!");
        OnPuzzleComplete?.Invoke();
    }

    private void RecalculatePlacedCount()
    {
        PlacedCount = 0;
        foreach (ClockJamPiece p in _pieces)
            if (p.IsPlaced) PlacedCount++;
    }

    public ClockJamPiece GetRandomTargetablePiece()
    {
        if (TotalCount == 0) return null;
        List<ClockJamPiece> candidates = new List<ClockJamPiece>();
        foreach (ClockJamPiece p in _pieces)
            if (p.CanBeTargeted) candidates.Add(p);

        if (candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)];
    }

    public void InterruptAllPendingAttacks()
    {
        foreach (ClockJamPiece p in _pieces)
            p.InterruptIntiAttack();
    }

    public void ResetPuzzle()
    {
        foreach (ClockJamPiece p in _pieces)
            p.ResetPiece();
        foreach (TapestrySeam seam in seams)
            if (seam.seamObject != null) seam.seamObject.SetActive(false);
        _completed = false;
        PlacedCount = 0;
    }
}