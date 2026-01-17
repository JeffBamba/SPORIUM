using System;
using UnityEngine;
using Sporae.Dome.PotSystem.Growth;

namespace Sporae.Dome.PotAutomation
{
    /// <summary>
    /// Procedural arm animation for Pot automation actions.
    /// Large arm moves only on Y; small arm moves on X and follows the parent.
    /// </summary>
    public class PotAutomationArmAnimator : MonoBehaviour
    {
        public enum PlantHeightReference
        {
            Center,
            Top,
            Bottom
        }

        public enum ScenicSpeed
        {
            x1 = 1,
            x2 = 2,
            x3 = 3
        }

        [Serializable]
        private struct ArmMotionProfile
        {
            public Vector2 verticalOffsetRange;
            public Vector2 horizontalOffsetRange;
            public Vector2 stepDurationRange;
            public Vector2 longDurationRange;
            public Vector2 stepDistanceRange;
            public Vector2 longDistanceRange;
            public Vector2 pauseDurationRange;
            [Range(0f, 1f)] public float stepChance;
            [Range(0f, 1f)] public float pauseChance;
            public float microJitterAmplitude;
            public float microJitterSpeed;
        }

        [Header("Arm References")]
        [SerializeField] private Transform largeArm;
        [SerializeField] private Transform smallArm;
        [SerializeField] private bool autoParentSmallArm = true;

        [Header("Plant Anchor (optional)")]
        [SerializeField] private SpriteRenderer plantRenderer;
        [SerializeField] private PlantHeightReference plantHeightReference = PlantHeightReference.Center;
        [SerializeField] private float plantYOffset = 0f;

        [Header("Window Bounds (optional)")]
        [SerializeField] private SpriteRenderer windowBoundsSprite;
        [SerializeField] private Collider2D windowBoundsCollider;
        [SerializeField] private Vector2 windowPadding = new Vector2(0.01f, 0.01f);

        [Header("Ranges (Offsets from rest)")]
        [SerializeField] private Vector2 largeArmLocalYRange = new Vector2(-0.5f, 0.6f);
        [SerializeField] private Vector2 smallArmLocalXRange = new Vector2(-0.45f, 0.45f);
        [SerializeField, Range(0.5f, 2f)] private float rangeMultiplier = 1.35f;
        [SerializeField, Range(0.5f, 2.5f)] private float speedMultiplier = 1.4f;

        [Header("Visibility")]
        [SerializeField] private bool hideWhenIdle = false;
        [SerializeField] private bool keepVisibleOnStop = true;

        [Header("Scenic Speed Overrides (x1/x2/x3)")]
        [SerializeField] private ScenicSpeed plantScenicSpeed = ScenicSpeed.x2;
        [SerializeField] private ScenicSpeed wateringScenicSpeed = ScenicSpeed.x2;
        [SerializeField] private ScenicSpeed ledBlueScenicSpeed = ScenicSpeed.x2;
        [SerializeField] private ScenicSpeed ledRedScenicSpeed = ScenicSpeed.x2;
        [SerializeField] private ScenicSpeed fertilizeScenicSpeed = ScenicSpeed.x1;
        [SerializeField] private ScenicSpeed sprayScenicSpeed = ScenicSpeed.x1;
        [SerializeField] private ScenicSpeed harvestScenicSpeed = ScenicSpeed.x1;
        [SerializeField] private ScenicSpeed uprootScenicSpeed = ScenicSpeed.x1;
        [SerializeField] private ScenicSpeed defaultScenicSpeed = ScenicSpeed.x1;

        [Header("Scenic Duration Overrides (seconds)")]
        [Tooltip("If > 0, limits the total scenic animation duration for Plant.")]
        [SerializeField] private float plantScenicDuration = 3f;
        [Tooltip("If > 0, limits the total scenic animation duration for Watering.")]
        [SerializeField] private float wateringScenicDuration = 3f;
        [Tooltip("If > 0, limits the total scenic animation duration for LED Blue.")]
        [SerializeField] private float ledBlueScenicDuration = 3f;
        [Tooltip("If > 0, limits the total scenic animation duration for LED Red.")]
        [SerializeField] private float ledRedScenicDuration = 3f;
        [Tooltip("If > 0, limits the total scenic animation duration for Fertilize.")]
        [SerializeField] private float fertilizeScenicDuration = 0f;
        [Tooltip("If > 0, limits the total scenic animation duration for Spray.")]
        [SerializeField] private float sprayScenicDuration = 0f;
        [Tooltip("If > 0, limits the total scenic animation duration for Harvest.")]
        [SerializeField] private float harvestScenicDuration = 60f;
        [Tooltip("If > 0, limits the total scenic animation duration for Uproot.")]
        [SerializeField] private float uprootScenicDuration = 0f;
        [Tooltip("If > 0, limits the total scenic animation duration for all other actions.")]
        [SerializeField] private float defaultScenicDuration = 0f;

