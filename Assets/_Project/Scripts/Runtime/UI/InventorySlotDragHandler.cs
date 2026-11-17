using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadAcres.UI
{
    /// <summary>
    /// Hace arrastrable una ranura del inventario: recoge los eventos de arrastre de la interfaz
    /// y se los pasa a la ventana, que es la que mueve los objetos.
    /// </summary>
    public sealed class InventorySlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IDropHandler
    {
        private InventoryView view;
        private int index;

        internal void Bind(InventoryView inventoryView, int slotIndex)
        {
            view = inventoryView;
            index = slotIndex;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (view != null) view.BeginDrag(index, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (view != null) view.Drag(eventData.position);
        }

        // Se llama en la ranura sobre la que se suelta, antes del OnEndDrag de la de origen.
        public void OnDrop(PointerEventData eventData)
        {
            if (view != null) view.DropOn(index);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (view != null) view.EndDrag();
        }
    }
}
