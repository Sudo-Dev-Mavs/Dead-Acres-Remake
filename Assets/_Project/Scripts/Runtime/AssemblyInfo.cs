using System.Runtime.CompilerServices;

// Los tests y las herramientas de editor configuran los ScriptableObjects
// mediante métodos internal, sin exponer setters públicos en runtime.
[assembly: InternalsVisibleTo("DeadAcres.Editor")]
[assembly: InternalsVisibleTo("DeadAcres.Tests.EditMode")]
