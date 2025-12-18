using MapleHomework.Models;
using MapleHomework.Data;

namespace MapleHomework.Services
{
    /// <summary>
    /// 보스별 메소 보상 데이터 (GameData에서 가져옴)
    /// </summary>
    public static class BossRewardData
    {
        /// <summary>
        /// 보스의 메소 보상을 가져옵니다.
        /// </summary>
        /// <param name="bossName">보스 이름</param>
        /// <param name="difficulty">난이도</param>
        /// <param name="partySize">파티원 수 (1~6)</param>
        /// <param name="isMonthly">월간 보스 여부</param>
        /// <returns>메소 보상 (파티원 수로 나눈 값)</returns>
        public static long GetReward(string bossName, BossDifficulty difficulty, int partySize, bool isMonthly = false)
        {
            return GameData.GetBossReward(bossName, difficulty, partySize, isMonthly);
        }

        /// <summary>
        /// 메소 값을 한국어 형식으로 포맷합니다.
        /// </summary>
        public static string FormatMeso(long meso)
        {
            if (meso >= 100_000_000)
            {
                double eok = meso / 100_000_000.0;
                return $"{eok:N1}억 메소";
            }
            else if (meso >= 10_000)
            {
                double man = meso / 10_000.0;
                return $"{man:N0}만 메소";
            }
            return $"{meso:N0} 메소";
        }

        /// <summary>
        /// 메소 값을 상세 형식으로 포맷합니다.
        /// </summary>
        public static string FormatMesoDetailed(long meso)
        {
            return $"{meso:N0} 메소";
        }
    }
}
