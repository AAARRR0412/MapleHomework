using System;
using System.Collections.Generic;

namespace MapleHomework.Rendering.Models
{
    /// <summary>
    /// 소비/기타 아이템 데이터 모델
    /// </summary>
    public class ItemData
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public string ItemDescription { get; set; } = "";
        public string ItemIcon { get; set; } = "";
        public int ItemCount { get; set; }
        public string ItemType { get; set; } = "";

        // 사용 효과
        public string UseEffect { get; set; } = "";

        // 판매가
        public long SellPrice { get; set; }

        // 거래 가능 여부
        public bool IsTradeable { get; set; }

        // 기간제 여부
        public bool IsTimeLimited { get; set; }
        public DateTime? ExpireDate { get; set; }
    }

    /// <summary>
    /// 세트 아이템 데이터
    /// </summary>
    public class SetItemData
    {
        public string SetName { get; set; } = "";
        public int TotalSetCount { get; set; }
        public int EquippedSetCount { get; set; }
        public List<SetItemPart> SetParts { get; set; } = new();
        public List<SetItemEffect> SetEffects { get; set; } = new();
    }

    public class SetItemPart
    {
        public string ItemName { get; set; } = "";
        public string ItemSlot { get; set; } = "";
        public bool IsEquipped { get; set; }
    }

    public class SetItemEffect
    {
        public int RequiredCount { get; set; }
        public string EffectDescription { get; set; } = "";
        public bool IsActive { get; set; }
    }
}

