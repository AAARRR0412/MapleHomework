using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Media;
using MapleHomework.Rendering.Core;

namespace MapleHomework.Rendering.Tooltips
{
    /// <summary>
    /// 툴팁 렌더러 기본 클래스 (WzComparerR2 TooltipRender.cs 기반)
    /// </summary>
    public abstract class TooltipRenderer
    {
        protected const int DefaultPicHeight = 4000; // 렌더링용 임시 높이
        protected const int TooltipWidth22 = 324;    // 22버전 너비
        protected const int TooltipWidthOld = 261;   // 구버전 너비

        /// <summary>
        /// 22버전 UI 사용 여부
        /// </summary>
        public bool Use22Style { get; set; } = true;

        /// <summary>
        /// 툴팁 너비
        /// </summary>
        protected int TooltipWidth => Use22Style ? TooltipWidth22 : TooltipWidthOld;

        /// <summary>
        /// 렌더링 실행 (GDI+ Bitmap 반환)
        /// </summary>
        public abstract Bitmap? Render();

        /// <summary>
        /// 렌더링 실행 (WPF ImageSource 반환)
        /// </summary>
        public virtual ImageSource? RenderToImageSource()
        {
            using (var bitmap = Render())
            {
                return WpfBitmapConverter.ToImageSource(bitmap);
            }
        }

        #region 배경 그리기

        /// <summary>
        /// 툴팁 배경 그리기 (22 버전)
        /// </summary>
        protected void DrawTooltipBackground22(Graphics g, int x, int y, int width, int height)
        {
            // 메인 배경
            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(230, 17, 23, 33)))
            {
                g.FillRectangle(brush, x, y, width, height);
            }

