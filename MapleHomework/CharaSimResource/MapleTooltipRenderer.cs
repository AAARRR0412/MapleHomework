using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Net.Http;
using MapleHomework.Models;

namespace MapleHomework.CharaSimResource
{
    public class MapleTooltipRenderer
    {
        private const int TooltipWidth = 324;
        private const int DefaultPicHeight = 1000;
        private const int Padding = 15; // 좌우 여백 (12 -> 15로 수정, 원본과 일치)

        // 리소스 캐시
        private static readonly Dictionary<string, Bitmap> _resourceCache = new Dictionary<string, Bitmap>();
        private static readonly Dictionary<string, TextureBrush> _brushCache = new Dictionary<string, TextureBrush>();
        private static readonly string _resourcePath;

        // 폰트
        public static Font ItemNameFont { get; private set; }
        public static Font EquipDetailFont { get; private set; } // 돋움 11px
        public static Font EquipMDMoris9Font { get; private set; } // 돋움 9pt (약 12px)

        // 색상 정의
        public static readonly Color TextColorGray = Color.FromArgb(153, 153, 153);
        public static readonly Color TextColorGreen = Color.FromArgb(204, 255, 0);
        public static readonly Color TextColorBlue = Color.FromArgb(102, 255, 255);
        public static readonly Color TextColorPurple = Color.FromArgb(175, 173, 255);
        public static readonly Color TextColorOrange = Color.FromArgb(255, 170, 0);
        public static readonly Color TextColorRed = Color.FromArgb(255, 0, 102);
        public static readonly Color TextColorEmphasis = Color.FromArgb(255, 204, 0);
        public static readonly Color TextColorWhite = Color.White;

        static MapleTooltipRenderer()
        {
            // 리소스 경로 설정 (이전과 동일)
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? "";
            string targetDir = Path.Combine(assemblyDir, "CharaSimResource", "Resources");
            if (!Directory.Exists(targetDir))
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.FullName;
                if (projectRoot != null)
                    targetDir = Path.Combine(projectRoot, "CharaSimResource", "Resources");
            }
            _resourcePath = targetDir;

            // 폰트 설정
            try
            {
                ItemNameFont = new Font("Dotum", 14f, FontStyle.Bold, GraphicsUnit.Pixel);
                EquipDetailFont = new Font("Dotum", 11f, GraphicsUnit.Pixel);
                EquipMDMoris9Font = new Font("Dotum", 12f, GraphicsUnit.Pixel); // 9pt ~= 12px
            }
            catch
            {
                ItemNameFont = new Font("Malgun Gothic", 14f, FontStyle.Bold, GraphicsUnit.Pixel);
                EquipDetailFont = new Font("Malgun Gothic", 11f, GraphicsUnit.Pixel);
                EquipMDMoris9Font = new Font("Malgun Gothic", 12f, GraphicsUnit.Pixel);
            }

            // TextureBrush 미리 로드 (9-Slice용)
            LoadTextureBrushes();
        }

        private static void LoadTextureBrushes()
        {
            string[] directions = { "n", "ne", "e", "se", "s", "sw", "w", "nw", "c" };
            foreach (var dir in directions)
            {
                var bmp = LoadResource($"UIToolTipNew.img.Item.Common.frame.flexible.{dir}.png");
                if (bmp != null)
                {
                    WrapMode mode = (dir == "n" || dir == "s" || dir == "e" || dir == "w" || dir == "c") ? WrapMode.Tile : WrapMode.Clamp;
                    _brushCache[dir] = new TextureBrush(bmp, mode);
                }
            }
        }

        private static Bitmap? LoadResource(string resourceName)
        {
            if (_resourceCache.TryGetValue(resourceName, out var cached)) return cached;
            try
            {
                var filePath = Path.Combine(_resourcePath, resourceName);
                if (File.Exists(filePath))
                {
                    var bmp = new Bitmap(filePath);
                    _resourceCache[resourceName] = bmp;
                    return bmp;
                }
            }
            catch { }
            return null;
        }

