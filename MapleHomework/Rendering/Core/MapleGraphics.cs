using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace MapleHomework.Rendering.Core
{
    /// <summary>
    /// 메이플스토리 UI 렌더링을 위한 그래픽 유틸리티 (WzComparerR2 GearGraphics.cs 기반)
    /// 22 버전 (최신 UI) 적용
    /// </summary>
    public static class MapleGraphics
    {
        #region 초기화

        static MapleGraphics()
        {
            SetFontFamily("돋움");
        }

        #endregion

        #region 폰트 정의

        /// <summary>
        /// 아이템 이름용 폰트 (14px Bold)
        /// </summary>
        public static Font ItemNameFont { get; private set; } = new Font("돋움", 14f, FontStyle.Bold, GraphicsUnit.Pixel);

        /// <summary>
        /// 아이템 설명용 폰트 (12px)
        /// </summary>
        public static Font ItemDetailFont { get; private set; } = new Font("돋움", 12f, GraphicsUnit.Pixel);

        /// <summary>
        /// 장비 상세용 폰트 (11px)
        /// </summary>
        public static Font EquipDetailFont { get; private set; } = new Font("돋움", 11f, GraphicsUnit.Pixel);

        /// <summary>
        /// 굴림 폰트 (12px)
        /// </summary>
        public static readonly Font GulimFont = new Font("굴림", 12f, GraphicsUnit.Pixel);

        /// <summary>
        /// 굴림 볼드 폰트 (14px)
        /// </summary>
        public static readonly Font GulimBoldFont = new Font("굴림", 14f, FontStyle.Bold, GraphicsUnit.Pixel);

        /// <summary>
        /// 폰트 패밀리 설정
        /// </summary>
        public static void SetFontFamily(string fontName)
        {
            ItemNameFont?.Dispose();
            ItemNameFont = new Font(fontName, 14f, FontStyle.Bold, GraphicsUnit.Pixel);

            ItemDetailFont?.Dispose();
            ItemDetailFont = new Font(fontName, 12f, GraphicsUnit.Pixel);

            EquipDetailFont?.Dispose();
            EquipDetailFont = new Font(fontName, 11f, GraphicsUnit.Pixel);
        }

        #endregion

        #region 색상 정의 (22 버전 최신 UI)

        // 배경색
        public static readonly Color TooltipBackColor = Color.FromArgb(204, 0, 51, 85);
        public static readonly Color EpicBackColor = Color.FromArgb(170, 68, 0, 0);
        public static readonly Color IconBackColor = Color.FromArgb(238, 187, 204, 221);

        // 텍스트 색상 - 기본
        public static readonly Color WhiteColor = Color.FromArgb(255, 255, 255);
        public static readonly Color GrayColor = Color.FromArgb(153, 153, 153);
        public static readonly Color DarkGrayColor = Color.FromArgb(85, 85, 85);

        // 텍스트 색상 - 22 버전 (GearGraphics.cs Equip22Brush* 시리즈)
        public static readonly Color Equip22Gray = Color.FromArgb(183, 191, 197);        // #B7BFC5
        public static readonly Color Equip22DarkGray = Color.FromArgb(133, 145, 159);    // #85919F
        public static readonly Color Equip22Red = Color.FromArgb(255, 138, 24);          // #FF8A18
        public static readonly Color Equip22Emphasis = Color.FromArgb(255, 204, 0);      // #FFCC00
        public static readonly Color Equip22EmphasisBright = Color.FromArgb(255, 245, 77); // #FFF54D

        // 옵션 색상
        public static readonly Color OrangeColor = Color.FromArgb(255, 153, 0);      // #c 태그
        public static readonly Color Orange2Color = Color.FromArgb(255, 170, 0);     // 속성
        public static readonly Color Orange3Color = Color.FromArgb(255, 204, 0);     // 강조
        public static readonly Color GreenColor = Color.FromArgb(204, 255, 0);       // 레전드리
        public static readonly Color ScrollColor = Color.FromArgb(175, 173, 255);    // 주문서 강화
        public static readonly Color BonusStatColor = Color.FromArgb(10, 227, 173);  // 22버전 추가옵션

        // 잠재능력 등급 색상 (22버전: itemPotentialColorTable 기반)
        public static readonly Color RareColor = Color.FromArgb(102, 255, 255);      // #66FFFF - Equip22BrushRare
        public static readonly Color EpicColor = Color.FromArgb(187, 129, 255);      // #BB81FF - Equip22BrushEpic
        public static readonly Color UniqueColor = Color.FromArgb(255, 204, 0);      // #FFCC00 - Equip22BrushEmphasis
        public static readonly Color LegendaryColor = Color.FromArgb(204, 255, 0);   // #CCFF00 - Equip22BrushLegendary
        public static readonly Color ExceptionalColor = Color.FromArgb(255, 51, 51); // #FF3333 - Equip22BrushExceptional

        // 아이템 품질 색상
        public static readonly Color QualityGray = Color.FromArgb(187, 187, 187);    // 추옵 < 0
        public static readonly Color QualityWhite = Color.FromArgb(255, 255, 255);   // 추옵 0~5
        public static readonly Color QualityOrange = Color.FromArgb(255, 170, 0);    // 추옵 0~5 + 스크롤
        public static readonly Color QualityCyan = Color.FromArgb(102, 255, 255);    // 추옵 6~22
        public static readonly Color QualityPurple = Color.FromArgb(153, 102, 255);  // 추옵 23~39
        public static readonly Color QualityGold = Color.FromArgb(255, 205, 0);      // 추옵 40~54
        public static readonly Color QualityGreen = Color.FromArgb(204, 255, 0);     // 추옵 55~69
        public static readonly Color QualityRed = Color.FromArgb(255, 0, 102);       // 추옵 70+

        // 기타 색상
        public static readonly Color SetItemColor = Color.FromArgb(119, 255, 0);     // 세트 아이템
        public static readonly Color BlockRedColor = Color.FromArgb(255, 0, 102);    // 사용 불가
        public static readonly Color BlueColor = Color.FromArgb(0, 204, 255);
        public static readonly Color PinkColor = Color.FromArgb(255, 102, 204);
        public static readonly Color PurpleColor = Color.FromArgb(187, 119, 255);

        #endregion

        #region 브러시 정의

        public static readonly Brush TooltipBackBrush = new SolidBrush(TooltipBackColor);
        public static readonly Brush WhiteBrush = new SolidBrush(WhiteColor);
        public static readonly Brush GrayBrush = new SolidBrush(GrayColor);
        public static readonly Brush OrangeBrush = new SolidBrush(OrangeColor);
        public static readonly Brush Orange2Brush = new SolidBrush(Orange2Color);
        public static readonly Brush Orange3Brush = new SolidBrush(Orange3Color);
        public static readonly Brush GreenBrush = new SolidBrush(GreenColor);
        public static readonly Brush ScrollBrush = new SolidBrush(ScrollColor);
        public static readonly Brush BonusStatBrush = new SolidBrush(BonusStatColor);

        // 22버전 브러시
        public static readonly Brush Equip22GrayBrush = new SolidBrush(Equip22Gray);
        public static readonly Brush Equip22DarkGrayBrush = new SolidBrush(Equip22DarkGray);
        public static readonly Brush Equip22RedBrush = new SolidBrush(Equip22Red);
        public static readonly Brush Equip22EmphasisBrush = new SolidBrush(Equip22Emphasis);
        public static readonly Brush Equip22EmphasisBrightBrush = new SolidBrush(Equip22EmphasisBright);

        // 잠재능력 브러시
        public static readonly Brush RareBrush = new SolidBrush(RareColor);
        public static readonly Brush EpicBrush = new SolidBrush(EpicColor);
        public static readonly Brush UniqueBrush = new SolidBrush(UniqueColor);
        public static readonly Brush LegendaryBrush = new SolidBrush(LegendaryColor);
        public static readonly Brush ExceptionalBrush = new SolidBrush(ExceptionalColor);

        #endregion

        #region 펜 정의

        public static readonly Pen TooltipBackPen = new Pen(TooltipBackColor);
        public static readonly Pen WhitePen = new Pen(WhiteColor);
        public static readonly Pen GrayPen = new Pen(GrayColor);

        // 아이템 테두리 펜 (잠재능력 등급별)
        public static readonly Pen RareBorderPen = new Pen(RareColor);
        public static readonly Pen EpicBorderPen = new Pen(EpicColor);
        public static readonly Pen UniqueBorderPen = new Pen(UniqueColor);
        public static readonly Pen LegendaryBorderPen = new Pen(LegendaryColor);

        #endregion

        #region 유틸리티 메서드

        /// <summary>
        /// 잠재능력 등급에 따른 브러시 반환
        /// </summary>
        public static Brush GetPotentialBrush(PotentialGrade grade)
        {
            return grade switch
            {
                PotentialGrade.Rare => RareBrush,
                PotentialGrade.Epic => EpicBrush,
                PotentialGrade.Unique => UniqueBrush,
                PotentialGrade.Legendary => LegendaryBrush,
                _ => WhiteBrush
            };
        }

        /// <summary>
        /// 잠재능력 등급에 따른 색상 반환
        /// </summary>
        public static Color GetPotentialColor(PotentialGrade grade)
        {
            return grade switch
            {
                PotentialGrade.Rare => RareColor,
                PotentialGrade.Epic => EpicColor,
                PotentialGrade.Unique => UniqueColor,
                PotentialGrade.Legendary => LegendaryColor,
                _ => WhiteColor
            };
        }

        /// <summary>
        /// 잠재능력 등급에 따른 테두리 펜 반환
        /// </summary>
        public static Pen? GetPotentialBorderPen(PotentialGrade grade)
        {
            return grade switch
            {
                PotentialGrade.Rare => RareBorderPen,
                PotentialGrade.Epic => EpicBorderPen,
                PotentialGrade.Unique => UniqueBorderPen,
                PotentialGrade.Legendary => LegendaryBorderPen,
                _ => null
            };
        }

        /// <summary>
        /// 추가 옵션 수치에 따른 아이템 이름 색상 반환
        /// </summary>
        public static Color GetItemNameColor(int bonusStatTotal, bool hasScroll, bool isCash = false)
        {
            if (isCash) return QualityWhite;
            if (bonusStatTotal < 0) return QualityGray;
            if (bonusStatTotal < 6)
            {
                return hasScroll ? QualityOrange : QualityWhite;
            }
            if (bonusStatTotal < 23) return QualityCyan;
            if (bonusStatTotal < 40) return QualityPurple;
            if (bonusStatTotal < 55) return QualityGold;
            if (bonusStatTotal < 70) return QualityGreen;
            return QualityRed;
        }

        /// <summary>
        /// 추가 옵션 수치에 따른 아이템 이름 브러시 반환
        /// </summary>
        public static Brush GetItemNameBrush(int bonusStatTotal, bool hasScroll, bool isCash = false)
        {
            return new SolidBrush(GetItemNameColor(bonusStatTotal, hasScroll, isCash));
        }

        #endregion

        #region 그리기 유틸리티

        /// <summary>
        /// 툴팁 배경 그리기 (22 버전)
        /// </summary>
        public static void DrawTooltipBackground(Graphics g, int x, int y, int width, int height)
        {
            using (var brush = new SolidBrush(TooltipBackColor))
            {
                g.FillRectangle(brush, x, y, width, height);
            }

            // 테두리
            using (var pen = new Pen(Color.FromArgb(170, 255, 255, 255)))
            {
                g.DrawRectangle(pen, x, y, width - 1, height - 1);
            }
        }

        /// <summary>
        /// 점선 구분선 그리기
        /// </summary>
        public static void DrawDotLine(Graphics g, int x1, int x2, int y)
        {
            using (var pen = new Pen(Color.FromArgb(102, 255, 255, 255)))
            {
                pen.DashStyle = DashStyle.Dot;
                g.DrawLine(pen, x1, y, x2, y);
            }
        }

        /// <summary>
        /// 실선 구분선 그리기
        /// </summary>
        public static void DrawLine(Graphics g, int x1, int x2, int y)
        {
            using (var pen = new Pen(Color.FromArgb(85, 255, 255, 255)))
            {
                g.DrawLine(pen, x1, y, x2, y);
            }
        }

        /// <summary>
        /// 스타포스 별 그리기
        /// </summary>
        public static void DrawStars(Graphics g, int x, int y, int currentStars, int maxStars = 25)
        {
            const int starWidth = 10;
            const int groupSpacing = 6;
            int starX = x;

            for (int i = 0; i < maxStars; i++)
            {
                // 5개마다 간격 추가
                if (i > 0 && i % 5 == 0)
                {
                    starX += groupSpacing;
                }

                // 별 그리기 (채워진/빈)
                var starColor = i < currentStars ? Color.FromArgb(255, 204, 0) : Color.FromArgb(80, 80, 80);
                using (var brush = new SolidBrush(starColor))
                {
                    // 간단한 별 모양 (5각형)
                    g.FillPolygon(brush, GetStarPoints(starX, y, starWidth / 2));
                }

                starX += starWidth;
            }
        }

        private static Point[] GetStarPoints(int centerX, int centerY, int radius)
        {
            var points = new Point[10];
            double angle = -Math.PI / 2; // 시작 각도 (위쪽)
            double angleStep = Math.PI / 5;

            for (int i = 0; i < 10; i++)
            {
                int r = (i % 2 == 0) ? radius : radius / 2;
                points[i] = new Point(
                    centerX + (int)(r * Math.Cos(angle)),
                    centerY + (int)(r * Math.Sin(angle))
                );
                angle += angleStep;
            }

            return points;
        }

        /// <summary>
        /// 아이콘 테두리 그리기 (잠재능력 등급별)
        /// </summary>
        public static void DrawIconBorder(Graphics g, int x, int y, int size, PotentialGrade grade)
        {
            var pen = GetPotentialBorderPen(grade);
            if (pen != null)
            {
                g.DrawRectangle(pen, x, y, size - 1, size - 1);
            }
        }

        /// <summary>
        /// 텍스트 그리기 (중앙 정렬)
        /// </summary>
        public static void DrawCenteredText(Graphics g, string text, Font font, Brush brush, int x, int width, int y)
        {
            var size = TextRenderer.MeasureText(text, font);
            int textX = x + (width - size.Width) / 2;
            TextRenderer.DrawText(g, text, font, new Point(textX, y), ((SolidBrush)brush).Color);
        }

        /// <summary>
        /// 텍스트 그리기 (왼쪽 정렬)
        /// </summary>
        public static void DrawText(Graphics g, string text, Font font, Brush brush, int x, int y)
        {
            TextRenderer.DrawText(g, text, font, new Point(x, y), ((SolidBrush)brush).Color);
        }

        /// <summary>
        /// 텍스트 그리기 (색상 직접 지정)
        /// </summary>
        public static void DrawText(Graphics g, string text, Font font, Color color, int x, int y)
        {
            TextRenderer.DrawText(g, text, font, new Point(x, y), color);
        }

        #endregion
    }

    #region 열거형

    /// <summary>
    /// 잠재능력 등급
    /// </summary>
    public enum PotentialGrade
    {
        None = 0,
        Rare = 1,      // 레어
        Epic = 2,      // 에픽
        Unique = 3,    // 유니크
        Legendary = 4  // 레전드리
    }

    /// <summary>
    /// 텍스트 정렬
    /// </summary>
    public enum TextAlignment
    {
        Left,
        Center,
        Right
    }

    #endregion
}