        [Header("Action Profiles")]
        [SerializeField] private ArmMotionProfile plantProfile = new ArmMotionProfile
        {
            verticalOffsetRange = new Vector2(-0.12f, 0.22f),
            horizontalOffsetRange = new Vector2(-0.32f, 0.32f),
            stepDurationRange = new Vector2(0.08f, 0.18f),
            longDurationRange = new Vector2(0.6f, 1.2f),
            stepDistanceRange = new Vector2(0.02f, 0.06f),
            longDistanceRange = new Vector2(0.15f, 0.30f),
            pauseDurationRange = new Vector2(0.05f, 0.25f),
            stepChance = 0.65f,
            pauseChance = 0.2f,
            microJitterAmplitude = 0.004f,
            microJitterSpeed = 12f
        };

        [SerializeField] private ArmMotionProfile fertilizeProfile = new ArmMotionProfile
        {
            verticalOffsetRange = new Vector2(-0.18f, 0.28f),
            horizontalOffsetRange = new Vector2(-0.38f, 0.38f),
            stepDurationRange = new Vector2(0.06f, 0.16f),
            longDurationRange = new Vector2(0.7f, 1.4f),
            stepDistanceRange = new Vector2(0.03f, 0.07f),
            longDistanceRange = new Vector2(0.18f, 0.34f),
            pauseDurationRange = new Vector2(0.05f, 0.22f),
            stepChance = 0.55f,
            pauseChance = 0.15f,
            microJitterAmplitude = 0.0035f,
            microJitterSpeed = 10f
        };

        [SerializeField] private ArmMotionProfile sprayProfile = new ArmMotionProfile
        {
            verticalOffsetRange = new Vector2(-0.10f, 0.20f),
            horizontalOffsetRange = new Vector2(-0.40f, 0.40f),
            stepDurationRange = new Vector2(0.05f, 0.12f),
            longDurationRange = new Vector2(0.5f, 1.0f),
            stepDistanceRange = new Vector2(0.02f, 0.05f),
            longDistanceRange = new Vector2(0.20f, 0.40f),
            pauseDurationRange = new Vector2(0.04f, 0.18f),
            stepChance = 0.7f,
            pauseChance = 0.25f,
            microJitterAmplitude = 0.0045f,
            microJitterSpeed = 14f
        };

        [SerializeField] private ArmMotionProfile waterProfile = new ArmMotionProfile
        {
            verticalOffsetRange = new Vector2(-0.16f, 0.24f),
            horizontalOffsetRange = new Vector2(-0.30f, 0.30f),
            stepDurationRange = new Vector2(0.07f, 0.16f),
            longDurationRange = new Vector2(0.6f, 1.1f),
            stepDistanceRange = new Vector2(0.02f, 0.05f),
            longDistanceRange = new Vector2(0.12f, 0.26f),
            pauseDurationRange = new Vector2(0.06f, 0.24f),
            stepChance = 0.6f,
            pauseChance = 0.2f,
            microJitterAmplitude = 0.003f,
            microJitterSpeed = 11f
        };

        [SerializeField] private ArmMotionProfile lightProfile = new ArmMotionProfile
        {
            verticalOffsetRange = new Vector2(-0.08f, 0.18f),
            horizontalOffsetRange = new Vector2(-0.28f, 0.28f),
            stepDurationRange = new Vector2(0.08f, 0.20f),
            longDurationRange = new Vector2(0.8f, 1.6f),
            stepDistanceRange = new Vector2(0.02f, 0.06f),
            longDistanceRange = new Vector2(0.12f, 0.24f),
            pauseDurationRange = new Vector2(0.08f, 0.28f),
            stepChance = 0.45f,
            pauseChance = 0.3f,
            microJitterAmplitude = 0.0025f,
            microJitterSpeed = 9f
        };

