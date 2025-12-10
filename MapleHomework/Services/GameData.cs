using System.Collections.Generic;
using MapleHomework.Models;

namespace MapleHomework.Services
{
    public static class GameData
    {
        // 일일 퀘스트 목록 (RequiredLevel = 해당 지역 최소 레벨)
        public static List<HomeworkTask> Dailies = new()
        {
            new() { Name = "몬스터파크", Category = TaskCategory.Daily, RequiredLevel = 0 },
            new() { Name = "소멸의 여로", Category = TaskCategory.Daily, RequiredLevel = 200 },
            new() { Name = "츄츄 아일랜드", Category = TaskCategory.Daily, RequiredLevel = 210 },
            new() { Name = "레헬른", Category = TaskCategory.Daily, RequiredLevel = 220 },
            new() { Name = "아르카나", Category = TaskCategory.Daily, RequiredLevel = 225 },
            new() { Name = "모라스", Category = TaskCategory.Daily, RequiredLevel = 230 },
            new() { Name = "에스페라", Category = TaskCategory.Daily, RequiredLevel = 235 },
            new() { Name = "문브릿지", Category = TaskCategory.Daily, RequiredLevel = 245 },
            new() { Name = "고통의 미궁", Category = TaskCategory.Daily, RequiredLevel = 250 },
            new() { Name = "리멘", Category = TaskCategory.Daily, RequiredLevel = 255 },
            new() { Name = "세르니움", Category = TaskCategory.Daily, RequiredLevel = 260 },
            new() { Name = "호텔 아르크스", Category = TaskCategory.Daily, RequiredLevel = 265 },
            new() { Name = "오디움", Category = TaskCategory.Daily, RequiredLevel = 270 },
            new() { Name = "도원경", Category = TaskCategory.Daily, RequiredLevel = 275 },
            new() { Name = "아르테리아", Category = TaskCategory.Daily, RequiredLevel = 280 },
            new() { Name = "카르시온", Category = TaskCategory.Daily, RequiredLevel = 285 },
            new() { Name = "탈라하트", Category = TaskCategory.Daily, RequiredLevel = 290 },
            new() { Name = "데일리 기프트", Category = TaskCategory.Daily, RequiredLevel = 0 }
        };

        // 주간 퀘스트 목록
        public static List<HomeworkTask> Weeklies = new()
        {
            new() { Name = "몬스터파크 익스트림", Category = TaskCategory.Weekly },
            new() { Name = "아즈모스 협곡", Category = TaskCategory.Weekly },
            new() { Name = "에픽 던전 : 하이마운틴", Category = TaskCategory.Weekly },
            new() { Name = "에픽 던전 : 앵글러 컴퍼니", Category = TaskCategory.Weekly },
            new() { Name = "에픽 던전 : 악몽 선경", Category = TaskCategory.Weekly },
            new() { Name = "무릉도장", Category = TaskCategory.Weekly },
            new() { Name = "길드 지하수로", Category = TaskCategory.Weekly },
            new() { Name = "길드 플래그 레이스", Category = TaskCategory.Weekly },
            new() { Name = "주간 퀘스트 (여로~에스페라)", Category = TaskCategory.Weekly },
            new() { Name = "크리티아스", Category = TaskCategory.Weekly },
            new() { Name = "타락한 세계수", Category = TaskCategory.Weekly },
            new() { Name = "헤이븐", Category = TaskCategory.Weekly },
            new() { Name = "유니온 주간 드래곤", Category = TaskCategory.Weekly }
        };

        // 보스 목록 - 최대 12개만 활성화 가능, 각 보스별 선택 가능한 난이도 지정
        public static List<HomeworkTask> Bosses = new()
        {
            // 기본 활성화 (12개)
            new() { Name = "자쿰", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Chaos, IsActive = true,
                    AvailableDifficulties = new() { BossDifficulty.Chaos } },
            new() { Name = "매그너스", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Hard, IsActive = true,
                    AvailableDifficulties = new() { BossDifficulty.Hard } },
            new() { Name = "힐라", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Hard, IsActive = true,
                    AvailableDifficulties = new() { BossDifficulty.Hard } },
            new() { Name = "파풀라투스", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Chaos, IsActive = true,
                    AvailableDifficulties = new() { BossDifficulty.Chaos } },
            new() { Name = "피에르", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Chaos, IsActive = true,
                    AvailableDifficulties = new() { BossDifficulty.Chaos } },
            new() { Name = "반반", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Chaos, IsActive = true,
                    AvailableDifficulties = new() { BossDifficulty.Chaos } },
            new() { Name = "블러디퀸", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Chaos, IsActive = true,
                    AvailableDifficulties = new() { BossDifficulty.Chaos } },
            new() { Name = "벨룸", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Chaos, IsActive = true,
                    AvailableDifficulties = new() { BossDifficulty.Chaos } },
            new() { Name = "핑크빈", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Chaos, IsActive = true,
                    AvailableDifficulties = new() { BossDifficulty.Chaos } },
            new() { Name = "시그너스", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Normal, IsActive = true,
                    AvailableDifficulties = new() { BossDifficulty.Easy, BossDifficulty.Normal } },
            new() { Name = "스우", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Normal, IsActive = true,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Hard, BossDifficulty.Extreme } },
            new() { Name = "데미안", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Normal, IsActive = true,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Hard } },

            // 기본 비활성화 (사용자 선택 시 활성화)
            new() { Name = "가디언 엔젤 슬라임", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Normal, IsActive = false,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Chaos } },
            new() { Name = "루시드", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Normal, IsActive = false,
                    AvailableDifficulties = new() { BossDifficulty.Easy, BossDifficulty.Normal, BossDifficulty.Hard } },
            new() { Name = "윌", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Normal, IsActive = false,
                    AvailableDifficulties = new() { BossDifficulty.Easy, BossDifficulty.Normal, BossDifficulty.Hard } },
            new() { Name = "더스크", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Normal, IsActive = false,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Chaos } },
            new() { Name = "진 힐라", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Normal, IsActive = false,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Hard } },
            new() { Name = "듄켈", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Normal, IsActive = false,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Hard } },
            new() { Name = "선택받은 세렌", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Normal, IsActive = false,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Hard, BossDifficulty.Extreme } },
            new() { Name = "감시자 칼로스", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Normal, IsActive = false,
                    AvailableDifficulties = new() { BossDifficulty.Easy, BossDifficulty.Normal, BossDifficulty.Chaos, BossDifficulty.Extreme } },
            new() { Name = "최초의 대적자", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Normal, IsActive = false,
                    AvailableDifficulties = new() { BossDifficulty.Easy, BossDifficulty.Normal, BossDifficulty.Hard, BossDifficulty.Extreme } },
            new() { Name = "카링", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Normal, IsActive = false,
                    AvailableDifficulties = new() { BossDifficulty.Easy, BossDifficulty.Normal, BossDifficulty.Hard, BossDifficulty.Extreme } },
            new() { Name = "림보", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Normal, IsActive = false,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Hard } },
            new() { Name = "발드릭스", Category = TaskCategory.Boss, Difficulty = BossDifficulty.Normal, IsActive = false,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Hard } }
        };

        // 월간 보스
        public static List<HomeworkTask> Monthlies = new()
        {
            new() { Name = "검은 마법사", Category = TaskCategory.Monthly, Difficulty = BossDifficulty.Hard,
                    AvailableDifficulties = new() { BossDifficulty.Hard, BossDifficulty.Extreme } }
        };
    }
}
