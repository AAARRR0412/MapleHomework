using System;
using System.Collections.Generic;
using System.Drawing;

namespace MapleHomework.Rendering.Models
{
    /// <summary>
    /// 스킬 툴팁 렌더링을 위한 데이터 모델
    /// API: /maplestory/v1/character/skill 응답 구조에 맞춤
    /// </summary>
    public class SkillTooltipData
    {
        // 기본 정보
        public int SkillID { get; set; }
        public string Name { get; set; } = "";
        public string IconUrl { get; set; } = "";
        public Bitmap? IconBitmap { get; set; }

        // API 필드: skill_description - [마스터 레벨 : N] + 스킬 설명
        public string Description { get; set; } = "";

        // API 필드: skill_effect - 현재 레벨 효과
        public string SkillEffect { get; set; } = "";

        // API 필드: skill_effect_next - 다음 레벨 효과
        public string SkillEffectNext { get; set; } = "";

        // 레벨 정보
        public int Level { get; set; }
        public int MaxLevel { get; set; }
        public int ReqLevel { get; set; }

        // 6차 스킬 여부
        public bool IsOrigin { get; set; }
        public bool IsAscent { get; set; }

        // 속성
        public bool IsHyperStat { get; set; }
        public bool IsTimeLimited { get; set; }
        public bool IsPetAutoBuff { get; set; }
        public bool IsSequenceOn { get; set; }

        public List<string> ActionDelays { get; set; } = new List<string>();
        public List<string> PropertyDescriptions { get; set; } = new List<string>();
        public Dictionary<int, int> ReqSkills { get; set; } = new Dictionary<int, int>();
        public Tuple<int, int>? RelationSkill { get; set; }
        public int AddAttackToolTipDescSkill { get; set; }
        public int AssistSkillLink { get; set; }

        public SkillTooltipData() { }
    }
}