        [SerializeField] private ArmMotionProfile harvestProfile = new ArmMotionProfile
        {
            verticalOffsetRange = new Vector2(-0.20f, 0.32f),
            horizontalOffsetRange = new Vector2(-0.36f, 0.36f),
            stepDurationRange = new Vector2(0.05f, 0.14f),
            longDurationRange = new Vector2(0.7f, 1.3f),
            stepDistanceRange = new Vector2(0.03f, 0.08f),
            longDistanceRange = new Vector2(0.18f, 0.34f),
            pauseDurationRange = new Vector2(0.04f, 0.16f),
            stepChance = 0.7f,
            pauseChance = 0.15f,
            microJitterAmplitude = 0.005f,
            microJitterSpeed = 13f
        };

        [SerializeField] private ArmMotionProfile uprootProfile = new ArmMotionProfile
        {
            verticalOffsetRange = new Vector2(-0.26f, 0.34f),
            horizontalOffsetRange = new Vector2(-0.34f, 0.34f),
            stepDurationRange = new Vector2(0.06f, 0.14f),
            longDurationRange = new Vector2(0.9f, 1.6f),
            stepDistanceRange = new Vector2(0.03f, 0.08f),
            longDistanceRange = new Vector2(0.20f, 0.36f),
            pauseDurationRange = new Vector2(0.05f, 0.18f),
            stepChance = 0.55f,
            pauseChance = 0.15f,
            microJitterAmplitude = 0.006f,
            microJitterSpeed = 12f
        };

        [SerializeField] private ArmMotionProfile defaultProfile = new ArmMotionProfile
        {
            verticalOffsetRange = new Vector2(-0.18f, 0.26f),
            horizontalOffsetRange = new Vector2(-0.32f, 0.32f),
            stepDurationRange = new Vector2(0.07f, 0.18f),
            longDurationRange = new Vector2(0.6f, 1.2f),
            stepDistanceRange = new Vector2(0.02f, 0.06f),
            longDistanceRange = new Vector2(0.14f, 0.30f),
            pauseDurationRange = new Vector2(0.06f, 0.22f),
            stepChance = 0.6f,
            pauseChance = 0.2f,
            microJitterAmplitude = 0.0035f,
            microJitterSpeed = 11f
        };

        private struct AxisMotion
        {
            public float start;
            public float target;
            public float duration;
            public float elapsed;
            public bool isStep;
            public bool isPaused;
        }

        private Vector3 _largeArmRestLocal;
        private Vector3 _smallArmRestLocal;
        private AxisMotion _yMotion;
        private AxisMotion _xMotion;
        private bool _isAnimating;
        private ArmMotionProfile _profile;
        private System.Random _rng;
        private float _scenicEndTime;
        private bool _scenicDurationActive;

        private void Awake()
        {
            if (largeArm == null || smallArm == null)
            {
                var renders = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
                if (largeArm == null && renders.Length > 0) largeArm = renders[0].transform;
                if (smallArm == null && renders.Length > 1) smallArm = renders[1].transform;
            }

            if (plantRenderer == null)
            {
                var windowContent = transform.Find("WindowContent");
                if (windowContent != null)
                    plantRenderer = windowContent.GetComponent<SpriteRenderer>();
            }

            if (largeArm != null) _largeArmRestLocal = largeArm.localPosition;
            if (smallArm != null) _smallArmRestLocal = smallArm.localPosition;

            if (autoParentSmallArm && largeArm != null && smallArm != null && smallArm.parent != largeArm)
            {
                smallArm.SetParent(largeArm, worldPositionStays: true);
                _smallArmRestLocal = smallArm.localPosition;
            }

            ApplyIdleVisibility();
        }

