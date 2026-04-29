using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using _Project.Sporae.Core;
using Sporae.UI.UIToolkit.FoodRoom;

namespace _Project
{
    [RequireComponent(typeof(Interactable))]
    public class FoodSynthMachine : MonoBehaviour
    {
        private const string BaseChildName = "Base";
        private const string ArmChildName = "Arm_Animated";
        private const string DisplayChildName = "Display_Animated";

        [SerializeField] private FoodRoomPanelController _foodRoomPanel;

        [Header("Rendering — allinea alla cucina in SCN_VaultMap")]
        [Tooltip("Applica ordinamenti SpriteRenderer fissi così copie / duplicati non finiscono in fascia 20+ per errore genitore.")]
        [SerializeField] private bool _applyCanonicalSpriteSorting = true;
        [SerializeField] private int _sortOrderBase = 0;
        [SerializeField] private int _sortOrderArm = 1;
        [SerializeField] private int _sortOrderDisplay = 2;

        private Interactable _interactable;
        private static bool _agentLoggedMv;

        /// <summary>
        /// Hypothesis H_MV: animated sprites + motion vectors can produce temporal fringe/shimmer on some URP paths.
        /// </summary>
        private void AgentMvHypothesisApplyAndLogOnce()
        {
            if (_agentLoggedMv)
                return;
            _agentLoggedMv = true;

            try
            {
                var baseTr = transform.Find(BaseChildName);
                if (baseTr == null)
                    return;

                SpriteRenderer baseSr = baseTr.GetComponent<SpriteRenderer>();
                SpriteRenderer armSr = baseTr.Find(ArmChildName)?.GetComponent<SpriteRenderer>();
                SpriteRenderer dispSr = baseTr.Find(DisplayChildName)?.GetComponent<SpriteRenderer>();

                void ForceNoMv(SpriteRenderer sr)
                {
                    if (sr == null)
                        return;
                    sr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                }

                ForceNoMv(baseSr);
                ForceNoMv(armSr);
                ForceNoMv(dispSr);

                var ms = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                var path = Path.Combine(Application.dataPath, "..", "debug-fcf266.log");

                static string Mode(SpriteRenderer sr) =>
                    sr == null ? "null" : ((int)sr.motionVectorGenerationMode).ToString(CultureInfo.InvariantCulture);

                var line =
                    "{\"sessionId\":\"fcf266\",\"hypothesisId\":\"H_MV\",\"location\":\"FoodSynthMachine.cs\",\"message\":\"sprite_motion_vectors\",\"data\":{\"baseMode\":"
                    + Mode(baseSr) + ",\"armMode\":" + Mode(armSr) + ",\"dispMode\":" + Mode(dispSr)
                    + "},\"timestamp\":" + ms + "}\n";
                File.AppendAllText(path, line);
            }
            catch
            {
                /* ignore agent log failures */
            }
        }

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            if (_foodRoomPanel == null)
                _foodRoomPanel = FindObjectOfType<FoodRoomPanelController>();
        }

        private void Start()
        {
#region agent log
            AgentMvHypothesisApplyAndLogOnce();
#endregion
            // Dopo Awake di tutti i componenti (Interactable, Animator, ecc.): alcuni sistemi
            // possono aver già impostato sortingOrder — ripeti qualche frame come nei log H5 (-999).
            if (_applyCanonicalSpriteSorting)
            {
                ApplyCanonicalSpriteSorting();
                StartCoroutine(ReapplyCanonicalSortingDeferred());
            }
        }

        private IEnumerator ReapplyCanonicalSortingDeferred()
        {
            for (var i = 0; i < 8; i++)
            {
                yield return null;
                ApplyCanonicalSpriteSorting();
            }
        }

        private void ApplyCanonicalSpriteSorting()
        {
            var baseTr = transform.Find(BaseChildName);
            if (baseTr == null)
                return;

            var baseSr = baseTr.GetComponent<SpriteRenderer>();
            if (baseSr != null)
                baseSr.sortingOrder = _sortOrderBase;

            var armSr = baseTr.Find(ArmChildName)?.GetComponent<SpriteRenderer>();
            if (armSr != null)
                armSr.sortingOrder = _sortOrderArm;

            var dispSr = baseTr.Find(DisplayChildName)?.GetComponent<SpriteRenderer>();
            if (dispSr != null)
                dispSr.sortingOrder = _sortOrderDisplay;
        }

        private void OnEnable()
        {
            if (_interactable != null)
                _interactable.OnInteract += OnInteractClicked;
        }

        private void OnDisable()
        {
            if (_interactable != null)
                _interactable.OnInteract -= OnInteractClicked;
        }

        private void OnInteractClicked()
        {
            if (_foodRoomPanel != null)
                _foodRoomPanel.Show();
        }
    }
}
