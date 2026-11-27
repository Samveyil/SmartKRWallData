using System;
using System.Runtime.Serialization;

namespace SmartKRWallData.Design
{
    // ---------------- Вертикальное армирование ----------------

    [DataContract]
    public class VertArmSection
    {
        public VertArmSection() => Init();

        [OnDeserializing]
        private void OnDeserializing(StreamingContext _) => Init();

        private void Init()
        {
            Row1 = new VertArmRow();
            Row2 = new VertArmRow();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext _)
        {
            if (Row1 == null) Row1 = new VertArmRow();
            if (Row2 == null) Row2 = new VertArmRow();
        }

        /// <summary>Создавать армирование: вычисляется, диаметр > 0. Пишется в ВертАрм.Вкл.</summary>
        [PalkaParam("ВертАрм.Вкл")]
        public bool Enabled => Diameter > 0;

        /// <summary>Диаметр 1 ряда, мм; 0 — нет ВертАрм.</summary>
        [PalkaParam("ВертАрм_Диаметр")]
        public int Diameter { get; internal set; }

        /// <summary>Диаметр 2 ряда, мм; 0 — 2 ряд как 1 ряд (симметричное армирование).</summary>
        [PalkaParam("ВертАрм_2ряд_Диаметр")]
        public int Row2Diameter { get; internal set; }

        /// <summary>Симметричное армирование: вычисляется, диаметр 2 ряда = 0.</summary>
        public bool Symmetrical => Row2Diameter == 0;

        /// <summary>Шаг (общий для обоих рядов), мм.</summary>
        [PalkaParam("ВертАрм_Шаг")]
        public int Step { get; internal set; }

        /// <summary>Кратность стержней.</summary>
        [DataMember(Name = "lengthMultiple", Order = 0)]
        public bool LengthMultiple { get; set; }

        /// <summary>Отступ от грани снизу, мм.</summary>
        [DataMember(Name = "offsetFromBottom", Order = 1)]
        public int OffsetFromBottom { get; set; }

        [DataMember(Name = "row1", Order = 2)]
        public VertArmRow Row1 { get; private set; }

        /// <summary>Настройки 2 ряда; используются только при несимметричном армировании.</summary>
        [DataMember(Name = "row2", Order = 3)]
        public VertArmRow Row2 { get; private set; }
    }

    [DataContract]
    public class VertArmRow
    {
        public VertArmRow() => Init();

        [OnDeserializing]
        private void OnDeserializing(StreamingContext _) => Init();

        private void Init()
        {
            Form = DesignValues.Form1;
            AnchorSide = DesignValues.AnchorLeft;
        }

        /// <summary>«1», «101_Г», «206_Г».</summary>
        [DataMember(Name = "form", Order = 0)]
        public string Form { get; set; }

        /// <summary>Формировать выпуск. Для формы «101_Г» выпуск невозможен.</summary>
        [DataMember(Name = "starterBars", Order = 1)]
        public bool StarterBars { get; set; }

        /// <summary>Длина выпуска, мм. Учитывается только при StarterBars = true (пока только хранится).</summary>
        [DataMember(Name = "starterLength", Order = 2)]
        public int StarterLength { get; set; }

        /// <summary>Отступ от верха плиты, мм. Учитывается только при StarterBars = false.</summary>
        [DataMember(Name = "offsetFromTopOfSlab", Order = 3)]
        public int OffsetFromTopOfSlab { get; set; }

        /// <summary>«Влево», «Вправо».</summary>
        [DataMember(Name = "anchorSide", Order = 4)]
        public string AnchorSide { get; set; }
    }

    // ---------------- Дополнительное вертикальное армирование ----------------

    [DataContract]
    public class VertArmDopSection
    {
        public VertArmDopSection() => Init();

        [OnDeserializing]
        private void OnDeserializing(StreamingContext _) => Init();

        private void Init()
        {
            Row1 = new DopRow();
            Row2 = new DopRow();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext _)
        {
            if (Row1 == null) Row1 = new DopRow();
            if (Row2 == null) Row2 = new DopRow();
        }

