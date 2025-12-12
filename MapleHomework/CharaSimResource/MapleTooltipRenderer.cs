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
        private const int Padding = 15; // 좌우 여백

        // 리소스 캐시
        private static readonly Dictionary<string, Bitmap> _resourceCache = new Dictionary<string, Bitmap>();
        private static readonly Dictionary<string, Bitmap> _iconCache = new Dictionary<string, Bitmap>(); // 아이템 아이콘 캐시
        private static readonly Dictionary<string, TextureBrush> _flexBrushCache = new Dictionary<string, TextureBrush>(); // flexible frame
        private static readonly Dictionary<string, TextureBrush> _fixedBrushCache = new Dictionary<string, TextureBrush>(); // fixed frame
        private static readonly HttpClient _httpClient = new HttpClient();
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
                    _flexBrushCache[dir] = new TextureBrush(bmp, mode);
                }
            }

            // 고정 프레임 (top/mid/line/btm)
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
                    _fixedBrushCache[key] = new TextureBrush(bmp, mode);
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

        // 넥슨 API 아이콘 URL을 비트맵으로 로드 (동기 + 캐시)
        private static Bitmap? LoadIconFromUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            if (_iconCache.TryGetValue(url, out var cached)) return cached;

            try
            {
                var bytes = _httpClient.GetByteArrayAsync(url).GetAwaiter().GetResult();
                using var ms = new MemoryStream(bytes);
                using var temp = new Bitmap(ms);
                var bmp = new Bitmap(temp); // 스트림 종속성 제거용 복사본
                _iconCache[url] = bmp;
                return bmp;
            }
            catch
            {
                return null;
            }
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
                // 아이템 이름 색상: 요청에 따라 항상 흰색
                DrawCenteredText(g, name, MapleGearGraphics.ItemNameFont, Color.White, TooltipWidth / 2, picH);
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

            bool hasPotential = !string.IsNullOrEmpty(item.PotentialOptionGrade) && item.PotentialOptionGrade != "없음";
            bool hasAddPotential = !string.IsNullOrEmpty(item.AdditionalPotentialOptionGrade) && item.AdditionalPotentialOptionGrade != "없음";
            bool hasFooter = !string.IsNullOrEmpty(item.ItemDescription);

            // --- [Potential] ---
            if (hasPotential)
            {
                var grade = item.PotentialOptionGrade ?? string.Empty;
                if (!string.IsNullOrEmpty(grade))
                {
                    var opts = new[] { item.PotentialOption1, item.PotentialOption2, item.PotentialOption3 }
                        .Select(x => x ?? string.Empty).ToArray();
                    DrawPotential(g, grade, opts, false, ref picH);
                }
            }

            if (hasAddPotential)
            {
                if (hasPotential) { picH += 4; DrawSeparator(g, picH); picH += 8; }
                var grade = item.AdditionalPotentialOptionGrade ?? string.Empty;
                if (!string.IsNullOrEmpty(grade))
                {
                    var opts = new[] { item.AdditionalPotentialOption1, item.AdditionalPotentialOption2, item.AdditionalPotentialOption3 }
                        .Select(x => x ?? string.Empty).ToArray();
                    DrawPotential(g, grade, opts, true, ref picH);
                }
            }

            // --- [Footer] --- (설명 있을 때만)
            if (hasFooter)
            {
                picH += 4;
                DrawSeparator(g, picH);
                picH += 8;
                DrawFooter(g, item, ref picH);
            }

                picH += 10; // Bottom Padding

                // 2. 최종 비트맵 생성 (배경 합성 + 내용 복사)
                var finalBitmap = new Bitmap(TooltipWidth, picH);
                using (var fg = Graphics.FromImage(finalBitmap))
                {
                    // 고정 프레임 배경
                    DrawFixedTooltipBack(fg, 0, 0, TooltipWidth, picH);
                    
                    // 내용 복사
                    fg.DrawImage(contentBitmap, 0, 0, new Rectangle(0, 0, TooltipWidth, picH), GraphicsUnit.Pixel);
                }

                // 확대 없이 원본 크기 반환
                return finalBitmap;
            }
        }

        // 고정 프레임 배경 (UIToolTipNew.img.Item.Common.frame.fixed.*)
        private static void DrawFixedTooltipBack(Graphics g, int x, int y, int width, int height)
        {
            // 리소스가 없으면 폴백
            if (!_fixedBrushCache.ContainsKey("top") || !_fixedBrushCache.ContainsKey("mid") || !_fixedBrushCache.ContainsKey("btm"))
            {
                using (var b = new SolidBrush(Color.FromArgb(220, 0, 0, 0)))
                    g.FillRectangle(b, x, y, width, height);
                return;
            }

            var top = _fixedBrushCache["top"];
            var mid = _fixedBrushCache["mid"];
            var btm = _fixedBrushCache["btm"];

            int topH = top.Image.Height;
            int btmH = btm.Image.Height;

            // 상단
            g.DrawImage(top.Image, x, y, width, topH);

            // 중앙(타일)
            int midH = Math.Max(0, height - topH - btmH);
            if (midH > 0)
            {
                FillRect(g, mid, x, y + topH, width, midH);
            }

            // 하단
            g.DrawImage(btm.Image, x, y + height - btmH, width, btmH);
        }

        private static void FillRect(Graphics g, TextureBrush brush, int x, int y, int w, int h)
        {
            brush.ResetTransform();
            brush.TranslateTransform(x, y);
            g.FillRectangle(brush, x, y, w, h);
        }

        private static void DrawSeparator(Graphics g, int y)
        {
            var lineBrush = _fixedBrushCache.ContainsKey("line") ? _fixedBrushCache["line"] : null;
            if (lineBrush != null)
            {
                g.DrawImage(lineBrush.Image, 0, y, TooltipWidth, lineBrush.Image.Height);
            }
            else
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
            int iconY = picH + 10;

            // 아이콘 배경
            var iconBase = LoadResource("UIToolTipNew.img.Item.Common.ItemIcon.base.custom.png")
                           ?? LoadResource("UIToolTipNew.img.Item.Common.ItemIcon.base.png");
            int baseW = iconBase?.Width ?? 72;
            int baseH = iconBase?.Height ?? 72;
            if (iconBase != null) g.DrawImage(iconBase, iconX, iconY);

            // 실제 아이템 아이콘 (넥슨 API URL)
            var icon = LoadIconFromUrl(item.ItemIcon ?? item.ItemShapeIcon);
            if (icon != null)
            {
                // 원본 비율 유지, 베이스 박스 거의 가득(여백 6px)
                int margin = 6;
                double scale = Math.Min((baseW - margin * 2) / (double)icon.Width, (baseH - margin * 2) / (double)icon.Height);
                scale = Math.Max(1.0, scale); // 너무 작게 그려지는 경우를 방지 (최소 100%)

                int drawW = (int)Math.Round(icon.Width * scale);
                int drawH = (int)Math.Round(icon.Height * scale);
                int drawX = iconX + (baseW - drawW) / 2;
                int drawY = iconY + (baseH - drawH) / 2;

                var prevInterp = g.InterpolationMode;
                g.InterpolationMode = InterpolationMode.NearestNeighbor; // 픽셀 보존
                g.DrawImage(icon, drawX, drawY, drawW, drawH);
                g.InterpolationMode = prevInterp;
            }

            // 아이콘 커버
            var iconShade = LoadResource("UIToolTipNew.img.Item.Common.ItemIcon.shade.png");
            if (iconShade != null) g.DrawImage(iconShade, iconX, iconY);

            // 전투력 증가량 (우측 상단)
            int rightX = TooltipWidth - 15;
            int textY = iconY + 2;
            DrawText(g, "전투력 증가량", MapleGearGraphics.EquipMDMoris9Font, MapleGearGraphics.Equip22DarkGray, rightX, textY, TextAlignment.Right);
            
            // 숫자 (이미지 대신 텍스트로 대체)
            DrawText(g, "+0", MapleGearGraphics.ItemNameFont, MapleGearGraphics.Equip22Emphasis, rightX, textY + 20, TextAlignment.Right);

            // 카테고리 태그 (우측 하단) - 중복 제거 후 역순 배치
            var rawTags = new[] { "전사", item.ItemEquipmentSlot ?? "모자", item.ItemEquipmentPart ?? "방어구" };
            var tags = new List<string>();
            foreach (var t in rawTags)
            {
                if (string.IsNullOrWhiteSpace(t)) continue;
                if (!tags.Contains(t)) tags.Add(t);
            }
            int tagY = iconY + baseH - 20;
            int currentX = rightX;

            var cw = LoadResource("UIToolTipNew.img.Item.Equip.frame.common.category.w.png");
            var cc = LoadResource("UIToolTipNew.img.Item.Equip.frame.common.category.c.png");
            var ce = LoadResource("UIToolTipNew.img.Item.Equip.frame.common.category.e.png");

            if (cw != null && cc != null && ce != null)
            {
                foreach (var tag in tags)
                {
                    // 텍스트 폭/높이 계산 (패딩 없이)
                    var textSize = TextRenderer.MeasureText(g, tag, MapleGearGraphics.EquipMDMoris9Font,
                        new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                    int textW = textSize.Width;
                    int textH = textSize.Height;
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
                    int textYTag = tagY + Math.Max(0, (cc.Height - textH) / 2);
                    DrawText(g, tag, MapleGearGraphics.EquipMDMoris9Font, MapleGearGraphics.Equip22Gray, currentX + boxW/2, textYTag, TextAlignment.Center);

                    currentX -= 4; // 간격
                }
            }

            // 섹션 높이: 아이콘 박스 전체 + 여백
            picH = iconY + baseH + 10;
        }

        private static void DrawStats(Graphics g, ItemEquipmentInfo item, ref int picH)
        {
            if (item.ItemTotalOption == null || item.ItemBaseOption == null) return;

            int startX = 15;
            int valX = 85; // 스탯 수치 시작 X좌표 (라벨과 더 가깝게)

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
                int total = ParseInt(stat.Total);           // 총합 (API item_total_option)
                int baseVal = ParseInt(stat.Base);          // 기본 (API item_base_option)
                int addVal = ParseInt(stat.Add);            // 추가옵션 (API item_add_option)
                int scrollVal = ParseInt(stat.Etc);         // 주문서 (API item_etc_option)
                int starVal = ParseInt(stat.Star);          // 스타포스 (API item_starforce_option)
                int enchVal = scrollVal + starVal;          // 강화(주문서+스타포스)

                if (total == 0) continue;

                // 색상 정의 (요구사항)
                Color labelColor = Color.White; // 라벨도 흰색으로 표시
                // 총합은 항상 흰색으로 표시
                Color totalColor = Color.White;
                Color baseColor = Color.White;
                Color addColorStat = Color.FromArgb(0xCC, 0xFF, 0x00); // 연두색 (추옵)
                Color starColorStat = MapleGearGraphics.Equip22Emphasis; // 스타포스: 노란색 계열
                Color scrollColorStat = Color.FromArgb(0xAA, 0xAA, 0xFF); // 보라색 (주문서)

                // 라벨
                DrawText(g, stat.Label, MapleGearGraphics.EquipMDMoris9Font, labelColor, startX, picH, TextAlignment.Left);

                // 합계
                string totalStr = $"+{total}";
                DrawText(g, totalStr, MapleGearGraphics.EquipMDMoris9Font, totalColor, valX, picH, TextAlignment.Left);

                // 세부 수치가 모두 0이면 괄호 없이 종료 (추옵/주문서/스타포스 0)
                if (addVal == 0 && starVal == 0 && scrollVal == 0)
                {
                    picH += 16;
                    continue;
                }

                // 상세 (기본 +추옵 +주문서/스타포스)
                int currentX = valX + System.Windows.Forms.TextRenderer.MeasureText(g, totalStr, MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width + 4;
                DrawText(g, "(", MapleGearGraphics.EquipMDMoris9Font, Color.White, currentX, picH, TextAlignment.Left);
                currentX += 5;

                // 기본 (0도 표시)
                string baseStr = $"{baseVal}";
                DrawText(g, baseStr, MapleGearGraphics.EquipMDMoris9Font, baseColor, currentX, picH, TextAlignment.Left);
                currentX += System.Windows.Forms.TextRenderer.MeasureText(g, baseStr, MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width;

                // 스타포스
                if (starVal > 0)
                {
                    string starStr = $" +{starVal}";
                    DrawText(g, starStr, MapleGearGraphics.EquipMDMoris9Font, starColorStat, currentX, picH, TextAlignment.Left);
                    currentX += System.Windows.Forms.TextRenderer.MeasureText(g, starStr, MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width;
                }

                // 주문서
                if (scrollVal > 0)
                {
                    string scrollStr = $" +{scrollVal}";
                    DrawText(g, scrollStr, MapleGearGraphics.EquipMDMoris9Font, scrollColorStat, currentX, picH, TextAlignment.Left);
                    currentX += System.Windows.Forms.TextRenderer.MeasureText(g, scrollStr, MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width;
                }

                // 추옵
                if (addVal > 0)
                {
                    string addStr = $" +{addVal}";
                    DrawText(g, addStr, MapleGearGraphics.EquipMDMoris9Font, addColorStat, currentX, picH, TextAlignment.Left);
                    currentX += System.Windows.Forms.TextRenderer.MeasureText(g, addStr, MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width;
                }

                DrawText(g, ")", MapleGearGraphics.EquipMDMoris9Font, Color.White, currentX, picH, TextAlignment.Left);

                picH += 16;
            }

            // 2. 올스탯 (%)
            int allStat = ParseInt(item.ItemTotalOption.AllStat);
            if (allStat > 0)
            {
                int baseAll = ParseInt(item.ItemBaseOption.AllStat);
                int addAll = ParseInt(item.ItemAddOption?.AllStat);
                
                DrawText(g, "올스탯", MapleGearGraphics.EquipMDMoris9Font, MapleGearGraphics.Equip22Gray, startX, picH, TextAlignment.Left);
                DrawText(g, $"+{allStat}%", MapleGearGraphics.EquipMDMoris9Font, Color.White, valX, picH, TextAlignment.Left);

                int currentX = valX + System.Windows.Forms.TextRenderer.MeasureText(g, $"+{allStat}%", MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width + 4;
                DrawText(g, "(", MapleGearGraphics.EquipMDMoris9Font, Color.White, currentX, picH, TextAlignment.Left);
                currentX += 5;
                string baseStr = $"{baseAll}%";
                DrawText(g, baseStr, MapleGearGraphics.EquipMDMoris9Font, Color.White, currentX, picH, TextAlignment.Left);
                currentX += System.Windows.Forms.TextRenderer.MeasureText(g, baseStr, MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width;
                if (addAll > 0)
                {
                    string addStr = $" +{addAll}%";
                    DrawText(g, addStr, MapleGearGraphics.EquipMDMoris9Font, MapleGearGraphics.Equip22BonusStat, currentX, picH, TextAlignment.Left);
                    currentX += System.Windows.Forms.TextRenderer.MeasureText(g, addStr, MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width;
                }
                DrawText(g, ")", MapleGearGraphics.EquipMDMoris9Font, Color.White, currentX, picH, TextAlignment.Left);
                picH += 16;
            }

            // 3. 특수 옵션 순서: 데미지 → 보스 데미지 → 방무 (올스탯은 위에서 처리)
            Color addColor = MapleGearGraphics.Equip22BonusStat; // 연두색

            // 데미지
            int damage = ParseInt(item.ItemTotalOption.Damage);
            if (damage > 0)
            {
                int baseDmg = ParseInt(item.ItemBaseOption?.Damage);
                int addDmg = ParseInt(item.ItemAddOption?.Damage);
                DrawText(g, $"데미지 : +{damage}%", MapleGearGraphics.EquipMDMoris9Font, Color.White, startX, picH, TextAlignment.Left);

                int cx = startX + System.Windows.Forms.TextRenderer.MeasureText(g, $"데미지 : +{damage}%", MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width + 4;
                DrawText(g, "(", MapleGearGraphics.EquipMDMoris9Font, Color.White, cx, picH, TextAlignment.Left);
                cx += 5;
                string b = $"{baseDmg}%";
                DrawText(g, b, MapleGearGraphics.EquipMDMoris9Font, Color.White, cx, picH, TextAlignment.Left);
                cx += System.Windows.Forms.TextRenderer.MeasureText(g, b, MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width;
                if (addDmg > 0)
                {
                    string a = $" +{addDmg}%";
                    DrawText(g, a, MapleGearGraphics.EquipMDMoris9Font, addColor, cx, picH, TextAlignment.Left);
                    cx += System.Windows.Forms.TextRenderer.MeasureText(g, a, MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width;
                }
                DrawText(g, ")", MapleGearGraphics.EquipMDMoris9Font, Color.White, cx, picH, TextAlignment.Left);
                picH += 16;
            }

            // 보스 데미지
            int bossDmg = ParseInt(item.ItemTotalOption.BossDamage);
            if (bossDmg > 0)
            {
                int baseBoss = ParseInt(item.ItemBaseOption?.BossDamage);
                int addBoss = ParseInt(item.ItemAddOption?.BossDamage);
                DrawText(g, $"보스 몬스터 데미지 : +{bossDmg}%", MapleGearGraphics.EquipMDMoris9Font, Color.White, startX, picH, TextAlignment.Left);

                int cx = startX + System.Windows.Forms.TextRenderer.MeasureText(g, $"보스 몬스터 데미지 : +{bossDmg}%", MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width + 4;
                DrawText(g, "(", MapleGearGraphics.EquipMDMoris9Font, Color.White, cx, picH, TextAlignment.Left);
                cx += 5;
                string b = $"{baseBoss}%";
                DrawText(g, b, MapleGearGraphics.EquipMDMoris9Font, Color.White, cx, picH, TextAlignment.Left);
                cx += System.Windows.Forms.TextRenderer.MeasureText(g, b, MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width;
                if (addBoss > 0)
                {
                    string a = $" +{addBoss}%";
                    DrawText(g, a, MapleGearGraphics.EquipMDMoris9Font, addColor, cx, picH, TextAlignment.Left);
                    cx += System.Windows.Forms.TextRenderer.MeasureText(g, a, MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width;
                }
                DrawText(g, ")", MapleGearGraphics.EquipMDMoris9Font, Color.White, cx, picH, TextAlignment.Left);
                picH += 16;
            }

            // 방어율 무시
            int ignoreDef = ParseInt(item.ItemTotalOption.IgnoreMonsterArmor);
            if (ignoreDef > 0)
            {
                int baseIgnore = ParseInt(item.ItemBaseOption?.IgnoreMonsterArmor);
                int addIgnore = ParseInt(item.ItemAddOption?.IgnoreMonsterArmor);
                DrawText(g, $"몬스터 방어율 무시 : +{ignoreDef}%", MapleGearGraphics.EquipMDMoris9Font, Color.White, startX, picH, TextAlignment.Left);

                int cx = startX + System.Windows.Forms.TextRenderer.MeasureText(g, $"몬스터 방어율 무시 : +{ignoreDef}%", MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width + 4;
                DrawText(g, "(", MapleGearGraphics.EquipMDMoris9Font, Color.White, cx, picH, TextAlignment.Left);
                cx += 5;
                string b = $"{baseIgnore}%";
                DrawText(g, b, MapleGearGraphics.EquipMDMoris9Font, Color.White, cx, picH, TextAlignment.Left);
                cx += System.Windows.Forms.TextRenderer.MeasureText(g, b, MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width;
                if (addIgnore > 0)
                {
                    string a = $" +{addIgnore}%";
                    DrawText(g, a, MapleGearGraphics.EquipMDMoris9Font, addColor, cx, picH, TextAlignment.Left);
                    cx += System.Windows.Forms.TextRenderer.MeasureText(g, a, MapleGearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding).Width;
                }
                DrawText(g, ")", MapleGearGraphics.EquipMDMoris9Font, Color.White, cx, picH, TextAlignment.Left);
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
            if (string.IsNullOrWhiteSpace(desc)) return;

            int maxWidth = TooltipWidth - 30; // 좌우 15px 여백
            DrawWrappedText(g, desc, MapleGearGraphics.EquipMDMoris9Font, Color.White, 15, picH, maxWidth, 16, ref picH);
        }

        // 단어 단위 줄바꿈 렌더링
        private static void DrawWrappedText(Graphics g, string text, Font font, Color color, int x, int y, int maxWidth, int lineHeight, ref int picH)
        {
            if (string.IsNullOrEmpty(text)) return;

            var words = text.Split(' ');
            string line = "";
            foreach (var word in words)
            {
                string candidate = string.IsNullOrEmpty(line) ? word : line + " " + word;
                var size = TextRenderer.MeasureText(g, candidate, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                if (size.Width > maxWidth && !string.IsNullOrEmpty(line))
                {
                    DrawText(g, line, font, color, x, y, TextAlignment.Left);
                    y += lineHeight;
                    picH += lineHeight;
                    line = word;
                }
                else
                {
                    line = candidate;
                }
            }

            if (!string.IsNullOrEmpty(line))
            {
                DrawText(g, line, font, color, x, y, TextAlignment.Left);
                picH += lineHeight;
            }
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