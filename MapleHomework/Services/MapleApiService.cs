using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MapleHomework.Models;

namespace MapleHomework.Services
{
    public class MapleApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://open.api.nexon.com/maplestory/v1";

        public MapleApiService()
        {
            _httpClient = new HttpClient();
        }

        // 1. 닉네임으로 OCID 조회
        public async Task<string?> GetOcidAsync(string apiKey, string characterName)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/id?character_name={characterName}");
                request.Headers.Add("x-nxopen-api-key", apiKey);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<OcidResponse>();
                return result?.Ocid;
            }
            catch
            {
                // 실제 배포 시엔 로깅 필요 (Debug.WriteLine 등)
                return null;
            }
        }

        // 2. OCID로 캐릭터 정보 조회 (날짜는 어제 날짜 기준 - API 데이터 갱신 시점 고려)
        public async Task<CharacterBasicResponse?> GetCharacterInfoAsync(string apiKey, string ocid)
        {
            try
            {
                // 조회 기준일: 어제 (넥슨 API는 실시간 데이터가 아닐 수 있음)
                string date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/character/basic?ocid={ocid}&date={date}");
                request.Headers.Add("x-nxopen-api-key", apiKey);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<CharacterBasicResponse>();
            }
            catch
            {
                return null;
            }
        }

        // 3. OCID로 심볼 장비 정보 조회
        public async Task<SymbolEquipmentResponse?> GetSymbolEquipmentAsync(string apiKey, string ocid)
        {
            try
            {
                string date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/character/symbol-equipment?ocid={ocid}&date={date}");
                request.Headers.Add("x-nxopen-api-key", apiKey);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<SymbolEquipmentResponse>();
            }
            catch
            {
                return null;
            }
        }

        // 4. 유니온 챔피언 정보 조회
        public async Task<UnionChampionResponse?> GetUnionChampionAsync(string apiKey, string ocid)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/user/union-champion?ocid={ocid}");
                request.Headers.Add("x-nxopen-api-key", apiKey);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<UnionChampionResponse>();
            }
            catch
            {
                return null;
            }
        }
    }
}
