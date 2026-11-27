using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SmartKRWallData.Design;
using SmartKRWallData.Validation;

namespace SmartKRWallData.Data
{
    /// <summary>
    /// Армирование в параметрах семейства палки, по блокам. Все блоки обязательные; блок без армирования —
    /// <c>XxxData.None</c>. Неверные значения — <see cref="VertArmDesignException"/>.
    /// Флаги «.Вкл» вычисляются: диаметр (длина зоны) > 0 — элемент есть.
    /// </summary>
    public sealed class PalkaReinforcementData
    {
        /// <param name="vertArm">
        /// Вертикальное армирование.
        /// </param>
        /// <param name="vertArmDop">
        /// Дополнительное вертикальное армирование.
        /// </param>
        /// <param name="vertArmTortsa">
        /// Армирование торца.
        /// </param>
        /// <param name="gorArm">
        /// Горизонтальное армирование.
        /// </param>
        /// <param name="gorPka">
        /// Горизонтальный П-стержень; пока пустой — <see cref="GorPkaData.None"/>.
        /// </param>
        /// <param name="vertPka">
        /// Вертикальный П-стержень / хомут.
        /// </param>
        /// <param name="shpilka">
        /// Шпильки.
        /// </param>
        /// <param name="startNode">
        /// Узел в начале линии палки. Нет — <see cref="NodeData.None"/>.
        /// </param>
        /// <param name="endNode">
        /// Узел в конце линии палки. Нет — <see cref="NodeData.None"/>.
        /// </param>
        public PalkaReinforcementData(
            VertArmData vertArm,
            VertArmDopData vertArmDop,
            VertArmTortsaData vertArmTortsa,
            GorArmData gorArm,
            GorPkaData gorPka,
            VertPkaData vertPka,
            ShpilkaData shpilka,
            NodeData startNode,
            NodeData endNode)
        {
            VertArm = vertArm;
            VertArmDop = vertArmDop;
            VertArmTortsa = vertArmTortsa;
            GorArm = gorArm;
            GorPka = gorPka;
            VertPka = vertPka;
            Shpilka = shpilka;
            StartNode = startNode;
            EndNode = endNode;

            // Все блоки присвоены — проверяем, что заполнено корректно.
            Validate();
        }

        /// <summary>
        /// Вертикальное армирование.
        /// </summary>
        public VertArmData VertArm { get; }

        /// <summary>
        /// Дополнительное вертикальное армирование.
        /// </summary>
        public VertArmDopData VertArmDop { get; }

        /// <summary>
        /// Армирование торца.
        /// </summary>
        public VertArmTortsaData VertArmTortsa { get; }

        /// <summary>
        /// Горизонтальное армирование.
        /// </summary>
        public GorArmData GorArm { get; }

        /// <summary>
        /// Горизонтальный П-стержень; пока пустой.
        /// </summary>
        public GorPkaData GorPka { get; }

        /// <summary>
        /// Вертикальный П-стержень / хомут.
        /// </summary>
        public VertPkaData VertPka { get; }

        /// <summary>
        /// Шпильки.
        /// </summary>
        public ShpilkaData Shpilka { get; }

        /// <summary>
        /// Узел в начале линии палки.
        /// </summary>
        public NodeData StartNode { get; }

        /// <summary>
        /// Узел в конце линии палки.
        /// </summary>
        public NodeData EndNode { get; }

        /// <summary>
        /// Проверяет все блоки: не null и значения корректны. Ошибки — <see cref="VertArmDesignException"/>
        /// со списком по всем блокам.
        /// </summary>
        private void Validate()
        {
            var errors = new List<string>();
            Check(errors, nameof(VertArm), VertArm, b => b.Errors());
            Check(errors, nameof(VertArmDop), VertArmDop, b => b.Errors());
            Check(errors, nameof(VertArmTortsa), VertArmTortsa, b => b.Errors());
            Check(errors, nameof(GorArm), GorArm, b => b.Errors());
            Check(errors, nameof(GorPka), GorPka, b => b.Errors());
            Check(errors, nameof(VertPka), VertPka, b => b.Errors());
            Check(errors, nameof(Shpilka), Shpilka, b => b.Errors());

            // Узлы проверяются вместе с армированием, из которого берутся диаметры.
            if (VertArmTortsa != null && GorArm != null)
            {
                CheckNode(errors, nameof(StartNode), PalkaRules.StartEnd, StartNode);
                CheckNode(errors, nameof(EndNode), PalkaRules.EndEnd, EndNode);
            }

            PalkaRules.ThrowIfAny(errors);
        }

