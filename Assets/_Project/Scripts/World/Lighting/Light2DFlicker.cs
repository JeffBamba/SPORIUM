using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _Project.World.Lighting
{
    /// <summary>
    /// Flicker animation for URP 2D Light (Light2D), tuned for "lab tube that barely works":
    /// low base intensity, random dropouts, occasional bursts, plus continuous noise.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light2D))]
    public class Light2DFlicker : MonoBehaviour
    {
        public enum FlickerMode
        {
            RandomFlicker = 0,
            StartupSequence = 1,
            PotGentle = 2
        }

        [Header("Mode")]
        [SerializeField] private FlickerMode mode = FlickerMode.StartupSequence;

        [Header("Intensity")]
        [Tooltip("Base intensity (the 'barely on' feeling).")]
        [SerializeField] private float baseIntensity = 2.0f;
        [SerializeField] private float minIntensity = 0.2f;
        [SerializeField] private float maxIntensity = 6.0f;

        [Header("Noise (continuous shimmer)")]
        [SerializeField] private float noiseSpeed = 8f;
        [SerializeField] private float noiseAmount = 0.25f;
        [SerializeField] private float noiseSeed = 0.123f;

        [Header("Dropouts (almost off)")]
        [SerializeField] private float dropoutChancePerSecond = 0.9f;
        [SerializeField] private Vector2 dropoutDuration = new Vector2(0.03f, 0.18f);
        [SerializeField] private float dropoutIntensity = 0.02f;

        [Header("Bursts (brief strong light)")]
        [SerializeField] private float burstChancePerSecond = 0.35f;
        [SerializeField] private Vector2 burstDuration = new Vector2(0.06f, 0.22f);
        [SerializeField] private float burstMultiplier = 1.8f;

        [Header("Random Flicker: Stable Pauses")]
        [Tooltip("If enabled, RandomFlicker will occasionally enter a stable pause where intensity stays fixed (no flicker).")]
        [SerializeField] private bool randomEnableStablePauses = false;
        [Tooltip("How often a stable pause starts (per second). Example: 0.05 ≈ once every ~20s on average.")]
        [SerializeField] private float randomStablePauseChancePerSecond = 0.05f;
        [Tooltip("Duration range of the stable pause (seconds). Set both to 7 for a fixed 7s pause.")]
        [SerializeField] private Vector2 randomStablePauseDuration = new Vector2(7f, 7f);
        [Tooltip("Intensity used during the stable pause. If 0, uses baseIntensity.")]
        [SerializeField] private float randomStablePauseIntensityOverride = 0f;

        [Header("Optional radius flicker (Point light)")]
        [SerializeField] private bool flickerRadius = false;
        [SerializeField] private float radiusNoiseAmount = 0.15f;

        [Header("Smoothing")]
        [Tooltip("Higher = snappier. Lower = smoother.")]
        [SerializeField] private float response = 25f;

        [Header("Pot Gentle Mode (subtle life)")]
        [Tooltip("Very subtle noise speed for pot lights.")]
        [SerializeField] private float potNoiseSpeed = 1.2f;
        [Tooltip("Very small intensity variance for pot lights.")]
        [SerializeField] private float potNoiseAmount = 0.08f;
        [Tooltip("Rare micro dips (per second).")]
        [SerializeField] private float potMicroDipChancePerSecond = 0.08f;
        [Tooltip("Duration of micro dips (seconds).")]
        [SerializeField] private Vector2 potMicroDipDuration = new Vector2(0.08f, 0.18f);
        [Tooltip("Intensity multiplier during dip (e.g. 0.92 = -8%).")]
        [Range(0.8f, 1f)]
        [SerializeField] private float potMicroDipMultiplier = 0.92f;
        [Tooltip("Rare micro boosts (per second).")]
        [SerializeField] private float potMicroBoostChancePerSecond = 0.06f;
        [Tooltip("Duration of micro boosts (seconds).")]
        [SerializeField] private Vector2 potMicroBoostDuration = new Vector2(0.08f, 0.16f);
        [Tooltip("Intensity multiplier during boost (e.g. 1.05 = +5%).")]
        [Range(1f, 1.2f)]
        [SerializeField] private float potMicroBoostMultiplier = 1.05f;

        [Header("Startup Sequence (lab tube)")]
        [Tooltip("Initial fast flicker duration (seconds).")]
        [SerializeField] private Vector2 bootFlickerDuration = new Vector2(0.9f, 1.6f);
        [Tooltip("Short stable moments during boot flicker.")]
        [SerializeField] private Vector2 bootStableChunks = new Vector2(0.08f, 0.22f);
        [Tooltip("Fast flicker noise parameters during boot.")]
        [SerializeField] private float bootNoiseSpeed = 18f;
        [SerializeField] private float bootNoiseAmount = 1.0f;

        [Tooltip("After boot: a short mostly-stable phase (seconds).")]
        [SerializeField] private Vector2 warmStableDuration = new Vector2(0.5f, 1.1f);
        [Tooltip("After warm stable: a few adjustment flickers (count).")]
        [SerializeField] private Vector2Int adjustFlickerCount = new Vector2Int(2, 5);
        [Tooltip("Intensity for stable-low phase (seconds).")]
        [SerializeField] private float stableLowIntensity = 1.6f;
        [SerializeField] private Vector2 stableLowDuration = new Vector2(0.7f, 1.5f);
        [Tooltip("Ramp to this stable high intensity, then stay on.")]
        [SerializeField] private float stableHighIntensity = 4.8f;
        [SerializeField] private float rampUpDuration = 1.2f;

        private Light2D _light;
        private float _t;
        private float _targetIntensity;
        private float _eventEndTime;
        private EventMode _eventMode;

        private float _baseOuterRadius;
        private float _baseInnerRadius;

        // Startup sequence state
        private float _phaseT;
        private float _phaseEnd;
        private float _stableChunkEnd;
        private bool _inStableChunk;
        private int _adjustLeft;
        private float _rampStartIntensity;
        private FlickerMode _lastMode;

        private bool _randomInStablePause;
        private float _randomStablePauseEndTime;
        private float _potMicroEventEndTime;
        private float _potMicroMultiplier = 1f;

        private enum EventMode
        {
            None = 0,
            Dropout = 1,
            Burst = 2
        }

        private enum Phase
        {
            BootFlicker = 0,
            WarmStable = 1,
            AdjustFlicker = 2,
            StableLow = 3,
            RampUp = 4,
            StableHigh = 5
        }

        private Phase _phase = Phase.BootFlicker;

        private void Awake()
        {
            _light = GetComponent<Light2D>();
            _targetIntensity = Mathf.Clamp(baseIntensity, minIntensity, maxIntensity);
            _light.intensity = Mathf.Clamp(_light.intensity, minIntensity, maxIntensity);

            _baseOuterRadius = _light.pointLightOuterRadius;
            _baseInnerRadius = _light.pointLightInnerRadius;
            _lastMode = mode;
        }

        private void OnEnable()
        {
            // Ensure references also during domain reload / enable toggles.
            if (_light == null)
            {
                _light = GetComponent<Light2D>();
                _baseOuterRadius = _light.pointLightOuterRadius;
                _baseInnerRadius = _light.pointLightInnerRadius;
            }

            // Avoid identical flicker across lights if multiple exist
            if (Mathf.Approximately(noiseSeed, 0f))
                noiseSeed = Random.value * 10f;

            if (mode == FlickerMode.StartupSequence)
                ResetSequence();

            _lastMode = mode;
        }

        private void Update()
        {
            if (_light == null)
                _light = GetComponent<Light2D>();

            // If the user changes mode at runtime, ensure the sequence restarts (otherwise it may look "stuck").
            if (_lastMode != mode)
            {
                if (mode == FlickerMode.StartupSequence)
                    ResetSequence();

                _lastMode = mode;
            }

            float dt = Time.deltaTime;
            _t += dt;

            if (mode == FlickerMode.StartupSequence)
                TickStartupSequence(dt);
            else if (mode == FlickerMode.PotGentle)
                TickPotGentle(dt);
            else
                TickRandomFlicker(dt);

            // Exponential smoothing (stable across framerate)
            float k = 1f - Mathf.Exp(-response * dt);
            _light.intensity = Mathf.Lerp(_light.intensity, _targetIntensity, k);

            if (flickerRadius)
            {
                float rn = Mathf.PerlinNoise(_t * (noiseSpeed * 0.75f), noiseSeed + 7.77f) * 2f - 1f;
                float rMul = 1f + rn * radiusNoiseAmount;
                _light.pointLightOuterRadius = _baseOuterRadius * rMul;
                _light.pointLightInnerRadius = _baseInnerRadius * rMul;
            }
        }

        [ContextMenu("Restart Startup Sequence")]
        public void RestartSequence()
        {
            mode = FlickerMode.StartupSequence;
            ResetSequence();
        }

        private void ResetSequence()
        {
            _phase = Phase.BootFlicker;
            _phaseT = 0f;
            _phaseEnd = Random.Range(bootFlickerDuration.x, bootFlickerDuration.y);
            _stableChunkEnd = 0f;
            _inStableChunk = false;
            _adjustLeft = Random.Range(adjustFlickerCount.x, adjustFlickerCount.y + 1);
            _rampStartIntensity = Mathf.Clamp(baseIntensity, minIntensity, maxIntensity);
        }

        private void TickStartupSequence(float dt)
        {
            _phaseT += dt;

            switch (_phase)
            {
                case Phase.BootFlicker:
                {
                    // Alternate fast flicker with short stable chunks (feels like tube trying to start).
                    if (_inStableChunk && Time.time >= _stableChunkEnd)
                        _inStableChunk = false;

                    if (!_inStableChunk && Random.value < 0.18f * dt)
                    {
                        _inStableChunk = true;
                        _stableChunkEnd = Time.time + Random.Range(bootStableChunks.x, bootStableChunks.y);
                    }

                    if (_inStableChunk)
                    {
                        _targetIntensity = Mathf.Clamp(baseIntensity, minIntensity, maxIntensity);
                    }
                    else
                    {
                        float n = Mathf.PerlinNoise(_t * bootNoiseSpeed, noiseSeed) * 2f - 1f;
                        float flick = baseIntensity + n * bootNoiseAmount;

                        // Quick dropouts sprinkled in
                        if (Random.value < dropoutChancePerSecond * dt * 1.4f)
                            flick = dropoutIntensity;

                        _targetIntensity = Mathf.Clamp(flick, minIntensity, maxIntensity);
                    }

                    if (_phaseT >= _phaseEnd)
                    {
                        _phase = Phase.WarmStable;
                        _phaseT = 0f;
                        _phaseEnd = Random.Range(warmStableDuration.x, warmStableDuration.y);
                    }
                    break;
                }

                case Phase.WarmStable:
                {
                    // Mostly stable, with very mild shimmer.
                    float n = Mathf.PerlinNoise(_t * (noiseSpeed * 0.6f), noiseSeed + 1.23f) * 2f - 1f;
                    _targetIntensity = Mathf.Clamp(baseIntensity + n * (noiseAmount * 0.15f), minIntensity, maxIntensity);

                    if (_phaseT >= _phaseEnd)
                    {
                        _phase = Phase.AdjustFlicker;
                        _phaseT = 0f;
                    }
                    break;
                }

                case Phase.AdjustFlicker:
                {
                    // Few discrete adjustment flickers, then settle.
                    float n = Mathf.PerlinNoise(_t * (noiseSpeed * 1.4f), noiseSeed + 2.34f) * 2f - 1f;
                    float flick = baseIntensity + n * (noiseAmount * 0.65f);

                    if (Random.value < dropoutChancePerSecond * dt * 0.6f)
                        flick = dropoutIntensity;
                    else if (Random.value < burstChancePerSecond * dt * 0.35f)
                        flick *= burstMultiplier;

                    _targetIntensity = Mathf.Clamp(flick, minIntensity, maxIntensity);

                    // Count down flickers by using short time windows
                    if (_phaseT >= 0.22f)
                    {
                        _phaseT = 0f;
                        _adjustLeft--;
                    }

                    if (_adjustLeft <= 0)
                    {
                        _phase = Phase.StableLow;
                        _phaseT = 0f;
                        _phaseEnd = Random.Range(stableLowDuration.x, stableLowDuration.y);
                    }
                    break;
                }

                case Phase.StableLow:
                {
                    _targetIntensity = Mathf.Clamp(stableLowIntensity, minIntensity, maxIntensity);
                    if (_phaseT >= _phaseEnd)
                    {
                        _phase = Phase.RampUp;
                        _phaseT = 0f;
                        _rampStartIntensity = _light.intensity;
                    }
                    break;
                }

                case Phase.RampUp:
                {
                    float t = Mathf.Clamp01(_phaseT / Mathf.Max(0.01f, rampUpDuration));
                    float eased = 1f - Mathf.Pow(1f - t, 3f);
                    _targetIntensity = Mathf.Lerp(_rampStartIntensity, stableHighIntensity, eased);
                    _targetIntensity = Mathf.Clamp(_targetIntensity, minIntensity, maxIntensity);

                    if (t >= 1f)
                    {
                        _phase = Phase.StableHigh;
                        _phaseT = 0f;
                    }
                    break;
                }

                case Phase.StableHigh:
                default:
                {
                    _targetIntensity = Mathf.Clamp(stableHighIntensity, minIntensity, maxIntensity);
                    break;
                }
            }
        }

        private void TickRandomFlicker(float dt)
        {
            if (randomEnableStablePauses)
            {
                if (_randomInStablePause)
                {
                    if (Time.time >= _randomStablePauseEndTime)
                    {
                        _randomInStablePause = false;
                        _eventMode = EventMode.None;
                    }
                    else
                    {
                        float pauseIntensity = randomStablePauseIntensityOverride > 0f ? randomStablePauseIntensityOverride : baseIntensity;
                        _targetIntensity = Mathf.Clamp(pauseIntensity, minIntensity, maxIntensity);
                        return;
                    }
                }
                else if (Random.value < randomStablePauseChancePerSecond * dt)
                {
                    _randomInStablePause = true;
                    float dur = Random.Range(randomStablePauseDuration.x, randomStablePauseDuration.y);
                    _randomStablePauseEndTime = Time.time + Mathf.Max(0f, dur);
                    _eventMode = EventMode.None;

                    float pauseIntensity = randomStablePauseIntensityOverride > 0f ? randomStablePauseIntensityOverride : baseIntensity;
                    _targetIntensity = Mathf.Clamp(pauseIntensity, minIntensity, maxIntensity);
                    return;
                }
            }

            // Smooth continuous noise (analog feel)
            float n = Mathf.PerlinNoise(_t * noiseSpeed, noiseSeed) * 2f - 1f; // -1..1
            float noisyBase = baseIntensity + n * noiseAmount;

            // End current event
            if (_eventMode != EventMode.None && Time.time >= _eventEndTime)
                _eventMode = EventMode.None;

            // Start new event
            if (_eventMode == EventMode.None)
            {
                // Dropout: goes almost off
                if (Random.value < dropoutChancePerSecond * dt)
                {
                    _eventMode = EventMode.Dropout;
                    _eventEndTime = Time.time + Random.Range(dropoutDuration.x, dropoutDuration.y);
                }
                // Burst: quick intense flash
                else if (Random.value < burstChancePerSecond * dt)
                {
                    _eventMode = EventMode.Burst;
                    _eventEndTime = Time.time + Random.Range(burstDuration.x, burstDuration.y);
                }
            }

            _targetIntensity = _eventMode switch
            {
                EventMode.Dropout => dropoutIntensity,
                EventMode.Burst => noisyBase * burstMultiplier,
                _ => noisyBase
            };

            _targetIntensity = Mathf.Clamp(_targetIntensity, minIntensity, maxIntensity);
        }

        private void TickPotGentle(float dt)
        {
            // Very subtle continuous shimmer
            float n = Mathf.PerlinNoise(_t * potNoiseSpeed, noiseSeed + 9.13f) * 2f - 1f;
            float target = baseIntensity + n * potNoiseAmount;

            // End micro event
            if (_potMicroEventEndTime > 0f && Time.time >= _potMicroEventEndTime)
            {
                _potMicroEventEndTime = 0f;
                _potMicroMultiplier = 1f;
            }

            // Start micro dip or boost
            if (_potMicroEventEndTime <= 0f)
            {
                if (Random.value < potMicroDipChancePerSecond * dt)
                {
                    _potMicroMultiplier = potMicroDipMultiplier;
                    _potMicroEventEndTime = Time.time + Random.Range(potMicroDipDuration.x, potMicroDipDuration.y);
                }
                else if (Random.value < potMicroBoostChancePerSecond * dt)
                {
                    _potMicroMultiplier = potMicroBoostMultiplier;
                    _potMicroEventEndTime = Time.time + Random.Range(potMicroBoostDuration.x, potMicroBoostDuration.y);
                }
            }

            _targetIntensity = Mathf.Clamp(target * _potMicroMultiplier, minIntensity, maxIntensity);
        }
    }
}

