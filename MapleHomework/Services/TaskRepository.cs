using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using MapleHomework.Models;

namespace MapleHomework.Services
{
    // 파일에 저장될 데이터 구조
    public class TaskData
    {
        public List<HomeworkTask> DailyTasks { get; set; } = new();
        public List<HomeworkTask> WeeklyTasks { get; set; } = new();
    }

    public static class TaskRepository
    {
        private const string FilePath = "homework_data.json";

        // 데이터 저장
        public static void Save(ObservableCollection<HomeworkTask> daily, ObservableCollection<HomeworkTask> weekly)
        {
            try
            {
                var data = new TaskData
                {
                    DailyTasks = new List<HomeworkTask>(daily),
                    WeeklyTasks = new List<HomeworkTask>(weekly)
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // 저장 실패 시 조용히 넘어감 (실무에선 로깅 필요)
            }
        }

        // 데이터 불러오기
        public static TaskData? Load()
        {
            if (!File.Exists(FilePath)) return null;

            try
            {
                string json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<TaskData>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}