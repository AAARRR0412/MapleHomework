using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace MapleHomework.CharaSimResource
{
    /// <summary>
    /// WzComparerR2의 CharaSimResource를 대체하는 리소스 로더
    /// PNG 파일들을 임베디드 리소스로 로드합니다.
    /// </summary>
    public static class Resource
    {
        private static readonly Dictionary<string, Bitmap> _cache = new Dictionary<string, Bitmap>();
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private static readonly string _resourcePrefix = "MapleHomework.CharaSimResource.Resources.";

        #region Tooltip Frame Resources (UIToolTip_img_Item_Frame2)
        public static Bitmap UIToolTip_img_Item_Frame2_n => GetResource("UIToolTip.img.Item.Frame2.n");
        public static Bitmap UIToolTip_img_Item_Frame2_ne => GetResource("UIToolTip.img.Item.Frame2.ne");
        public static Bitmap UIToolTip_img_Item_Frame2_e => GetResource("UIToolTip.img.Item.Frame2.e");
        public static Bitmap UIToolTip_img_Item_Frame2_se => GetResource("UIToolTip.img.Item.Frame2.se");
        public static Bitmap UIToolTip_img_Item_Frame2_s => GetResource("UIToolTip.img.Item.Frame2.s");
        public static Bitmap UIToolTip_img_Item_Frame2_sw => GetResource("UIToolTip.img.Item.Frame2.sw");
        public static Bitmap UIToolTip_img_Item_Frame2_w => GetResource("UIToolTip.img.Item.Frame2.w");
        public static Bitmap UIToolTip_img_Item_Frame2_nw => GetResource("UIToolTip.img.Item.Frame2.nw");
        public static Bitmap UIToolTip_img_Item_Frame2_c => GetResource("UIToolTip.img.Item.Frame2.c");
        public static Bitmap UIToolTip_img_Item_Frame2_cover => GetResource("UIToolTip.img.Item.Frame2.cover");
        #endregion

        #region New Tooltip Frame Resources (UIToolTipNew_img_Item_Common_frame_flexible)
        public static Bitmap UIToolTipNew_img_Item_Common_frame_flexible_n => GetResource("UIToolTipNew.img.Item.Common.frame.flexible.n");
        public static Bitmap UIToolTipNew_img_Item_Common_frame_flexible_ne => GetResource("UIToolTipNew.img.Item.Common.frame.flexible.ne");
        public static Bitmap UIToolTipNew_img_Item_Common_frame_flexible_e => GetResource("UIToolTipNew.img.Item.Common.frame.flexible.e");
        public static Bitmap UIToolTipNew_img_Item_Common_frame_flexible_se => GetResource("UIToolTipNew.img.Item.Common.frame.flexible.se");
        public static Bitmap UIToolTipNew_img_Item_Common_frame_flexible_s => GetResource("UIToolTipNew.img.Item.Common.frame.flexible.s");
        public static Bitmap UIToolTipNew_img_Item_Common_frame_flexible_sw => GetResource("UIToolTipNew.img.Item.Common.frame.flexible.sw");
        public static Bitmap UIToolTipNew_img_Item_Common_frame_flexible_w => GetResource("UIToolTipNew.img.Item.Common.frame.flexible.w");
        public static Bitmap UIToolTipNew_img_Item_Common_frame_flexible_nw => GetResource("UIToolTipNew.img.Item.Common.frame.flexible.nw");
        public static Bitmap UIToolTipNew_img_Item_Common_frame_flexible_c => GetResource("UIToolTipNew.img.Item.Common.frame.flexible.c");
        public static Bitmap UIToolTipNew_img_Item_Common_frame_flexible_dotline => GetResource("UIToolTipNew.img.Item.Common.frame.flexible.dotline");
        #endregion

        #region Fixed Frame Resources
        public static Bitmap UIToolTipNew_img_Item_Common_frame_fixed_top => GetResource("UIToolTipNew.img.Item.Common.frame.fixed.top");
        public static Bitmap UIToolTipNew_img_Item_Common_frame_fixed_mid => GetResource("UIToolTipNew.img.Item.Common.frame.fixed.mid");
        public static Bitmap UIToolTipNew_img_Item_Common_frame_fixed_line => GetResource("UIToolTipNew.img.Item.Common.frame.fixed.line");
        public static Bitmap UIToolTipNew_img_Item_Common_frame_fixed_btm => GetResource("UIToolTipNew.img.Item.Common.frame.fixed.btm");
        #endregion

        #region Item Icon Resources
        public static Bitmap UIToolTipNew_img_Item_Common_ItemIcon_base => GetResource("UIToolTipNew.img.Item.Common.ItemIcon.base");
        public static Bitmap UIToolTipNew_img_Item_Common_ItemIcon_base_custom => GetResource("UIToolTipNew.img.Item.Common.ItemIcon.base.custom");
        public static Bitmap UIToolTipNew_img_Item_Common_ItemIcon_shade => GetResource("UIToolTipNew.img.Item.Common.ItemIcon.shade");
        #endregion

        #region Category Frame Resources
        public static Bitmap UIToolTipNew_img_Item_Equip_frame_common_category_w => GetResource("UIToolTipNew.img.Item.Equip.frame.common.category.w");
        public static Bitmap UIToolTipNew_img_Item_Equip_frame_common_category_c => GetResource("UIToolTipNew.img.Item.Equip.frame.common.category.c");
        public static Bitmap UIToolTipNew_img_Item_Equip_frame_common_category_e => GetResource("UIToolTipNew.img.Item.Equip.frame.common.category.e");
        public static Bitmap UIToolTipNew_img_Item_Equip_frame_common_box => GetResource("UIToolTipNew.img.Item.Equip.frame.common.box");
        #endregion

        #region Potential Grade Icons
        public static Bitmap UIToolTipNew_img_Item_Equip_textIcon_potential_title_normal => GetResource("UIToolTipNew.img.Item.Equip.textIcon.potential.title.normal");
        public static Bitmap UIToolTipNew_img_Item_Equip_textIcon_potential_title_rare => GetResource("UIToolTipNew.img.Item.Equip.textIcon.potential.title.rare");
        public static Bitmap UIToolTipNew_img_Item_Equip_textIcon_potential_title_epic => GetResource("UIToolTipNew.img.Item.Equip.textIcon.potential.title.epic");
        public static Bitmap UIToolTipNew_img_Item_Equip_textIcon_potential_title_unique => GetResource("UIToolTipNew.img.Item.Equip.textIcon.potential.title.unique");
        public static Bitmap UIToolTipNew_img_Item_Equip_textIcon_potential_title_legendary => GetResource("UIToolTipNew.img.Item.Equip.textIcon.potential.title.legendary");
        
        public static Bitmap UIToolTipNew_img_Item_Equip_textIcon_potential_detail_rare => GetResource("UIToolTipNew.img.Item.Equip.textIcon.potential.detail.rare");
        public static Bitmap UIToolTipNew_img_Item_Equip_textIcon_potential_detail_epic => GetResource("UIToolTipNew.img.Item.Equip.textIcon.potential.detail.epic");
        public static Bitmap UIToolTipNew_img_Item_Equip_textIcon_potential_detail_unique => GetResource("UIToolTipNew.img.Item.Equip.textIcon.potential.detail.unique");
        public static Bitmap UIToolTipNew_img_Item_Equip_textIcon_potential_detail_legendary => GetResource("UIToolTipNew.img.Item.Equip.textIcon.potential.detail.legendary");
        
        public static Bitmap UIToolTipNew_img_Item_Equip_textIcon_additionalPotential_normal => GetResource("UIToolTipNew.img.Item.Equip.textIcon.additionalPotential.normal");
        public static Bitmap UIToolTipNew_img_Item_Equip_textIcon_soulWeapon_normal => GetResource("UIToolTipNew.img.Item.Equip.textIcon.soulWeapon.normal");
        public static Bitmap UIToolTipNew_img_Item_Equip_textIcon_set_guide => GetResource("UIToolTipNew.img.Item.Equip.textIcon.set.guide");
        #endregion

        #region Star Resources
        public static Bitmap UIToolTipNew_img_Item_Equip_Star_Star0_0 => GetResource("UIToolTipNew.img.Item.Equip.Star.Star0.0");
        public static Bitmap UIToolTipNew_img_Item_Equip_Star_Star0_1 => GetResource("UIToolTipNew.img.Item.Equip.Star.Star0.1");
        public static Bitmap UIToolTipNew_img_Item_Equip_Star_Star1_0 => GetResource("UIToolTipNew.img.Item.Equip.Star.Star1.0");
        public static Bitmap UIToolTipNew_img_Item_Equip_Star_Star1_1 => GetResource("UIToolTipNew.img.Item.Equip.Star.Star1.1");
        public static Bitmap UIToolTipNew_img_Item_Equip_Star_Star2_0 => GetResource("UIToolTipNew.img.Item.Equip.Star.Star2.0");
        public static Bitmap UIToolTipNew_img_Item_Equip_Star_Star2_1 => GetResource("UIToolTipNew.img.Item.Equip.Star.Star2.1");
        #endregion

        #region Attack Power Font Resources
        public static Bitmap UIToolTipNew_img_Item_Equip_imgFont_atkPow_plus_0 => GetResource("UIToolTipNew.img.Item.Equip.imgFont.atkPow.plus.0");
        public static Bitmap UIToolTipNew_img_Item_Equip_imgFont_atkPow_plus_1 => GetResource("UIToolTipNew.img.Item.Equip.imgFont.atkPow.plus.1");
        public static Bitmap UIToolTipNew_img_Item_Equip_imgFont_atkPow_plus_2 => GetResource("UIToolTipNew.img.Item.Equip.imgFont.atkPow.plus.2");
        public static Bitmap UIToolTipNew_img_Item_Equip_imgFont_atkPow_plus_3 => GetResource("UIToolTipNew.img.Item.Equip.imgFont.atkPow.plus.3");
        public static Bitmap UIToolTipNew_img_Item_Equip_imgFont_atkPow_plus_4 => GetResource("UIToolTipNew.img.Item.Equip.imgFont.atkPow.plus.4");
        public static Bitmap UIToolTipNew_img_Item_Equip_imgFont_atkPow_plus_5 => GetResource("UIToolTipNew.img.Item.Equip.imgFont.atkPow.plus.5");
        public static Bitmap UIToolTipNew_img_Item_Equip_imgFont_atkPow_plus_6 => GetResource("UIToolTipNew.img.Item.Equip.imgFont.atkPow.plus.6");
        public static Bitmap UIToolTipNew_img_Item_Equip_imgFont_atkPow_plus_7 => GetResource("UIToolTipNew.img.Item.Equip.imgFont.atkPow.plus.7");
        public static Bitmap UIToolTipNew_img_Item_Equip_imgFont_atkPow_plus_8 => GetResource("UIToolTipNew.img.Item.Equip.imgFont.atkPow.plus.8");
        public static Bitmap UIToolTipNew_img_Item_Equip_imgFont_atkPow_plus_9 => GetResource("UIToolTipNew.img.Item.Equip.imgFont.atkPow.plus.9");
        #endregion

        #region Cash Item Labels
        public static Bitmap CashItem_0 => GetResource("CashItem.0");
        public static Bitmap CashShop_img_CashItem_label_0 => GetResource("CashShop.img.CashItem_label.0");
        public static Bitmap CashShop_img_CashItem_label_1 => GetResource("CashShop.img.CashItem_label.1");
        public static Bitmap CashShop_img_CashItem_label_2 => GetResource("CashShop.img.CashItem_label.2");
        public static Bitmap CashShop_img_CashItem_label_3 => GetResource("CashShop.img.CashItem_label.3");
        #endregion

        #region Old Tooltip Frame Resources
        public static Bitmap UIToolTip_img_Item_Frame_top => GetResource("UIToolTip.img.Item.Frame.top");
        public static Bitmap UIToolTip_img_Item_Frame_line => GetResource("UIToolTip.img.Item.Frame.line");
        public static Bitmap UIToolTip_img_Item_Frame_dotline => GetResource("UIToolTip.img.Item.Frame.dotline");
        public static Bitmap UIToolTip_img_Item_Frame_bottom => GetResource("UIToolTip.img.Item.Frame.bottom");
        public static Bitmap UIToolTip_img_Item_Frame_cover => GetResource("UIToolTip.img.Item.Frame.cover");
        #endregion

        #region Font Resources
        public static byte[] NanumGothicExtraBold => GetResourceBytes("NanumGothicExtraBold.ttf");
        #endregion

        /// <summary>
        /// 리소스 이름으로 Bitmap을 로드합니다.
        /// </summary>
        private static Bitmap GetResource(string name)
        {
            if (_cache.TryGetValue(name, out var cached))
                return cached;

            var resourceName = _resourcePrefix + name.Replace(".", "_") + ".png";
            
            // 대체 리소스 이름 시도
            var alternativeNames = new[]
            {
                _resourcePrefix + name + ".png",
                _resourcePrefix + name.Replace(".", "_") + ".png",
            };

            foreach (var resName in alternativeNames)
            {
                try
                {
                    using var stream = _assembly.GetManifestResourceStream(resName);
                    if (stream != null)
                    {
                        var bitmap = new Bitmap(stream);
                        _cache[name] = bitmap;
                        return bitmap;
                    }
                }
                catch { }
            }

            // 리소스를 찾지 못한 경우 빈 비트맵 반환
            var emptyBitmap = new Bitmap(1, 1);
            _cache[name] = emptyBitmap;
            return emptyBitmap;
        }

        /// <summary>
        /// 리소스 바이트 배열을 로드합니다.
        /// </summary>
        private static byte[] GetResourceBytes(string name)
        {
            var resourceName = _resourcePrefix + name;
            try
            {
                using var stream = _assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
            catch { }
            return Array.Empty<byte>();
        }

        /// <summary>
        /// 캐시된 모든 리소스를 해제합니다.
        /// </summary>
        public static void Dispose()
        {
            foreach (var bitmap in _cache.Values)
            {
                bitmap?.Dispose();
            }
            _cache.Clear();
        }
    }
}

