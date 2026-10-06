using System;
using UnityEngine;

namespace Rushitadel.Presentation
{
    [Serializable]
    public sealed class SettingsData
    {
        public int width = 1920, height = 1080, mode = 1, fps = 60;
        public bool vsync = true;
        public float master = 0.8f, music = 0.7f, effects = 0.8f;
        public SettingsData Copy() => JsonUtility.FromJson<SettingsData>(JsonUtility.ToJson(this));
        public bool Same(SettingsData other) => JsonUtility.ToJson(this) == JsonUtility.ToJson(other);
        public void Sanitize()
        {
            width = Mathf.Clamp(width, 640, 7680); height = Mathf.Clamp(height, 480, 4320);
            mode = Mathf.Clamp(mode, 0, 2);
            if (fps != 30 && fps != 60 && fps != 120 && fps != 144 && fps != -1) fps = 60;
            master = FiniteVolume(master); music = FiniteVolume(music); effects = FiniteVolume(effects);
        }
        private static float FiniteVolume(float value) => float.IsFinite(value) ? Mathf.Clamp01(value) : 0.8f;
    }
    public static class GameSettings
    {
        public static string StorageKey = "Rushitadel.Settings.v1";
        public static SettingsData Saved { get; private set; }
        public static SettingsData Live { get; private set; }
        public static event Action AudioChanged;
        public static void Load()
        {
            try { Saved = JsonUtility.FromJson<SettingsData>(PlayerPrefs.GetString(StorageKey, "")); } catch { Saved = null; }
            Saved ??= new SettingsData(); Saved.Sanitize(); Apply(Saved);
        }
        public static void Apply(SettingsData data)
        {
            Live = data.Copy(); Live.Sanitize();
            QualitySettings.vSyncCount = Live.vsync ? 1 : 0; Application.targetFrameRate = Live.fps;
            if (!Application.isEditor) Screen.SetResolution(Live.width, Live.height, Live.mode == 0 ? FullScreenMode.ExclusiveFullScreen : Live.mode == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            PreviewAudio(Live);
        }
        public static void PreviewAudio(SettingsData data)
        {
            Live = data.Copy(); AudioListener.volume = data.master; AudioChanged?.Invoke();
        }
        public static bool Save(SettingsData data, out string error)
        {
            error = null;
            try { data.Sanitize(); PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(data)); PlayerPrefs.Save(); Saved = data.Copy(); Apply(Saved); return true; }
            catch (Exception ex) { error = "설정 저장 실패: " + ex.Message; return false; }
        }
    }
}
