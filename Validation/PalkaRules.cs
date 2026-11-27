using System;
using System.Collections.Generic;
using System.Linq;
using SmartKRWallData.Design;

namespace SmartKRWallData.Validation
{
    /// <summary>
    /// Правила для параметров армирования палки.
    /// </summary>
    internal static class PalkaRules
    {
        public static void ThrowIfAny(List<string> errors)
        {
            if (errors.Count > 0)
                throw new VertArmDesignException(errors);
        }

        public static List<string> VertArm(int diameter, int row2Diameter, int step)
        {
            var errors = new List<string>();
            NotNegative(errors, "ВертАрм_Диаметр", diameter);
            NotNegative(errors, "ВертАрм_2ряд_Диаметр", row2Diameter);
            if (diameter > 0)
                PositiveWith(errors, "ВертАрм_Шаг", step, "диаметре", diameter);
            return errors;
        }

        public static List<string> VertArmDop(int diameter, int row2Diameter, int tortsaDiameter)
        {
            var errors = new List<string>();
            NotNegative(errors, "ВертАрм_Доп_Диаметр", diameter);
            NotNegative(errors, "ВертАрм_Доп_2ряд_Диаметр", row2Diameter);
            NotNegative(errors, "ВертАрмТорца_Доп_Диаметр", tortsaDiameter);
            return errors;
        }

        public static List<string> VertArmTortsa(int diameter, int step, int offset, int countInRow, int countPerEdge)
        {
            var errors = new List<string>();
            NotNegative(errors, "ВертАрмТорца_Диаметр", diameter);

            if (diameter > 0)
            {
                PositiveWith(errors, "ВертАрмТорца_Шаг", step, "диаметре", diameter);
                PositiveWith(errors, "ВертАрмТорца_КоличВРяду", countInRow, "диаметре", diameter);
                PositiveWith(errors, "ВертАрмТорца_КоличНаТорец", countPerEdge, "диаметре", diameter);
                NotNegative(errors, "ВертАрмТорца_Отступ", offset);
            }

            return errors;
        }

        /// <summary>«Начало» / «Конец» — как в именах параметров палки.</summary>
        public const string StartEnd = "Начало";
        public const string EndEnd = "Конец";

        /// <summary>
        /// Узел: есть, если в нём ВертАрмТорца или ГорПка. Диаметр ГорПка берётся из ГорАрм.
        /// </summary>
        public static List<string> Node(string end, bool vertArmTortsa, bool gorPka, string type,
            int vertArmTortsaDiameter, int gorArmDiameter)
        {
            var errors = new List<string>();
            if (!vertArmTortsa && !gorPka)
                return errors;

            if (vertArmTortsa && vertArmTortsaDiameter <= 0)
                errors.Add($"ВертАрмТорца_{end}.Вкл: включён, а ВертАрмТорца_Диаметр = {vertArmTortsaDiameter}.");
            if (gorPka && gorArmDiameter <= 0)
                errors.Add($"ГорПка_{end}.Вкл: включён, а ГорАрм нет (ГорАрм_Диаметр = {gorArmDiameter}); диаметр ГорПка берётся из ГорАрм.");
            if (!DesignValues.NodeTypes.Contains(type))
                errors.Add($"Узел «{end}»: тип «{type}» — допустимо: {string.Join(", ", DesignValues.NodeTypes)}.");

            return errors;
        }

        /// <summary>
        /// Узел пилона: только ВертАрмТорца, без ГорПка, тип «Торец».
        /// </summary>
        public static List<string> PilonNode(string end, bool vertArmTortsa, bool gorPka, string type)
        {
            var errors = new List<string>();
            if (gorPka)
                errors.Add($"ГорПка_{end}.Вкл: у пилона ГорПка нет.");
            if (vertArmTortsa && type != DesignValues.NodeTorets)
                errors.Add($"Узел «{end}»: у пилона тип только «{DesignValues.NodeTorets}» (сейчас «{type}»).");
            return errors;
        }

        public static List<string> GorArm(int diameter, int step, int offset, int offsetFromEdge)
        {
            var errors = new List<string>();
            NotNegative(errors, "ГорАрм_Диаметр", diameter);
            if (diameter > 0)
            {
                PositiveWith(errors, "ГорАрм_Шаг", step, "диаметре", diameter);
                NotNegative(errors, "ГорАрм_Отступ", offset);
                NotNegative(errors, "ГорАрм_ОтступОтТорца", offsetFromEdge);
            }
            return errors;
        }

        public static List<string> Zone(string name, int length, int step, int gorArmDiameter)
        {
            var errors = new List<string>();
            NotNegative(errors, name + ": длина", length);
            if (length > 0)
            {
                PositiveWith(errors, name + ": шаг", step, "длине", length);
                if (gorArmDiameter <= 0)
                    errors.Add($"{name}: задана (длина {length}), а ГорАрм нет (ГорАрм_Диаметр = {gorArmDiameter}).");
            }
            return errors;
        }

        public static List<string> VertPka(int diameter, int step)
        {
            var errors = new List<string>();
            NotNegative(errors, "ВертПка_Диаметр", diameter);
            if (diameter > 0)
                PositiveWith(errors, "Шаг ВертПка", step, "диаметре", diameter);
            return errors;
        }

        public static List<string> Diameter(string name, int diameter)
        {
            var errors = new List<string>();
            NotNegative(errors, name, diameter);
            return errors;
        }

        private static void NotNegative(List<string> errors, string name, int value)
        {
            if (value < 0)
                errors.Add($"{name}: не может быть отрицательным (сейчас {value}).");
        }

        private static void PositiveWith(List<string> errors, string name, int value, string what, int whatValue)
        {
            if (value <= 0)
                errors.Add($"{name}: должен быть больше 0 при {what} {whatValue} (сейчас {value}).");
        }
    }
}
