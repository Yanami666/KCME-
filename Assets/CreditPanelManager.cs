using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CreditPanelManager : MonoBehaviour
{
    public GameObject creditPanel;

    // Hover sprite references
    public Image closeButtonImage;
    public Sprite normalSprite;
    public Sprite hoverSprite;

    public void ShowPanel()
    {
        creditPanel.SetActive(true);
    }

    public void HidePanel()
    {
        creditPanel.SetActive(false);
    }

    // These methods are called via EventTrigger component on the button
    public void OnHoverEnter()
    {
        if (closeButtonImage != null && hoverSprite != null)
        {
            closeButtonImage.sprite = hoverSprite;
        }
    }

    public void OnHoverExit()
    {
        if (closeButtonImage != null && normalSprite != null)
        {
            closeButtonImage.sprite = normalSprite;
        }
    }
}