        private void Update()
        {
            if (!_isAnimating || largeArm == null || smallArm == null)
                return;

            float dt = Time.deltaTime;
            UpdateAxisMotion(ref _yMotion, dt, isVertical: true);
            UpdateAxisMotion(ref _xMotion, dt, isVertical: false);

            if (_scenicDurationActive && Time.time >= _scenicEndTime)
            {
                StopAnimation();
                return;
            }

            float jitter = _profile.microJitterAmplitude > 0f
                ? Mathf.Sin(Time.time * _profile.microJitterSpeed) * _profile.microJitterAmplitude
                : 0f;

            var parent = largeArm.parent != null ? largeArm.parent : transform;
            float baseY = GetPlantBaseLocalY(parent);
            float yOffset = Mathf.Lerp(_yMotion.start, _yMotion.target, GetMotionT(ref _yMotion));
            float desiredY = baseY + yOffset + jitter;
            desiredY = ClampVertical(desiredY, parent, baseY);

            var largePos = largeArm.localPosition;
            largePos.y = desiredY;
            largeArm.localPosition = largePos;

            float xOffset = Mathf.Lerp(_xMotion.start, _xMotion.target, GetMotionT(ref _xMotion));
            float desiredX = _smallArmRestLocal.x + xOffset + jitter * 0.6f;
            desiredX = ClampHorizontal(desiredX, smallArm.parent != null ? smallArm.parent : transform);
            var smallPos = smallArm.localPosition;
            smallPos.x = desiredX;
            smallArm.localPosition = smallPos;

        }

        public void StartActionAnimation(PotAutomationRunner.AutomationActionType actionType, string potIdSeed = null)
        {
            if (largeArm == null || smallArm == null)
                return;

            _profile = GetProfile(actionType);
            float scenicSpeed = GetScenicSpeedMultiplier(actionType);
            _profile = ApplyScenicSpeed(_profile, scenicSpeed);
            float scenicDuration = GetScenicDurationSeconds(actionType);
            if (scenicDuration > 0f)
            {
                _scenicDurationActive = true;
                _scenicEndTime = Time.time + scenicDuration;
            }
            else
            {
                _scenicDurationActive = false;
                _scenicEndTime = 0f;
            }
            int seed = Environment.TickCount ^ (int)actionType ^ (potIdSeed != null ? potIdSeed.GetHashCode() : 0);
            _rng = new System.Random(seed);

            _isAnimating = true;
            ApplyIdleVisibility();

            var parent = largeArm.parent != null ? largeArm.parent : transform;
            float baseY = GetPlantBaseLocalY(parent);
            float currentYOffset = largeArm.localPosition.y - baseY;
            float currentXOffset = smallArm.localPosition.x - _smallArmRestLocal.x;
            ResetAxis(ref _yMotion, currentYOffset, isVertical: true);
            ResetAxis(ref _xMotion, currentXOffset, isVertical: false);

        }

        /// <summary>
        /// Returns the configured scenic duration for the given action type (seconds).
        /// If 0, the animation is not duration-limited and will run until StopAnimation is called.
        /// </summary>
        public float GetConfiguredScenicDurationSeconds(PotAutomationRunner.AutomationActionType actionType)
        {
            return Mathf.Max(0f, GetScenicDurationSeconds(actionType));
        }

        public void StopAnimation()
        {
            _isAnimating = false;
            _rng = null;
            _scenicDurationActive = false;
            _scenicEndTime = 0f;

            if (largeArm != null)
            {
                var parent = largeArm.parent != null ? largeArm.parent : transform;
                float baseY = GetPlantBaseLocalY(parent);
                float bottomY = GetBottomRestY(parent, baseY);
                var largePos = largeArm.localPosition;
                largePos.y = bottomY;
                largeArm.localPosition = largePos;
            }
            if (smallArm != null)
            {
                var smallPos = smallArm.localPosition;
                smallPos.x = _smallArmRestLocal.x;
                smallArm.localPosition = smallPos;
                smallArm.gameObject.SetActive(true);
            }
            if (largeArm != null)
                largeArm.gameObject.SetActive(true);

            ApplyIdleVisibility();
        }

        private float GetBottomRestY(Transform space, float baseY)
        {
            float bottom = baseY + largeArmLocalYRange.x;
            if (TryGetWindowLocalBounds(space, out var min, out var max))
                bottom = Mathf.Max(bottom, min.y + windowPadding.y);
            return bottom;
        }

        private void ApplyIdleVisibility()
        {
            if (!hideWhenIdle) return;
            bool show = _isAnimating || keepVisibleOnStop;
            if (largeArm != null) largeArm.gameObject.SetActive(show);
            if (smallArm != null) smallArm.gameObject.SetActive(show);

        }

