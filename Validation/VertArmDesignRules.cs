using System;
using System.Collections.Generic;
using System.Linq;
using SmartKRWallData.Design;

namespace SmartKRWallData.Validation
{
    /// <summary>Данные ВертАрм заполнены неверно. <see cref="Errors"/> — список ошибок, по одной на строку.</summary>
    [Serializable]
    public sealed class VertArmDesignException : Exception
    {
        public VertArmDesignException(IList<string> errors)
            : base("Данные ВертАрм заполнены неверно:\n" + string.Join("\n", errors ?? new string[0]))
        {
            Errors = (errors ?? new string[0]).ToList().AsReadOnly();
        }

        public IReadOnlyList<string> Errors { get; }
    }

    /// <summary>Требования к заполнению <see cref="VertArmDesign"/>.</summary>
    public static class VertArmDesignRules
    {
        /// <summary>
        /// Список ошибок; пустой — данные корректны. С палкой удобнее <c>VertArmDesignStorage.Validate(palka, design)</c>:
        /// она ещё сверит <see cref="VertArmDesign.IsPilon"/> с семейством.
        /// </summary>
        public static IList<string> Validate(VertArmDesign d)
        {
            var errors = new List<string>();

            if (d == null)
            {
                errors.Add("Нет данных.");
                return errors;
            }

            if (d.SchemaVersion > VertArmDesign.CurrentSchemaVersion)
                errors.Add($"schemaVersion {d.SchemaVersion} новее поддерживаемой ({VertArmDesign.CurrentSchemaVersion}). Обновите плагин.");

            ValidateWall(errors, d.Wall);
            ValidateDrawing(errors, d.Drawing);

            if (d.Points.Section == null)
                errors.Add("points.section: не задана точка на разрезе.");

            ValidateVertArm(errors, d.VertArm);
            ValidateVertArmDop(errors, d.VertArmDop);
            ValidateVertArmTortsa(errors, d.VertArmTortsa);
            ValidateGorArm(errors, d.GorArm, d.IsPilon);
            ValidateNodes(errors, d);

            NotNegative(errors, "cover.left", d.Cover.Left);
            NotNegative(errors, "cover.right", d.Cover.Right);

            ValidateVertPka(errors, d.VertPka);
            ValidateShpilka(errors, d.Shpilka);

            return errors;
        }

        // ---------------- Разделы ----------------

        private static void ValidateWall(List<string> errors, WallSection w)
        {
            Positive(errors, "wall.thickness (OLP_Толщина стены)", w.Thickness);
            if (w.TopElevation <= w.BottomElevation)
                errors.Add($"wall: OLP_Отметка верха ({w.TopElevation}) должна быть выше OLP_Отметка низа ({w.BottomElevation}).");
            NotNegative(errors, "wall.topDrop (ПерепадСверху_Толщина)", w.TopDrop);
            NotNegative(errors, "wall.baseHeight2 (ВысотаОсновы_2этаж)", w.BaseHeight2);
            NotNegative(errors, "wall.topDrop2 (ПерепадСверху_2этаж_Толщина)", w.TopDrop2);
        }

        private static void ValidateDrawing(List<string> errors, DrawingSection dr)
        {
            if (string.IsNullOrWhiteSpace(dr.Mark))
                errors.Add("drawing.mark (ADSK_Марка изделия): не задана марка палки.");
            if (string.IsNullOrWhiteSpace(dr.DrawingSet))
                errors.Add("drawing.drawingSet (ADSK_Комплект чертежей): не задан комплект.");
        }

        // Правила для параметров палки — общие с блоками ReinforcementData2D (PalkaRules),
        // здесь к ним добавляются проверки ES-полей.

        private static void ValidateVertArm(List<string> errors, VertArmSection v)
        {
            errors.AddRange(PalkaRules.VertArm(v.Diameter, v.Row2Diameter, v.Step));
            if (!v.Enabled)
                return;

            NotNegative(errors, "vertArm.offsetFromBottom", v.OffsetFromBottom);
            VertArmRow(errors, "vertArm.row1", v.Row1);

            if (!v.Symmetrical)
                VertArmRow(errors, "vertArm.row2", v.Row2);
        }

