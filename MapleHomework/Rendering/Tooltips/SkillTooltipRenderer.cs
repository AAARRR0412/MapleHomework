using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Threading.Tasks;
using MapleHomework.Rendering.Core;
using MapleHomework.Rendering.Models;

namespace MapleHomework.Rendering.Tooltips
{
    /// <summary>
    /// 스킬 툴팁 렌더러 (SkillTooltipRender2.cs 기반)
    /// </summary>
    public class SkillTooltipRenderer : TooltipRenderer
    {
        private SkillData? _skill;
        private Bitmap? _iconBitmap;

        public SkillData? Skill
        {
            get => _skill;
            set => _skill = value;
        }

        public Bitmap? IconBitmap
        {
            get => _iconBitmap;
            set => _iconBitmap = value;
        }

        /// <summary>
        /// 아이콘 URL에서 비트맵 로드
        /// </summary>
        public async Task LoadIconAsync()
        {
            if (string.IsNullOrEmpty(Skill?.SkillIcon))
                return;

            try
            {
                using var client = new HttpClient();
                var bytes = await client.GetByteArrayAsync(Skill.SkillIcon);
                using var stream = new System.IO.MemoryStream(bytes);
                _iconBitmap = new Bitmap(stream);
            }
            catch
            {
                _iconBitmap = null;
            }
        }

        public override Bitmap? Render()
        {
            if (Skill == null)
                return null;

            int width = TooltipWidth;
            var bitmap = new Bitmap(width, DefaultPicHeight);
            using var g = Graphics.FromImage(bitmap);
            InitGraphics(g);

            int y = 12;
            int leftPadding = 12;
            int rightPadding = width - 12;
            int contentWidth = rightPadding - leftPadding;

            // 스킬 이름
            using (var nameBrush = new SolidBrush(MapleGraphics.WhiteColor))
            {
                DrawCenteredText(g, Skill.SkillName, MapleGraphics.ItemNameFont, nameBrush, 0, width, y);
            }
            y += 20;

            // 헥사 코어 레벨 표시
            if (Skill.HexaCoreLevel > 0)
            {
                using (var levelBrush = new SolidBrush(MapleGraphics.Orange3Color))
                {
                    DrawCenteredText(g, $"[마스터 레벨 : {Skill.HexaCoreLevel}]", MapleGraphics.EquipDetailFont, levelBrush, 0, width, y);
                }
                y += 16;
            }
            else if (Skill.SkillLevel > 0)
            {
                using (var levelBrush = new SolidBrush(MapleGraphics.Equip22Gray))
                {
                    DrawCenteredText(g, $"[현재 레벨 : {Skill.SkillLevel}]", MapleGraphics.EquipDetailFont, levelBrush, 0, width, y);
                }
                y += 16;
            }

            y += 4;
            DrawDotLine(g, leftPadding, rightPadding, y);
            y += 8;

            // 아이콘 및 설명
            int iconX = leftPadding;
            int iconY = y;
            int iconSize = 36;

            // 아이콘 배경
            DrawIconBox(g, iconX, iconY, iconSize);

            // 아이콘 이미지
            if (_iconBitmap != null)
            {
                g.DrawImage(_iconBitmap, iconX + 2, iconY + 2, iconSize - 4, iconSize - 4);
            }

            // 스킬 설명 (아이콘 오른쪽)
            int descX = iconX + iconSize + 10;
            int descWidth = rightPadding - descX;

            if (!string.IsNullOrEmpty(Skill.SkillDescription))
            {
                y = DrawWrappedText(g, Skill.SkillDescription, descX, iconY, descWidth, MapleGraphics.Equip22Gray);
                y = Math.Max(y, iconY + iconSize);
            }
            else
            {
                y = iconY + iconSize;
            }

            y += 8;

            // 스킬 효과
            if (!string.IsNullOrEmpty(Skill.SkillEffect))
            {
                DrawDotLine(g, leftPadding, rightPadding, y);
                y += 8;

                using (var effectBrush = new SolidBrush(MapleGraphics.WhiteColor))
                {
                    y = DrawWrappedText(g, Skill.SkillEffect, leftPadding, y, contentWidth, MapleGraphics.WhiteColor);
                }
            }

            y += 12;

            // 최종 비트맵 생성
            var finalBitmap = new Bitmap(width, y);
            using (var fg = Graphics.FromImage(finalBitmap))
            {
                InitGraphics(fg);
                DrawBackground(fg, 0, 0, width, y);
                fg.DrawImage(bitmap, 0, 0);
            }

            bitmap.Dispose();
            return finalBitmap;
        }

        private int DrawWrappedText(Graphics g, string text, int x, int y, int maxWidth, Color color)
        {
            if (string.IsNullOrEmpty(text))
                return y;

            using var brush = new SolidBrush(color);
            var font = MapleGraphics.EquipDetailFont;

            // 간단한 줄바꿈 처리
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var words = line.Split(' ');
                var currentLine = "";

                foreach (var word in words)
                {
                    var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                    var size = g.MeasureString(testLine, font);

                    if (size.Width > maxWidth && !string.IsNullOrEmpty(currentLine))
                    {
                        g.DrawString(currentLine, font, brush, x, y);
                        y += 14;
                        currentLine = word;
                    }
                    else
                    {
                        currentLine = testLine;
                    }
                }

                if (!string.IsNullOrEmpty(currentLine))
                {
                    g.DrawString(currentLine, font, brush, x, y);
                    y += 14;
                }
            }

            return y;
        }
    }
}

