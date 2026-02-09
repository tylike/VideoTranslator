using FlaUI.Core.Definitions;
using System;

namespace PublishToBilibili.Models
{
    public class ControlLocator
    {
        public FormFieldType FieldType { get; set; }
        public ControlType ControlType { get; set; }
        public string NameContains { get; set; }
        public string AutomationId { get; set; }
        public int? MinX { get; set; }
        public int? MaxX { get; set; }
        public int? MinY { get; set; }
        public int? MaxY { get; set; }
        public int? Index { get; set; }
        public bool MustBeEnabled { get; set; } = true;
        public bool MustBeVisible { get; set; } = true;

        public ControlLocator Clone()
        {
            return new ControlLocator
            {
                FieldType = FieldType,
                ControlType = ControlType,
                NameContains = NameContains,
                AutomationId = AutomationId,
                MinX = MinX,
                MaxX = MaxX,
                MinY = MinY,
                MaxY = MaxY,
                Index = Index,
                MustBeEnabled = MustBeEnabled,
                MustBeVisible = MustBeVisible
            };
        }
    }
}