        public static Bitmap RenderEquipmentTooltip(ItemEquipmentInfo item)
        {
            // 1. 임시 비트맵에 내용 그리기 (배경 제외)
            var contentBitmap = new Bitmap(TooltipWidth, DefaultPicHeight);
            using (var g = Graphics.FromImage(contentBitmap))
            {
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.SmoothingMode = SmoothingMode.None;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                int picH = 10;

                // --- [Header] ---
                // 스타포스
                int starforce = ParseInt(item.Starforce);
                if (starforce > 0)
                {
                    DrawStarforce(g, starforce, 25, ref picH);
                }

                // 아이템 이름
                string name = item.ItemName ?? "";
                if (ParseInt(item.ScrollUpgrade) > 0) name += $" (+{item.ScrollUpgrade})";
                Color nameColor = GetGradeColor(item.PotentialOptionGrade);
                DrawCenteredText(g, name, MapleGearGraphics.ItemNameFont, nameColor, TooltipWidth / 2, picH);
                picH += 20;

                // 추가 속성 (교환 불가 등)
                // 실제 데이터 로직 필요 (예시)
                DrawCenteredText(g, "(교환 불가)", MapleGearGraphics.EquipDetailFont, Color.White, TooltipWidth / 2, picH);
                picH += 16;

                // --- 구분선 ---
                picH += 5; // 구분선 위 여백
                DrawSeparator(g, picH);
                picH += 8; // 구분선 아래 여백

                // --- [Basic Info] ---
                DrawIconAndBaseInfo(g, item, ref picH);

                DrawSeparator(g, picH);
                picH += 8;

                // --- [Stats] ---
                DrawStats(g, item, ref picH);

                DrawSeparator(g, picH);
                picH += 8;

                // --- [Potential] ---
                if (!string.IsNullOrEmpty(item.PotentialOptionGrade) && item.PotentialOptionGrade != "없음")
                {
                    var opts = new[] { item.PotentialOption1, item.PotentialOption2, item.PotentialOption3 }
                        .Select(x => x ?? string.Empty).ToArray();
                    DrawPotential(g, item.PotentialOptionGrade, opts, false, ref picH);
                }

                if (!string.IsNullOrEmpty(item.AdditionalPotentialOptionGrade) && item.AdditionalPotentialOptionGrade != "없음")
                {
                    picH += 4;
                    DrawSeparator(g, picH);
                    picH += 8;
                    var opts = new[] { item.AdditionalPotentialOption1, item.AdditionalPotentialOption2, item.AdditionalPotentialOption3 }
                        .Select(x => x ?? string.Empty).ToArray();
                    DrawPotential(g, item.AdditionalPotentialOptionGrade, opts, true, ref picH);
                }

                // --- [Footer] ---
                picH += 4;
                DrawSeparator(g, picH);
                picH += 8;
                DrawFooter(g, item, ref picH);

                picH += 10; // Bottom Padding

                // 2. 최종 비트맵 생성 (배경 합성 + 내용 복사)
                var finalBitmap = new Bitmap(TooltipWidth, picH);
                using (var fg = Graphics.FromImage(finalBitmap))
                {
                    // **핵심: GearGraphics.DrawNewTooltipBack 로직 적용**
                    DrawNewTooltipBack(fg, 0, 0, TooltipWidth, picH);
                    
                    // 내용 복사
                    fg.DrawImage(contentBitmap, 0, 0, new Rectangle(0, 0, TooltipWidth, picH), GraphicsUnit.Pixel);
                }
                
                return finalBitmap;
            }
        }

        // GearGraphics.DrawNewTooltipBack 구현 (TextureBrush 사용)
        private static void DrawNewTooltipBack(Graphics g, int x, int y, int width, int height)
        {
            // 리소스가 없으면 폴백
            if (!_brushCache.ContainsKey("n")) 
            {
                using (var b = new SolidBrush(Color.FromArgb(220, 0, 0, 0)))
                    g.FillRectangle(b, x, y, width, height);
                return;
            }

            var n = _brushCache["n"]; var ne = _brushCache["ne"]; var e = _brushCache["e"];
            var se = _brushCache["se"]; var s = _brushCache["s"]; var sw = _brushCache["sw"];
            var w = _brushCache["w"]; var nw = _brushCache["nw"]; var c = _brushCache["c"];

            int topH = n.Image.Height;
            int btmH = s.Image.Height;
            int leftW = w.Image.Width;
            int rightW = e.Image.Width;

            // 중앙
            FillRect(g, c, x + leftW, y + topH, width - leftW - rightW, height - topH - btmH);

            // 상하좌우 (Tile)
            FillRect(g, n, x + leftW, y, width - leftW - rightW, topH);
            FillRect(g, s, x + leftW, y + height - btmH, width - leftW - rightW, btmH);
            FillRect(g, w, x, y + topH, leftW, height - topH - btmH);
            FillRect(g, e, x + width - rightW, y + topH, rightW, height - topH - btmH);

            // 모서리 (Image Draw)
            g.DrawImage(nw.Image, x, y);
            g.DrawImage(ne.Image, x + width - ne.Image.Width, y);
            g.DrawImage(sw.Image, x, y + height - sw.Image.Height);
            g.DrawImage(se.Image, x + width - se.Image.Width, y + height - se.Image.Height);
        }

