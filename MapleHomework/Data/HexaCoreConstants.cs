using System.Collections.Generic;

namespace MapleHomework.Data
{
    public static class HexaCoreConstants
    {
        public class HexaLevelCost
        {
            public int Level { get; set; }
            public int SolErda { get; set; }
            public int Fragment { get; set; }

            public HexaLevelCost(int level, int solErda, int fragment)
            {
                Level = level;
                SolErda = solErda;
                Fragment = fragment;
            }
        }

        public static readonly List<HexaLevelCost> SkillCore = new()
        {
            new(1, 0, 0),
            new(2, 1, 30), new(3, 1, 35), new(4, 1, 40), new(5, 2, 45),
            new(6, 2, 50), new(7, 2, 55), new(8, 3, 60), new(9, 3, 65),
            new(10, 10, 200),
            new(11, 3, 80), new(12, 3, 90), new(13, 4, 100), new(14, 4, 110),
            new(15, 4, 120),
            new(16, 4, 130), new(17, 4, 140), new(18, 4, 150), new(19, 5, 160),
            new(20, 15, 350),
            new(21, 5, 170), new(22, 5, 180), new(23, 5, 190), new(24, 5, 200),
            new(25, 5, 210),
            new(26, 6, 220), new(27, 6, 230), new(28, 6, 240), new(29, 7, 250),
            new(30, 20, 500)
        };

        public static readonly List<HexaLevelCost> MasteryCore = new()
        {
            new(1, 3, 50),
            new(2, 1, 15), new(3, 1, 18), new(4, 1, 20), new(5, 1, 23),
            new(6, 1, 25), new(7, 1, 28), new(8, 2, 30), new(9, 2, 33),
            new(10, 5, 100),
            new(11, 2, 40), new(12, 2, 45), new(13, 2, 50), new(14, 2, 55),
            new(15, 2, 60),
            new(16, 2, 65), new(17, 2, 70), new(18, 2, 75), new(19, 3, 80),
            new(20, 8, 175),
            new(21, 3, 85), new(22, 3, 90), new(23, 3, 95), new(24, 3, 100),
            new(25, 3, 105),
            new(26, 3, 110), new(27, 3, 115), new(28, 3, 120), new(29, 4, 125),
            new(30, 10, 250)
        };

        public static readonly List<HexaLevelCost> ReinforcementCore = new()
        {
            new(1, 4, 75),
            new(2, 1, 23), new(3, 1, 27), new(4, 1, 30), new(5, 2, 34),
            new(6, 2, 38), new(7, 2, 42), new(8, 3, 45), new(9, 3, 49),
            new(10, 8, 150),
            new(11, 3, 60), new(12, 3, 68), new(13, 3, 75), new(14, 3, 83),
            new(15, 3, 90),
            new(16, 3, 98), new(17, 3, 105), new(18, 3, 113), new(19, 4, 120),
            new(20, 12, 263),
            new(21, 4, 128), new(22, 4, 135), new(23, 4, 143), new(24, 4, 150),
            new(25, 4, 158),
            new(26, 5, 165), new(27, 5, 173), new(28, 5, 180), new(29, 6, 188),
            new(30, 15, 375)
        };

        public static readonly List<HexaLevelCost> CommonCore = new()
        {
            new(1, 7, 125),
            new(2, 2, 38), new(3, 2, 44), new(4, 2, 50), new(5, 3, 57),
            new(6, 3, 63), new(7, 3, 69), new(8, 5, 75), new(9, 5, 82),
            new(10, 14, 300),
            new(11, 5, 110), new(12, 5, 124), new(13, 6, 138), new(14, 6, 152),
            new(15, 6, 165),
            new(16, 6, 179), new(17, 6, 193), new(18, 6, 207), new(19, 7, 220),
            new(20, 17, 525),
            new(21, 7, 234), new(22, 7, 248), new(23, 7, 262), new(24, 7, 275),
            new(25, 7, 289),
            new(26, 9, 303), new(27, 9, 317), new(28, 9, 330), new(29, 10, 344),
            new(30, 20, 750)
        };
    }
}
