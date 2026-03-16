using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sporae.Core.Localization;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace _Project
{
    /// <summary>
    /// Controller del popup Opzioni (menu ESC).
    /// Gestisce la selezione della lingua e la persistenza in GameLanguageSettings.
    /// Per abilitare la scelta lingua: aggiungi un TMP_Dropdown nel pannello Opzioni
    /// e assegnalo a _languageDropdown; oppure usa i pulsanti _btnLanguageIt / _btnLanguageEn.
    /// </summary>
    public class OptionsPopupController : MonoBehaviour
    {
        [Header("Lingua (multilanguage)")]
        [Tooltip("Opzionale: dropdown Lingua nel pannello Opzioni. Se assegnato, viene popolato con Auto / Italiano / English.")]
        [SerializeField] private TMP_Dropdown _languageDropdown;

        [Tooltip("Opzionale: pulsante per imposta Italiano (alternativa al dropdown).")]
        [SerializeField] private Button _btnLanguageIt;

        [Tooltip("Opzionale: pulsante per imposta English (alternativa al dropdown).")]
        [SerializeField] private Button _btnLanguageEn;

        private void OnEnable()
        {
            if (_languageDropdown != null)
            {
                SetupDropdown();
                _languageDropdown.onValueChanged.RemoveListener(OnLanguageDropdownChanged);
                _languageDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);
            }

            if (_btnLanguageIt != null)
                _btnLanguageIt.onClick.AddListener(() => SetLanguage(GameLanguage.Italian));

            if (_btnLanguageEn != null)
                _btnLanguageEn.onClick.AddListener(() => SetLanguage(GameLanguage.English));
        }

        private void OnDisable()
        {
            if (_languageDropdown != null)
                _languageDropdown.onValueChanged.RemoveListener(OnLanguageDropdownChanged);
            if (_btnLanguageIt != null)
                _btnLanguageIt.onClick.RemoveAllListeners();
            if (_btnLanguageEn != null)
                _btnLanguageEn.onClick.RemoveAllListeners();
        }

        private void SetupDropdown()
        {
            _languageDropdown.ClearOptions();
            _languageDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Sistema (Auto)",
                "Italiano",
                "English"
            });

            var current = GameLanguageSettings.CurrentLanguage;
            int index = current switch
            {
                GameLanguage.Auto => 0,
                GameLanguage.Italian => 1,
                GameLanguage.English => 2,
                _ => 0
            };
            _languageDropdown.SetValueWithoutNotify(index);
        }

        private void OnLanguageDropdownChanged(int index)
        {
            var lang = index switch
            {
                0 => GameLanguage.Auto,
                1 => GameLanguage.Italian,
                2 => GameLanguage.English,
                _ => GameLanguage.Auto
            };
            SetLanguage(lang);
        }

        private void SetLanguage(GameLanguage lang)
        {
            GameLanguageSettings.CurrentLanguage = lang;

            // Sincronizza il sottosistema notifiche (usa la stessa lingua)
            NotificationLocalization.OverrideLanguage = lang switch
            {
                GameLanguage.Italian => NotificationLanguage.It,
                GameLanguage.English => NotificationLanguage.En,
                _ => NotificationLanguage.Auto
            };
        }
    }
}
