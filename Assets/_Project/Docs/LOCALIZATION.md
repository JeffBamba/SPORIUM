# Localizzazione (multilanguage) – SPORIUM

## Stato attuale

- **Testi in italiano**: tutti i testi utente sono stati unificati in italiano (default).
- **Predisposizione multilanguage**: il progetto può mostrare notifiche e testi in italiano o inglese in base all’impostazione **Lingua** nel menu **Opzioni (ESC)**.

## Architettura

- **`GameLanguage`** (`Core/Localization/GameLanguage.cs`): enum `Auto`, `Italian`, `English`.
- **`GameLanguageSettings`** (`Core/Localization/GameLanguageSettings.cs`): legge/scrive la lingua in **PlayerPrefs** (`Sporium_Language`). Usato da Opzioni e dal sistema notifiche.
- **`LocalizationManager`** (`Core/Localization/LocalizationManager.cs`): punto centrale per chiave → stringa (IT/EN). Estendibile con `Register(key, it, en)`. Per ora usato dove serve; in futuro si possono sostituire stringhe hardcoded con `LocalizationManager.GetString("chiave")`.
- **`NotificationLocalization`** (notifiche): usa `GameLanguageSettings.GetEffectiveLanguage()` quando `OverrideLanguage == Auto`, così le notifiche rispettano la lingua scelta in Opzioni.
- **`OptionsPopupController`** (`UI/MainMenu/OptionsPopupController.cs`): aggiunto automaticamente al popup Opzioni. Se nel pannello Opzioni aggiungi un **TMP_Dropdown** e lo assegni a **Language Dropdown**, il menu mostrerà **Sistema (Auto) / Italiano / English** e salverà la scelta.

## Come aggiungere la scelta Lingua nel pannello Opzioni (Unity)

1. Apri il prefab **Menu** (o la scena del menu principale).
2. Seleziona il GameObject **OptionsPopup**.
3. Aggiungi un **UI > Dropdown - TextMeshPro** (o un sotto-pannello con label "Lingua" e un dropdown).
4. Aggiungi al **OptionsPopup** il componente **Options Popup Controller** (se non c’è già: viene aggiunto a runtime da `MainMenuOptions`).
5. Nel componente **Options Popup Controller**, assegna il dropdown al campo **Language Dropdown**.

In alternativa puoi usare i campi **Btn Language It** e **Btn Language En** (pulsanti per Italiano e English) senza dropdown.

## Estendere le stringhe localizzate

- Per le **notifiche**: usare `NotificationTypeSpec.TemplateIt` / `TemplateEn` e `NotificationLocalization.ResolveTemplate()`.
- Per **nuove stringhe generiche**: aggiungere la coppia in `LocalizationManager.Table` o chiamare `LocalizationManager.Register("chiave", "Testo IT", "Text EN")` e usare `LocalizationManager.GetString("chiave")` nell’UI.

## Persistenza

La lingua è salvata in **PlayerPrefs** (`Sporium_Language`), non nel salvataggio di gioco. Resta quindi uguale per tutte le partite e dopo il riavvio del gioco.
