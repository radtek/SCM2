using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class ButtonSoundBhr : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public void OnPointerUp(PointerEventData eventData)
    {
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StaticSoundMgr.Instance.PlaySound("BtnClick");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
    }
}
