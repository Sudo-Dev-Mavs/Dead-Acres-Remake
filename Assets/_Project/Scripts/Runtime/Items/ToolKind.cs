namespace DeadAcres.Items
{
    /// <summary>Qué hace una herramienta al usarla con la acción primaria.</summary>
    public enum ToolKind : byte
    {
        None = 0,

        /// <summary>Azada: ara una celda de la granja.</summary>
        Hoe = 1,

        /// <summary>Hoz dentada: cosecha un cultivo maduro. Sin ella no se puede cosechar.</summary>
        Sickle = 2,

        /// <summary>Martillo: recoge un objeto colocado y lo devuelve al inventario.</summary>
        Hammer = 3,
    }
}
