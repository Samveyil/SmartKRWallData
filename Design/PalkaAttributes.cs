using System;

namespace SmartKRWallData.Design
{
    // ---------------- Атрибуты хранения ----------------

    /// <summary>
    /// Поле хранится в параметре семейства палки, а не в ES. bool — параметр «Да/Нет», int — длина в мм
    /// (в параметре — внутренние единицы) или целое число, double — число без единиц, string — текст.
    /// Свойство только для чтения (вычисляемое) пишется в палку, но не читается из неё. В имени допустимы подстановки:
    /// {end} — «Начало»/«Конец» (см. <see cref="PalkaEndAttribute"/>), {step}/{length} — см. <see cref="ZoneParamsAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PalkaParamAttribute : Attribute
    {
        public PalkaParamAttribute(string name) => Name = name;

        public string Name { get; }

        /// <summary>GUID общего параметра. Если задан, параметр ищется сначала по GUID, затем по имени.</summary>
        public string Guid { get; set; }

        /// <summary>Параметр есть только у стены: для пилона (<see cref="VertArmDesign.IsPilon"/>) не пишется и не читается.</summary>
        public bool WallOnly { get; set; }
    }

    /// <summary>К какому концу линии палки («Начало»/«Конец») относится вложенный <see cref="NodeSection"/>.</summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PalkaEndAttribute : Attribute
    {
        public PalkaEndAttribute(string end) => End = end;

        public string End { get; }
    }

    /// <summary>Имена параметров палки для вложенной <see cref="DenseZone"/>.</summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ZoneParamsAttribute : Attribute
    {
        public ZoneParamsAttribute(string step, string length)
        {
            Step = step;
            Length = length;
        }

        public string Step { get; }

        public string Length { get; }
    }
}
