using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using MapleHomework.Rendering.Models;

namespace MapleHomework.Rendering
{
    /// <summary>
    /// WzComparerR2 스타일 스킬 툴팁 렌더러
    /// </summary>
    public class SkillTooltipRenderer
    {
        #region Constants
        private const int TooltipWidth = 430;
        private const int DefaultPicHeight = 1000;
        private const int Padding = 13;
        private const int LineHeight = 16;
        #endregion

        #region Resource Cache
        private static readonly Dictionary<string, Bitmap> _resourceCache = new Dictionary<string, Bitmap>();
        private static readonly Dictionary<string, TextureBrush> _flexBrushCache = new Dictionary<string, TextureBrush>(); // 9-slice flexible frame
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private static readonly string _resourcePrefix = "MapleHomework.Rendering.Resources.";
        private static bool _initialized = false;
        #endregion

        #region Colors
        private static readonly Color ColorWhite = Color.White;
        private static readonly Color ColorYellow = Color.FromArgb(255, 204, 0);
        private static readonly Color ColorCyan = Color.FromArgb(102, 255, 255);
        #endregion

        #region Fonts
        private static Font ItemNameFont;
        private static Font EquipDetailFont;

        static SkillTooltipRenderer()
        {
            try
            {
                ItemNameFont = new Font("Gulim", 14f, FontStyle.Bold, GraphicsUnit.Pixel);
                EquipDetailFont = new Font("Gulim", 11f, GraphicsUnit.Pixel);
            }
            catch
            {
                ItemNameFont = new Font("Malgun Gothic", 14f, FontStyle.Bold, GraphicsUnit.Pixel);
                EquipDetailFont = new Font("Malgun Gothic", 11f, GraphicsUnit.Pixel);
            }
            LoadTextureBrushes();
        }

        private static void LoadTextureBrushes()
        {
            if (_initialized) return;
            _initialized = true;

            // 1. 9-slice flexible frame 리소스 로드
            string[] directions = { "n", "ne", "e", "se", "s", "sw", "w", "nw", "c" };
            foreach (var dir in directions)
            {
                var bmp = LoadResource($"UIToolTipNew.img.Item.Common.frame.flexible.{dir}.png");
                if (bmp != null)
                {
                    WrapMode mode = (dir == "n" || dir == "s" || dir == "e" || dir == "w" || dir == "c") ? WrapMode.Tile : WrapMode.Clamp;
                    _flexBrushCache[dir] = new TextureBrush(bmp, mode);
                }
            }

            // 2. Fixed frame 리소스 로드 (Fallback용)
            var fixedMap = new (string key, string res, WrapMode mode)[]
            {
                ("top",  "UIToolTipNew.img.Item.Common.frame.fixed.top.png",  WrapMode.Clamp),
                ("mid",  "UIToolTipNew.img.Item.Common.frame.fixed.mid.png",  WrapMode.Tile),
                ("line", "UIToolTipNew.img.Item.Common.frame.fixed.line.png", WrapMode.Clamp),
                ("btm",  "UIToolTipNew.img.Item.Common.frame.fixed.btm.png",  WrapMode.Clamp),
            };
            foreach (var (key, res, mode) in fixedMap)
            {
                var bmp = LoadResource(res);
                if (bmp != null)
                {
                    _flexBrushCache[key] = new TextureBrush(bmp, mode);
                }
            }
            // 3. Skill Type Badges
            var badges = new[] { "UIWindow2.img.Skill.skillTypeIcon.origin.png", "UIWindow2.img.Skill.skillTypeIcon.ascent.png" };
            foreach (var badge in badges)
            {
                LoadResource(badge);
            }
        }
        #endregion

        #region Properties
        public SkillTooltipData Skill { get; set; }
        public bool ShowProperties { get; set; } = true;
        #endregion

        public SkillTooltipRenderer(SkillTooltipData skill)
        {
            this.Skill = skill;
        }

