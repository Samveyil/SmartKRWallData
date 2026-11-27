using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using SmartKRWallData.Data;
using SmartKRWallData.Validation;

// Общий формат данных ВертАрм для палки (285_). Этот файл не зависит от Revit API и одинаков
// во всех плагинах, которые пишут или читают данные палки (SmartKR, rvtVertArmUtils).
//
// Где хранится поле:
//   [PalkaParam("…")] — параметр семейства палки (в JSON не попадает);
//   [DataMember]      — JSON в Extensible Storage палки.
//
// Единицы: точки (points) — внутренние единицы Revit, как XYZ из API; все остальные длины — мм, целые.
//
// Создание: new VertArmDesign(армирование, геометрия, оформление) — обязательные параметры палки (папка Data),
// затем ES-поля разделов. Разделы никогда не null: заменить их нельзя (private set), при чтении JSON
// отсутствующие разделы создаются пустыми.

namespace SmartKRWallData.Design
{
    /// <summary>Данные для построения ВертАрм по одной палке.</summary>
    [DataContract]
    public class VertArmDesign
    {
        /// <summary>Версия формата. Увеличивается при несовместимых изменениях.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>Данные палки: обязательные параметры семейства, затем ES-поля разделов.</summary>
        /// <param name="reinforcement">Армирование в параметрах палки.</param>
        /// <param name="wall">Геометрия стены.</param>
        /// <param name="drawing">Оформление: марка, комплект, этаж.</param>
        public VertArmDesign(PalkaReinforcementData reinforcement, WallGeometryData wall, DrawingInfoData drawing)
        {
            if (reinforcement == null)
                throw new ArgumentNullException(nameof(reinforcement));
            if (wall == null)
                throw new ArgumentNullException(nameof(wall));
            if (drawing == null)
                throw new ArgumentNullException(nameof(drawing));

            // Пилон или стена — по геометрии; при записи сверяется с семейством палки.
            var isPilon = wall.Base.ElementType == GeometryElementType.Pilon;
            if (isPilon)
            {
                var errors = new List<string>();
                errors.AddRange(PalkaRules.PilonNode(PalkaRules.StartEnd, reinforcement.StartNode.VertArmTortsa,
                    reinforcement.StartNode.GorPka, reinforcement.StartNode.Type));
                errors.AddRange(PalkaRules.PilonNode(PalkaRules.EndEnd, reinforcement.EndNode.VertArmTortsa,
                    reinforcement.EndNode.GorPka, reinforcement.EndNode.Type));
                PalkaRules.ThrowIfAny(errors);
            }

            Init();
            IsPilon = isPilon;
            Apply(reinforcement);
            Apply(wall);
            Apply(drawing);
        }

        /// <summary>Пустой объект: для чтения из ES и палки.</summary>
        internal VertArmDesign() => Init();

        [OnDeserializing]
        private void OnDeserializing(StreamingContext _) => Init();

        /// <summary>В JSON раздел может быть явно null («"vertPka": null») — заменяем пустым.</summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext _)
        {
            if (Points == null) Points = new DesignPoints();
            if (VertArm == null) VertArm = new VertArmSection();
            if (VertArmDop == null) VertArmDop = new VertArmDopSection();
            if (VertArmTortsa == null) VertArmTortsa = new VertArmTortsaSection();
            if (GorArm == null) GorArm = new GorArmSection();
            if (StartNode == null) StartNode = new NodeSection();
            if (EndNode == null) EndNode = new NodeSection();
            if (Cover == null) Cover = new CoverSection();
            if (VertPka == null) VertPka = new VertPkaSection();
            if (Shpilka == null) Shpilka = new ShpilkaSection();
        }

        private void Init()
        {
            SchemaVersion = CurrentSchemaVersion;
            Wall = new WallSection();
            Drawing = new DrawingSection();
            Points = new DesignPoints();
            VertArm = new VertArmSection();
            VertArmDop = new VertArmDopSection();
            VertArmTortsa = new VertArmTortsaSection();
            GorArm = new GorArmSection();
            GorPka = new GorPkaSection();
            StartNode = new NodeSection();
            EndNode = new NodeSection();
            Cover = new CoverSection();
            VertPka = new VertPkaSection();
            Shpilka = new ShpilkaSection();
        }