        private float GetPlantBaseLocalY(Transform space)
        {
            if (plantRenderer != null && plantRenderer.enabled && plantRenderer.sprite != null)
            {
                var bounds = plantRenderer.bounds;
                float worldY = bounds.center.y;
                if (plantHeightReference == PlantHeightReference.Top) worldY = bounds.max.y;
                if (plantHeightReference == PlantHeightReference.Bottom) worldY = bounds.min.y;
                worldY += plantYOffset;
                return space.InverseTransformPoint(new Vector3(bounds.center.x, worldY, bounds.center.z)).y;
            }
            return _largeArmRestLocal.y;
        }

        private void UpdateAxisMotion(ref AxisMotion motion, float dt, bool isVertical)
        {
            motion.elapsed += dt;
            if (motion.elapsed < motion.duration)
                return;

            motion.start = motion.target;
            motion.isPaused = Roll(_profile.pauseChance);
            motion.isStep = Roll(_profile.stepChance);

            if (motion.isPaused)
            {
                motion.duration = Range(_profile.pauseDurationRange);
                motion.target = motion.start;
            }
            else
            {
                float distance = motion.isStep ? Range(_profile.stepDistanceRange) : Range(_profile.longDistanceRange);
                distance *= Mathf.Max(0.1f, rangeMultiplier);
                float sign = Roll(0.5f) ? 1f : -1f;
                motion.target = motion.start + sign * distance;
                motion.duration = motion.isStep ? Range(_profile.stepDurationRange) : Range(_profile.longDurationRange);
                motion.duration /= Mathf.Max(0.1f, speedMultiplier);
                motion.target = ClampAxisTarget(motion.target, isVertical);
            }

            motion.elapsed = 0f;
        }

        private void ResetAxis(ref AxisMotion motion, float startValue, bool isVertical)
        {
            motion.start = startValue;
            motion.target = startValue;
            motion.duration = 0f;
            motion.elapsed = 0f;
            motion.isStep = false;
            motion.isPaused = false;
            UpdateAxisMotion(ref motion, 0f, isVertical);
        }

        private float GetMotionT(ref AxisMotion motion)
        {
            if (motion.duration <= 0f) return 1f;
            float t = Mathf.Clamp01(motion.elapsed / motion.duration);
            if (motion.isPaused) return 0f;
            if (motion.isStep) return t < 0.3f ? t / 0.3f : 1f;
            return t * t * (3f - 2f * t);
        }

        private float GetScenicSpeedMultiplier(PotAutomationRunner.AutomationActionType type)
        {
            ScenicSpeed speed = type switch
            {
                PotAutomationRunner.AutomationActionType.Plant => plantScenicSpeed,
                PotAutomationRunner.AutomationActionType.HydrationToggle => wateringScenicSpeed,
                PotAutomationRunner.AutomationActionType.LedBlueToggle => ledBlueScenicSpeed,
                PotAutomationRunner.AutomationActionType.LedRedToggle => ledRedScenicSpeed,
                PotAutomationRunner.AutomationActionType.Fertilize => fertilizeScenicSpeed,
                PotAutomationRunner.AutomationActionType.Spray => sprayScenicSpeed,
                PotAutomationRunner.AutomationActionType.Prune => sprayScenicSpeed,
                PotAutomationRunner.AutomationActionType.Harvest => harvestScenicSpeed,
                PotAutomationRunner.AutomationActionType.Uproot => uprootScenicSpeed,
                _ => defaultScenicSpeed
            };

            return Mathf.Max(1f, (float)speed);
        }

        private float GetScenicDurationSeconds(PotAutomationRunner.AutomationActionType type)
        {
            return type switch
            {
                PotAutomationRunner.AutomationActionType.Plant => plantScenicDuration,
                PotAutomationRunner.AutomationActionType.HydrationToggle => wateringScenicDuration,
                PotAutomationRunner.AutomationActionType.LedBlueToggle => ledBlueScenicDuration,
                PotAutomationRunner.AutomationActionType.LedRedToggle => ledRedScenicDuration,
                PotAutomationRunner.AutomationActionType.Fertilize => fertilizeScenicDuration,
                PotAutomationRunner.AutomationActionType.Spray => sprayScenicDuration,
                PotAutomationRunner.AutomationActionType.Prune => sprayScenicDuration,
                PotAutomationRunner.AutomationActionType.Harvest => harvestScenicDuration,
                PotAutomationRunner.AutomationActionType.Uproot => uprootScenicDuration,
                _ => defaultScenicDuration
            };
        }

