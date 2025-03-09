using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelManager : MonoBehaviour
{
    public float panelSlideAnimDuration;

    private bool changing = false;
    private int currentPanel = 0;

    [SerializeField]
    private Animator[] panelAnims;
    [SerializeField]
    private Animator[] buttonAnims;

    private void Start()
    {
        for (int panel = 0; panel < panelAnims.Length; panel++)
        {
            panelAnims[panel].gameObject.SetActive(panel == currentPanel);
            buttonAnims[panel].Play(panel == currentPanel ? "Pressed" : "Unpressed");
        }
    }

    public void ChangePanel(int newPanel)
    {
        if ((currentPanel == newPanel) || changing)
            return;

        StartCoroutine(ChangePanelAnim(newPanel));
    }

    private IEnumerator ChangePanelAnim(int newPanel)
    {
        changing = true;

        panelAnims[newPanel].transform.SetAsLastSibling();

        panelAnims[newPanel].gameObject.SetActive(true);
        panelAnims[newPanel].Play("PanelSlideIn");

        buttonAnims[newPanel].Play("Pressing");
        buttonAnims[currentPanel].Play("Unpressing");

        yield return new WaitForSeconds(panelSlideAnimDuration);

        panelAnims[currentPanel].gameObject.SetActive(false);

        currentPanel = newPanel;

        changing = false;
    }
}
