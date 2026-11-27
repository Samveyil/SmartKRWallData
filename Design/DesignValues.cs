using System;
using System.Collections.Generic;

namespace SmartKRWallData.Design
{
    // ---------------- Допустимые значения ----------------

    public static class DesignValues
    {
        public const string Form1 = "1";
        public const string Form101G = "101_Г";
        public const string Form206G = "206_Г";
        public const string Form201P = "201_П";
        public const string Form501X = "501_Х";
        public const string Form603X = "603_Х";

        public const string AnchorLeft = "Влево";
        public const string AnchorRight = "Вправо";

        public const string NodeTorets = "Торец";
        public const string NodeG = "Г-узел";
        public const string NodeT = "Т-узел";
        public const string NodeGTS = "ГТС";

        /// <summary>Формы ВертАрм, ВертАрм_Доп, ВертАрмТорца.</summary>
        public static readonly IReadOnlyList<string> VertForms = Array.AsReadOnly(new[] { Form1, Form101G, Form206G });

        /// <summary>Формы ГорАрм у стены.</summary>
        public static readonly IReadOnlyList<string> GorArmWallForms = Array.AsReadOnly(new[] { Form1 });

        /// <summary>Формы ГорАрм у пилона.</summary>
        public static readonly IReadOnlyList<string> GorArmPilonForms = Array.AsReadOnly(new[] { Form201P, Form501X, Form603X });

        /// <summary>Формы ВертПка.</summary>
        public static readonly IReadOnlyList<string> VertPkaForms = Array.AsReadOnly(new[] { Form201P, Form501X, Form603X });

        public static readonly IReadOnlyList<string> AnchorSides = Array.AsReadOnly(new[] { AnchorLeft, AnchorRight });

        public static readonly IReadOnlyList<string> NodeTypes = Array.AsReadOnly(new[] { NodeTorets, NodeG, NodeT, NodeGTS });
    }
}
