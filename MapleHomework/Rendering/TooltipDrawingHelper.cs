using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace MapleHomework.Rendering
{
    public static class MapleGearGraphics
    {
        #region Fonts (22버전 UI - 굴림)
        public static readonly Font ItemNameFont = new Font("굴림", 14f, FontStyle.Bold, GraphicsUnit.Pixel);
        public static readonly Font ItemDetailFont = new Font("굴림", 11f, GraphicsUnit.Pixel);
        public static readonly Font EquipDetailFont = new Font("굴림", 11f, GraphicsUnit.Pixel);
        public static readonly Font EquipMDMoris9Font = new Font("굴림", 11f, GraphicsUnit.Pixel);
        public static readonly Font EquipMDMoris9FontBold = new Font("굴림", 11f, FontStyle.Bold, GraphicsUnit.Pixel);
        public static readonly Font ItemGulimFont = new Font("굴림", 11f, GraphicsUnit.Pixel);
        public static readonly Font ItemGulimFontBold = new Font("굴림", 14f, FontStyle.Bold, GraphicsUnit.Pixel);
        #endregion

        #region Colors (22버전 UI)
        // 기본 배경색
        public static readonly Color GearBackColor = Color.FromArgb(204, 0, 51, 85);
        public static readonly Color Gear22BackColor = Color.FromArgb(240, 24, 29, 40); // #F0181D28

        // 22버전 텍스트 색상
        public static readonly Color Equip22Gray = Color.FromArgb(183, 191, 197);         // #B7BFC5
        public static readonly Color Equip22DarkGray = Color.FromArgb(133, 145, 159);     // #85919F
        public static readonly Color Equip22Red = Color.FromArgb(255, 138, 24);           // #FF8A18
        public static readonly Color Equip22Emphasis = Color.FromArgb(255, 204, 0);       // #FFCC00
        public static readonly Color Equip22EmphasisBright = Color.FromArgb(255, 245, 77); // #FFF54D
        public static readonly Color Equip22Scroll = Color.FromArgb(175, 173, 255);       // #AFADFF
        public static readonly Color Equip22BonusStat = Color.FromArgb(10, 227, 173);     // #0AE3AD

        // 잠재능력 등급 색상
        public static readonly Color Equip22Rare = Color.FromArgb(102, 255, 255);         // #66FFFF
        public static readonly Color Equip22Epic = Color.FromArgb(187, 129, 255);         // #BB81FF
        public static readonly Color Equip22Unique = Color.FromArgb(255, 204, 0);         // #FFCC00
        public static readonly Color Equip22Legendary = Color.FromArgb(204, 255, 0);      // #CCFF00
        public static readonly Color Equip22Exceptional = Color.FromArgb(255, 51, 51);    // #FF3333
        #endregion

        #region Brushes (22버전 UI)
        public static readonly Brush Equip22BrushGray = new SolidBrush(Equip22Gray);
        public static readonly Brush Equip22BrushDarkGray = new SolidBrush(Equip22DarkGray);
        public static readonly Brush Equip22BrushRed = new SolidBrush(Equip22Red);
        public static readonly Brush Equip22BrushEmphasis = new SolidBrush(Equip22Emphasis);
        public static readonly Brush Equip22BrushEmphasisBright = new SolidBrush(Equip22EmphasisBright);
        public static readonly Brush Equip22BrushScroll = new SolidBrush(Equip22Scroll);
        public static readonly Brush Equip22BrushBonusStat = new SolidBrush(Equip22BonusStat);
        public static readonly Brush Equip22BrushRare = new SolidBrush(Equip22Rare);
        public static readonly Brush Equip22BrushEpic = new SolidBrush(Equip22Epic);
        public static readonly Brush Equip22BrushLegendary = new SolidBrush(Equip22Legendary);
        public static readonly Brush Equip22BrushExceptional = new SolidBrush(Equip22Exceptional);

        public static readonly Brush OrangeBrush = new SolidBrush(Color.FromArgb(255, 153, 0));
        public static readonly Brush OrangeBrush3 = new SolidBrush(Color.FromArgb(255, 204, 0));
        public static readonly Brush GreenBrush2 = new SolidBrush(Color.FromArgb(204, 255, 0));
        public static readonly Brush GrayBrush2 = new SolidBrush(Color.FromArgb(153, 153, 153));
        public static readonly Brush SetItemNameBrush = new SolidBrush(Color.FromArgb(119, 255, 0));
        #endregion

        #region Color Tables (22버전)
        /// <summary>
        /// 22버전 장비 툴팁 색상 테이블
        /// </summary>
        public static readonly Dictionary<string, Color> Equip22ColorTable = new Dictionary<string, Color>
        {
            { "c", Equip22Emphasis },           // 강조 (노란색)
            { "$y", Color.FromArgb(102, 255, 255) }, // 시안
            { "$r", Equip22Red },               // 빨강/주황
            { "$e", Equip22EmphasisBright },    // 밝은 노란
            { "$b", Equip22BonusStat },         // 추가옵션 (청록)
            { "$s", Equip22Scroll },            // 스크롤 강화 (보라)
            { "$g", Equip22Gray },              // 회색
            { "$d", Equip22DarkGray },          // 진회색
        };

        /// <summary>
        /// 잠재능력 색상 테이블
        /// </summary>
        public static readonly Dictionary<string, Color> PotentialColorTable = new Dictionary<string, Color>
        {
            { "$n", Equip22DarkGray },  // 없음
            { "$r", Equip22Rare },      // 레어
            { "$e", Equip22Epic },      // 에픽
            { "$u", Equip22Emphasis },  // 유니크
            { "$l", Equip22Legendary }, // 레전드리
        };
        #endregion

        #region Drawing Methods
        /// <summary>
        /// 22버전 스타일의 새 툴팁 배경을 그립니다.
        /// </summary>
        public static void DrawNewTooltipBack(Graphics g, int x, int y, int width, int height)
        {
            // 배경 채우기
            using var backBrush = new SolidBrush(Gear22BackColor);
            g.FillRectangle(backBrush, x, y, width, height);

            // 테두리 그리기
            using var borderPen = new Pen(Color.FromArgb(128, 80, 96, 112), 1);
            g.DrawRectangle(borderPen, x, y, width - 1, height - 1);
        }

        /// <summary>
        /// 텍스트를 색상 코드와 함께 그립니다.
        /// #c, #r 등의 색상 태그를 지원합니다.
        /// </summary>
        public static void DrawString(Graphics g, string text, Font font, Dictionary<string, Color>? colorTable,
            int x, int maxRight, ref int y, int lineHeight, TextAlignment alignment = TextAlignment.Left)
        {
            if (string.IsNullOrEmpty(text)) return;

            var defaultColor = Color.White;
            var currentColor = defaultColor;
            var currentX = x;

            // 색상 태그 파싱
            var segments = ParseColorTags(text, colorTable ?? Equip22ColorTable, defaultColor);

            foreach (var segment in segments)
            {
                var size = TextRenderer.MeasureText(g, segment.Text, font,
                    new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);

                TextRenderer.DrawText(g, segment.Text, font, new Point(currentX, y),
                    segment.Color, TextFormatFlags.NoPadding);

                currentX += size.Width;
            }

            y += lineHeight;
        }

        /// <summary>
        /// 일반 텍스트를 그립니다.
        /// </summary>
        public static void DrawPlainText(Graphics g, string text, Font font, Color color,
            int x, int maxRight, ref int y, int lineHeight)
        {
            if (string.IsNullOrEmpty(text)) return;

            TextRenderer.DrawText(g, text, font, new Point(x, y), color, TextFormatFlags.NoPadding);
            y += lineHeight;
        }

        /// <summary>
        /// 구분선을 그립니다.
        /// </summary>
        public static void DrawLine(Graphics g, int x, int y, int width)
        {
            using var pen = new Pen(Color.FromArgb(64, 255, 255, 255), 1);
            g.DrawLine(pen, x, y, x + width, y);
        }

        /// <summary>
        /// 구분선을 그립니다. (페이드 효과)
        /// </summary>
        public static void DrawFadeLine(Graphics g, int x, int y, int width)
        {
            using var brush = new LinearGradientBrush(
                new Point(x, y), new Point(x + width, y),
                Color.FromArgb(0, 255, 255, 255), Color.FromArgb(85, 255, 255, 255));

            // 중앙이 가장 밝도록
            brush.SetBlendTriangularShape(0.5f);

            using var pen = new Pen(brush, 1);
            g.DrawLine(pen, x, y, x + width, y);
        }

        /// <summary>
        /// 비트맵을 2배로 확대합니다.
        /// </summary>
        public static Bitmap EnlargeBitmap(Bitmap source)
        {
            if (source == null) return new Bitmap(1, 1);

            var result = new Bitmap(source.Width * 2, source.Height * 2);
            using var g = Graphics.FromImage(result);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(source, 0, 0, result.Width, result.Height);
            return result;
        }
        #endregion

        #region Potential Grade Methods
        /// <summary>
        /// 잠재능력 등급 문자열을 반환합니다.
        /// </summary>
        public static string GetPotentialString(int grade)
        {
            return grade switch
            {
                0 => "없음",
                1 => "레어",
                2 => "에픽",
                3 => "유니크",
                4 => "레전드리",
                _ => "-"
            };
        }

        /// <summary>
        /// 잠재능력 등급 색상을 반환합니다.
        /// </summary>
        public static Color GetPotentialColor(int grade)
        {
            return grade switch
            {
                1 => Equip22Rare,
                2 => Equip22Epic,
                3 => Equip22Unique,
                4 => Equip22Legendary,
                _ => Equip22DarkGray
            };
        }

        /// <summary>
        /// 잠재능력 등급 색상 태그를 반환합니다.
        /// </summary>
        public static string GetPotentialColorTag(int grade)
        {
            return grade switch
            {
                0 => "$n",
                1 => "$r",
                2 => "$e",
                3 => "$u",
                4 => "$l",
                _ => "$n"
            };
        }
        #endregion

        #region Helper Methods
        private static List<TextSegment> ParseColorTags(string text, Dictionary<string, Color> colorTable, Color defaultColor)
        {
            var segments = new List<TextSegment>();
            var currentColor = defaultColor;
            var currentText = "";
            var i = 0;

            while (i < text.Length)
            {
                if (text[i] == '#' && i + 1 < text.Length)
                {
                    // 현재 텍스트 저장
                    if (!string.IsNullOrEmpty(currentText))
                    {
                        segments.Add(new TextSegment(currentText, currentColor));
                        currentText = "";
                    }

                    // 색상 태그 파싱
                    var tagEnd = i + 1;

                    // $로 시작하는 태그 확인
                    if (text[tagEnd] == '$' && tagEnd + 1 < text.Length)
                    {
                        var tag = "$" + text[tagEnd + 1];
                        if (colorTable.TryGetValue(tag, out var color))
                        {
                            currentColor = color;
                            i += 3; // #$x
                            continue;
                        }
                    }
                    else
                    {
                        // 단일 문자 태그 (c, r 등)
                        var tag = text[tagEnd].ToString();
                        if (colorTable.TryGetValue(tag, out var color))
                        {
                            currentColor = color;
                            i += 2; // #x
                            continue;
                        }
                        else if (text[tagEnd] == '#')
                        {
                            // ## = 색상 초기화
                            currentColor = defaultColor;
                            i += 2;
                            continue;
                        }
                    }
                }

                currentText += text[i];
                i++;
            }

            // 남은 텍스트 저장
            if (!string.IsNullOrEmpty(currentText))
            {
                segments.Add(new TextSegment(currentText, currentColor));
            }

            return segments;
        }

        private struct TextSegment
        {
            public string Text;
            public Color Color;

            public TextSegment(string text, Color color)
            {
                Text = text;
                Color = color;
            }
        }
        #endregion
    }

    public enum TextAlignment
    {
        Left,
        Center,
        Right
    }
}

