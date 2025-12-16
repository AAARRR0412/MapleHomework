using System.Collections.Generic;
using System.Threading.Tasks;
using MapleHomework.Models;

namespace MapleHomework.Services
{
    public interface IMapleApiService
    {
        Task<string?> GetOcidAsync(string characterName);

        Task<CharacterBasicResponse?> GetCharacterInfoAsync(string ocid, string? date = null);
        Task<ItemEquipmentResponse?> GetItemEquipmentAsync(string ocid, string? date = null);
        Task<SymbolEquipmentResponse?> GetSymbolEquipmentAsync(string ocid, string? date = null);
        Task<CharacterSkillResponse?> GetCharacterSkillAsync(string ocid, string? date = null, string grade = "6");
        Task<CharacterStatResponse?> GetCharacterStatAsync(string ocid, string? date = null);
        Task<UnionResponse?> GetUnionInfoAsync(string ocid, string? date = null);
        Task<UnionRaiderResponse?> GetUnionRaiderInfoAsync(string ocid, string? date = null);
        Task<UnionChampionResponse?> GetUnionChampionAsync(string ocid, string? date = null);
        Task<HexaMatrixStatResponse?> GetHexaMatrixStatAsync(string ocid, string? date = null);
        Task<HexaStatResponse?> GetHexaStatAsync(string ocid, string? date = null);
        Task<RingExchangeResponse?> GetRingExchangeAsync(string ocid, string? date = null);

        Task<BatchCollectResponse?> BatchCollectAsync(string ocid, List<string> dates, List<string>? types = null);
        Task<GrowthHistoryResult> CollectGrowthHistoryBatchAsync(string ocid, string characterId, string characterName, List<DateTime> targetDates, IProgress<int>? progress = null);
        Task<GrowthHistoryResult> CollectGrowthHistoryAsync(string ocid, string characterId, string characterName, int daysBack = 30, IProgress<int>? progress = null);

        string CachePath { get; }
        Task<CharacterCacheData?> LoadCharacterCache(string ocid);
        Task SaveCharacterCache(string ocid, CharacterCacheData data);
    }
}