        private static ArmMotionProfile ApplyScenicSpeed(ArmMotionProfile profile, float speed)
        {
            float s = Mathf.Max(1f, speed);
            profile.stepDurationRange = profile.stepDurationRange / s;
            profile.longDurationRange = profile.longDurationRange / s;
            profile.pauseDurationRange = profile.pauseDurationRange / s;
            return profile;
        }

        private float ClampAxisTarget(float target, bool isVertical)
        {
            if (isVertical)
            {
                float min = Mathf.Max(largeArmLocalYRange.x, _profile.verticalOffsetRange.x);
                float max = Mathf.Min(largeArmLocalYRange.y, _profile.verticalOffsetRange.y);
                if (min > max)
                {
                    min = largeArmLocalYRange.x;
                    max = largeArmLocalYRange.y;
                }
                return Mathf.Clamp(target, min, max);
            }

            float hMin = Mathf.Max(smallArmLocalXRange.x, _profile.horizontalOffsetRange.x);
            float hMax = Mathf.Min(smallArmLocalXRange.y, _profile.horizontalOffsetRange.y);
            if (hMin > hMax)
            {
                hMin = smallArmLocalXRange.x;
                hMax = smallArmLocalXRange.y;
            }
            return Mathf.Clamp(target, hMin, hMax);
        }

        private float ClampVertical(float localY, Transform space, float baseY)
        {
            float clamped = Mathf.Clamp(localY, baseY + largeArmLocalYRange.x, baseY + largeArmLocalYRange.y);
            if (TryGetWindowLocalBounds(space, out var min, out var max))
            {
                clamped = Mathf.Clamp(clamped, min.y + windowPadding.y, max.y - windowPadding.y);
            }

            return clamped;
        }

        private float ClampHorizontal(float localX, Transform space)
        {
            float clamped = Mathf.Clamp(localX, _smallArmRestLocal.x + smallArmLocalXRange.x, _smallArmRestLocal.x + smallArmLocalXRange.y);
            if (TryGetWindowLocalBounds(space, out var min, out var max))
            {
                clamped = Mathf.Clamp(clamped, min.x + windowPadding.x, max.x - windowPadding.x);
            }
            return clamped;
        }

        private bool TryGetWindowLocalBounds(Transform space, out Vector2 min, out Vector2 max)
        {
            min = Vector2.zero;
            max = Vector2.zero;

            Bounds bounds;
            if (windowBoundsCollider != null)
            {
                bounds = windowBoundsCollider.bounds;
            }
            else if (windowBoundsSprite != null)
            {
                bounds = windowBoundsSprite.bounds;
            }
            else
            {
                return false;
            }

            Vector3 localMin = space.InverseTransformPoint(bounds.min);
            Vector3 localMax = space.InverseTransformPoint(bounds.max);
            min = new Vector2(localMin.x, localMin.y);
            max = new Vector2(localMax.x, localMax.y);

            // Degenerate bounds (zero size) -> skip window clamp.
            if (Mathf.Abs(max.x - min.x) < 0.001f || Mathf.Abs(max.y - min.y) < 0.001f)
                return false;

            return true;
        }

        private bool Roll(float chance)
        {
            if (_rng == null) return UnityEngine.Random.value < chance;
            return _rng.NextDouble() < chance;
        }

        private float Range(Vector2 range)
        {
            if (_rng == null) return UnityEngine.Random.Range(range.x, range.y);
            return (float)(_rng.NextDouble() * (range.y - range.x) + range.x);
        }

        private ArmMotionProfile GetProfile(PotAutomationRunner.AutomationActionType actionType)
        {
            return actionType switch
            {
                PotAutomationRunner.AutomationActionType.Plant => plantProfile,
                PotAutomationRunner.AutomationActionType.Fertilize => fertilizeProfile,
                PotAutomationRunner.AutomationActionType.Spray => sprayProfile,
                PotAutomationRunner.AutomationActionType.Prune => sprayProfile,
                PotAutomationRunner.AutomationActionType.HydrationToggle => waterProfile,
                PotAutomationRunner.AutomationActionType.LedRedToggle => lightProfile,
                PotAutomationRunner.AutomationActionType.LedBlueToggle => lightProfile,
                PotAutomationRunner.AutomationActionType.Harvest => harvestProfile,
                PotAutomationRunner.AutomationActionType.Uproot => uprootProfile,
                _ => defaultProfile
            };
        }
    }
}
