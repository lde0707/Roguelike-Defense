#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rushitadel.Combat;
using Rushitadel.Player;
using Rushitadel.Presentation;

namespace Rushitadel.Progression
{
    public sealed class CampaignProbe : MonoBehaviour
    {
        private string output;
        private static void Check(bool ok, string label) { if (!ok) throw new Exception("CAMPAIGN FAIL: " + label); Debug.Log("CAMPAIGN PASS: " + label); }
        private static void Click(string label) => FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == label && b.gameObject.activeInHierarchy && b.interactable).onClick.Invoke();
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            output = Path.GetFullPath(Path.Combine(Application.dataPath,"../../output/campaign-validation-20261006")); Directory.CreateDirectory(output);
            RunStore.TestDirectory = Path.Combine(output,"saves-" + DateTime.Now.ToString("HHmmss"));
            Application.logMessageReceived += (message, stack, type) => {
                if ((type == LogType.Error || type == LogType.Exception) && !message.StartsWith("Curl error")) EditorApplication.delayCall += () => EditorApplication.Exit(1);
            };
        }
        private IEnumerator Start()
        {
            yield return null;
            var initial = new RunData {slot=1,seed=789,theme=0,runId=Guid.NewGuid().ToString()};
            Check(RunStore.Write(initial, out _), "new v2 save");
            RunSession.Pending = initial; SceneManager.LoadScene("inGame"); yield return null; yield return null;
            var d = CampaignDirector.Instance;
            Check(d != null && d.Stage == 1 && d.Round == 1, "campaign starts S1 R1");
            var crystal = new GameObject("Test XP",typeof(CircleCollider2D),typeof(ExperiencePickup)).GetComponent<ExperiencePickup>();
            crystal.transform.position = PlayerHealth.Instance.transform.position; crystal.SetValue(100);
            Check(crystal.AttractionDelay >= 1 && crystal.AttractionDelay <= 2, "XP delay is 1-2 seconds");
            yield return new WaitForSecondsRealtime(.5f);
            Check(d.Experience.TotalExperience == 0, "XP cannot collect before delay");
            Capture("xp-drop.png");
            yield return new WaitForSecondsRealtime(2f);
            Check(d.Experience.Level == 2 && GameFrontend.Instance.CurrentPage == "augment" && Time.timeScale == 0, "one level opens frozen 3-choice UI");
            Capture("augment.png");
            var card = FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name.Contains("강화 포신")); card.onClick.Invoke();
            Click("선택 확정 Button"); yield return null;
            Check(CampaignDirector.Rank(1,0) == 1 && d.PendingAugments == 0, "augment applies once to selected T1");
            Check(!d.ChooseAugment(1,0), "duplicate augment rejected");
            d.Experience.AddExperience(250); yield return null;
            Check(d.PendingAugments == 2, "multi-level XP queues two choices");
            GameFrontend.Instance.ShowPause();
            typeof(GameFrontend).GetMethod("ResumeGame",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(GameFrontend.Instance,null);
            Check(GameFrontend.Instance.CurrentPage == "augment" && Time.timeScale == 0, "pause resumes pending augment without bypass");
            d.ChooseAugment(2,1); d.ReopenPage(); Check(d.PendingAugments == 1 && Time.timeScale == 0,"second choice remains paused");
            d.ChooseAugment(3,2); d.ReopenPage(); Check(d.PendingAugments == 0 && Time.timeScale == 1,"all queued choices resume combat");
            var targetA = new GameObject("Effect target A").AddComponent<EnemyController>(); targetA.transform.position = new Vector3(100,100,0); targetA.ConfigureRun(100,20,true);
            var targetB = new GameObject("Effect target B").AddComponent<EnemyController>(); targetB.transform.position = new Vector3(100.3f,100,0); targetB.ConfigureRun(100,20,true);
            var testShot = new GameObject("Modifier test").AddComponent<ProjectileController>(); testShot.transform.position = targetA.transform.position;
            testShot.Launch(Vector3.up,10,5); testShot.SetModifiers(1,true,true); testShot.Hit(targetA);
            Check(targetA.CurrentHealth == 90 && targetB.CurrentHealth == 95 && testShot.IsFlying,"pierce and splash affect real HP");
            testShot.Hit(targetA); Check(targetA.CurrentHealth == 90,"pierce cannot damage same target twice");
            testShot.Hit(targetB); Check(!testShot.IsFlying && targetB.CurrentHealth == 85,"pierce ends after extra target");
            yield return new WaitForSecondsRealtime(1.1f);
            Check(targetA.CurrentHealth <= 82 && targetB.CurrentHealth <= 77,"poison deals periodic real damage");
            Destroy(targetA.gameObject); Destroy(targetB.gameObject); yield return null;
            Capture("hud.png");
            bool reloadTested = false, bossTested = false;
            int maintenanceCount = 0;
            float deadline = Time.realtimeSinceStartup + 100;
            while (Time.realtimeSinceStartup < deadline)
            {
                d = CampaignDirector.Instance;
                if (d.State.phase == RunPhase.Complete) break;
                CheckAlive();
                if (GameFrontend.Instance.CurrentPage == "augment")
                {
                    d.ChooseAugment(1 + d.State.augmentsChosen % 5, d.State.augmentsChosen % 3); d.ReopenPage();
                }
                else if (d.State.phase == RunPhase.Maintenance)
                {
                    maintenanceCount++;
                    Check(Time.timeScale == 0 && !EnemyRegistry.ActiveEnemies.Any(), "safe maintenance " + maintenanceCount);
                    d.Recover(0); d.Recover(0); // Second call must not grant another recovery.
                    if (!reloadTested)
                    {
                        int level = d.Experience.Level, before = d.Wallet;
                        Check(!d.BuyWeb(2), "disconnected web node rejected");
                        Check(d.BuyWeb(0), "connected web purchase");
                        Check(d.Wallet == before-40 && d.Experience.Level == level && !d.BuyWeb(0), "XP spend preserves level and rejects duplicate");
                        Check(d.BuyWeb(4) && d.BuyWeb(5) && PlayerHealth.Instance.MaximumHealth == 125,"power/support web apply and HP cap increases");
                        Check(d.BuyWeb(1) && d.BuyWeb(2) && d.BuyWeb(3),"all linked web branches can be purchased");
                        GameFrontend.Instance.ShowMaintenance(); Capture("web.png");
                        Check(RunStore.Read(1,out var saved,out _), "maintenance save readable");
                        int rank = saved.campaign.augmentsChosen;
                        RunSession.Pending = saved; SceneManager.LoadScene("inGame"); yield return null; yield return null;
                        d = CampaignDirector.Instance;
                        Check(d.State.phase == RunPhase.Maintenance && d.State.recovered && d.State.web[0] && d.State.augmentsChosen == rank && Time.timeScale == 0, "maintenance restore preserves web/augments/recovery/pause");
                        reloadTested = true;
                    }
                    d.Depart();
                }
                else if (d.State.phase == RunPhase.Theme)
                {
                    int theme = d.State.themeChoices[0];
                    Check((d.State.usedThemes & (1<<theme)) == 0, "next theme excludes visited themes");
                    d.SelectTheme(theme);
                }
                else
                {
                    Time.timeScale = 6;
                    typeof(CampaignDirector).GetField("nextSpawn",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(d,0f);
                    if (d.State.phase == RunPhase.Boss && d.Boss != null && !bossTested)
                    {
                        Time.timeScale = 1; yield return null; Capture("boss.png");
                        float until = Time.realtimeSinceStartup + 8;
                        while (d.Boss != null && d.Boss.ShotsFired == 0 && Time.realtimeSinceStartup < until)
                        {
                            CheckAlive();
                            if (GameFrontend.Instance.CurrentPage == "augment") { d.ChooseAugment(2,0); d.ReopenPage(); }
                            yield return null;
                        }
                        Check(d.Boss != null && d.Boss.ShotsFired > 0, "boss actually fires projectiles"); bossTested = true;
                    }
                    foreach (var enemy in EnemyRegistry.ActiveEnemies.ToArray()) if (enemy != null) enemy.Eliminate();
                    CampaignDirector.ClearProjectiles();
                }
                yield return null;
            }
            d = CampaignDirector.Instance;
            Check(d.State.phase == RunPhase.Complete && d.Stage == 3 && d.Round == 3 && d.State.defeatedBosses == 3 && maintenanceCount == 9, "all 9 rounds and 3 bosses complete exactly once");
            Check(GameFrontend.Instance.CurrentPage == "result" && Time.timeScale == 0, "victory result freezes gameplay");
            Capture("victory.png");
            Check(RunStore.Read(1,out var completed,out _) && completed.campaign.phase == RunPhase.Complete, "completed run saved");
            Debug.Log("RUSHITADEL_CAMPAIGN_VALIDATION_SUCCESS"); EditorApplication.Exit(0);
        }
        private static void CheckAlive()
        {
            var h = PlayerHealth.Instance; Check(h.IsAlive,"player alive"); h.Heal(100);
            var pos = new Vector3(0,Camera.main.transform.position.y-1,0); h.transform.position = pos; h.GetComponent<Rigidbody2D>().position = pos;
        }
        private void Capture(string filename)
        {
            var canvas = GameFrontend.Instance.GetComponentInChildren<Canvas>(); var camera = Camera.main;
            var target = new RenderTexture(1920,1080,24); var oldTarget = camera.targetTexture; var old = RenderTexture.active;
            camera.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest {destination=target});
            RenderTexture.active = target; var tex = new Texture2D(1920,1080,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,1920,1080),0,0); tex.Apply();
            File.WriteAllBytes(Path.Combine(output,filename),tex.EncodeToPNG()); RenderTexture.active = old; camera.targetTexture = oldTarget; canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Destroy(tex); Destroy(target);
        }
    }
}
#endif