        private void CheckNode(List<string> errors, string name, string end, NodeData node)
        {
            if (node == null)
                errors.Add($"{name}: узел не задан (null). Если узла нет — NodeData.None.");
            else
                errors.AddRange(PalkaRules.Node(end, node.VertArmTortsa, node.GorPka, node.Type, VertArmTortsa.Diameter, GorArm.Diameter));
        }

        private static void Check<T>(List<string> errors, string name, T block, Func<T, List<string>> blockErrors) where T : class
        {
            if (block == null)
                errors.Add($"{name}: блок не задан (null). Если армирования нет — {name}Data.None.");
            else
                errors.AddRange(blockErrors(block));
        }

        /// <summary>
        /// Вертикальное армирование (ВертАрм).
        /// Row2Diameter = 0 — симметричное.
        /// </summary>
        public sealed class VertArmData
        {
            public VertArmData() { }

            /// <param name="diameter">
            /// ВертАрм_Диаметр, мм. 0 — ВертАрм нет.
            /// </param>
            /// <param name="row2Diameter">
            /// ВертАрм_2ряд_Диаметр, мм. 0 — симметричное армирование.
            /// </param>
            /// <param name="step">
            /// ВертАрм_Шаг, мм.
            /// </param>
            [SetsRequiredMembers]
            public VertArmData(int diameter, int row2Diameter, int step)
            {
                Diameter = diameter;
                Row2Diameter = row2Diameter;
                Step = step;
            }

            /// <summary>
            /// Симметричное армирование
            /// </summary>
            /// <param name="diameter">
            /// ВертАрм_Диаметр, мм. 0 — ВертАрм нет.
            /// </param>
            /// <param name="step">
            /// ВертАрм_Шаг, мм.
            /// </param>
            [SetsRequiredMembers]
            public VertArmData(int diameter, int step) : this(diameter, 0, step) { }

            /// <summary>
            /// ВертАрм нет.
            /// </summary>
            public static VertArmData None => new VertArmData(0, 0, 0);

            /// <summary>
            /// ВертАрм_Диаметр, мм.
            /// </summary>
            public required int Diameter { get; init; }

            /// <summary>
            /// ВертАрм_2ряд_Диаметр, мм. 0 — симметричное армирование.
            /// </summary>
            public required int Row2Diameter { get; init; }

            /// <summary>
            /// ВертАрм_Шаг, мм.
            /// </summary>
            public required int Step { get; init; }

            internal List<string> Errors() => PalkaRules.VertArm(Diameter, Row2Diameter, Step);
        }

        /// <summary>
        /// Дополнительное вертикальное армирование (ВертАрм_Доп).
        /// </summary>
        public sealed class VertArmDopData
        {
            public VertArmDopData() { }

            /// <param name="diameter">
            /// ВертАрм_Доп_Диаметр, мм. 0 — нет.
            /// </param>
            /// <param name="row2Diameter">
            /// ВертАрм_Доп_2ряд_Диаметр, мм. 0 — нет.
            /// </param>
            /// <param name="tortsaDiameter">
            /// ВертАрмТорца_Доп_Диаметр, мм. 0 — нет.
            /// </param>
            [SetsRequiredMembers]
            public VertArmDopData(int diameter, int row2Diameter, int tortsaDiameter)
            {
                Diameter = diameter;
                Row2Diameter = row2Diameter;
                TortsaDiameter = tortsaDiameter;
            }

            /// <summary>
            /// Симметричное армирование: 2 ряд равен 1 ряду
            /// </summary>
            /// <param name="diameter">
            /// ВертАрм_Доп_Диаметр и ВертАрм_Доп_2ряд_Диаметр, мм.
            /// </param>
            /// <param name="tortsaDiameter">
            /// ВертАрмТорца_Доп_Диаметр, мм. 0 — нет.
            /// </param>
            public static VertArmDopData Symmetric(int diameter, int tortsaDiameter = 0) => new VertArmDopData(diameter, diameter, tortsaDiameter);