        public Bitmap Render()
        {
            if (this.Skill == null)
            {
                return new Bitmap(1, 1);
            }

            var contentBitmap = new Bitmap(TooltipWidth, DefaultPicHeight);
            int picH = 10;

            using (var g = Graphics.FromImage(contentBitmap))
            {
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.SmoothingMode = SmoothingMode.None;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                // 스킬 이름
                DrawCenteredText(g, Skill.Name ?? "(Unknown)", ItemNameFont, ColorWhite, TooltipWidth / 2, picH);

                // 뱃지 렌더링 (헤더 영역: 스킬명과 같은 줄 왼쪽)
                if (Skill.IsOrigin)
                {
                    var badge = LoadResource("UIWindow2.img.Skill.skillTypeIcon.origin.png");
                    if (badge != null)
                    {
                        // (Padding, picH) 위치에 67x14 크기로 그리기
                        g.DrawImage(badge, Padding, picH + 1, 67, 14); // 가로 중앙 정렬 미세 조정 (+1)
                    }
                }
                else if (Skill.IsAscent)
                {
                    var badge = LoadResource("UIWindow2.img.Skill.skillTypeIcon.ascent.png");
                    if (badge != null)
                    {
                        g.DrawImage(badge, Padding, picH + 1, 67, 14);
                    }
                }

                picH += 24; // 스킬 이름과 설명 사이 간격 추가

                // 아이콘 영역
                int iconAreaTop = picH;
                int iconSize = 64; // 2x 확대된 아이콘 크기 (원본 32x32 -> 64x64)
                int textLeft = Padding + iconSize + 10; // 아이콘 + 간격

                if (Skill.IconBitmap != null)
                {
                    // 아이콘 2배 확대 (배경/커버 없이 아이콘만)
                    using (var enlarged = EnlargeBitmap(Skill.IconBitmap))
                    {
                        int iconX = Padding;
                        int iconY = picH;
                        g.DrawImage(enlarged, iconX, iconY);

                    }
                }
                else
                {
                    textLeft = Padding;
                }

                // 스킬 설명
                if (!string.IsNullOrEmpty(Skill.Description))
                {
                    int descY = iconAreaTop;
                    var descLines = Skill.Description.Replace("\r\n", "\n").Split('\n');
                    foreach (var line in descLines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            descY += 5;
                            continue;
                        }
                        DrawWrappedText(g, line.Trim(), EquipDetailFont, ColorWhite, textLeft, TooltipWidth - Padding - textLeft, ref descY);
                    }
                    picH = Math.Max(picH + 72, descY + 4);
                }
                else
                {
                    picH += 72;
                }

                // 구분선
                picH += 4;
                DrawSeparator(g, picH);
                picH += 8;

                // [현재레벨 N]
                if (Skill.Level > 0 && !string.IsNullOrEmpty(Skill.SkillEffect))
                {
                    DrawText(g, $"[현재레벨 {Skill.Level}]", EquipDetailFont, ColorYellow, Padding, picH);
                    picH += LineHeight;

                    var effectLines = Skill.SkillEffect.Replace("\r\n", "\n").Split('\n');
                    foreach (var line in effectLines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            picH += 4;
                            continue;
                        }
                        // 재사용 대기시간은 노란색으로 표시
                        Color lineColor = line.Trim().StartsWith("재사용") ? ColorYellow : ColorWhite;
                        DrawWrappedText(g, line.Trim(), EquipDetailFont, lineColor, Padding, TooltipWidth - Padding * 2, ref picH);
                    }
                    picH += 5;
                }

                // [다음레벨 N+1]
                if (Skill.Level < Skill.MaxLevel && !string.IsNullOrEmpty(Skill.SkillEffectNext))
                {
                    DrawText(g, $"[다음레벨 {Skill.Level + 1}]", EquipDetailFont, ColorYellow, Padding, picH);
                    picH += LineHeight;

                    var nextLines = Skill.SkillEffectNext.Replace("\r\n", "\n").Split('\n');
                    foreach (var line in nextLines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            picH += 4;
                            continue;
                        }
                        DrawWrappedText(g, line.Trim(), EquipDetailFont, ColorWhite, Padding, TooltipWidth - Padding * 2, ref picH);
                    }
                    picH += 5;
                }

