using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace Rushitadel.Presentation
{
    [Serializable]
    public sealed class RunData
    {
        public int version = 2, slot, seed, theme, stage = 1, wave = 1, experience;
        public Rushitadel.Progression.CampaignState campaign = new();
        public string runId, savedUtc, checkpoint = "테마 확정 · 시작 지점";
        public float health = 100f, power = 100f, playerX, playerY = -1f, cameraY, distance, seconds, turretAngle;
        public bool recovering;
    }
    [Serializable] internal sealed class RunEnvelope { public string payload, checksum; }
    public static class RunStore
    {
#if UNITY_EDITOR
        public static string TestDirectory;
#endif
        public static string DirectoryPath =>
#if UNITY_EDITOR
            TestDirectory ??
#endif
            Path.Combine(Application.persistentDataPath, "Runs");
        public static string PathFor(int slot) => Path.Combine(DirectoryPath, "slot" + slot + ".json");
        private static string Hash(string value)
        {
            using var sha = SHA256.Create(); return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }
        public static bool Exists(int slot) => File.Exists(PathFor(slot));
        public static bool Validate(RunData data, int slot, out string error)
        {
            error = "저장 데이터의 필수 값이 올바르지 않습니다.";
            if (data == null) return false;
            if (data.version != 1 && data.version != 2) { error = "지원하지 않는 저장 버전입니다. 원본을 보존했습니다."; return false; }
            if (data.version == 1) { data.campaign = new(); data.version = 2; }
            if (data.slot != slot || slot < 1 || slot > 3 || !Guid.TryParse(data.runId, out _) || !DateTime.TryParse(data.savedUtc, out _) || data.theme < 0 || data.theme > 4 || data.stage < 1 || data.stage > 3 || data.wave < 1 || data.wave > 3 || data.experience < 0 || string.IsNullOrEmpty(data.checkpoint)) return false;
            var c = data.campaign;
            if (c == null || c.augments == null || c.augments.Length != 15 || c.web == null || c.web.Length != 6 || c.themeChoices == null || c.themeChoices.Length != 2 || c.spentXp < 0 || c.spentXp > data.experience || c.augmentsChosen < 0 || c.augmentsChosen > data.experience / 100 || c.defeatedBosses < 0 || c.defeatedBosses > 3 || c.usedThemes < 0 || c.usedThemes > 31 || !Enum.IsDefined(typeof(Rushitadel.Progression.RunPhase), c.phase)) return false;
            int ranks = 0; foreach (int rank in c.augments) { if (rank < 0 || rank > 10000) return false; ranks += rank; }
            if (ranks != c.augmentsChosen) return false;
            for (int i = 1; i <= 3; i++) if (c.web[i] && !c.web[i-1]) return false;
            foreach (int theme in c.themeChoices) if (theme < 0 || theme > 4) return false;
            foreach (float v in new[] {data.health, data.power, data.playerX, data.playerY, data.cameraY, data.distance, data.seconds, data.turretAngle}) if (!float.IsFinite(v)) return false;
            if (data.health <= 0 || data.health > (c.web[5] ? 125 : 100) || data.power < 0 || data.power > 100 || Math.Abs(data.playerX) > 5.4f || Math.Abs(data.playerY - data.cameraY) > 5f || data.distance < 0 || data.seconds < 0) return false;
            error = null; return true;
        }
        private static RunData Decode(string text, int slot)
        {
            var envelope = JsonUtility.FromJson<RunEnvelope>(text);
            if (envelope == null || envelope.payload == null || Hash(envelope.payload) != envelope.checksum) throw new InvalidDataException("저장 파일 검증 실패. 원본을 보존했습니다.");
            var data = JsonUtility.FromJson<RunData>(envelope.payload);
            if (!Validate(data, slot, out string error)) throw new InvalidDataException(error);
            return data;
        }
        public static bool Read(int slot, out RunData data, out string error)
        {
            data = null; error = null;
            if (!Exists(slot)) { error = "빈 슬롯 · 저장된 런이 없습니다."; return false; }
            try { data = Decode(File.ReadAllText(PathFor(slot)), slot); return true; }
            catch (Exception ex) { error = ex.Message; return false; }
        }
        public static bool Write(RunData data, out string error)
        {
            error = null;
            try
            {
                data.savedUtc = DateTime.UtcNow.ToString("O");
                if (!Validate(data, data.slot, out error)) return false;
                Directory.CreateDirectory(DirectoryPath);
                string path = PathFor(data.slot), temp = path + ".tmp", payload = JsonUtility.ToJson(data);
                File.WriteAllText(temp, JsonUtility.ToJson(new RunEnvelope {payload = payload, checksum = Hash(payload)}));
                Decode(File.ReadAllText(temp), data.slot);
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak"); else File.Move(temp, path);
                return true;
            }
            catch (Exception ex) { error = "저장 실패: " + ex.Message; return false; }
        }
        public static RunData Latest()
        {
            RunData latest = null;
            for (int i = 1; i <= 3; i++) if (Read(i, out var data, out _) && (latest == null || string.CompareOrdinal(data.savedUtc, latest.savedUtc) > 0)) latest = data;
            return latest;
        }
    }
}
