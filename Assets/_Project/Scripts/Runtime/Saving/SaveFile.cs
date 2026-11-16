using System;
using System.IO;
using UnityEngine;

namespace DeadAcres.Saving
{
    /// <summary>El archivo de guardado: una sola partida, en JSON, en la carpeta de datos del usuario.</summary>
    public static class SaveFile
    {
        private const string FileName = "savegame.json";

        public static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists() => File.Exists(Path);

        /// <returns>False si no hay archivo, no se puede leer o es de una versión que no se entiende.</returns>
        public static bool TryRead(out SaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(Path))
                {
                    return false;
                }

                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path));
                if (data == null || data.version != SaveData.CurrentVersion)
                {
                    data = null;
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"No se pudo leer la partida guardada: {exception.Message}");
                data = null;
                return false;
            }
        }

        public static bool Write(SaveData data)
        {
            try
            {
                // Primero a un temporal: si el juego se cierra a mitad, el guardado anterior sigue entero.
                string temporary = Path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (File.Exists(Path))
                {
                    File.Delete(Path);
                }

                File.Move(temporary, Path);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"No se pudo guardar la partida: {exception.Message}");
                return false;
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(Path))
                {
                    File.Delete(Path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"No se pudo borrar la partida guardada: {exception.Message}");
            }
        }
    }
}
