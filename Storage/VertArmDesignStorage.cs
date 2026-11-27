using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using SmartKRWallData.Common;
using SmartKRWallData.Design;
using SmartKRWallData.Validation;

// Чтение и запись VertArmDesign в палку: поля [PalkaParam] — в параметры семейства,
// остальное — JSON в Extensible Storage. Схема ES должна быть одинаковой во всех плагинах,
// поэтому её определение живёт только здесь.

namespace SmartKRWallData.Storage
{
    public static class VertArmDesignStorage
    {
        public static readonly Guid SchemaGuid = new Guid("ddfbc3a2-b594-4a28-a7a6-9f6064f4977a");
        public const string SchemaName = "OLP_VertArmDesign";
        public const string DataField = "Data";

        /// <summary>Есть ли у палки данные ВертАрм в ES.</summary>
        public static bool HasDesign(Element palka) => ReadJson(palka) != null;

        /// <summary>
        /// Проверяет данные для этой палки и возвращает список ошибок (пустой — всё верно).
        /// <see cref="VertArmDesign.IsPilon"/> (из геометрии: Base.ElementType) сверяется с семейством палки.
        /// Удобно вызвать перед <see cref="Write"/>, чтобы показать ошибки пользователю.
        /// </summary>
        public static IList<string> Validate(FamilyInstance palka, VertArmDesign design)
        {
            if (palka == null)
                throw new ArgumentNullException(nameof(palka));

            var errors = VertArmDesignRules.Validate(design);

            var familyIsPilon = IsPilonFamily(palka);
            if (design != null && design.IsPilon != familyIsPilon)
                errors.Insert(0, familyIsPilon
                    ? "Палка — пилон (семейство «…Пилон»), а в геометрии Base.ElementType не Pilon."
                    : "Палка — стена, а в геометрии Base.ElementType = Pilon.");

            return errors;
        }

        /// <summary>
        /// Записывает данные в палку. Вызывать внутри открытой транзакции, иначе <see cref="System.InvalidOperationException"/>.
        /// <see cref="VertArmDesign.IsPilon"/> должен совпадать с семейством. Пишет только изменившиеся значения.
        /// Возвращает предупреждения: параметры, которых нет у семейства или которые доступны только для чтения.
        /// Некорректные данные — <see cref="VertArmDesignException"/> со списком ошибок.
        /// </summary>
        public static IList<string> Write(FamilyInstance palka, VertArmDesign design)
        {
            if (palka == null)
                throw new ArgumentNullException(nameof(palka));
            if (!palka.Document.IsModifiable)
                throw new InvalidOperationException("VertArmDesignStorage.Write: вызывайте внутри открытой транзакции.");

            var errors = Validate(palka, design);
            if (errors.Count > 0)
                throw new VertArmDesignException(errors);

            design.SchemaVersion = VertArmDesign.CurrentSchemaVersion;

            var warnings = new List<string>();
            Walk(design, new Context(palka, design.IsPilon, warnings), write: true);

            var json = VertArmDesignJson.ToJson(design);
            if (ReadJson(palka) != json)
            {
                var entity = new Entity(GetOrCreateSchema());
                entity.Set(DataField, json);
                palka.SetEntity(entity);
            }

            return warnings;
        }

        /// <summary>
        /// Читает данные палки: ES + параметры семейства. null — у палки нет данных в ES.
        /// <see cref="VertArmDesign.IsPilon"/> заполняется по семейству. Неверный JSON — <see cref="FormatException"/>.
        /// </summary>
        public static VertArmDesign Read(FamilyInstance palka)
        {
            if (palka == null)
                throw new ArgumentNullException(nameof(palka));

            var json = ReadJson(palka);
            if (json == null)
                return null;

            var design = VertArmDesignJson.FromJson(json);
            design.IsPilon = IsPilonFamily(palka);
            Walk(design, new Context(palka, design.IsPilon, new List<string>()), write: false);

            return design;
        }

        /// <summary>Удаляет данные ES палки (параметры семейства не трогает). Внутри открытой транзакции.</summary>
        public static void Delete(Element palka)
        {
            if (palka == null)
                throw new ArgumentNullException(nameof(palka));
            if (!palka.Document.IsModifiable)
                throw new InvalidOperationException("VertArmDesignStorage.Delete: вызывайте внутри открытой транзакции.");

            var schema = Schema.Lookup(SchemaGuid);
            if (schema != null && palka.GetEntity(schema).IsValid())
                palka.DeleteEntity(schema);
        }