        /// <summary>Создавать армирование: вычисляется, есть 1 или 2 ряд.</summary>
        public bool Enabled => Row1Enabled || Row2Enabled;

        /// <summary>Диаметр 1 ряда, мм; 0 — нет.</summary>
        [PalkaParam("ВертАрм_Доп_Диаметр")]
        public int Row1Diameter { get; internal set; }

        /// <summary>1 ряд есть: вычисляется, диаметр > 0.</summary>
        public bool Row1Enabled => Row1Diameter > 0;

        /// <summary>Диаметр 2 ряда, мм; 0 — нет.</summary>
        [PalkaParam("ВертАрм_Доп_2ряд_Диаметр")]
        public int Row2Diameter { get; internal set; }

        /// <summary>2 ряд есть: вычисляется, диаметр > 0.</summary>
        public bool Row2Enabled => Row2Diameter > 0;

        /// <summary>Диаметр доп. армирования торца, мм; 0 — нет.</summary>
        [PalkaParam("ВертАрмТорца_Доп_Диаметр")]
        public int EdgeDiameter { get; internal set; }

        /// <summary>Доп. армирование торца есть: вычисляется, диаметр > 0.</summary>
        public bool EdgeEnabled => EdgeDiameter > 0;

        /// <summary>Кратность стержней.</summary>
        [DataMember(Name = "lengthMultiple", Order = 0)]
        public bool LengthMultiple { get; set; }

        [DataMember(Name = "symmetrical", Order = 1)]
        public bool Symmetrical { get; set; }

        /// <summary>Анкеровка, мм.</summary>
        [DataMember(Name = "anchorage", Order = 2)]
        public int Anchorage { get; set; }

        [DataMember(Name = "row1", Order = 3)]
        public DopRow Row1 { get; private set; }

        [DataMember(Name = "row2", Order = 4)]
        public DopRow Row2 { get; private set; }
    }

    /// <summary>Ряд доп. армирования.</summary>
    [DataContract]
    public class DopRow
    {
        public DopRow() => Init();

        [OnDeserializing]
        private void OnDeserializing(StreamingContext _) => Init();

        private void Init() => Form = DesignValues.Form1;

        /// <summary>«1», «101_Г», «206_Г».</summary>
        [DataMember(Name = "form", Order = 0)]
        public string Form { get; set; }

        /// <summary>Длина выпуска, мм; 0 — без выпуска. Для формы «101_Г» — 0 (пока только хранится).</summary>
        [DataMember(Name = "starterLength", Order = 1)]
        public int StarterLength { get; set; }
    }

    // ---------------- Вертикальное армирование торца ----------------

    [DataContract]
    public class VertArmTortsaSection
    {
        public VertArmTortsaSection() => Init();

        [OnDeserializing]
        private void OnDeserializing(StreamingContext _) => Init();

        private void Init() => Form = DesignValues.Form1;

        /// <summary>Диаметр, мм; 0 — нет армирования торца.</summary>
        [PalkaParam("ВертАрмТорца_Диаметр")]
        public int Diameter { get; internal set; }

        /// <summary>Создавать армирование: вычисляется, диаметр > 0.</summary>
        public bool Enabled => Diameter > 0;

        /// <summary>Шаг, мм.</summary>
        [PalkaParam("ВертАрмТорца_Шаг")]
        public int Step { get; internal set; }

        /// <summary>Отступ от торца, мм.</summary>
        [PalkaParam("ВертАрмТорца_Отступ")]
        public int Offset { get; internal set; }

        [PalkaParam("ВертАрмТорца_КоличВРяду")]
        public int CountInRow { get; internal set; }

        [PalkaParam("ВертАрмТорца_КоличНаТорец")]
        public int CountPerEdge { get; internal set; }

        /// <summary>«1», «101_Г», «206_Г».</summary>
        [DataMember(Name = "form", Order = 0)]
        public string Form { get; set; }