            /// <summary>
            /// Доп. армирования нет.
            /// </summary>
            public static VertArmDopData None => new VertArmDopData(0, 0, 0);

            /// <summary>
            /// ВертАрм_Доп_Диаметр, мм.
            /// </summary>
            public required int Diameter { get; init; }

            /// <summary>
            /// ВертАрм_Доп_2ряд_Диаметр, мм.
            /// </summary>
            public required int Row2Diameter { get; init; }

            /// <summary>
            /// ВертАрмТорца_Доп_Диаметр, мм.
            /// </summary>
            public required int TortsaDiameter { get; init; }

            internal List<string> Errors() => PalkaRules.VertArmDop(Diameter, Row2Diameter, TortsaDiameter);
        }

        /// <summary>
        /// Армирование торца (ВертАрмТорца).
        /// </summary>
        public sealed class VertArmTortsaData
        {
            public VertArmTortsaData() { }

            /// <param name="diameter">
            /// ВертАрмТорца_Диаметр, мм. 0 — нет.
            /// </param>
            /// <param name="step">
            /// ВертАрмТорца_Шаг, мм.
            /// </param>
            /// <param name="offset">
            /// ВертАрмТорца_Отступ, мм.
            /// </param>
            /// <param name="countInRow">
            /// ВертАрмТорца_КоличВРяду.
            /// </param>
            /// <param name="countPerEdge">
            /// ВертАрмТорца_КоличНаТорец.
            /// </param>
            [SetsRequiredMembers]
            public VertArmTortsaData(int diameter, int step, int offset, int countInRow, int countPerEdge)
            {
                Diameter = diameter;
                Step = step;
                Offset = offset;
                CountInRow = countInRow;
                CountPerEdge = countPerEdge;
            }

            /// <summary>
            /// Типовое армирование торца:
            /// 2 в ряду, 4 на торец.
            /// </summary>
            /// <param name="diameter">
            /// ВертАрмТорца_Диаметр, мм. 0 — нет.
            /// </param>
            /// <param name="step">
            /// ВертАрмТорца_Шаг, мм.
            /// </param>
            /// <param name="offset">
            /// ВертАрмТорца_Отступ, мм.
            /// </param>
            [SetsRequiredMembers]
            public VertArmTortsaData(int diameter, int step, int offset)
                : this(diameter, step, offset, 2, 4) { }

            /// <summary>
            /// Армирования торца нет.
            /// </summary>
            public static VertArmTortsaData None => new VertArmTortsaData(0, 0, 0, 0, 0);

            /// <summary>
            /// ВертАрмТорца_Диаметр, мм.
            /// </summary>
            public required int Diameter { get; init; }

            /// <summary>
            /// ВертАрмТорца_Шаг, мм.
            /// </summary>
            public required int Step { get; init; }

            /// <summary>
            /// ВертАрмТорца_Отступ, мм.
            /// </summary>
            public required int Offset { get; init; }

            /// <summary>
            /// ВертАрмТорца_КоличВРяду.
            /// </summary>
            public required int CountInRow { get; init; }

            /// <summary>
            /// ВертАрмТорца_КоличНаТорец.
            /// </summary>
            public required int CountPerEdge { get; init; }

            internal List<string> Errors() => PalkaRules.VertArmTortsa(Diameter, Step, Offset, CountInRow, CountPerEdge);
        }

        /// <summary>
        /// Горизонтальное армирование (ГорАрм).
        /// </summary>
        public sealed class GorArmData
        {
            public GorArmData() { }

            /// <param name="diameter">
            /// ГорАрм_Диаметр, мм. 0 — ГорАрм нет.
            /// </param>
            /// <param name="step">
            /// ГорАрм_Шаг, мм.
            /// </param>
            /// <param name="offset">
            /// ГорАрм_Отступ — снизу и сверху, мм.
            /// </param>
            /// <param name="offsetFromEdge">
            /// ГорАрм_ОтступОтТорца, мм.
            /// </param>
            /// <param name="inTopDrop">
            /// ГорАрм_ПерепадСверху.Вкл.
            /// </param>
            /// <param name="bottomZone">
            /// Нижняя зона учащения. Нет — <see cref="ZoneData.None"/>.
            /// </param>
            /// <param name="topZone">
            /// Верхняя зона учащения. Нет — <see cref="ZoneData.None"/>.
            /// </param>
            [SetsRequiredMembers]
            public GorArmData(int diameter, int step, int offset, int offsetFromEdge, bool inTopDrop, ZoneData bottomZone, ZoneData topZone)
            {
                Diameter = diameter;
                Step = step;
                Offset = offset;
                OffsetFromEdge = offsetFromEdge;
                InTopDrop = inTopDrop;
                BottomZone = bottomZone;
                TopZone = topZone;
            }

