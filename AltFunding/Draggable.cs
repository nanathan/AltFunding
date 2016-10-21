using UnityEngine;
using UnityEngine.EventSystems;

namespace AltFunding
{
    public class Draggable : MonoBehaviour, IDragHandler
    {
        public RectTransform dragTransform = null;

        void Start()
        {
            Debug.Log("[AltFunding] Draggable.Start()");
            if(dragTransform == null)
                dragTransform = GetComponent<RectTransform>();
        }

        public void OnDrag(PointerEventData eventData)
        {
            dragTransform.position += new Vector3(eventData.delta.x, eventData.delta.y);
        }
    }
}
