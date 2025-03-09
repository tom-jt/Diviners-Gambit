using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public enum CardZoomType
{
    Hold = 0,
    Hover = 1
}

public class Save
{
    public float BGV;
    public float SFX;
    public CardZoomType zoomType;
    public int theme;

    public Save(float _BGV, float _SFX, CardZoomType _zoomType, int _theme)
    {
        BGV = _BGV;
        SFX = _SFX;
        zoomType = _zoomType;
        theme = _theme;
    }
}

public class MenuSettingsManager : MonoBehaviour
{
    public static Func<Save> GetSave;
    private Save save;

    [SerializeField]
    private TMP_Dropdown zoomDropdown;
    [SerializeField]
    private TMP_Dropdown themeDropdown;
    [SerializeField]
    private Slider bgvSlider;
    [SerializeField]
    private AudioSource bgvSource;
    [SerializeField]
    private Slider sfxSlider;
    [SerializeField]
    private AudioSource sfxSource;
    [SerializeField]
    private GameObject settingsPanel;
    [SerializeField]
    private Button settingsButton;
    [SerializeField]
    private Button exitSettingsButton;

    [Header("New Save Defaults")]
    float defBgm = 0.7f;
    float defSfx = 0.4f;
    CardZoomType defZoomType = CardZoomType.Hover;
    int defTheme = 0;

    private void Awake()
    {
        if (FindObjectsByType<MenuSettingsManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        LoadSave();
    }

    private void LoadSave()
    {
        if (PlayerPrefs.HasKey("save"))
        {
            string json = PlayerPrefs.GetString("save");

            save = JsonUtility.FromJson<Save>(json);
            
        }

        save ??= new(defBgm, defSfx, defZoomType, defTheme);

        ChangeBGV(save.BGV);
        ChangeSFX(save.SFX);
        ChangeCardZoomType((int)save.zoomType);
        ChangeTheme(save.theme);
    }

    private void OnEnable()
    {
        settingsPanel.SetActive(false);
        settingsButton.onClick.AddListener(delegate { ToggleSettings(true); });
        exitSettingsButton.onClick.AddListener(delegate { ToggleSettings(false); });
        bgvSlider.onValueChanged.AddListener(ChangeBGV);
        sfxSlider.onValueChanged.AddListener(ChangeSFX);
        zoomDropdown.onValueChanged.AddListener(ChangeCardZoomType);
        themeDropdown.onValueChanged.AddListener(ChangeTheme);

        GetSave = ReturnSave;

        AudioListener.volume = 1f;
        ToggleSettings(false);
    }

    private Save ReturnSave() => save;

    private void OnDisable()
    {
        settingsButton.onClick.RemoveAllListeners();
        exitSettingsButton.onClick.RemoveAllListeners();
        bgvSlider.onValueChanged.RemoveListener(ChangeBGV);
        sfxSlider.onValueChanged.RemoveListener(ChangeSFX);
        zoomDropdown.onValueChanged.RemoveListener(ChangeCardZoomType);
        themeDropdown.onValueChanged.RemoveListener(ChangeTheme);
    }

    private void ToggleSettings(bool value)
    {
        settingsPanel.SetActive(value);
    }

    private void ChangeBGV(float newValue)
    {
        newValue = Mathf.Clamp01(newValue);

        bgvSource.volume = newValue;
        bgvSlider.value = newValue;
        save.BGV = newValue;
    }

    private void ChangeSFX(float newValue)
    {
        newValue = Mathf.Clamp01(newValue);

        sfxSource.volume = newValue;
        sfxSlider.value = newValue;
        save.SFX = newValue;
    }

    private void ChangeCardZoomType(int type)
    {
        save.zoomType = (CardZoomType)type;
        zoomDropdown.value = type;
    }

    private void ChangeTheme(int theme) {
        FindObjectOfType<AudioBGInitialiser>().ChangeTheme(theme);
        themeDropdown.value = theme;
        save.theme = theme;
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.DeleteKey("save");

        if (save != null) {
            string json = JsonUtility.ToJson(save);

            PlayerPrefs.SetString("save", json);
        }

        PlayerPrefs.Save();
    }
}
