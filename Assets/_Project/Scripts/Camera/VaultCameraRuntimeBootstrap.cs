using System.Collections;
using System.Reflection;
using Cinemachine;
using UnityEngine;

namespace Sporae.CameraSystem
{
    /// <summary>
    /// Ensures VaultMap camera starts with deterministic lens/confiner state.
    /// This targets only the attached Virtual Camera and avoids global bootstrap side effects.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public sealed class VaultCameraRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private float _targetOrthographicSize = 5f;
        [SerializeField] private int _waitFramesAfterLive = 1;
        [SerializeField] private bool _disableConfinerAtRuntime = true;

        private CinemachineVirtualCamera _vcam;
        private CinemachineConfiner2D _confiner;
        private bool _applied;

        private void Awake()
        {
            _vcam = GetComponent<CinemachineVirtualCamera>();
            _confiner = GetComponent<CinemachineConfiner2D>();
        }

        private void OnEnable()
        {
            _applied = false;
            StartCoroutine(ApplyWhenLiveRoutine());
        }

        private IEnumerator ApplyWhenLiveRoutine()
        {
            while (!_applied)
            {
                var brain = CinemachineCore.Instance?.FindPotentialTargetBrain(_vcam);
                var isLive = brain != null && IsLiveOnBrain(brain, _vcam);

                if (isLive)
                {
                    for (int i = 0; i < Mathf.Max(0, _waitFramesAfterLive); i++)
                        yield return null;

                    ApplyLensAndRefresh();
                    _applied = true;
                    yield break;
                }

                yield return null;
            }
        }

        private static bool IsLiveOnBrain(CinemachineBrain brain, CinemachineVirtualCamera vcam)
        {
            var active = brain.ActiveVirtualCamera;
            return active != null && active.VirtualCameraGameObject == vcam.gameObject;
        }

        private void ApplyLensAndRefresh()
        {
            if (_vcam == null)
                return;

            var lens = _vcam.m_Lens;
            lens.OrthographicSize = Mathf.Max(0.01f, _targetOrthographicSize);
            _vcam.m_Lens = lens;

            // Force state rebuild on next pipeline evaluation.
            _vcam.PreviousStateIsValid = false;

            if (_confiner != null)
            {
                TryInvokeNoArgs(_confiner, "InvalidateBoundingShapeCache");
                TryInvokeNoArgs(_confiner, "InvalidateLensCache");
                TryInvokeNoArgs(_confiner, "InvalidateCache");

                if (_disableConfinerAtRuntime)
                {
                    _confiner.enabled = false;
                }
            }
        }

        private static void TryInvokeNoArgs(object target, string methodName)
        {
            var mi = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: System.Type.EmptyTypes,
                modifiers: null);

            if (mi != null)
                mi.Invoke(target, null);
        }
    }
}
