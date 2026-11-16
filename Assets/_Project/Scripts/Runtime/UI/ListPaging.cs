using System;

namespace DeadAcres.UI
{
    /// <summary>Cuentas de paginación para listas que no caben enteras en pantalla. C# puro.</summary>
    public static class ListPaging
    {
        public static int GetPageCount(int itemCount, int pageSize)
        {
            pageSize = Math.Max(1, pageSize);
            return Math.Max(1, (Math.Max(0, itemCount) + pageSize - 1) / pageSize);
        }

        /// <summary>Pasada la última página (o si la lista se acortó) se vuelve a la primera.</summary>
        public static int WrapPage(int page, int itemCount, int pageSize)
        {
            return page < 0 || page >= GetPageCount(itemCount, pageSize) ? 0 : page;
        }
    }
}