            /// <summary>
            /// Без зон учащения и армирования в перепаде
            /// </summary>
            /// <param name="diameter">
            /// ГорАрм_Диаметр, мм. 0 — ГорАрм нет.
            /// </param>
            /// <param name="step">
            /// ГорАрм_Шаг, мм.
            /// </param>
            /// <param name="offset">
            /// ГорАрм_Отступ — снизу и сверху, мм.
            /// </param>
            /// <param name="offsetFromEdge">
            /// ГорАрм_ОтступОтТорца, мм.
            /// </param>
            [SetsRequiredMembers]
            public GorArmData(int diameter, int step, int offset, int offsetFromEdge) : this(diameter, step, offset, offsetFromEdge, false, ZoneData.None, ZoneData.None) { }

            /// <summary>
            /// ГорАрм нет.
            /// </summary>
            public static GorArmData None => new GorArmData(0, 0, 0, 0, false, ZoneData.None, ZoneData.None);

            /// <summary>
            /// ГорАрм_Диаметр, мм.
            /// </summary>
            public required int Diameter { get; init; }

            /// <summary>
            /// ГорАрм_Шаг, мм.
            /// </summary>
            public required int Step { get; init; }

            /// <summary>
            /// ГорАрм_Отступ — снизу и сверху, мм.
            /// </summary>
            public required int Offset { get; init; }

            /// <summary>
            /// ГорАрм_ОтступОтТорца, мм.
            /// </summary>
            public required int OffsetFromEdge { get; init; }

            /// <summary>
            /// ГорАрм_ПерепадСверху.Вкл.
            /// </summary>
            public required bool InTopDrop { get; init; }

            /// <summary>
            /// Нижняя зона учащения.
            /// </summary>
            public required ZoneData BottomZone { get; init; }

            /// <summary>
            /// Верхняя зона учащения.
            /// </summary>
            public required ZoneData TopZone { get; init; }

            internal List<string> Errors()
            {
                var errors = PalkaRules.GorArm(Diameter, Step, Offset, OffsetFromEdge);

                if (BottomZone == null)
                    errors.Add("BottomZone: не задана (null). Если зоны нет — ZoneData.None.");
                else
                    errors.AddRange(PalkaRules.Zone("Нижняя зона учащения ГорАрм", BottomZone.Length, BottomZone.Step, Diameter));

                if (TopZone == null)
                    errors.Add("TopZone: не задана (null). Если зоны нет — ZoneData.None.");
                else
                    errors.AddRange(PalkaRules.Zone("Верхняя зона учащения ГорАрм", TopZone.Length, TopZone.Step, Diameter));

                return errors;
            }
        }

        /// <summary>
        /// Зона учащения ГорАрм.
        /// </summary>
        public sealed class ZoneData
        {
            /// <param name="length">
            /// OLP_ГорАрм_Нижняя/Верхняя зона учащения, мм. 0 — зоны нет.
            /// </param>
            /// <param name="step">
            /// ГорАрм_ДопШагСнизу/Сверху, мм.
            /// </param>
            [SetsRequiredMembers]
            public ZoneData(int length, int step)
            {
                Length = length;
                Step = step;
            }

            /// <summary>
            /// Зоны нет.
            /// </summary>
            public static ZoneData None => new ZoneData(0, 0);

            /// <summary>
            /// Длина зоны, мм.
            /// </summary>
            public required int Length { get; init; }

            /// <summary>
            /// Шаг в зоне, мм.
            /// </summary>
            public required int Step { get; init; }
        }

        /// <summary>
        /// Горизонтальный П-стержень (ГорПка). Только стена.
        /// Пока пустой: диаметр берётся из ГорАрм, наличие в узле — <see cref="NodeData.GorPka"/>.
        /// </summary>
        public sealed class GorPkaData
        {
            public GorPkaData() { }

