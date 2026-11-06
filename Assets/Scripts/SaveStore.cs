using System;
using System.IO;
using UnityEngine;

namespace Frostline
{
    public sealed class SaveStore
    {
        public string Path { get; private set; }
        public string Notice { get; private set; }
        public SaveStore(string path) { Path = path; }
        public RescueSave Load()
        {
            Notice = "";
            if (!File.Exists(Path)) return new RescueSave();
            try
            {
                string json = File.ReadAllText(Path);
                if (!json.Contains("\"version\"") || !json.Contains("\"unlocked\"")) throw new InvalidDataException("Missing fields");
                var data = JsonUtility.FromJson<RescueSave>(json);
                if (data == null || data.version != 1 || data.best == null || data.best.Length != 4 || data.unlocked < 1 || data.unlocked > 4 || float.IsNaN(data.volume) || float.IsNaN(data.sensitivity)) throw new InvalidDataException("Invalid save");
                data.volume = Mathf.Clamp01(data.volume);
                data.sensitivity = Mathf.Clamp(data.sensitivity, 0.4f, 1.6f);
                data.quality = Mathf.Clamp(data.quality, 0, 2);
                return data;
            }
            catch (Exception exception)
            {
                Notice = "存档无法读取，已恢复默认进度。原文件保留为 .recovered。";
                Debug.LogWarning("SAVE_RECOVERY " + exception.GetType().Name);
                try { File.Copy(Path, Path + ".recovered", true); } catch (Exception) { }
                return new RescueSave();
            }
        }
        public bool Write(RescueSave data)
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                string temporary = Path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (File.Exists(Path)) File.Replace(temporary, Path, Path + ".backup");
                else File.Move(temporary, Path);
                Notice = ""; return true;
            }
            catch (Exception exception)
            {
                Notice = "本次进度暂未保存，当前任务仍可继续。";
                Debug.LogWarning("SAVE_WRITE_FAILED " + exception.GetType().Name); return false;
            }
        }
    }
}