        private static void VertArmRow(List<string> errors, string name, VertArmRow row)
        {
            OneOf(errors, name + ".form", row.Form, DesignValues.VertForms);
            OneOf(errors, name + ".anchorSide", row.AnchorSide, DesignValues.AnchorSides);

            // Мягкая проверка выпуска: при выпуске нужна длина, без выпуска — отступ от верха плиты.
            if (row.StarterBars)
            {
                if (row.Form == DesignValues.Form101G)
                    errors.Add($"{name}.starterBars: для формы 101_Г выпуск невозможен.");
                else if (row.StarterLength <= 0)
                    errors.Add($"{name}.starterLength: при выпуске должна быть больше 0 (сейчас {row.StarterLength}).");
            }
            else
            {
                NotNegative(errors, name + ".offsetFromTopOfSlab", row.OffsetFromTopOfSlab);
            }
        }

        private static void ValidateVertArmDop(List<string> errors, VertArmDopSection dop)
        {
            errors.AddRange(PalkaRules.VertArmDop(dop.Row1Diameter, dop.Row2Diameter, dop.EdgeDiameter));

            if (dop.Row1Enabled)
                DopRow(errors, "vertArmDop.row1", dop.Row1);
            if (dop.Row2Enabled)
                DopRow(errors, "vertArmDop.row2", dop.Row2);
            if (dop.Enabled)
                NotNegative(errors, "vertArmDop.anchorage", dop.Anchorage);

            // Симметричное доп. армирование задаётся 1 рядом. 2 ряд можно не заполнять (диаметр 0),
            // а если заполнен — он должен совпадать с 1 рядом по диаметру, форме и выпуску.
            if (!dop.Symmetrical)
                return;

            if (!dop.Row1Enabled)
                errors.Add($"vertArmDop.symmetrical = true: ВертАрм_Доп_Диаметр должен быть больше 0 (сейчас {dop.Row1Diameter}).");

            if (!dop.Row2Enabled)
                return;

            if (dop.Row2Diameter != dop.Row1Diameter)
                errors.Add($"vertArmDop.symmetrical = true: ВертАрм_Доп_2ряд_Диаметр должен быть 0 или равен ВертАрм_Доп_Диаметр " +
                           $"(сейчас {dop.Row1Diameter} и {dop.Row2Diameter}).");
            if (dop.Row1.Form != dop.Row2.Form)
                errors.Add($"vertArmDop.symmetrical = true: формы рядов должны совпадать " +
                           $"(row1.form «{dop.Row1.Form}», row2.form «{dop.Row2.Form}»).");
            if (dop.Row1.StarterLength != dop.Row2.StarterLength)
                errors.Add($"vertArmDop.symmetrical = true: длины выпуска рядов должны совпадать " +
                           $"(row1.starterLength {dop.Row1.StarterLength}, row2.starterLength {dop.Row2.StarterLength}).");
        }

        private static void DopRow(List<string> errors, string name, DopRow row)
        {
            OneOf(errors, name + ".form", row.Form, DesignValues.VertForms);
            LengthStarter(errors, name, row.Form, row.StarterLength);
        }

        private static void ValidateVertArmTortsa(List<string> errors, VertArmTortsaSection t)
        {
            errors.AddRange(PalkaRules.VertArmTortsa(t.Diameter, t.Step, t.Offset, t.CountInRow, t.CountPerEdge));
            if (t.Enabled)
            {
                OneOf(errors, "vertArmTortsa.form", t.Form, DesignValues.VertForms);
                LengthStarter(errors, "vertArmTortsa", t.Form, t.StarterLength);
            }
        }

