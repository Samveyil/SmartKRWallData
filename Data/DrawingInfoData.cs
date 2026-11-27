namespace SmartKRWallData.Data
{
    /// <summary>
    /// Оформление: идентификация палки и комплект.
    /// </summary>
    public sealed class DrawingInfoData
    {
        /// <param name="mark">
        /// ADSK_Марка изделия — номер палки.
        /// </param>
        /// <param name="drawingSet">
        /// ADSK_Комплект чертежей.
        /// </param>
        /// <param name="floor">
        /// OLP_Этаж, например -1.
        /// </param>
        /// <param name="section">
        /// OLP_Номер секции. Можно не задавать.
        /// </param>
        /// <param name="building">
        /// ADSK_Номер здания. Можно не задавать.
        /// </param>
        public DrawingInfoData(string drawingSet, double floor, string mark, string section = "", string building = "")
        {
            DrawingSet = drawingSet ?? "";
            Floor = floor;
            Mark = mark ?? "";
            Section = section ?? "";
            Building = building ?? "";
        }

        /// <summary>
        /// ADSK_Комплект чертежей.
        /// </summary>
        public string DrawingSet { get; }

        /// <summary>
        /// OLP_Этаж.
        /// </summary>
        public double Floor { get; }

        /// <summary>
        /// ADSK_Марка изделия.
        /// </summary>
        public string Mark { get; }

        /// <summary>
        /// OLP_Номер секции.
        /// </summary>
        public string Section { get; }

        /// <summary>
        /// ADSK_Номер здания.
        /// </summary>
        public string Building { get; }
    }
}