            /// <summary>
            /// ГорПка нет. У пилона — всегда.
            /// </summary>
            public static GorPkaData None => new GorPkaData();

            // TODO ГорПка_Диаметр: пока не задаётся, в палку пишется диаметр ГорАрм.
            ///// <summary>
            ///// ГорПка_Диаметр, мм.
            ///// </summary>
            //public required int Diameter { get; init; }

            internal List<string> Errors() => new List<string>();
        }

        /// <summary>
        /// Узел в конце линии палки.
        /// Узел есть, если в нём есть ВертАрмТорца или ГорПка.
        /// </summary>
        public sealed class NodeData
        {
            public NodeData() { }

            /// <param name="vertArmTortsa">
            /// ВертАрмТорца_Начало/Конец.Вкл.
            /// </param>
            /// <param name="gorPka">
            /// ГорПка_Начало/Конец.Вкл. У пилона — false.
            /// </param>
            /// <param name="type">
            /// Тип узла: «Торец», «Г-узел», «Т-узел», «ГТС». У пилона — «Торец». Хранится в ES.
            /// </param>
            [SetsRequiredMembers]
            public NodeData(bool vertArmTortsa, bool gorPka, string type)
            {
                VertArmTortsa = vertArmTortsa;
                GorPka = gorPka;
                Type = type;
            }

            /// <summary>
            /// Узел пилона: только ВертАрмТорца, тип «Торец».
            /// </summary>
            /// <param name="vertArmTortsa">
            /// ВертАрмТорца_Начало/Конец.Вкл.
            /// </param>
            public static NodeData Pilon(bool vertArmTortsa) => new NodeData(vertArmTortsa, false, DesignValues.NodeTorets);

            /// <summary>
            /// Узла нет.
            /// </summary>
            public static NodeData None => new NodeData(false, false, DesignValues.NodeTorets);

            /// <summary>
            /// ВертАрмТорца_Начало/Конец.Вкл.
            /// </summary>
            public required bool VertArmTortsa { get; init; }

            /// <summary>
            /// ГорПка_Начало/Конец.Вкл.
            /// </summary>
            public required bool GorPka { get; init; }

            /// <summary>
            /// Тип узла. Хранится в ES.
            /// </summary>
            public required string Type { get; init; }

            /// <summary>
            /// Узел есть.
            /// </summary>
            public bool Exists => VertArmTortsa || GorPka;
        }

        /// <summary>
        /// Вертикальный П-стержень / хомут (ВертПка).
        /// </summary>
        public sealed class VertPkaData
        {
            public VertPkaData() { }

            /// <param name="diameter">
            /// ВертПка_Диаметр, мм. 0 — ВертПка нет.
            /// </param>
            /// <param name="step">
            /// Шаг ВертПка, мм. Хранится в ES, в палке параметра нет.
            /// </param>
            [SetsRequiredMembers]
            public VertPkaData(int diameter, int step)
            {
                Diameter = diameter;
                Step = step;
            }

            /// <summary>
            /// ВертПка нет.
            /// </summary>
            public static VertPkaData None => new VertPkaData(0, 0);

            /// <summary>
            /// ВертПка_Диаметр, мм.
            /// </summary>
            public required int Diameter { get; init; }

            /// <summary>
            /// Шаг ВертПка, мм. Хранится в ES, в палке параметра нет.
            /// </summary>
            public required int Step { get; init; }

            internal List<string> Errors() => PalkaRules.VertPka(Diameter, Step);
        }

        /// <summary>
        /// Шпильки.
        /// </summary>
        public sealed class ShpilkaData
        {
            public ShpilkaData() { }

            /// <param name="diameter">
            /// Шпилька_Диаметр, мм. 0 — шпилек нет.
            /// </param>
            [SetsRequiredMembers]
            public ShpilkaData(int diameter) => Diameter = diameter;

            /// <summary>
            /// Шпилек нет.
            /// </summary>
            public static ShpilkaData None => new ShpilkaData(0);

            /// <summary>
            /// Шпилька_Диаметр, мм.
            /// </summary>
            public required int Diameter { get; init; }

            internal List<string> Errors() => PalkaRules.Diameter("Шпилька_Диаметр", Diameter);
        }
    }
}