        /// <summary>Длина выпуска, мм; 0 — без выпуска. Для формы «101_Г» — 0 (пока только хранится).</summary>
        [DataMember(Name = "starterLength", Order = 1)]
        public int StarterLength { get; set; }
    }

    // ---------------- Горизонтальный П-стержень ----------------

    /// <summary>ГорПка. Только стена. Целиком хранится в параметрах палки; наличие в узле — <see cref="NodeSection.GorPka"/>.</summary>
    public class GorPkaSection
    {
        /// <summary>
        /// ГорПка_Диаметр, мм. У пилона параметра нет.
        /// TODO ГорПка_Диаметр: пока равен диаметру ГорАрм (если ГорПка есть хотя бы в одном узле).
        /// </summary>
        [PalkaParam("ГорПка_Диаметр", WallOnly = true)]
        public int Diameter { get; internal set; }
    }

    // ---------------- Узлы ----------------

    /// <summary>
    /// Узел у одного из концов линии палки: «Начало» — первая точка линии, «Конец» — вторая.
    /// Узел есть, если в нём ВертАрмТорца или ГорПка. Задаётся через Data (NodeData).
    /// </summary>
    [DataContract]
    public class NodeSection
    {
        public NodeSection() => Init();

        [OnDeserializing]
        private void OnDeserializing(StreamingContext _) => Init();

        private void Init() => Type = DesignValues.NodeTorets;

        /// <summary>Узел есть: вычисляется, ВертАрмТорца или ГорПка.</summary>
        public bool Enabled => VertArmTortsa || GorPka;

        /// <summary>«Торец», «Г-узел», «Т-узел», «ГТС». У пилона — «Торец».</summary>
        [DataMember(Name = "type", Order = 0)]
        public string Type { get; internal set; }

        /// <summary>ВертАрмТорца в узле.</summary>
        [PalkaParam("ВертАрмТорца_{end}.Вкл")]
        public bool VertArmTortsa { get; internal set; }

        /// <summary>Горизонтальный П-стержень в узле. Только для стены: у пилона параметра нет.</summary>
        [PalkaParam("ГорПка_{end}.Вкл", WallOnly = true)]
        public bool GorPka { get; internal set; }
    }

    // ---------------- Горизонтальное армирование ----------------

    [DataContract]
    public class GorArmSection
    {
        public GorArmSection() => Init();

        [OnDeserializing]
        private void OnDeserializing(StreamingContext _) => Init();

        private void Init()
        {
            Form = DesignValues.Form1;
            BottomZone = new DenseZone();
            TopZone = new DenseZone();
        }

        /// <summary>Создавать армирование: вычисляется, диаметр > 0. Пишется в ГорАрм.Вкл.</summary>
        [PalkaParam("ГорАрм.Вкл")]
        public bool Enabled => Diameter > 0;

        /// <summary>Диаметр, мм.</summary>
        [PalkaParam("ГорАрм_Диаметр")]
        public int Diameter { get; internal set; }

        /// <summary>Шаг, мм.</summary>
        [PalkaParam("ГорАрм_Шаг")]
        public int Step { get; internal set; }

        /// <summary>Отступ снизу и сверху (одинаковый), мм.</summary>
        [PalkaParam("ГорАрм_Отступ")]
        public int Offset { get; internal set; }

        /// <summary>Отступ от торца, мм.</summary>
        [PalkaParam("ГорАрм_ОтступОтТорца")]
        public int OffsetFromEdge { get; internal set; }

        /// <summary>Нижняя зона учащения.</summary>
        [ZoneParams("ГорАрм_ДопШагСнизу", "OLP_ГорАрм_Нижняя зона учащения")]
        public DenseZone BottomZone { get; private set; }

        /// <summary>Верхняя зона учащения.</summary>
        [ZoneParams("ГорАрм_ДопШагСверху", "OLP_ГорАрм_Верхняя зона учащения")]
        public DenseZone TopZone { get; private set; }

        /// <summary>Армирование в перепаде.</summary>
        [PalkaParam("ГорАрм_ПерепадСверху.Вкл")]
        public bool InTopDrop { get; internal set; }

