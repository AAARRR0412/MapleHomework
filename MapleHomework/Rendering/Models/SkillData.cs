using System;
using System.Collections.Generic;

namespace MapleHomework.Rendering.Models
{
    /// <summary>
    /// 스킬 데이터 모델 (Nexon API 응답 기반)
    /// </summary>
    public class SkillData
    {
        public string SkillName { get; set; } = "";
        public string SkillDescription { get; set; } = "";
        public string SkillEffect { get; set; } = "";
        public string SkillIcon { get; set; } = "";
        public int SkillLevel { get; set; }
        public int SkillEffectNext { get; set; }

        // 6차 스킬 관련
        public string HexaCoreType { get; set; } = "";
        public string HexaCoreName { get; set; } = "";
        public int HexaCoreLevel { get; set; }
        public List<string> LinkedSkills { get; set; } = new();
    }

    /// <summary>
    /// 헥사 코어 데이터
    /// </summary>
    public class HexaCoreData
    {
        public string HexaCoreName { get; set; } = "";
        public string HexaCoreType { get; set; } = "";
        public int HexaCoreLevel { get; set; }
        public List<HexaLinkedSkill> LinkedSkill { get; set; } = new();
    }

    public class HexaLinkedSkill
    {
        public string HexaSkillId { get; set; } = "";
    }
}

