using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LocalizationManager : Singleton<LocalizationManager>
{
    private Coroutine localizedCoroutine;

    public void ChangeLanguage(string localeCode)
    {
        if (localizedCoroutine != null)
            localizedCoroutine = null;

        localizedCoroutine = StartCoroutine(ChangeLanguageCoroutine(localeCode));
    }

    private IEnumerator ChangeLanguageCoroutine(string localeCode)
    {

        var selectedLocale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
        LocalizationSettings.SelectedLocale = selectedLocale;

        LocalizationSettings.InitializationOperation.WaitForCompletion();

        yield break;
    }
}
