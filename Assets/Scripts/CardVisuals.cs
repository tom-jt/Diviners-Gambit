using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CardVisuals : NetworkBehaviour
{
    private CardZoomType zoomStyle;

    private Transform previousParent;
    private int siblingIndex;

    private bool isDragging = false;
    private bool isOverDropZone = false;

    [HideInInspector]
    public bool isDraggable = false;

    [Header("Assignments")]
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private GameObject cardBorder;
    [SerializeField]
    private AudioClip sfxHighlight;

    public void ToggleDraggable(bool value)
    {
        isDraggable = value;
        cardBorder.SetActive(value);
    }

    public void ToggleDrag(bool toggleOn)
    {
        if (!hasAuthority)
            return;

        if (toggleOn)
        {
            if (!isDraggable)
                return;

            isDragging = true;
            ForefrontCard(true);
            Zoom(false);
        }
        else
        {
            if (!isDraggable && !isDragging)
                return;

            isDragging = false;
            if (isOverDropZone) //if card is over dropzone, check with server to see if player can play it
            {
                NetworkIdentity identity = NetworkClient.connection.identity;
                GamePlayerManager playerScript = identity.GetComponent<GamePlayerManager>();
                playerScript.CmdTryPlayCard(gameObject);
            }
            else //otherwise put back in hand
            {
                ForefrontCard(false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        isOverDropZone = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (isDraggable)
        {
            isOverDropZone = false;
        }
    }

    public void ForefrontCard(bool toggleOn, Transform previousParentDefault = null)
    {
        if (previousParentDefault)
            previousParent = previousParentDefault;

        Transform parent = toggleOn ? FindObjectOfType<DragCardRoot>().transform : previousParent;
        if (transform.parent == parent)
            return;

        if (toggleOn)
        {
            previousParent = transform.parent;
            siblingIndex = transform.GetSiblingIndex();
        }

        transform.SetParent(parent, toggleOn);

        if (!toggleOn)
        {
            transform.SetSiblingIndex(siblingIndex);

            isOverDropZone = false;
        }
    }

    private void Update()
    {
        if (isDragging)
        {
            transform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        }

        Save save = MenuSettingsManager.GetSave?.Invoke();
        if (save != null)
        {
            zoomStyle = save.zoomType;
        }
        else
        {
            zoomStyle = CardZoomType.Hold;
        }
    }

    public void Flip()
    {
        animator.SetTrigger("FlipCard");
    }

    public void HoldZoom(bool toggleOn)
    {
        if (zoomStyle == CardZoomType.Hold)
        {
            if (!isDragging)
                Zoom(toggleOn);
        }
    }

    public void HoverZoom(bool toggleOn)
    {
        if (zoomStyle == CardZoomType.Hover)
        {
            if (!isDragging)
                Zoom(toggleOn);
        }
    }

    public void Zoom(bool toggleOn)
    {
        //when zooming move it in front of the dropzone visual effects
        if (toggleOn)
            transform.parent.SetAsLastSibling();
        else
            transform.parent.SetAsFirstSibling();

        animator.SetTrigger((isOverDropZone ? "S" : "") + (toggleOn ? "ZoomIn" : "ZoomOut"));
        animator.ResetTrigger((isOverDropZone ? "S" : "") + (toggleOn ? "ZoomOut" : "ZoomIn"));

        if (toggleOn) {
            FindObjectOfType<AudioManager>().PlayClipInstance(sfxHighlight);
        }
    }

    public void RevealEnemyCard()
    {
        isOverDropZone = true;
        animator.SetTrigger("FlipCard");
    }
}