        private void Apply(PalkaReinforcementData r)
        {
            VertArm.Diameter = r.VertArm.Diameter;
            VertArm.Row2Diameter = r.VertArm.Row2Diameter;
            VertArm.Step = r.VertArm.Step;

            VertArmDop.Row1Diameter = r.VertArmDop.Diameter;
            VertArmDop.Row2Diameter = r.VertArmDop.Row2Diameter;
            VertArmDop.EdgeDiameter = r.VertArmDop.TortsaDiameter;

            VertArmTortsa.Diameter = r.VertArmTortsa.Diameter;
            VertArmTortsa.Step = r.VertArmTortsa.Step;
            VertArmTortsa.Offset = r.VertArmTortsa.Offset;
            VertArmTortsa.CountInRow = r.VertArmTortsa.CountInRow;
            VertArmTortsa.CountPerEdge = r.VertArmTortsa.CountPerEdge;

            GorArm.Diameter = r.GorArm.Diameter;
            GorArm.Step = r.GorArm.Step;
            GorArm.Offset = r.GorArm.Offset;
            GorArm.OffsetFromEdge = r.GorArm.OffsetFromEdge;
            GorArm.InTopDrop = r.GorArm.InTopDrop;
            GorArm.BottomZone.Length = r.GorArm.BottomZone.Length;
            GorArm.BottomZone.Step = r.GorArm.BottomZone.Step;
            GorArm.TopZone.Length = r.GorArm.TopZone.Length;
            GorArm.TopZone.Step = r.GorArm.TopZone.Step;

            // TODO ГорПка_Диаметр: пока берём диаметр ГорАрм (только если ГорПка есть — у пилона должен быть 0).
            GorPka.Diameter = r.StartNode.GorPka || r.EndNode.GorPka ? r.GorArm.Diameter : 0;

            Apply(StartNode, r.StartNode);
            Apply(EndNode, r.EndNode);

            VertPka.Diameter = r.VertPka.Diameter;
            VertPka.Step = r.VertPka.Step;
            Shpilka.Diameter = r.Shpilka.Diameter;
        }

        private static void Apply(NodeSection node, PalkaReinforcementData.NodeData n)
        {
            node.VertArmTortsa = n.VertArmTortsa;
            node.GorPka = n.GorPka;
            node.Type = n.Type;
        }

        private void Apply(WallGeometryData w)
        {
            // В палку пишется только основная стена; нижняя и верхняя пока никуда.
            Wall.BottomElevation = w.Base.BottomElevation;
            Wall.TopElevation = w.Base.TopElevation;
            Wall.Thickness = w.Base.Thickness;
            Wall.TopDrop = w.Base.TopDrop;
            Wall.BaseHeight2 = w.Base.BaseHeight2;
            Wall.TopDrop2 = w.Base.TopDrop2;
        }

        private void Apply(DrawingInfoData d)
        {
            Drawing.Mark = d.Mark;
            Drawing.DrawingSet = d.DrawingSet;
            Drawing.Floor = d.Floor;
            Drawing.Section = d.Section;
            Drawing.Building = d.Building;
        }

        [DataMember(Name = "schemaVersion", Order = 0)]
        public int SchemaVersion { get; internal set; }

        /// <summary>Геометрия стены (параметры палки).</summary>
        public WallSection Wall { get; private set; }

        /// <summary>Оформление (параметры палки).</summary>
        public DrawingSection Drawing { get; private set; }

        /// <summary>
        /// Палка — пилон (семейство «285_ВертАрм_Пилон»). Заполняется автоматически по семейству палки
        /// в VertArmDesignStorage.Write / Read / Validate(palka, …) — задавать не нужно.
        /// У пилона нет ГорПка в узлах, формы ГорАрм — «201_П», «501_Х», «603_Х»; у стены — «1».
        /// </summary>
        [DataMember(Name = "isPilon", Order = 1)]
        public bool IsPilon { get; internal set; }

        [DataMember(Name = "points", Order = 2)]
        public DesignPoints Points { get; private set; }

        /// <summary>Вертикальное армирование.</summary>
        [DataMember(Name = "vertArm", Order = 3)]
        public VertArmSection VertArm { get; private set; }

        /// <summary>Дополнительное вертикальное армирование.</summary>
        [DataMember(Name = "vertArmDop", Order = 4)]
        public VertArmDopSection VertArmDop { get; private set; }

        /// <summary>Вертикальное армирование торца.</summary>
        [DataMember(Name = "vertArmTortsa", Order = 5)]
        public VertArmTortsaSection VertArmTortsa { get; private set; }

        /// <summary>Горизонтальное армирование.</summary>
        [DataMember(Name = "gorArm", Order = 6)]
        public GorArmSection GorArm { get; private set; }

        /// <summary>Горизонтальный П-стержень (параметры палки).</summary>
        public GorPkaSection GorPka { get; private set; }

