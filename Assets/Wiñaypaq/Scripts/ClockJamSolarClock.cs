using System;
using UnityEngine;

public class ClockJamSolarClock : MonoBehaviour
{
    public float horaInicio = 6f;
    public float horaFin = 18f;
    public float dayDurationSeconds = 180f;

    public event Action<float> OnHourChanged;
    public event Action<float> OnTimeChanged;
    public event Action OnDayComplete;

    private float _hora;
    private bool _running;

    public float CurrentHour => _hora;
    public bool IsRunning => _running;
    public float TLinear => Mathf.InverseLerp(horaInicio, horaFin, _hora);

    private void Awake()
    {
        _hora = horaInicio;
        _running = true;
    }

    private void Update()
    {
        if (!_running) return;

        float prev = _hora;
        float speed = dayDurationSeconds > 0f ? (horaFin - horaInicio) / dayDurationSeconds : 0f;
        _hora = Mathf.Min(_hora + speed * Time.deltaTime, horaFin);

        float prevFloor = Mathf.Floor(prev);
        float crossed = Mathf.Floor(_hora);
        for (float h = prevFloor + 1f; h <= crossed; h++)
            OnHourChanged?.Invoke(h);

        OnTimeChanged?.Invoke(TLinear);

        if (_hora >= horaFin)
        {
            _running = false;
            OnDayComplete?.Invoke();
        }
    }

    public void Stop() => _running = false;

    public void ResetClock()
    {
        _hora = horaInicio;
        _running = true;
        OnTimeChanged?.Invoke(TLinear);
    }
}
