using DeadAcres.Player;
using UnityEngine;

namespace DeadAcres.Building
{
    /// <summary>
    /// Puerta colocable: Interactuar (F) la abre o la cierra. Abierta, la hoja gira 90° sobre su
    /// bisagra y deja de ser sólida (pasa cualquiera, zombis incluidos), pero se le puede seguir
    /// apuntando para cerrarla.
    /// </summary>
    public sealed class Gate : MonoBehaviour, IInteractable
    {
        [Tooltip("La hoja de la puerta. Necesita un collider.")]
        [SerializeField] private Transform door;

        [Tooltip("Hacia qué lado (X local) queda la bisagra, medido desde el centro de la hoja.")]
        [SerializeField] private float hingeOffset = -0.45f;

        private Collider doorCollider;
        private Vector3 closedPosition;
        private Quaternion closedRotation;
        private bool isOpen;

        public string Prompt => isOpen ? "Cerrar puerta" : "Abrir puerta";
        public bool CanInteract => door != null;
        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (door == null)
            {
                return;
            }

            doorCollider = door.GetComponent<Collider>();
            closedPosition = door.localPosition;
            closedRotation = door.localRotation;
        }

        public void Interact(GameObject player)
        {
            if (door == null)
            {
                return;
            }

            isOpen = !isOpen;
            if (isOpen)
            {
                // La hoja queda perpendicular, pegada al lado de la bisagra.
                door.localPosition = closedPosition + new Vector3(hingeOffset, 0f, Mathf.Abs(hingeOffset));
                door.localRotation = closedRotation * Quaternion.Euler(0f, 90f, 0f);
            }
            else
            {
                door.localPosition = closedPosition;
                door.localRotation = closedRotation;
            }

            // Como trigger no frena a nadie ni para las balas, pero el rayo de interacción la ve.
            if (doorCollider != null) doorCollider.isTrigger = isOpen;
        }

        internal void Configure(Transform doorTransform) => door = doorTransform;
    }
}
