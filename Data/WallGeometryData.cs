using SmartKRWallData.Validation;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SmartKRWallData.Data
{
    /// <summary>
    /// Тип элемента геометрии.
    /// </summary>
    public enum GeometryElementType
    {
        /// <summary>
        /// Стена.
        /// </summary>
        Wall,

        /// <summary>
        /// Проём.
        /// </summary>
        Opening,

        /// <summary>
        /// Пилон.
        /// </summary>
        Pilon,
    }

    /// <summary>
    /// Общие свойства стены: нижней, основной и верхней.
    /// </summary>
    public abstract class GeometryData
    {
        protected GeometryData() { }

        [SetsRequiredMembers]
        protected GeometryData(GeometryElementType elementType, int thickness, int? concreteClass)
        {
            ElementType = elementType;
            Thickness = thickness;
            ConcreteClass = concreteClass;
        }

        /// <summary>
        /// Тип элемента.
        /// </summary>
        public required GeometryElementType ElementType { get; init; }

        /// <summary>
        /// Толщина, мм.
        /// </summary>
        public required int Thickness { get; init; }

        /// <summary>
        /// Класс бетона, например 25 — B25. Можно не задавать.
        /// </summary>
        public int? ConcreteClass { get; init; }

        internal virtual List<string> Errors(string name)
        {
            var errors = new List<string>();
            if (Thickness <= 0)
                errors.Add($"{name}: толщина должна быть больше 0 (сейчас {Thickness}).");
            if (ConcreteClass <= 0)
                errors.Add($"{name}: класс бетона должен быть больше 0 (сейчас {ConcreteClass}).");
            return errors;
        }

        private protected static void Elevations(List<string> errors, string name, int? bottom, int? top)
        {
            if (bottom.HasValue && top.HasValue && top <= bottom)
                errors.Add($"{name}: отметка верха ({top}) должна быть выше отметки низа ({bottom}).");
        }
    }

    /// <summary>
    /// Основная стена — стена палки.
    /// </summary>
    public sealed class BaseGeometryData : GeometryData
    {
        public BaseGeometryData() { }

        /// <param name="elementType">
        /// Тип элемента.
        /// </param>
        /// <param name="thickness">
        /// OLP_Толщина стены, мм.
        /// </param>
        /// <param name="bottomElevation">
        /// OLP_Отметка низа, мм.
        /// </param>
        /// <param name="topElevation">
        /// OLP_Отметка верха, мм.
        /// </param>
        /// <param name="topDrop">
        /// ПерепадСверху_Толщина, мм. 0 — перепада нет.
        /// </param>
        /// <param name="baseHeight2">
        /// ВысотаОсновы_2этаж, мм. 0 — армирование на один этаж.
        /// </param>
        /// <param name="topDrop2">
        /// ПерепадСверху_2этаж_Толщина, мм. 0 — перепада на 2 этаже нет.
        /// </param>
        /// <param name="concreteClass">
        /// Класс бетона, например 25 — B25. Можно не задавать.
        /// </param>
        [SetsRequiredMembers]
        public BaseGeometryData(GeometryElementType elementType, int thickness, int bottomElevation, int topElevation,
            int topDrop, int baseHeight2, int topDrop2, int? concreteClass = null)
            : base(elementType, thickness, concreteClass)
        {
            BottomElevation = bottomElevation;
            TopElevation = topElevation;
            TopDrop = topDrop;
            BaseHeight2 = baseHeight2;
            TopDrop2 = topDrop2;
        }

        /// <summary>
        /// Без перепада и одноуровневое армирование.
        /// </summary>
        /// <param name="elementType">
        /// Тип элемента.
        /// </param>
        /// <param name="thickness">
        /// OLP_Толщина стены, мм.
        /// </param>
        /// <param name="bottomElevation">
        /// OLP_Отметка низа, мм.
        /// </param>
        /// <param name="topElevation">
        /// OLP_Отметка верха, мм.
        /// </param>
        /// <param name="concreteClass">
        /// Класс бетона, например 25 — B25. Можно не задавать.
        /// </param>
        [SetsRequiredMembers]
        public BaseGeometryData(GeometryElementType elementType, int thickness, int bottomElevation, int topElevation, int? concreteClass = null)
            : this(elementType, thickness, bottomElevation, topElevation, 0, 0, 0, concreteClass) { }

        /// <summary>
        /// OLP_Отметка низа, мм.
        /// </summary>
        public required int BottomElevation { get; init; }

        /// <summary>
        /// OLP_Отметка верха, мм.
        /// </summary>
        public required int TopElevation { get; init; }

        /// <summary>
        /// ПерепадСверху_Толщина, мм.
        /// </summary>
        public required int TopDrop { get; init; }

        /// <summary>
        /// ВысотаОсновы_2этаж, мм.
        /// </summary>
        public required int BaseHeight2 { get; init; }

        /// <summary>
        /// ПерепадСверху_2этаж_Толщина, мм.
        /// </summary>
        public required int TopDrop2 { get; init; }

        internal override List<string> Errors(string name)
        {
            var errors = base.Errors(name);
            Elevations(errors, name, BottomElevation, TopElevation);
            if (TopDrop < 0)
                errors.Add($"{name}: ПерепадСверху_Толщина не может быть отрицательным (сейчас {TopDrop}).");
            if (BaseHeight2 < 0)
                errors.Add($"{name}: ВысотаОсновы_2этаж не может быть отрицательной (сейчас {BaseHeight2}).");
            if (TopDrop2 < 0)
                errors.Add($"{name}: ПерепадСверху_2этаж_Толщина не может быть отрицательным (сейчас {TopDrop2}).");
            return errors;
        }
    }

    /// <summary>
    /// Нижняя стена — под основной.
    /// Толщина 0 — стены нет.
    /// </summary>
    public sealed class BotGeometryData : GeometryData
    {
        public BotGeometryData() { }

        /// <param name="elementType">
        /// Тип элемента.
        /// </param>
        /// <param name="thickness">
        /// Толщина, мм. 0 — стены нет.
        /// </param>
        /// <param name="bottomElevation">
        /// Отметка низа, мм. Можно не задавать.
        /// </param>
        /// <param name="topElevation">
        /// Отметка верха, мм. Можно не задавать.
        /// </param>
        /// <param name="concreteClass">
        /// Класс бетона, например 25 — B25. Можно не задавать.
        /// </param>
        [SetsRequiredMembers]
        public BotGeometryData(GeometryElementType elementType, int thickness,
            int? bottomElevation = null, int? topElevation = null, int? concreteClass = null)
            : base(elementType, thickness, concreteClass)
        {
            BottomElevation = bottomElevation;
            TopElevation = topElevation;
        }

        /// <summary>
        /// Нижней стены нет.
        /// </summary>
        public static BotGeometryData None => new BotGeometryData(GeometryElementType.Wall, 0);

        /// <summary>
        /// Отметка низа, мм.
        /// </summary>
        public int? BottomElevation { get; init; }

        /// <summary>
        /// Отметка верха, мм.
        /// </summary>
        public int? TopElevation { get; init; }

        internal bool Exists => Thickness != 0;

        internal override List<string> Errors(string name)
        {
            if (!Exists)
                return new List<string>();

            var errors = base.Errors(name);
            Elevations(errors, name, BottomElevation, TopElevation);
            return errors;
        }
    }

    /// <summary>
    /// Верхняя стена — над основной.
    /// Толщина 0 — стены нет.
    /// </summary>
    public sealed class UpGeometryData : GeometryData
    {
        public UpGeometryData() { }

        /// <param name="elementType">
        /// Тип элемента.
        /// </param>
        /// <param name="thickness">
        /// Толщина, мм. 0 — стены нет.
        /// </param>
        /// <param name="bottomElevation">
        /// Отметка низа, мм. Можно не задавать.
        /// </param>
        /// <param name="topElevation">
        /// Отметка верха, мм. Можно не задавать.
        /// </param>
        /// <param name="concreteClass">
        /// Класс бетона, например 25 — B25. Можно не задавать.
        /// </param>
        [SetsRequiredMembers]
        public UpGeometryData(GeometryElementType elementType, int thickness,
            int? bottomElevation = null, int? topElevation = null, int? concreteClass = null)
            : base(elementType, thickness, concreteClass)
        {
            BottomElevation = bottomElevation;
            TopElevation = topElevation;
        }

        /// <summary>
        /// Верхней стены нет.
        /// </summary>
        public static UpGeometryData None => new UpGeometryData(GeometryElementType.Wall, 0);

        /// <summary>
        /// Отметка низа, мм.
        /// </summary>
        public int? BottomElevation { get; init; }

        /// <summary>
        /// Отметка верха, мм.
        /// </summary>
        public int? TopElevation { get; init; }

        internal bool Exists => Thickness != 0;

        internal override List<string> Errors(string name)
        {
            if (!Exists)
                return new List<string>();

            var errors = base.Errors(name);
            Elevations(errors, name, BottomElevation, TopElevation);
            return errors;
        }
    }

    /// <summary>
    /// Геометрия: нижняя, основная и верхняя стены.
    /// Значения основной стены пишутся в палку, остальное пока никуда.
    /// </summary>
    public sealed class WallGeometryData
    {
        /// <param name="bot">
        /// Нижняя стена. Нет — <see cref="BotGeometryData.None"/>.
        /// </param>
        /// <param name="baseWall">
        /// Основная стена.
        /// </param>
        /// <param name="up">
        /// Верхняя стена. Нет — <see cref="UpGeometryData.None"/>.
        /// </param>
        public WallGeometryData(BotGeometryData bot, BaseGeometryData baseWall, UpGeometryData up)
        {
            Bot = bot;
            Base = baseWall;
            Up = up;

            // Все стены присвоены — проверяем, что заполнено корректно.
            Validate();
        }

        /// <summary>
        /// Нижняя стена.
        /// </summary>
        public BotGeometryData Bot { get; }

        /// <summary>
        /// Основная стена.
        /// </summary>
        public BaseGeometryData Base { get; }

        /// <summary>
        /// Верхняя стена.
        /// </summary>
        public UpGeometryData Up { get; }

        /// <summary>
        /// Проверяет все стены и их взаимное положение.
        /// Ошибки — <see cref="VertArmDesignException"/> со списком.
        /// </summary>
        private void Validate()
        {
            var errors = new List<string>();

            if (Bot == null)
                errors.Add("Bot: нижняя стена не задана (null). Если её нет — BotGeometryData.None.");
            else
                errors.AddRange(Bot.Errors("Нижняя стена"));

            if (Base == null)
                errors.Add("Base: основная стена не задана (null).");
            else
                errors.AddRange(Base.Errors("Основная стена"));

            if (Up == null)
                errors.Add("Up: верхняя стена не задана (null). Если её нет — UpGeometryData.None.");
            else
                errors.AddRange(Up.Errors("Верхняя стена"));

            // Стены не пересекаются по высоте. Отметки совпадают — плиты между стенами нет.
            if (Base != null)
            {
                if (Bot != null && Bot.Exists && Bot.TopElevation > Base.BottomElevation)
                    errors.Add($"Нижняя стена: отметка верха ({Bot.TopElevation}) выше отметки низа основной стены ({Base.BottomElevation}).");
                if (Up != null && Up.Exists && Up.BottomElevation < Base.TopElevation)
                    errors.Add($"Верхняя стена: отметка низа ({Up.BottomElevation}) ниже отметки верха основной стены ({Base.TopElevation}).");
            }

            PalkaRules.ThrowIfAny(errors);
        }
    }
}
