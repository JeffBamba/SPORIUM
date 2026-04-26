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

        [Tooltip("Se il prefab non ha controlli lingua assegnati, crea una riga runtime Auto / IT / EN.")]
        [SerializeField] private bool _autoCreateLanguageControls = true;

        private Button _btnLanguageAuto;
        private TextMeshProUGUI _languageLabel;
        private TextMeshProUGUI _autoLabel;
        private TextMeshProUGUI _itLabel;
        private TextMeshProUGUI _enLabel;

        private void OnEnable()
        {
            EnsureLanguageControls();

            if (_languageDropdown != null)
            {
                SetupDropdown();
                _languageDropdown.onValueChanged.RemoveListener(OnLanguageDropdownChanged);
                _languageDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);
            }

            if (_btnLanguageAuto != null)
            {
                _btnLanguageAuto.onClick.RemoveListener(OnLanguageAutoClicked);
                _btnLanguageAuto.onClick.AddListener(OnLanguageAutoClicked);
            }

            if (_btnLanguageIt != null)
            {
                _btnLanguageIt.onClick.RemoveListener(OnLanguageItClicked);
                _btnLanguageIt.onClick.AddListener(OnLanguageItClicked);
            }

            if (_btnLanguageEn != null)
            {
                _btnLanguageEn.onClick.RemoveListener(OnLanguageEnClicked);
                _btnLanguageEn.onClick.AddListener(OnLanguageEnClicked);
            }

            RefreshLanguageLabels();
        }

        private void OnDisable()
        {
            if (_languageDropdown != null)
                _languageDropdown.onValueChanged.RemoveListener(OnLanguageDropdownChanged);
            if (_btnLanguageAuto != null)
                _btnLanguageAuto.onClick.RemoveListener(OnLanguageAutoClicked);
            if (_btnLanguageIt != null)
                _btnLanguageIt.onClick.RemoveListener(OnLanguageItClicked);
            if (_btnLanguageEn != null)
                _btnLanguageEn.onClick.RemoveListener(OnLanguageEnClicked);
        }

        private void SetupDropdown()
        {
            _languageDropdown.ClearOptions();
            _languageDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                LocalizationManager.GetString("options.language.auto"),
                LocalizationManager.GetString("options.language.it"),
                LocalizationManager.GetString("options.language.en")
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

        private void OnLanguageAutoClicked() => SetLanguage(GameLanguage.Auto);
        private void OnLanguageItClicked() => SetLanguage(GameLanguage.Italian);
        private void OnLanguageEnClicked() => SetLanguage(GameLanguage.English);

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

            RefreshLanguageLabels();
        }

        private void EnsureLanguageControls()
        {
            if (!_autoCreateLanguageControls)
                return;
            if (_languageDropdown != null || _btnLanguageIt != null || _btnLanguageEn != null)
                return;

            var existing = transform.Find("LanguageRuntimeRow");
            if (existing != null)
            {
                _btnLanguageAuto = existing.Find("BtnAuto")?.GetComponent<Button>();
                _btnLanguageIt = existing.Find("BtnItalian")?.GetComponent<Button>();
                _btnLanguageEn = existing.Find("BtnEnglish")?.GetComponent<Button>();
                CacheRuntimeLabels(existing);
                return;
            }

            var row = new GameObject("LanguageRuntimeRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(transform, false);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 0f);
            rowRect.anchorMax = new Vector2(1f, 0f);
            rowRect.sizeDelta = new Vector2(0f, 42f);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            _languageLabel = CreateLabel(row.transform, "LanguageLabel", 120f);
            _btnLanguageAuto = CreateLanguageButton(row.transform, "BtnAuto", out _autoLabel);
            _btnLanguageIt = CreateLanguageButton(row.transform, "BtnItalian", out _itLabel);
            _btnLanguageEn = CreateLanguageButton(row.transform, "BtnEnglish", out _enLabel);
        }

        private void CacheRuntimeLabels(Transform row)
        {
            _languageLabel = row.Find("LanguageLabel")?.GetComponent<TextMeshProUGUI>();
            _autoLabel = row.Find("BtnAuto/Text")?.GetComponent<TextMeshProUGUI>();
            _itLabel = row.Find("BtnItalian/Text")?.GetComponent<TextMeshProUGUI>();
            _enLabel = row.Find("BtnEnglish/Text")?.GetComponent<TextMeshProUGUI>();
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, float preferredWidth)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
            layout.preferredHeight = 36f;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.fontSize = 18f;
            text.color = Color.white;
            return text;
        }

        private static Button CreateLanguageButton(Transform parent, string name, out TextMeshProUGUI label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = 95f;
            layout.preferredHeight = 36f;
            var image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.12f, 0.16f, 0.95f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            label = textGo.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 16f;
            label.color = Color.white;
            return button;
        }

        private void RefreshLanguageLabels()
        {
            if (_languageDropdown != null && _languageDropdown.options.Count >= 3)
            {
                _languageDropdown.options[0].text = LocalizationManager.GetString("options.language.auto");
                _languageDropdown.options[1].text = LocalizationManager.GetString("options.language.it");
                _languageDropdown.options[2].text = LocalizationManager.GetString("options.language.en");
                _languageDropdown.RefreshShownValue();
            }
            if (_languageLabel != null)
                _languageLabel.text = LocalizationManager.GetString("options.language");
            if (_autoLabel != null)
                _autoLabel.text = LocalizationManager.GetString("options.language.auto");
            if (_itLabel != null)
                _itLabel.text = LocalizationManager.GetString("options.language.it");
            if (_enLabel != null)
                _enLabel.text = LocalizationManager.GetString("options.language.en");
        }
    }
}
