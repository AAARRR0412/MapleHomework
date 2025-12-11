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
    /// 장비 툴팁 렌더러 (GearTooltipRender22.cs 기반 - 최신 UI)
    /// </summary>
    public class GearTooltipRenderer : TooltipRenderer
    {
        private GearData? _gear;
        private Bitmap? _iconBitmap;

        public GearData? Gear
        {
            get => _gear;
            set => _gear = value;
        }

        /// <summary>
        /// 아이콘 비트맵 (URL에서 로드된)
        /// </summary>
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
            if (string.IsNullOrEmpty(Gear?.ItemIcon))
                return;

            try
            {
                using var client = new HttpClient();
                var bytes = await client.GetByteArrayAsync(Gear.ItemIcon);
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
            if (Gear == null)
                return null;

            int width = TooltipWidth;
            var bitmap = new Bitmap(width, DefaultPicHeight);
            using var g = Graphics.FromImage(bitmap);
            InitGraphics(g);

            int y = 0;
            int leftPadding = 12;
            int rightPadding = width - 12;
            int contentWidth = rightPadding - leftPadding;

            // 스타포스
            if (Gear.Starforce > 0)
            {
                y += 8;
                int starWidth = CalculateStarWidth(Gear.Starforce);
                int starX = (width - starWidth) / 2;
                DrawStarforce(g, starX, y, Gear.Starforce);
                y += Gear.Starforce > 15 ? 24 : 14;
            }

            y += 6;

            // 아이템 이름
            var nameColor = MapleGraphics.GetItemNameColor(Gear.GetBonusStatTotal(), Gear.HasScrollUpgrade());
            using (var nameBrush = new SolidBrush(nameColor))
            {
                DrawCenteredText(g, Gear.ItemName, MapleGraphics.ItemNameFont, nameBrush, 0, width, y);
            }
            y += 18;

            // 교환 불가 표시
            if (Gear.IsUntradeable())
            {
                using (var brush = new SolidBrush(MapleGraphics.Orange2Color))
                {
                    DrawCenteredText(g, "(교환 불가)", MapleGraphics.EquipDetailFont, brush, 0, width, y);
                }
                y += 14;
            }

            y += 4;
            DrawDotLine(g, leftPadding, rightPadding, y);
            y += 8;

            // 아이콘 및 기본 정보
            int iconX = leftPadding;
            int iconY = y;
            int iconSize = 42;

            // 아이콘 배경
            DrawIconBox(g, iconX, iconY, iconSize);

            // 아이콘 이미지
            if (_iconBitmap != null)
            {
                g.DrawImage(_iconBitmap, iconX + 3, iconY + 3, iconSize - 6, iconSize - 6);
            }

            // 잠재능력 테두리
            var potGrade = Gear.GetPotentialGrade();
            if (potGrade != PotentialGrade.None)
            {
                DrawPotentialBorder(g, iconX, iconY, iconSize, potGrade);
            }

            // 기본 정보 (아이콘 오른쪽)
            int infoX = iconX + iconSize + 10;

            using (var grayBrush = new SolidBrush(MapleGraphics.Equip22Gray))
            using (var whiteBrush = new SolidBrush(MapleGraphics.WhiteColor))
            {
                DrawText(g, $"REQ LEV : ", MapleGraphics.EquipDetailFont, grayBrush, infoX, iconY);
                var reqSize = g.MeasureString("REQ LEV : ", MapleGraphics.EquipDetailFont);
                DrawText(g, Gear.RequiredLevel.ToString(), MapleGraphics.EquipDetailFont, whiteBrush, infoX + (int)reqSize.Width, iconY);

                DrawText(g, $"장비분류 : ", MapleGraphics.EquipDetailFont, grayBrush, infoX, iconY + 14);
                var typeSize = g.MeasureString("장비분류 : ", MapleGraphics.EquipDetailFont);
                DrawText(g, Gear.ItemEquipmentPart, MapleGraphics.EquipDetailFont, whiteBrush, infoX + (int)typeSize.Width, iconY + 14);

                if (Gear.ScrollUpgradeableCount + Gear.ScrollUpgradeCount > 0)
                {
                    DrawText(g, $"업그레이드 가능 횟수 : ", MapleGraphics.EquipDetailFont, grayBrush, infoX, iconY + 28);
                    var upgSize = g.MeasureString("업그레이드 가능 횟수 : ", MapleGraphics.EquipDetailFont);
                    DrawText(g, Gear.ScrollUpgradeableCount.ToString(), MapleGraphics.EquipDetailFont, whiteBrush, infoX + (int)upgSize.Width, iconY + 28);
                }
            }

            y = iconY + iconSize + 8;

            DrawDotLine(g, leftPadding, rightPadding, y);
            y += 8;

            // 스탯 정보
            y = DrawStats(g, leftPadding, y, contentWidth);

            // 잠재능력
            if (potGrade != PotentialGrade.None)
            {
                y += 4;
                DrawDotLine(g, leftPadding, rightPadding, y);
                y += 8;
                y = DrawPotential(g, leftPadding, y, contentWidth);
            }

            // 에디셔널 잠재능력
            var addPotGrade = Gear.GetAdditionalPotentialGrade();
            if (addPotGrade != PotentialGrade.None)
            {
                y += 4;
                DrawDotLine(g, leftPadding, rightPadding, y);
                y += 8;
                y = DrawAdditionalPotential(g, leftPadding, y, contentWidth);
            }

            // 소울 정보
            if (!string.IsNullOrEmpty(Gear.SoulName))
            {
                y += 4;
                DrawDotLine(g, leftPadding, rightPadding, y);
                y += 8;
                y = DrawSoul(g, leftPadding, y, contentWidth);
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

        private int CalculateStarWidth(int stars)
        {
            int starSize = 10;
            int groups = (stars - 1) / 5;
            return stars * starSize + groups * 6;
        }

        private int DrawStats(Graphics g, int x, int y, int width)
        {
            if (Gear == null || Gear.TotalOption == null)
                return y;

            var total = Gear.TotalOption;
            var baseOpt = Gear.BaseOption ?? new GearStatOption();
            var addOpt = Gear.AddOption ?? new GearStatOption();
            var etcOpt = Gear.EtcOption ?? new GearStatOption();
            var starOpt = Gear.StarforceOption ?? new GearStatOption();

            // 스탯 항목들
            var stats = new (string Name, int Total, int Base, int Add, int Etc)[]
            {
                ("STR", total.Str, baseOpt.Str, addOpt.Str, etcOpt.Str + starOpt.Str),
                ("DEX", total.Dex, baseOpt.Dex, addOpt.Dex, etcOpt.Dex + starOpt.Dex),
                ("INT", total.Int, baseOpt.Int, addOpt.Int, etcOpt.Int + starOpt.Int),
                ("LUK", total.Luk, baseOpt.Luk, addOpt.Luk, etcOpt.Luk + starOpt.Luk),
                ("최대 HP", total.MaxHp, baseOpt.MaxHp, addOpt.MaxHp, etcOpt.MaxHp + starOpt.MaxHp),
                ("최대 MP", total.MaxMp, baseOpt.MaxMp, addOpt.MaxMp, etcOpt.MaxMp + starOpt.MaxMp),
                ("공격력", total.AttackPower, baseOpt.AttackPower, addOpt.AttackPower, etcOpt.AttackPower + starOpt.AttackPower),
                ("마력", total.MagicPower, baseOpt.MagicPower, addOpt.MagicPower, etcOpt.MagicPower + starOpt.MagicPower),
                ("방어력", total.Armor, baseOpt.Armor, addOpt.Armor, etcOpt.Armor + starOpt.Armor),
            };

            using var whiteBrush = new SolidBrush(MapleGraphics.WhiteColor);
            using var grayBrush = new SolidBrush(MapleGraphics.GrayColor);
            using var bonusBrush = new SolidBrush(MapleGraphics.BonusStatColor);
            using var scrollBrush = new SolidBrush(MapleGraphics.ScrollColor);

            foreach (var (name, totalVal, baseVal, addVal, etcVal) in stats)
            {
                if (totalVal == 0) continue;

                // 기본: "STR : +123"
                string text = $"{name} : +{totalVal}";
                DrawText(g, text, MapleGraphics.EquipDetailFont, whiteBrush, x, y);

                // 상세: "(기본 +100, 추옵 +10, 스크롤 +13)"
                if (addVal > 0 || etcVal > 0)
                {
                    var mainSize = g.MeasureString(text, MapleGraphics.EquipDetailFont);
                    int detailX = x + (int)mainSize.Width + 4;

                    string detail = $"(+{baseVal}";
                    DrawText(g, detail, MapleGraphics.EquipDetailFont, grayBrush, detailX, y);
                    var detailSize = g.MeasureString(detail, MapleGraphics.EquipDetailFont);
                    detailX += (int)detailSize.Width;

                    if (addVal > 0)
                    {
                        string addText = $" +{addVal}";
                        DrawText(g, addText, MapleGraphics.EquipDetailFont, bonusBrush, detailX, y);
                        detailX += (int)g.MeasureString(addText, MapleGraphics.EquipDetailFont).Width;
                    }

                    if (etcVal > 0)
                    {
                        string etcText = $" +{etcVal}";
                        DrawText(g, etcText, MapleGraphics.EquipDetailFont, scrollBrush, detailX, y);
                        detailX += (int)g.MeasureString(etcText, MapleGraphics.EquipDetailFont).Width;
                    }

                    DrawText(g, ")", MapleGraphics.EquipDetailFont, grayBrush, detailX, y);
                }

                y += 15;
            }

            // 보스 데미지, 방무 등
            if (total.BossDamage > 0)
            {
                DrawText(g, $"보스 몬스터 공격 시 데미지 : +{total.BossDamage}%", MapleGraphics.EquipDetailFont, whiteBrush, x, y);
                y += 15;
            }

            if (total.IgnoreMonsterArmor > 0)
            {
                DrawText(g, $"몬스터 방어율 무시 : +{total.IgnoreMonsterArmor}%", MapleGraphics.EquipDetailFont, whiteBrush, x, y);
                y += 15;
            }

            if (total.AllStat > 0)
            {
                DrawText(g, $"올스탯 : +{total.AllStat}%", MapleGraphics.EquipDetailFont, whiteBrush, x, y);
                y += 15;
            }

            return y;
        }

        private int DrawPotential(Graphics g, int x, int y, int width)
        {
            if (Gear == null)
                return y;

            var grade = Gear.GetPotentialGrade();
            var gradeColor = MapleGraphics.GetPotentialColor(grade);
            var gradeName = GetGradeName(grade);

            // 등급 표시
            using (var brush = new SolidBrush(gradeColor))
            {
                DrawText(g, $"잠재옵션 ({gradeName} 아이템)", MapleGraphics.EquipDetailFont, brush, x, y);
            }
            y += 15;

            // 옵션 목록
            using var whiteBrush = new SolidBrush(MapleGraphics.WhiteColor);
            foreach (var option in Gear.GetPotentialOptions())
            {
                DrawText(g, $"■ {option}", MapleGraphics.EquipDetailFont, whiteBrush, x, y);
                y += 15;
            }

            return y;
        }

        private int DrawAdditionalPotential(Graphics g, int x, int y, int width)
        {
            if (Gear == null)
                return y;

            var grade = Gear.GetAdditionalPotentialGrade();
            var gradeColor = MapleGraphics.GetPotentialColor(grade);
            var gradeName = GetGradeName(grade);

            // 등급 표시
            using (var brush = new SolidBrush(gradeColor))
            {
                DrawText(g, $"에디셔널 잠재옵션 ({gradeName} 아이템)", MapleGraphics.EquipDetailFont, brush, x, y);
            }
            y += 15;

            // 옵션 목록
            using var whiteBrush = new SolidBrush(MapleGraphics.WhiteColor);
            foreach (var option in Gear.GetAdditionalPotentialOptions())
            {
                DrawText(g, $"+ {option}", MapleGraphics.EquipDetailFont, whiteBrush, x, y);
                y += 15;
            }

            return y;
        }

        private int DrawSoul(Graphics g, int x, int y, int width)
        {
            if (Gear == null)
                return y;

            using var yellowBrush = new SolidBrush(MapleGraphics.Orange3Color);
            using var whiteBrush = new SolidBrush(MapleGraphics.WhiteColor);

            DrawText(g, Gear.SoulName, MapleGraphics.EquipDetailFont, yellowBrush, x, y);
            y += 15;

            if (!string.IsNullOrEmpty(Gear.SoulOption))
            {
                DrawText(g, Gear.SoulOption, MapleGraphics.EquipDetailFont, whiteBrush, x, y);
                y += 15;
            }

            return y;
        }

        private string GetGradeName(PotentialGrade grade)
        {
            return grade switch
            {
                PotentialGrade.Rare => "레어",
                PotentialGrade.Epic => "에픽",
                PotentialGrade.Unique => "유니크",
                PotentialGrade.Legendary => "레전드리",
                _ => ""
            };
        }
    }
}