            // 테두리 (그라디언트 효과)
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(100, 150, 180, 200)))
            {
                g.DrawRectangle(pen, x, y, width - 1, height - 1);
            }

            // 내부 테두리
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 255, 255, 255)))
            {
                g.DrawRectangle(pen, x + 1, y + 1, width - 3, height - 3);
            }
        }

        /// <summary>
        /// 툴팁 배경 그리기 (구버전)
        /// </summary>
        protected void DrawTooltipBackgroundOld(Graphics g, int x, int y, int width, int height)
        {
            // 메인 배경
            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(204, 0, 51, 85)))
            {
                g.FillRectangle(brush, x, y, width, height);
            }

            // 테두리
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(170, 255, 255, 255)))
            {
                g.DrawRectangle(pen, x, y, width - 1, height - 1);
            }
        }

        /// <summary>
        /// 현재 스타일에 맞는 배경 그리기
        /// </summary>
        protected void DrawBackground(Graphics g, int x, int y, int width, int height)
        {
            if (Use22Style)
                DrawTooltipBackground22(g, x, y, width, height);
            else
                DrawTooltipBackgroundOld(g, x, y, width, height);
        }

        #endregion

        #region 구분선 그리기

        /// <summary>
        /// 점선 구분선 그리기
        /// </summary>
        protected void DrawDotLine(Graphics g, int x1, int x2, int y)
        {
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(102, 255, 255, 255)))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                g.DrawLine(pen, x1, y, x2, y);
            }
        }

        /// <summary>
        /// 실선 구분선 그리기
        /// </summary>
        protected void DrawSolidLine(Graphics g, int x1, int x2, int y)
        {
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(85, 255, 255, 255)))
            {
                g.DrawLine(pen, x1, y, x2, y);
            }
        }

        #endregion

        #region 텍스트 그리기

        /// <summary>
        /// 텍스트 그리기 (중앙 정렬)
        /// </summary>
        protected void DrawCenteredText(Graphics g, string text, Font font, System.Drawing.Brush brush, int x, int width, int y)
        {
            var size = TextRenderer.MeasureText(text, font);
            int textX = x + (width - size.Width) / 2;
            g.DrawString(text, font, brush, textX, y);
        }

        /// <summary>
        /// 텍스트 그리기 (왼쪽 정렬)
        /// </summary>
        protected void DrawText(Graphics g, string text, Font font, System.Drawing.Brush brush, int x, int y)
        {
            g.DrawString(text, font, brush, x, y);
        }

        /// <summary>
        /// 텍스트 그리기 (색상 직접 지정)
        /// </summary>
        protected void DrawText(Graphics g, string text, Font font, System.Drawing.Color color, int x, int y)
        {
            using (var brush = new SolidBrush(color))
            {
                g.DrawString(text, font, brush, x, y);
            }
        }

        #endregion

        #region 스타포스 그리기

        /// <summary>
        /// 스타포스 별 그리기 (22 버전)
        /// </summary>
        protected void DrawStarforce(Graphics g, int x, int y, int currentStars, int maxStars = 25)
        {
            const int starSize = 10;
            const int groupSpacing = 6;
            int starX = x;

            // 15개 이상이면 2줄로 표시
            int starsPerRow = maxStars > 15 ? 15 : maxStars;
            int row = 0;

            for (int i = 0; i < maxStars; i++)
            {
                if (i > 0 && i % starsPerRow == 0)
                {
                    row++;
                    starX = x;
                }

                // 5개마다 간격 추가
                if (i % starsPerRow > 0 && i % 5 == 0)
                {
                    starX += groupSpacing;
                }

                // 별 색상 결정
                var starColor = i < currentStars
                    ? System.Drawing.Color.FromArgb(255, 204, 0)   // 채워진 별 (금색)
                    : System.Drawing.Color.FromArgb(60, 60, 60);   // 빈 별 (어두운 회색)

                // 별 그리기
                DrawStar(g, starX + starSize / 2, y + row * (starSize + 2) + starSize / 2, starSize / 2, starColor);

                starX += starSize;
            }
        }

        private void DrawStar(Graphics g, int centerX, int centerY, int radius, System.Drawing.Color color)
        {
            var points = new PointF[10];
            double angle = -Math.PI / 2;
            double angleStep = Math.PI / 5;

            for (int i = 0; i < 10; i++)
            {
                float r = (i % 2 == 0) ? radius : radius * 0.4f;
                points[i] = new PointF(
                    centerX + (float)(r * Math.Cos(angle)),
                    centerY + (float)(r * Math.Sin(angle))
                );
                angle += angleStep;
            }

            using (var brush = new SolidBrush(color))
            {
                g.FillPolygon(brush, points);
            }
        }

        #endregion

        #region 아이콘 그리기

        /// <summary>
        /// 아이콘 박스 그리기
        /// </summary>
        protected void DrawIconBox(Graphics g, int x, int y, int size = 36)
        {
            // 배경
            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(51, 0, 0, 0)))
            {
                g.FillRectangle(brush, x, y, size, size);
            }

            // 테두리
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(102, 255, 255, 255)))
            {
                g.DrawRectangle(pen, x, y, size - 1, size - 1);
            }
        }

        /// <summary>
        /// 잠재능력 등급 테두리 그리기
        /// </summary>
        protected void DrawPotentialBorder(Graphics g, int x, int y, int size, PotentialGrade grade)
        {
            var pen = MapleGraphics.GetPotentialBorderPen(grade);
            if (pen != null)
            {
                g.DrawRectangle(pen, x, y, size - 1, size - 1);
            }
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 비트맵 최종 크기로 자르기
        /// </summary>
        protected Bitmap CropBitmap(Bitmap source, int width, int height)
        {
            var result = new Bitmap(width, height);
            using (var g = Graphics.FromImage(result))
            {
                g.DrawImage(source, 0, 0, new Rectangle(0, 0, width, height), GraphicsUnit.Pixel);
            }
            return result;
        }

        /// <summary>
        /// Graphics 초기화 (안티앨리어싱 등)
        /// </summary>
        protected void InitGraphics(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        }

        #endregion
    }
}

