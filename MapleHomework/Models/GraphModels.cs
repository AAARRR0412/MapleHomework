using System;

namespace MapleHomework.Models
{
    // 경험치 그래프 아이템
    public class ExpGraphItem
    {
        public string DateLabel { get; set; } = "";
        public double ExpRate { get; set; }
        public string ExpRateText { get; set; } = "";
        public double GraphRate { get; set; }
        public long ExpGain { get; set; }
    }
}