        /// <summary>Пилон — семейство палки с «Пилон» в имени (как в окне «Создать ВертАрм»).</summary>
        public static bool IsPilonFamily(FamilyInstance palka) =>
            palka.Symbol.FamilyName.IndexOf("Пилон", StringComparison.OrdinalIgnoreCase) >= 0;

        private static string ReadJson(Element palka)
        {
            var schema = Schema.Lookup(SchemaGuid);
            if (schema == null)
                return null;

            var entity = palka.GetEntity(schema);
            if (!entity.IsValid())
                return null;

            var json = entity.Get<string>(DataField);
            return string.IsNullOrWhiteSpace(json) ? null : json;
        }

        private static Schema GetOrCreateSchema()
        {
            var schema = Schema.Lookup(SchemaGuid);
            if (schema != null)
                return schema;

            // Public: писать и читать могут любые плагины, VendorId не требуется.
            var builder = new SchemaBuilder(SchemaGuid);
            builder.SetSchemaName(SchemaName);
            builder.SetReadAccessLevel(AccessLevel.Public);
            builder.SetWriteAccessLevel(AccessLevel.Public);
            builder.SetDocumentation("Данные для построения ВертАрм по палке (JSON).");
            builder.AddSimpleField(DataField, typeof(string));

            return builder.Finish();
        }

        // ---------------- Описание полей (собирается один раз на тип) ----------------

        private sealed class DesignField
        {
            public PropertyInfo Property;

            /// <summary>Параметр палки; null — вложенный раздел.</summary>
            public PalkaParamAttribute Param;

            /// <summary>GUID общего параметра из атрибута (если задан).</summary>
            public Guid? ParamGuid;

            /// <summary>Для вложенного раздела: подстановки в имена его параметров ({end}, {step}, {length}).</summary>
            public KeyValuePair<string, string>[] Tokens;
        }

        private static readonly ConcurrentDictionary<Type, DesignField[]> Fields = new ConcurrentDictionary<Type, DesignField[]>();

        private static DesignField[] GetFields(Type type) => Fields.GetOrAdd(type, t =>
        {
            var list = new List<DesignField>();

            foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                var param = prop.GetCustomAttribute<PalkaParamAttribute>();
                if (param != null)
                {
                    list.Add(new DesignField
                    {
                        Property = prop,
                        Param = param,
                        ParamGuid = System.Guid.TryParse(param.Guid, out var guid) ? guid : (Guid?)null
                    });
                    continue;
                }

                if (!IsDesignType(prop.PropertyType))
                    continue;

                var tokens = new List<KeyValuePair<string, string>>();

                var palkaEnd = prop.GetCustomAttribute<PalkaEndAttribute>();
                if (palkaEnd != null)
                    tokens.Add(new KeyValuePair<string, string>("end", palkaEnd.End));

                var zone = prop.GetCustomAttribute<ZoneParamsAttribute>();
                if (zone != null)
                {
                    tokens.Add(new KeyValuePair<string, string>("step", zone.Step));
                    tokens.Add(new KeyValuePair<string, string>("length", zone.Length));
                }

                list.Add(new DesignField { Property = prop, Tokens = tokens.ToArray() });
            }

            return list.ToArray();
        });

        private static bool IsDesignType(Type type) =>
            type.IsClass && type != typeof(string) && type != typeof(PointXyz) && type.Namespace == typeof(VertArmDesign).Namespace;

        // ---------------- Обход полей [PalkaParam] ----------------

        private sealed class Context
        {
            public Context(FamilyInstance palka, bool isPilon, List<string> warnings)
                : this(palka, isPilon, warnings, new Dictionary<string, string>())
            {
            }

            private Context(FamilyInstance palka, bool isPilon, List<string> warnings, Dictionary<string, string> tokens)
            {
                Palka = palka;
                IsPilon = isPilon;
                Warnings = warnings;
                Tokens = tokens;
            }

            public FamilyInstance Palka { get; }
            public bool IsPilon { get; }
            public List<string> Warnings { get; }