        private static void FillRect(Graphics g, TextureBrush brush, int x, int y, int w, int h)
        {
            brush.ResetTransform();
            brush.TranslateTransform(x, y);
            g.FillRectangle(brush, x, y, w, h);
        }

        private static void DrawSeparator(Graphics g, int y)
        {
            var line = LoadResource("UIToolTipNew.img.Item.Common.frame.fixed.line.png");
            if (line != null)
                g.DrawImage(line, 0, y, TooltipWidth, line.Height);
            else
            {
                using (var p = new Pen(Color.Gray) { DashStyle = DashStyle.Dot })
                    g.DrawLine(p, 10, y, TooltipWidth - 10, y);
            }
        }

        private static void DrawStarforce(Graphics g, int stars, int max, ref int picH)
        {
            var starFilled = LoadResource("UIToolTipNew.img.Item.Equip.textIcon.starForce.star.png");
            var starEmpty = LoadResource("UIToolTipNew.img.Item.Equip.textIcon.starForce.empty.png");
            
            if (starFilled == null) return;

            int starW = starFilled.Width;
            int starH = starFilled.Height;
            int groupGap = 6; // 5개 단위 간격

            // 중앙 정렬 계산
            // 한 줄에 최대 15개 (메이플 규칙: 15, 10, 5)
            // 여기서는 단순화하여 15개씩 끊음
            for (int i = 0; i < max; i += 15)
            {
                int count = Math.Min(max - i, 15);
                int groups = (count - 1) / 5;
                int totalW = (count * starW) + (groups * groupGap);
                int x = (TooltipWidth - totalW) / 2;

                for (int j = 0; j < count; j++)
                {
                    int idx = i + j;
                    var img = (idx < stars) ? starFilled : starEmpty;
                    if (img != null)
                    {
                        g.DrawImage(img, x, picH);
                    }
                    
                    x += starW;
                    if ((j + 1) % 5 == 0 && j < count - 1) x += groupGap;
                }
                picH += starH + 4;
            }
            picH += 4;
        }

        private static void DrawIconAndBaseInfo(Graphics g, ItemEquipmentInfo item, ref int picH)
        {
            int iconX = 15;
            int iconSize = 72;

            // 아이콘 배경
            var iconBase = LoadResource("UIToolTipNew.img.Item.Common.ItemIcon.base.png");
            if (iconBase != null) g.DrawImage(iconBase, iconX, picH);

            // 아이콘 커버
            var iconShade = LoadResource("UIToolTipNew.img.Item.Common.ItemIcon.shade.png");
            if (iconShade != null) g.DrawImage(iconShade, iconX, picH);

            // 전투력 증가량 (우측 상단)
            int rightX = TooltipWidth - 15;
            int textY = picH + 10;
            DrawText(g, "전투력 증가량", MapleGearGraphics.EquipMDMoris9Font, MapleGearGraphics.Equip22DarkGray, rightX, textY, TextAlignment.Right);
            
            // 숫자 (이미지 대신 텍스트로 대체)
            DrawText(g, "+0", MapleGearGraphics.ItemNameFont, MapleGearGraphics.Equip22Emphasis, rightX, textY + 20, TextAlignment.Right);

            // 카테고리 태그 (우측 하단)
            // [전사] [모자] [방어구] (역순 배치)
            string[] tags = { "전사", item.ItemEquipmentSlot ?? "모자", item.ItemEquipmentPart ?? "방어구" };
            int tagY = picH + iconSize - 20;
            int currentX = rightX;

            var cw = LoadResource("UIToolTipNew.img.Item.Equip.frame.common.category.w.png");
            var cc = LoadResource("UIToolTipNew.img.Item.Equip.frame.common.category.c.png");
            var ce = LoadResource("UIToolTipNew.img.Item.Equip.frame.common.category.e.png");

            if (cw != null && cc != null && ce != null)
            {
                foreach (var tag in tags)
                {
                    int textW = TextRenderer.MeasureText(g, tag, MapleGearGraphics.EquipMDMoris9Font).Width;
                    int boxW = cw.Width + textW + ce.Width;
                    
                    currentX -= boxW;

                    // Draw Tag Box
                    g.DrawImage(cw, currentX, tagY);
                    using (var tb = new TextureBrush(cc, WrapMode.Tile))
                    {
                        tb.TranslateTransform(currentX + cw.Width, tagY);
                        g.FillRectangle(tb, currentX + cw.Width, tagY, textW, cc.Height);
                    }
                    g.DrawImage(ce, currentX + cw.Width + textW, tagY);

                    // Draw Text
                    DrawText(g, tag, MapleGearGraphics.EquipMDMoris9Font, MapleGearGraphics.Equip22Gray, currentX + boxW/2, tagY + 2, TextAlignment.Center);

                    currentX -= 4; // 간격
                }
            }

            picH += iconSize + 10;
        }