        /// <summary>Стена: «1»; пилон: «201_П», «501_Х», «603_Х».</summary>
        [DataMember(Name = "form", Order = 0)]
        public string Form { get; set; }
    }

    /// <summary>Зона учащения ГорАрм. Целиком хранится в параметрах палки.</summary>
    public class DenseZone
    {
        /// <summary>Зона есть: вычисляется, длина > 0. Пишется в «{step}.Вкл».</summary>
        [PalkaParam("{step}.Вкл")]
        public bool Enabled => Length > 0;

        /// <summary>Длина зоны, мм.</summary>
        [PalkaParam("{length}")]
        public int Length { get; internal set; }

        /// <summary>Шаг в зоне, мм.</summary>
        [PalkaParam("{step}")]
        public int Step { get; internal set; }
    }

    // ---------------- Защитный слой ----------------

    [DataContract]
    public class CoverSection
    {
        /// <summary>Защитный слой слева, мм.</summary>
        [DataMember(Name = "left", Order = 0)]
        public int Left { get; set; }

        /// <summary>Защитный слой справа, мм.</summary>
        [DataMember(Name = "right", Order = 1)]
        public int Right { get; set; }
    }

    // ---------------- Вертикальный П-стержень / хомут ----------------

    [DataContract]
    public class VertPkaSection
    {
        public VertPkaSection() => Init();

        [OnDeserializing]
        private void OnDeserializing(StreamingContext _) => Init();

        private void Init() => Form = DesignValues.Form201P;

        /// <summary>Создавать армирование: вычисляется, диаметр > 0. Пишется в ВертПка.Вкл.</summary>
        [PalkaParam("ВертПка.Вкл")]
        public bool Enabled => Diameter > 0;

        /// <summary>Диаметр, мм.</summary>
        [PalkaParam("ВертПка_Диаметр")]
        public int Diameter { get; internal set; }

        /// <summary>Шаг, мм. Задаётся через Data (VertPkaData), хранится в ES.</summary>
        [DataMember(Name = "step", Order = 0)]
        public int Step { get; internal set; }

        /// <summary>«201_П», «501_Х», «603_Х».</summary>
        [DataMember(Name = "form", Order = 1)]
        public string Form { get; set; }

        /// <summary>Отступ от грани, мм.</summary>
        [DataMember(Name = "offset", Order = 2)]
        public int Offset { get; set; }

        /// <summary>П-стержень снизу (только для 201_П).</summary>
        [DataMember(Name = "bottom", Order = 3)]
        public bool Bottom { get; set; }

        /// <summary>П-стержень сверху (только для 201_П).</summary>
        [DataMember(Name = "top", Order = 4)]
        public bool Top { get; set; }

        /// <summary>Горизонтальное усиление снизу.</summary>
        [DataMember(Name = "horReinfBottom", Order = 5)]
        public bool HorReinfBottom { get; set; }

        /// <summary>Горизонтальное усиление сверху.</summary>
        [DataMember(Name = "horReinfTop", Order = 6)]
        public bool HorReinfTop { get; set; }
    }

    // ---------------- Шпильки ----------------

    [DataContract]
    public class ShpilkaSection
    {
        /// <summary>Создавать армирование: вычисляется, диаметр > 0. Пишется в Шпилька.Вкл.</summary>
        [PalkaParam("Шпилька.Вкл")]
        public bool Enabled => Diameter > 0;

        /// <summary>Диаметр, мм.</summary>
        [PalkaParam("Шпилька_Диаметр")]
        public int Diameter { get; internal set; }

        /// <summary>Длина отгиба, мм.</summary>
        [DataMember(Name = "hookLength", Order = 0)]
        public int HookLength { get; set; }

        /// <summary>Диаметр оправки, мм.</summary>
        [DataMember(Name = "mandrelDiameter", Order = 1)]
        public int MandrelDiameter { get; set; }

        /// <summary>Шпилька в плите.</summary>
        [DataMember(Name = "inSlab", Order = 2)]
        public bool InSlab { get; set; }
    }
}