        private static void ValidateNodes(List<string> errors, VertArmDesign d)
        {
            ValidateNode(errors, PalkaRules.StartEnd, d.StartNode, d.VertArmTortsa.Diameter, d.GorArm.Diameter, d.IsPilon);
            ValidateNode(errors, PalkaRules.EndEnd, d.EndNode, d.VertArmTortsa.Diameter, d.GorArm.Diameter, d.IsPilon);

            if (d.IsPilon && d.GorPka.Diameter != 0)
                errors.Add($"ГорПка_Диаметр: у пилона ГорПка нет (сейчас {d.GorPka.Diameter}).");
        }

        private static void ValidateNode(List<string> errors, string end, NodeSection node, int vertArmTortsaDiameter,
            int gorArmDiameter, bool isPilon)
        {
            errors.AddRange(PalkaRules.Node(end, node.VertArmTortsa, node.GorPka, node.Type, vertArmTortsaDiameter, gorArmDiameter));
            if (isPilon)
                errors.AddRange(PalkaRules.PilonNode(end, node.VertArmTortsa, node.GorPka, node.Type));
        }

        private static void ValidateGorArm(List<string> errors, GorArmSection g, bool isPilon)
        {
            errors.AddRange(PalkaRules.GorArm(g.Diameter, g.Step, g.Offset, g.OffsetFromEdge));
            errors.AddRange(PalkaRules.Zone("Нижняя зона учащения ГорАрм", g.BottomZone.Length, g.BottomZone.Step, g.Diameter));
            errors.AddRange(PalkaRules.Zone("Верхняя зона учащения ГорАрм", g.TopZone.Length, g.TopZone.Step, g.Diameter));

            if (g.Enabled)
                OneOf(errors, isPilon ? "gorArm.form (пилон)" : "gorArm.form (стена)", g.Form,
                    isPilon ? DesignValues.GorArmPilonForms : DesignValues.GorArmWallForms);
        }

        private static void ValidateVertPka(List<string> errors, VertPkaSection p)
        {
            errors.AddRange(PalkaRules.VertPka(p.Diameter, p.Step));
            if (!p.Enabled)
                return;

            NotNegative(errors, "vertPka.offset", p.Offset);
            OneOf(errors, "vertPka.form", p.Form, DesignValues.VertPkaForms);

            if ((p.Bottom || p.Top) && p.Form != DesignValues.Form201P)
                errors.Add("vertPka.bottom / vertPka.top: допускаются только для формы 201_П.");
        }

        private static void ValidateShpilka(List<string> errors, ShpilkaSection s)
        {
            errors.AddRange(PalkaRules.Diameter("Шпилька_Диаметр", s.Diameter));
            if (!s.Enabled)
                return;

            NotNegative(errors, "shpilka.hookLength", s.HookLength);
            NotNegative(errors, "shpilka.mandrelDiameter", s.MandrelDiameter);
        }

        // ---------------- Общие проверки ----------------

        /// <summary>Выпуск ВертАрм_Доп / ВертАрмТорца: задаётся только длиной (0 — нет), для 101_Г — 0.</summary>
        private static void LengthStarter(List<string> errors, string name, string form, int starterLength)
        {
            if (starterLength < 0)
                errors.Add($"{name}.starterLength: не может быть отрицательной (сейчас {starterLength}).");
            else if (form == DesignValues.Form101G && starterLength != 0)
                errors.Add($"{name}.starterLength: для формы 101_Г должна быть 0 (сейчас {starterLength}).");
        }

        private static void Positive(List<string> errors, string name, int value)
        {
            if (value <= 0)
                errors.Add($"{name}: должно быть больше 0 (сейчас {value}).");
        }

        private static void NotNegative(List<string> errors, string name, int value)
        {
            if (value < 0)
                errors.Add($"{name}: не может быть отрицательным (сейчас {value}).");
        }

        private static void OneOf(List<string> errors, string name, string value, IReadOnlyList<string> allowed)
        {
            if (!allowed.Contains(value))
                errors.Add($"{name}: «{value}» — допустимо: {string.Join(", ", allowed)}.");
        }
    }
}