        private static void DrawStats(Graphics g, ItemEquipmentInfo item, ref int picH)
        {
            if (item.ItemTotalOption == null || item.ItemBaseOption == null) return;

            int startX = 15;
            int valX = 100; // 스탯 수치 시작 X좌표

            // 1. 일반 스탯 (STR, DEX, INT, LUK, HP, MP, ATT, MAG, DEF, SPEED, JUMP)
            var statList = new List<(string Label, string? Total, string? Base, string? Add, string? Etc, string? Star)>
            {
                ("STR", item.ItemTotalOption.Str, item.ItemBaseOption.Str, item.ItemAddOption?.Str, item.ItemEtcOption?.Str, item.ItemStarforceOption?.Str),
                ("DEX", item.ItemTotalOption.Dex, item.ItemBaseOption.Dex, item.ItemAddOption?.Dex, item.ItemEtcOption?.Dex, item.ItemStarforceOption?.Dex),
                ("INT", item.ItemTotalOption.Int, item.ItemBaseOption.Int, item.ItemAddOption?.Int, item.ItemEtcOption?.Int, item.ItemStarforceOption?.Int),
                ("LUK", item.ItemTotalOption.Luk, item.ItemBaseOption.Luk, item.ItemAddOption?.Luk, item.ItemEtcOption?.Luk, item.ItemStarforceOption?.Luk),
                ("최대 HP", item.ItemTotalOption.MaxHp, item.ItemBaseOption.MaxHp, item.ItemAddOption?.MaxHp, item.ItemEtcOption?.MaxHp, item.ItemStarforceOption?.MaxHp),
                ("최대 MP", item.ItemTotalOption.MaxMp, item.ItemBaseOption.MaxMp, item.ItemAddOption?.MaxMp, item.ItemEtcOption?.MaxMp, item.ItemStarforceOption?.MaxMp),
                ("공격력", item.ItemTotalOption.AttackPower, item.ItemBaseOption.AttackPower, item.ItemAddOption?.AttackPower, item.ItemEtcOption?.AttackPower, item.ItemStarforceOption?.AttackPower),
                ("마력", item.ItemTotalOption.MagicPower, item.ItemBaseOption.MagicPower, item.ItemAddOption?.MagicPower, item.ItemEtcOption?.MagicPower, item.ItemStarforceOption?.MagicPower),
                ("방어력", item.ItemTotalOption.Armor, item.ItemBaseOption.Armor, item.ItemAddOption?.Armor, item.ItemEtcOption?.Armor, item.ItemStarforceOption?.Armor),
                ("이동속도", item.ItemTotalOption.Speed, item.ItemBaseOption.Speed, item.ItemAddOption?.Speed, item.ItemEtcOption?.Speed, item.ItemStarforceOption?.Speed),
                ("점프력", item.ItemTotalOption.Jump, item.ItemBaseOption.Jump, item.ItemAddOption?.Jump, item.ItemEtcOption?.Jump, item.ItemStarforceOption?.Jump),
            };

            foreach (var stat in statList)
            {
                int total = ParseInt(stat.Total);
                if (total == 0) continue;

                int baseVal = ParseInt(stat.Base);
                int addVal = ParseInt(stat.Add); // 추옵
                // 주문서(Etc) + 스타포스(Star) 합산하여 강화 수치로 표기 (파란색/보라색)
                int enchVal = ParseInt(stat.Etc) + ParseInt(stat.Star); 

                // 라벨 그리기 (연회색)
                DrawText(g, stat.Label, MapleGearGraphics.EquipMDMoris9Font, MapleGearGraphics.Equip22Gray, startX, picH, TextAlignment.Left);

                // 합계 그리기 (강화/추옵 있으면 하늘색, 없으면 흰색)
                Color totalColor = (addVal > 0 || enchVal > 0) ? TextColorBlue : Color.White;
                string totalStr = $"+{total}";
                
                DrawText(g, totalStr, MapleGearGraphics.EquipMDMoris9Font, totalColor, valX, picH, TextAlignment.Left);

                // 상세 수치 그리기 (기본 +추옵 +강화)
                if (addVal > 0 || enchVal > 0)
                {
                    // 합계 텍스트 길이 측정 후 괄호 시작 위치 계산
                    int currentX = valX + System.Windows.Forms.TextRenderer.MeasureText(g, totalStr, MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width + 4;

                    // 괄호 시작
                    DrawText(g, "(", MapleGearGraphics.EquipMDMoris9Font, Color.White, currentX, picH, TextAlignment.Left);
                    currentX += 5;

                    // 기본값 (흰색)
                    DrawText(g, $"{baseVal}", MapleGearGraphics.EquipMDMoris9Font, Color.White, currentX, picH, TextAlignment.Left);
                    currentX += System.Windows.Forms.TextRenderer.MeasureText(g, $"{baseVal}", MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width;

                    // 추옵 (연두색)
                    if (addVal > 0)
                    {
                        string addStr = $" +{addVal}";
                        DrawText(g, addStr, MapleGearGraphics.EquipMDMoris9Font, MapleGearGraphics.Equip22BonusStat, currentX, picH, TextAlignment.Left);
                        currentX += System.Windows.Forms.TextRenderer.MeasureText(g, addStr, MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width;
                    }

                    // 강화 (주문서+스타포스) (하늘색/보라색)
                    if (enchVal > 0)
                    {
                        string enchStr = $" +{enchVal}";
                        // 스타포스가 포함되어 있으면 보통 파란색(Blue), 주문서만이면 보라색(Scroll)을 쓰지만, 신형 UI에선 합쳐서 파란색 계열을 많이 씀
                         // 여기선 하늘색(TextColorBlue) 사용
                         DrawText(g, enchStr, MapleGearGraphics.EquipMDMoris9Font, TextColorBlue, currentX, picH, TextAlignment.Left);
                        currentX += System.Windows.Forms.TextRenderer.MeasureText(g, enchStr, MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width;
                    }

                    // 괄호 닫기
                    DrawText(g, ")", MapleGearGraphics.EquipMDMoris9Font, Color.White, currentX, picH, TextAlignment.Left);
                }

                picH += 16;
            }

            // 2. 올스탯 (%)
            int allStat = ParseInt(item.ItemTotalOption.AllStat);
            if (allStat > 0)
            {
                int baseAll = ParseInt(item.ItemBaseOption.AllStat);
                int addAll = ParseInt(item.ItemAddOption?.AllStat);
                
                DrawText(g, "올스탯", MapleGearGraphics.EquipMDMoris9Font, MapleGearGraphics.Equip22Gray, startX, picH, TextAlignment.Left);
                
                Color totalColor = (addAll > 0) ? TextColorBlue : Color.White;
                DrawText(g, $"+{allStat}%", MapleGearGraphics.EquipMDMoris9Font, totalColor, valX, picH, TextAlignment.Left);

                if (addAll > 0)
                {
                    int currentX = valX + System.Windows.Forms.TextRenderer.MeasureText(g, $"+{allStat}%", MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width + 4;
                    
                    DrawText(g, $"(0% +{addAll}%)", MapleGearGraphics.EquipMDMoris9Font, Color.White, currentX, picH, TextAlignment.Left);
                    // 괄호 내부 색상 처리는 복잡하므로 여기선 흰색으로 통일하거나 위 로직처럼 분리 필요
                    // 올스탯은 보통 기본 0%에 추옵이 붙는 구조가 많음
                }
                picH += 16;
            }

            // 3. 특수 옵션 (보공, 방무, 데미지) - 줄바꿈 없이 한 줄씩
            // (착용 레벨 감소는 DrawRequirement에서 처리함)
            
            // 보스 공격력
            int bossDmg = ParseInt(item.ItemTotalOption.BossDamage);
            if (bossDmg > 0)
            {
                DrawText(g, $"보스 몬스터 공격 시 데미지 : +{bossDmg}%", MapleGearGraphics.EquipMDMoris9Font, Color.White, startX, picH, TextAlignment.Left);
                picH += 16;
            }

            // 방어율 무시
            int ignoreDef = ParseInt(item.ItemTotalOption.IgnoreMonsterArmor);
            if (ignoreDef > 0)
            {
                DrawText(g, $"몬스터 방어율 무시 : +{ignoreDef}%", MapleGearGraphics.EquipMDMoris9Font, Color.White, startX, picH, TextAlignment.Left);
                picH += 16;
            }

            // 데미지
            int damage = ParseInt(item.ItemTotalOption.Damage);
            if (damage > 0)
            {
                DrawText(g, $"데미지 : +{damage}%", MapleGearGraphics.EquipMDMoris9Font, Color.White, startX, picH, TextAlignment.Left);
                picH += 16;
            }
        }

        private static void DrawPotential(Graphics g, string grade, string[] options, bool isAdditional, ref int picH)
        {
            int x = 15;
            
            // 등급 아이콘
            string iconName = "UIToolTipNew.img.Item.Equip.textIcon.potential.title.";
            iconName += grade switch { "레전드리" => "legendary", "유니크" => "unique", "에픽" => "epic", _ => "rare" };
            iconName += ".png";
            var icon = LoadResource(iconName);
            if (icon != null)
            {
                g.DrawImage(icon, x, picH);
                x += icon.Width + 5;
            }

            string title = isAdditional ? "에디셔널 잠재능력" : "잠재능력";
            DrawText(g, $"{title} : {grade}", MapleGearGraphics.EquipMDMoris9Font, GetGradeColor(grade), x, picH, TextAlignment.Left);
            picH += 18;

            foreach (var opt in options)
            {
                if (string.IsNullOrEmpty(opt)) continue;
                // Blob(점) 대신 텍스트로
                DrawText(g, "· " + opt, MapleGearGraphics.EquipMDMoris9Font, Color.White, 15, picH, TextAlignment.Left);
                picH += 16;
            }
        }

        private static void DrawFooter(Graphics g, ItemEquipmentInfo item, ref int picH)
        {
            string desc = item.ItemDescription ?? "";
            DrawText(g, desc, MapleGearGraphics.EquipMDMoris9Font, Color.White, 15, picH, TextAlignment.Left);
            picH += 16;
        }

        // Helpers
        private static void DrawText(Graphics g, string text, Font font, Color color, int x, int y, TextAlignment align)
        {
            TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
            Size size = TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue), flags);
            Point p = new Point(x, y);
            if (align == TextAlignment.Center) p.X -= size.Width / 2;
            else if (align == TextAlignment.Right) p.X -= size.Width;
            
            TextRenderer.DrawText(g, text, font, p, color, flags);
        }

        private static void DrawCenteredText(Graphics g, string text, Font font, Color color, int cx, int y)
        {
            DrawText(g, text, font, color, cx, y, TextAlignment.Center);
        }

        private static int ParseInt(string? val) => int.TryParse(val, out var i) ? i : 0;

        private static Color GetGradeColor(string? grade)
        {
            return grade switch
            {
                "레전드리" => MapleGearGraphics.Equip22Legendary,
                "유니크" => MapleGearGraphics.Equip22Emphasis,
                "에픽" => MapleGearGraphics.Equip22Epic,
                "레어" => MapleGearGraphics.Equip22Rare,
                _ => Color.White
            };
        }
    }
}