using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AudioBGInitialiser : MonoBehaviour
{
    private AudioManager audioScript;
    
    [SerializeField]
    private ThemeSO[] themes;
    [SerializeField]
    private Image background;
    [SerializeField]
    private bool isGameScene;

    [SerializeField]
    private AudioClip sfxButtonPress;
    [SerializeField]
    private AudioClip sfxToggleOn;
    [SerializeField]
    private AudioClip sfxToggleOff;

    private void Start() 
    {
        if (!audioScript) 
        {
            audioScript = FindObjectOfType<AudioManager>();
        }

        Save save = MenuSettingsManager.GetSave?.Invoke();
        if (save != null) 
        {
            ChangeTheme(save.theme);
        }

        foreach (Button button in FindObjectsOfType<Button>(true))
        {
            button.onClick.AddListener(OnButtonClick);
        }

        foreach (Toggle toggle in FindObjectsOfType<Toggle>(true)) 
        {
            toggle.onValueChanged.AddListener(OnToggleClick);
        }
    }

    public void ChangeTheme(int i) 
    {
        if (!audioScript) 
        {
            audioScript = FindObjectOfType<AudioManager>();
        }
        
        audioScript.ChangeBGM(isGameScene ? themes[i].bgmGame : themes[i].bgmStart);
        background.sprite = isGameScene ? themes[i].bgGame : themes[i].bgStart;
    }

    private void OnButtonClick() 
    {
        audioScript.PlayClipInstance(sfxButtonPress);
    }

    private void OnToggleClick(bool state) 
    {
        audioScript.PlayClipInstance(state ? sfxToggleOn : sfxToggleOff);
    }

    private void OnDisabe() 
    {
        foreach (Button button in FindObjectsOfType<Button>(true)) 
        {
            button.onClick.RemoveListener(OnButtonClick);
        }

        foreach (Toggle toggle in FindObjectsOfType<Toggle>(true)) 
        {
            toggle.onValueChanged.RemoveListener(OnToggleClick);
        }
    }
}