                picH += 8;
            }

            // 최종 비트맵 (업스케일 없이 원본 크기)
            var finalBitmap = new Bitmap(TooltipWidth, picH);
            using (var fg = Graphics.FromImage(finalBitmap))
            {
                DrawNewTooltipBack(fg, 0, 0, TooltipWidth, picH);
                fg.DrawImage(contentBitmap, 0, 0, new Rectangle(0, 0, TooltipWidth, picH), GraphicsUnit.Pixel);
            }

            contentBitmap.Dispose();
            return finalBitmap;
        }

        #region Drawing Helpers
        /// <summary>
        /// 9-slice flexible frame 배경 그리기
        /// </summary>
        private static void DrawNewTooltipBack(Graphics g, int x, int y, int width, int height)
        {
            TextureBrush? Get(string k) => MapleTooltipRenderer.GetFlexBrush(k);

            // 필수 리소스 체크 (없으면 Fixed Frame으로 폴백)
            if (Get("nw") == null || Get("n") == null || Get("c") == null)
            {
                DrawFixedTooltipBack(g, x, y, width, height);
                return;
            }

            // 가이드라인 계산
            var wImg = Get("w")!.Image; var eImg = Get("e")!.Image;
            var nImg = Get("n")!.Image; var sImg = Get("s")!.Image;

            int[] guideX = new int[4] { 0, wImg.Width, width - eImg.Width, width };
            int[] guideY = new int[4] { 0, nImg.Height, height - sImg.Height, height };
            for (int i = 0; i < guideX.Length; i++) guideX[i] += x;
            for (int i = 0; i < guideY.Length; i++) guideY[i] += y;

            // ... (Drawing) ...

            // 4개 모서리
            FillRect(g, Get("nw"), guideX, guideY, 0, 0, 1, 1);
            FillRect(g, Get("ne"), guideX, guideY, 2, 0, 3, 1);
            FillRect(g, Get("sw"), guideX, guideY, 0, 2, 1, 3);
            FillRect(g, Get("se"), guideX, guideY, 2, 2, 3, 3);

            // 상단/하단 변
            if (guideX[2] > guideX[1])
            {
                FillRect(g, Get("n"), guideX, guideY, 1, 0, 2, 1);
                FillRect(g, Get("s"), guideX, guideY, 1, 2, 2, 3);
            }

            // 좌측/우측 변
            if (guideY[2] > guideY[1])
            {
                FillRect(g, Get("w"), guideX, guideY, 0, 1, 1, 2);
                FillRect(g, Get("e"), guideX, guideY, 2, 1, 3, 2);
            }

            // 중앙
            if (guideX[2] > guideX[1] && guideY[2] > guideY[1])
            {
                FillRect(g, Get("c"), guideX, guideY, 1, 1, 2, 2);
            }
        }

        private static void FillRect(Graphics g, TextureBrush? brush, int[] guideX, int[] guideY, int x0, int y0, int x1, int y1)
        {
            if (brush == null) return;
            brush.ResetTransform();
            brush.TranslateTransform(guideX[x0], guideY[y0]);
            g.FillRectangle(brush, guideX[x0], guideY[y0], guideX[x1] - guideX[x0], guideY[y1] - guideY[y0]);
        }

        /// <summary>
        /// Fixed frame 배경 그리기 (top/mid/btm 3단) - Fallback
        /// </summary>
        /// <summary>
        /// Fixed frame 배경 그리기 (top/mid/btm 3단) - Fallback
        /// </summary>
        private static void DrawFixedTooltipBack(Graphics g, int x, int y, int width, int height)
        {
            /* [DEBUG] 중복 렌더링 확인을 위해 비워둠
            TextureBrush? Get(string k) => MapleTooltipRenderer.GetFixedBrush(k);

            // 필수 리소스 체크
            if (Get("top") == null || Get("mid") == null || Get("btm") == null)
            {
                using (var b = new SolidBrush(Color.FromArgb(220, 0, 0, 0)))
                    g.FillRectangle(b, x, y, width, height);

                using (Font debugFont = new Font("Arial", 10))
                {
                    g.DrawString("Res Load Failed.", debugFont, Brushes.Red, x + 10, y + 10);
                }
                return;
            }

            var top = Get("top");
            var mid = Get("mid");
            var btm = Get("btm");

            int topH = top.Image.Height;
            int btmH = btm.Image.Height;

            // 상단 (타일링)
            top.ResetTransform();
            top.TranslateTransform(x, y);
            g.FillRectangle(top, x, y, width, topH);

            // 중간 (타일링)
            int midH = Math.Max(0, height - topH - btmH);
            if (midH > 0)
            {
                mid.ResetTransform();
                mid.TranslateTransform(x, y + topH);
                g.FillRectangle(mid, x, y + topH, width, midH);
            }

            // 하단 (타일링)
            btm.ResetTransform();
            btm.TranslateTransform(x, y + height - btmH);
            g.FillRectangle(btm, x, y + height - btmH, width, btmH);
            */
        }

        private static void DrawSeparator(Graphics g, int y)
        {
            // flexible dotline 먼저 확인
            if (LoadResource("UIToolTipNew.img.Item.Common.frame.flexible.dotline.png") is Bitmap dotline)
            {
                using (var brush = new TextureBrush(dotline, WrapMode.Tile))
                {
                    brush.TranslateTransform(Padding, y);
                    g.FillRectangle(brush, Padding, y, TooltipWidth - Padding * 2, dotline.Height);
                }
            }
            // 없으면 fixed line 확인
            else if (_flexBrushCache.TryGetValue("line", out var lineBrush))
            {
                lineBrush.ResetTransform();
                lineBrush.TranslateTransform(Padding, y);
                g.FillRectangle(lineBrush, Padding, y, TooltipWidth - Padding * 2, lineBrush.Image.Height);
            }
            else
            {
                using (var p = new Pen(Color.Gray) { DashStyle = DashStyle.Dot })
                    g.DrawLine(p, Padding, y, TooltipWidth - Padding, y);
            }
        }

        private static void DrawText(Graphics g, string text, Font font, Color color, int x, int y)
        {
            TextRenderer.DrawText(g, text, font, new Point(x, y), color, TextFormatFlags.NoPadding);
        }

        private static void DrawCenteredText(Graphics g, string text, Font font, Color color, int centerX, int y)
        {
            var size = TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, text, font, new Point(centerX - size.Width / 2, y), color, TextFormatFlags.NoPadding);
        }

        private static void DrawWrappedText(Graphics g, string text, Font font, Color color, int x, int maxWidth, ref int y)
        {
            if (string.IsNullOrEmpty(text)) return;

            var currentLine = "";
            foreach (char c in text)
            {
                var testLine = currentLine + c;
                var size = TextRenderer.MeasureText(g, testLine, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);

                if (size.Width > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    TextRenderer.DrawText(g, currentLine, font, new Point(x, y), color, TextFormatFlags.NoPadding);
                    y += LineHeight;
                    currentLine = c.ToString();
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                TextRenderer.DrawText(g, currentLine, font, new Point(x, y), color, TextFormatFlags.NoPadding);
                y += LineHeight;
            }
        }

        private static Bitmap EnlargeBitmap(Bitmap source)
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

        #region Resource Loading
        private static Bitmap? LoadResource(string resourceName)
        {
            if (_resourceCache.TryGetValue(resourceName, out var cached)) return cached;

            var embeddedName = resourceName.EndsWith(".png")
                ? _resourcePrefix + resourceName
                : _resourcePrefix + resourceName + ".png";

            try
            {
                var stream = _assembly.GetManifestResourceStream(embeddedName);
                if (stream != null)
                {
                    var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    stream.Dispose();
                    ms.Position = 0;

                    var bmp = new Bitmap(ms);
                    _resourceCache[resourceName] = bmp;
                    return bmp;
                }
            }
            catch { }

            return null;
        }
        #endregion
    }
}
