using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class ClockJamPiece : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public enum PieceState { Idle, Targeted, Burning, Dragging, Placed }

    public ClockJamPuzzleManager puzzleManager;
    public Transform snapTarget;
    public Transform origen;

    public ParticleSystem fireEffect;
    public ParticleSystem respawnEffect;

    public float warningDuration = 2.5f;
    public float respawnTime = 4f;
    public float savedCooldown = 3f;
    public float snapRadius = 1.15f;
    public float returnDuration = 0.28f;

    public Color targetedColor = new Color(1f, 0.6f, 0.2f, 1f);
    public Color burningColor = new Color(0.12f, 0.04f, 0f, 1f);

    public event System.Action<ClockJamPiece> OnPlaced;

    private PieceState _state = PieceState.Idle;
    private SpriteRenderer _sprite;
    private Collider2D _collider;
    private Color _baseColor;
    private Vector3 _dragOffset;
    private float _nextTargetableTime;
    private Coroutine _attackRoutine;
    private Coroutine _returnRoutine;
    private Vector3 _fallbackPos;
    private Quaternion _fallbackRot;
    private Vector3 _fallbackScale;

    public PieceState State => _state;
    public bool IsPlaced => _state == PieceState.Placed;
    public bool CanBeTargeted => _state == PieceState.Idle && Time.time >= _nextTargetableTime;
    private bool IsInteractable => _state == PieceState.Idle || _state == PieceState.Targeted;

    public Vector3 OrigenPosition => origen != null ? origen.position : _fallbackPos;
    public Quaternion OrigenRotation => origen != null ? origen.rotation : _fallbackRot;
    public Vector3 OrigenScale => origen != null ? origen.localScale : _fallbackScale;

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        _baseColor = _sprite != null ? _sprite.color : Color.white;

        if (origen == null)
        {
            _fallbackPos = transform.position;
            _fallbackRot = transform.rotation;
            _fallbackScale = transform.localScale;
        }
    }

    private void Start()
    {
        if (puzzleManager != null) puzzleManager.RegisterPiece(this);
        SnapToOrigen();
    }

    private void OnDestroy()
    {
        if (puzzleManager != null) puzzleManager.UnregisterPiece(this);
    }

    public void SnapToOrigen()
    {
        transform.position = OrigenPosition;
        transform.rotation = OrigenRotation;
        transform.localScale = OrigenScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable) return;
        if (_state == PieceState.Targeted) InterruptIntiAttack();
        StopReturnRoutine();

        _state = PieceState.Dragging;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        _dragOffset = transform.position - GetWorldPosition(eventData);
        if (_sprite != null) _sprite.sortingOrder = 100;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_state != PieceState.Dragging) return;
        transform.position = GetWorldPosition(eventData) + _dragOffset;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_state != PieceState.Dragging) return;
        if (_sprite != null) _sprite.sortingOrder = 0;

        if (snapTarget != null && Vector2.Distance(transform.position, snapTarget.position) <= snapRadius)
        {
            PlaceAtTarget();
            return;
        }

        _state = PieceState.Idle;
        if (_sprite != null) _sprite.color = _baseColor;
        _returnRoutine = StartCoroutine(ReturnRoutine());
    }

    private Vector3 GetWorldPosition(PointerEventData eventData)
    {
        Camera cam = Camera.main;
        if (cam == null) return eventData.position;
        Vector3 screen = eventData.position;
        screen.z = -cam.transform.position.z;
        return cam.ScreenToWorldPoint(screen);
    }

    public void StartIntiAttack(float warningSeconds, float respawnSeconds)
    {
        if (!CanBeTargeted || _attackRoutine != null) return;
        _attackRoutine = StartCoroutine(IntiAttackRoutine(warningSeconds, respawnSeconds));
    }

    public bool InterruptIntiAttack()
    {
        if (_state != PieceState.Targeted) return false;
        if (_attackRoutine != null) StopCoroutine(_attackRoutine);
        _attackRoutine = null;

        _state = PieceState.Idle;
        transform.localScale = OrigenScale;
        if (_sprite != null) _sprite.color = _baseColor;
        _nextTargetableTime = Time.time + savedCooldown;
        if (fireEffect != null) fireEffect.Stop();
        return true;
    }

    public void ResetPiece()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }
        StopReturnRoutine();

        _state = PieceState.Idle;
        SnapToOrigen();
        _nextTargetableTime = 0f;

        if (_sprite != null)
        {
            _sprite.enabled = true;
            _sprite.color = _baseColor;
            _sprite.sortingOrder = 0;
        }
        if (_collider != null) _collider.enabled = true;
        StopEffects();
    }

    private void PlaceAtTarget()
    {
        _state = PieceState.Placed;
        transform.position = snapTarget.position;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        if (_sprite != null) _sprite.sortingOrder = 0;
        if (_collider != null) _collider.enabled = false;
        StopEffects();

        OnPlaced?.Invoke(this);
    }

    private IEnumerator ReturnRoutine()
    {
        Vector3 p0 = transform.position;
        Vector3 s0 = transform.localScale;
        Quaternion r0 = transform.rotation;
        Vector3 p1 = OrigenPosition;
        Vector3 s1 = OrigenScale;
        Quaternion r1 = OrigenRotation;

        float elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            if (_state != PieceState.Idle) yield break;

            float k = returnDuration > 0f ? Mathf.Clamp01(elapsed / returnDuration) : 1f;
            k = k * k * (3f - 2f * k);
            transform.position = Vector3.Lerp(p0, p1, k);
            transform.localScale = Vector3.Lerp(s0, s1, k);
            transform.rotation = Quaternion.Slerp(r0, r1, k);
            yield return null;
        }

        SnapToOrigen();
        _returnRoutine = null;
    }

    private void StopReturnRoutine()
    {
        if (_returnRoutine == null) return;
        StopCoroutine(_returnRoutine);
        _returnRoutine = null;
    }

    private void StopEffects()
    {
        if (fireEffect != null) fireEffect.Stop();
        if (respawnEffect != null) respawnEffect.Stop();
    }

    private IEnumerator IntiAttackRoutine(float warningSeconds, float respawnSeconds)
    {
        _state = PieceState.Targeted;
        if (fireEffect != null) fireEffect.Play();
        float elapsed = 0f;
        while (elapsed < warningSeconds)
        {
            if (_state != PieceState.Targeted) yield break;
            float pulse = 0.5f + 0.5f * Mathf.PingPong(Time.time * 5f, 1f);
            if (_sprite != null) _sprite.color = Color.Lerp(_baseColor, targetedColor, 0.35f + 0.65f * pulse);
            elapsed += Time.deltaTime;
            yield return null;
        }
        yield return StartCoroutine(BurnRoutine(respawnSeconds));
    }

    private IEnumerator BurnRoutine(float respawnSeconds)
    {
        _state = PieceState.Burning;
        if (_collider != null) _collider.enabled = false;
        if (fireEffect != null) fireEffect.Play();

        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            if (_sprite != null)
            {
                Color c = Color.Lerp(targetedColor, burningColor, t / 0.6f);
                c.a = 1f - t / 0.6f;
                _sprite.color = c;
            }
            yield return null;
        }
        if (_sprite != null) _sprite.enabled = false;
        if (respawnEffect != null) respawnEffect.Play();

        yield return new WaitForSeconds(respawnSeconds);

        SnapToOrigen();
        if (_sprite != null)
        {
            _sprite.enabled = true;
            _sprite.color = _baseColor;
        }
        if (_collider != null) _collider.enabled = true;
        StopEffects();

        _state = PieceState.Idle;
        _attackRoutine = null;
    }
}