            /// <summary>Подстановки в имя параметра: {end}, {step}, {length}.</summary>
            private Dictionary<string, string> Tokens { get; }

            public Context With(KeyValuePair<string, string>[] tokens)
            {
                if (tokens.Length == 0)
                    return this;

                var merged = new Dictionary<string, string>(Tokens);
                foreach (var kv in tokens)
                    merged[kv.Key] = kv.Value;

                return new Context(Palka, IsPilon, Warnings, merged);
            }

            public string Resolve(string name) =>
                Tokens.Aggregate(name, (current, kv) => current.Replace("{" + kv.Key + "}", kv.Value));

            /// <summary>Параметр палки: сначала по GUID (если задан), затем по имени.</summary>
            public Parameter Find(DesignField field, string name)
            {
                if (field.ParamGuid.HasValue)
                {
                    var byGuid = Palka.get_Parameter(field.ParamGuid.Value);
                    if (byGuid != null)
                        return byGuid;
                }

                return Palka.LookupParameter(name);
            }
        }

        private static void Walk(object obj, Context context, bool write)
        {
            if (obj == null)
                return;

            foreach (var field in GetFields(obj.GetType()))
            {
                if (field.Param == null)
                {
                    Walk(field.Property.GetValue(obj), context.With(field.Tokens), write);
                    continue;
                }

                if (field.Param.WallOnly && context.IsPilon)
                    continue;

                if (write)
                    WriteParam(obj, field, context);
                else
                    ReadParam(obj, field, context);
            }
        }

        private static void WriteParam(object obj, DesignField field, Context context)
        {
            var name = context.Resolve(field.Param.Name);
            var parameter = context.Find(field, name);

            if (parameter == null)
            {
                context.Warnings.Add($"У палки нет параметра «{name}».");
                return;
            }

            if (parameter.IsReadOnly)
            {
                context.Warnings.Add($"Параметр палки «{name}» доступен только для чтения.");
                return;
            }

            var value = field.Property.GetValue(obj);

            switch (parameter.StorageType)
            {
                case StorageType.String:
                    var text = value?.ToString() ?? "";
                    if ((parameter.AsString() ?? "") != text)
                        parameter.Set(text);
                    break;

                case StorageType.Integer:
                    var intValue = value is bool b ? (b ? 1 : 0) : Convert.ToInt32(value);
                    if (parameter.AsInteger() != intValue)
                        parameter.Set(intValue);
                    break;

                case StorageType.Double:
                    // int — длина в мм (переводим во внутренние единицы), double — число без единиц.
                    var doubleValue = value is bool flag ? (flag ? 1d : 0d)
                        : value is double number ? number
                        : Convert.ToDouble(value).FromMillimeters();
                    if (Math.Abs(parameter.AsDouble() - doubleValue) > 1e-9)
                        parameter.Set(doubleValue);
                    break;

                default:
                    context.Warnings.Add($"Параметр палки «{name}»: неожиданный тип {parameter.StorageType}.");
                    break;
            }
        }

        private static void ReadParam(object obj, DesignField field, Context context)
        {
            var prop = field.Property;
            if (!prop.CanWrite)
                return;

            var parameter = context.Find(field, context.Resolve(field.Param.Name));
            if (parameter == null)
                return;

            if (prop.PropertyType == typeof(bool))
            {
                var on = parameter.StorageType == StorageType.Double
                    ? Math.Abs(parameter.AsDouble()) > 1e-9
                    : parameter.AsInteger() != 0;
                prop.SetValue(obj, on);
            }
            else if (prop.PropertyType == typeof(int))
            {
                var value = parameter.StorageType == StorageType.Double
                    ? (int)Math.Round(parameter.AsDouble().ToMillimeters())
                    : parameter.AsInteger();
                prop.SetValue(obj, value);
            }
            else if (prop.PropertyType == typeof(double))
            {
                var value = parameter.StorageType == StorageType.Double
                    ? parameter.AsDouble()
                    : parameter.AsInteger();
                prop.SetValue(obj, value);
            }
            else if (prop.PropertyType == typeof(string))
            {
                prop.SetValue(obj, parameter.StorageType == StorageType.String
                    ? parameter.AsString() ?? ""
                    : parameter.AsValueString() ?? "");
            }
        }
    }
}
