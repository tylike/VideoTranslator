using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using PublishToBilibili.Models;

namespace PublishToBilibili.Services
{
   

    public class ControlInfo
    {
        public ControlType ControlType { get; set; }
        public string Name { get; set; }
        public string AutomationId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsOffscreen { get; set; }
    }
}
