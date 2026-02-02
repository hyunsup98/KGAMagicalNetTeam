using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DropdownLocalized : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    [SerializeField] private List<LocalizedString> keyOptions;
    [SerializeField] private LocalizedAsset<TMP_FontAsset> localizedFont;
    [SerializeField] private LocalizedAsset<Material> localizedMaterial;

    private int prevIndex = 0;

    private void Start()
    {
        RefreshDropdown();
        RefreshAsset();

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale locale)
    {
        RefreshDropdown();
        RefreshAsset();
    }

    private void RefreshDropdown()
    {
        if (dropdown == null || keyOptions.Count == 0) return;

        prevIndex = dropdown.value;

        dropdown.ClearOptions();

        List<string> options = new List<string>();

        foreach(var key in keyOptions)
        {
            options.Add(key.GetLocalizedString());
        }

        dropdown.AddOptions(options);

        dropdown.value = prevIndex;
    }

    private void RefreshAsset()
    {
        var fontHandle = localizedFont.LoadAssetAsync();
        fontHandle.Completed += (handle) =>
        {
            if(handle.Status == AsyncOperationStatus.Succeeded)
            {
                TMP_FontAsset font = handle.Result;
                dropdown.captionText.font = font;
                dropdown.itemText.font = font;
            }
        };

        var materialHandle = localizedMaterial.LoadAssetAsync();
        materialHandle.Completed += (handle) =>
        {
            if(handle.Status == AsyncOperationStatus.Succeeded)
            {
                Material material = handle.Result;
                dropdown.captionText.fontSharedMaterial = material;
                dropdown.itemText.fontSharedMaterial = material;
            }
        };

        dropdown.captionText.ForceMeshUpdate();
        dropdown.itemText.ForceMeshUpdate();
    }

    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }
}
