namespace SmartKRWallData.Common
{
    internal static class DesignUnits
    {
        private const double MillimetersPerFoot = 304.8;

        /// <summary>мм → внутренние единицы Revit.</summary>
        public static double FromMillimeters(this double millimeters) => millimeters / MillimetersPerFoot;

        /// <summary>Внутренние единицы Revit → мм.</summary>
        public static double ToMillimeters(this double feet) => feet * MillimetersPerFoot;
    }
}
