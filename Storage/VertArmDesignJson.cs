using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using SmartKRWallData.Design;

// Не зависит от Revit API. Newtonsoft не используем: Revit и надстройки делят одну его копию на процесс,
// и разные версии приводят к FileLoadException.

namespace SmartKRWallData.Storage
{
    public static class VertArmDesignJson
    {
        // Создание сериализатора дорогое (строит контракт по типам) — один экземпляр на всё время работы.
        private static readonly DataContractJsonSerializer Serializer = new DataContractJsonSerializer(
            typeof(VertArmDesign),
            new DataContractJsonSerializerSettings { EmitTypeInformation = EmitTypeInformation.Never });

        /// <summary>JSON с отступами. Поля из параметров палки ([PalkaParam]) в него не попадают.</summary>
        public static string ToJson(VertArmDesign design)
        {
            if (design == null)
                throw new ArgumentNullException(nameof(design));

            using (var stream = new MemoryStream())
            {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, true, "  "))
                {
                    Serializer.WriteObject(writer, design);
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        /// <summary>
        /// Читает JSON. Отсутствующие поля получают значения по умолчанию, неизвестные — игнорируются.
        /// Неверный JSON — <see cref="FormatException"/>.
        /// </summary>
        public static VertArmDesign FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new FormatException("Пустые данные ВертАрм.");

            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return (VertArmDesign)Serializer.ReadObject(stream) ?? new VertArmDesign();
                }
            }
            catch (SerializationException ex)
            {
                throw new FormatException($"Неверный формат данных ВертАрм: {ex.Message}", ex);
            }
        }
    }
}
