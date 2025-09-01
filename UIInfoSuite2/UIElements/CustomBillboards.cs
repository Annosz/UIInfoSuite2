///<summary>Seperates Billboard class into two, allowing type check.</summary>

namespace UIInfoSuite2.UIElements
{
    public class CalendarBillboard : StardewValley.Menus.Billboard
    {
        public CalendarBillboard() : base(false) { }
    }

    public class QuestBillboard : StardewValley.Menus.Billboard
    {
        public QuestBillboard() : base(true) { }
    }
}