        /// <summary>Узел в начале линии палки (параметры палки «…_Начало»).</summary>
        [DataMember(Name = "startNode", Order = 7)]
        [PalkaEnd("Начало")]
        public NodeSection StartNode { get; private set; }

        /// <summary>Узел в конце линии палки (параметры палки «…_Конец»).</summary>
        [DataMember(Name = "endNode", Order = 8)]
        [PalkaEnd("Конец")]
        public NodeSection EndNode { get; private set; }

        /// <summary>Защитный слой.</summary>
        [DataMember(Name = "cover", Order = 9)]
        public CoverSection Cover { get; private set; }

        /// <summary>Вертикальный П-стержень / хомут.</summary>
        [DataMember(Name = "vertPka", Order = 10)]
        public VertPkaSection VertPka { get; private set; }

        /// <summary>Шпильки.</summary>
        [DataMember(Name = "shpilka", Order = 11)]
        public ShpilkaSection Shpilka { get; private set; }
    }

    // ---------------- Геометрия стены и оформление (параметры палки) ----------------

    /// <summary>Геометрия стены. Целиком хранится в параметрах палки.</summary>
    public class WallSection
    {
        /// <summary>Отметка низа, мм.</summary>
        [PalkaParam("OLP_Отметка низа", Guid = "8a58ad74-0e15-499b-bcaf-35b45cd7fc1f")]
        public int BottomElevation { get; internal set; }

        /// <summary>Отметка верха, мм.</summary>
        [PalkaParam("OLP_Отметка верха", Guid = "ec353104-09f1-44f4-85cf-1d638dce02d3")]
        public int TopElevation { get; internal set; }

        /// <summary>Толщина стены, мм.</summary>
        [PalkaParam("OLP_Толщина стены", Guid = "a506ea75-dab1-4c28-8921-c59c841ebf70")]
        public int Thickness { get; internal set; }

        /// <summary>Перепад сверху, мм; 0 — нет перепада.</summary>
        [PalkaParam("ПерепадСверху_Толщина")]
        public int TopDrop { get; internal set; }

        /// <summary>Высота основы 2 этажа, мм; 0 — армирование на один этаж.</summary>
        [PalkaParam("ВысотаОсновы_2этаж")]
        public int BaseHeight2 { get; internal set; }

        /// <summary>Перепад сверху на 2 этаже, мм.</summary>
        [PalkaParam("ПерепадСверху_2этаж_Толщина")]
        public int TopDrop2 { get; internal set; }
    }

    /// <summary>Оформление. Целиком хранится в параметрах палки.</summary>
    public class DrawingSection
    {
        /// <summary>Номер палки.</summary>
        [PalkaParam("ADSK_Марка изделия", Guid = "92ae0425-031b-40a9-8904-023f7389963b")]
        public string Mark { get; internal set; }

        [PalkaParam("ADSK_Комплект чертежей", Guid = "e1b06433-f527-403c-8986-af9a01e6be7f")]
        public string DrawingSet { get; internal set; }

        /// <summary>Этаж (число, без единиц).</summary>
        [PalkaParam("OLP_Этаж", Guid = "f59036bb-fa3d-40a8-bcf6-f6bbc54f26b6")]
        public double Floor { get; internal set; }

        /// <summary>Номер секции; может быть пустым.</summary>
        [PalkaParam("OLP_Номер секции")]
        public string Section { get; internal set; }

        /// <summary>Номер здания; может быть пустым.</summary>
        [PalkaParam("ADSK_Номер здания")]
        public string Building { get; internal set; }
    }

    /// <summary>Точки построения: правый конец стены.</summary>
    [DataContract]
    public class DesignPoints
    {
        /// <summary>На разрезе: правый нижний угол стены (сейчас пользователь указывает его кликом).</summary>
        [DataMember(Name = "section", Order = 0)]
        public PointXyz Section { get; set; }

        /// <summary>На узле (горизонтальный разрез): правый угол торца стены. Необязательна.</summary>
        [DataMember(Name = "node", Order = 1)]
        public PointXyz Node { get; set; }
    }

    /// <summary>Точка во внутренних единицах Revit, координаты модели — как XYZ из API: new XYZ(X, Y, Z).</summary>
    [DataContract]
    public class PointXyz
    {
        public PointXyz() { }

        public PointXyz(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        [DataMember(Name = "x", Order = 0)] public double X { get; set; }
        [DataMember(Name = "y", Order = 1)] public double Y { get; set; }
        [DataMember(Name = "z", Order = 2)] public double Z { get; set; }
    }
}
