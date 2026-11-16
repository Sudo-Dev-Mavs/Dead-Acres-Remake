using System;
using System.IO;
using UnityEngine;

namespace DeadAcres.Saving
{
    /// <summary>
    /// Los archivos de guardado: cinco ranuras en total, cada una un JSON en la carpeta de datos
    /// del usuario. La ranura se elige en el menú al empezar la partida.
    /// </summary>
    public static class SaveFile
    {
        public const int SlotCount = 5;

        private const string LegacyFileName = "savegame.json";

        public static bool IsValidSlot(int slot) => slot >= 0 && slot < SlotCount;

        public static string GetPath(int slot)
        {
            return Path.Combine(Application.persistentDataPath, $"save_slot_{slot + 1}.json");
        }

        public static bool Exists(int slot) => IsValidSlot(slot) && File.Exists(GetPath(slot));

        /// <returns>False si la ranura está vacía, no se puede leer o es de una versión que no se entiende.</returns>
        public static bool TryRead(int slot, out SaveData data)
        {
            data = null;
            if (!IsValidSlot(slot))
            {
                return false;
            }

            try
            {
                string path = GetPath(slot);
                if (!File.Exists(path))
                {
                    return false;
                }

                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null || data.version != SaveData.CurrentVersion)
                {
                    data = null;
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"No se pudo leer la ranura {slot + 1}: {exception.Message}");
                data = null;
                return false;
            }
        }

        public static bool Write(int slot, SaveData data)
        {
            if (!IsValidSlot(slot))
            {
                return false;
            }

            try
            {
                // Primero a un temporal: si el juego se cierra a mitad, el guardado anterior sigue entero.
                string path = GetPath(slot);
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(temporary, path);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"No se pudo guardar en la ranura {slot + 1}: {exception.Message}");
                return false;
            }
        }

        public static void Delete(int slot)
        {
            if (!IsValidSlot(slot))
            {
                return;
            }

            try
            {
                string path = GetPath(slot);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"No se pudo borrar la ranura {slot + 1}: {exception.Message}");
            }
        }

        /// <summary>
        /// La partida única de versiones anteriores pasa a la primera ranura libre. Se llama al
        /// abrir el menú; si no hay archivo antiguo no hace nada.
        /// </summary>
        public static void MigrateLegacy()
        {
            try
            {
                string legacy = Path.Combine(Application.persistentDataPath, LegacyFileName);
                if (!File.Exists(legacy))
                {
                    return;
                }

                for (int slot = 0; slot < SlotCount; slot++)
                {
                    if (!File.Exists(GetPath(slot)))
                    {
                        File.Move(legacy, GetPath(slot));
                        return;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"No se pudo traer la partida antigua a las ranuras: {exception.Message}");
            }
        }
    }
}
