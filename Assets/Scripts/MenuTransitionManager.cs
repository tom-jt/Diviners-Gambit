using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MenuTransitionManager : MonoBehaviour
{
    public static MenuTransitionManager singleton = null;
    public float menuTransitionDelay = 0.25f;
    private bool currentLoadState = true;

    [SerializeField]
    private Image[] bgImages;
    [SerializeField]
    private Animator anim;
    [SerializeField]
    private AudioClip sfxTransIn;
    [SerializeField]
    private AudioClip sfxTransOut;

    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
            DontDestroyOnLoad(transform.parent);
            currentLoadState = true;
        }
        else
        {
            Destroy(transform.parent.gameObject);
        }
    }

    private bool ToggleTransition(bool state)
    {
        if (currentLoadState == state)
            return false;

        string animName = state ? "TransitionOut" : "TransitionIn";
        anim.Play(animName);

        currentLoadState = state;

        FindObjectOfType<AudioManager>().PlayClipInstance(state ? sfxTransOut : sfxTransIn);

        if (state) 
        {
            Sprite bannerImage = SpriteDatabase.GetBannerImage(Random.Range(0, SpriteDatabase.bannerImages.Count));
            foreach (Image image in bgImages) 
            {
                image.sprite = bannerImage;
            }
        }
        return true;
    }

    public static IEnumerator LoadingDelay(Action action, bool exitLoad = true, bool skipDelayIfAlrLoading = true)
    {
        if (singleton.ToggleTransition(true) && skipDelayIfAlrLoading)
        {
            yield return new WaitForSeconds(singleton.menuTransitionDelay);
        }

        action?.Invoke();

        if (exitLoad)
        {
            singleton.ToggleTransition(false);
        }
    }
}
