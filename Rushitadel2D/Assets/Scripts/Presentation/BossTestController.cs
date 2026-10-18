using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rushitadel.Progression;
using Rushitadel.Player;
using Rushitadel.Combat;

namespace Rushitadel.Presentation
{
    // The Test scene loads the real battlefield with a slot-zero, boss-only session.
    public sealed class BossTestController : MonoBehaviour
    {
        private static BossTestController instance;
        public static bool Active => instance != null;
        public static bool PanelVisible => Active && !instance.hidden;
        private static float PanelScale => Mathf.Max(.01f,Mathf.Min(Screen.height/1080f,(BattleSideSpace.LeftPixels-16)/324));
        public static bool Invulnerable => Active && instance.invulnerable;
        public static bool PointerOverPanel
        {
            get
            {
                if(!Active || UnityEngine.InputSystem.Mouse.current==null)return false;
                float scale=PanelScale;
                Vector2 p=UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                return new Rect(12,instance.hidden?960:120,300,instance.hidden?65:800).Contains(new Vector2(p.x/scale,(Screen.height-p.y)/scale));
            }
        }
        private readonly string[] names = { "풀 · 적월 나비", "불 · 용광로 거신", "얼음 · 빙환 수호자", "물/전기 · 쌍성" };
        private readonly string[] patterns = { "전체 순환", "01 기본 패턴", "02 기본 패턴", "03 기본 패턴", "04 기본 패턴", "05 기본 패턴", "06 애니메이션 연계", "07 강화 연계", "08 교차 탄막", "09 이동 관문", "10 박동 포대", "11 분신 레이저", "12 돌진 / 귀환", "13 이동 포대" };
        private int theme, stage = 1, phase, pattern;
        private bool invulnerable = true, towers, loading, paused, hidden;
        private float speed = 1;
        private Font font;
        public CampaignBoss Boss {get;private set;}
        public bool Ready => !loading && Boss != null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;
        private void Awake()
        {
            if(instance != null){Destroy(gameObject);return;}
            instance=this; DontDestroyOnLoad(gameObject);
            font=Resources.Load<Font>("Fonts/NeoDunggeunmo");
        }
        private void Start() => Restart();
        public void SelectBoss(int selectedTheme, int selectedStage=1, int selectedPhase=0, int selectedPattern=-1)
        {
            if(loading)return;
            theme=Mathf.Clamp(selectedTheme,0,3);stage=Mathf.Clamp(selectedStage,1,3);
            phase=Mathf.Clamp(selectedPhase,0,2);pattern=Mathf.Clamp(selectedPattern+1,0,13);Restart();
        }
        private void Restart(){if(!loading)StartCoroutine(LoadBattle());}
        private IEnumerator LoadBattle()
        {
            loading=true;paused=false;Time.timeScale=1;
            RunSession.Pending=new RunData{slot=0,seed=77,theme=theme,stage=stage,wave=3,
                runId=System.Guid.NewGuid().ToString(),campaign=new CampaignState{phase=RunPhase.Boss}};
            yield return SceneManager.LoadSceneAsync("inGame");
            // RunSession.Start creates the director and boss after sceneLoaded.
            yield return null;
            var director=CampaignDirector.Instance;
            if(director==null || director.Boss==null){Debug.LogError("보스 테스트 초기화 실패");loading=false;yield break;}
            director.enabled=false;Boss=director.Boss;
            Boss.PreviewPhase=phase;Boss.PreviewPattern=pattern-1;
            GameFrontend.Instance.ShowBattle();
            foreach(var tower in PlayerHealth.Instance.GetComponentsInChildren<TowerController>())tower.enabled=towers;
            loading=false;Time.timeScale=speed;
        }
        private void Update()
        {
            if(loading)return;
            var key=UnityEngine.InputSystem.Keyboard.current;
            if(key!=null && key.tabKey.wasPressedThisFrame)hidden=!hidden;
            if(key!=null && key.escapeKey.wasPressedThisFrame){paused=!paused;Time.timeScale=paused?0:speed;}
        }
        private void OnGUI()
        {
            var old=GUI.matrix;
            float scale=PanelScale;
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            var skinFont=GUI.skin.font;if(font!=null)GUI.skin.font=font;
            GUILayout.BeginArea(new Rect(12,hidden?960:120,300,hidden?65:800),GUI.skin.box);
            if(GUILayout.Button(hidden?"보스 테스트 펼치기 [Tab]":"보스 테스트 접기 [Tab]",GUILayout.Height(34)))hidden=!hidden;
            if(!hidden)
            {
                GUILayout.Label("저장 안 함 · 일반 웨이브 없음");
                GUI.enabled=!loading;
                for(int i=0;i<4;i++)if(GUILayout.Button((theme==i?"▶ ":"")+names[i],GUILayout.Height(32)))SelectBoss(i,stage,phase,pattern-1);
                GUILayout.Label("스테이지 난이도 / 보스 페이즈");
                int s=GUILayout.Toolbar(stage-1,new[]{"1단계","2단계","3단계"})+1;
                int p=GUILayout.Toolbar(phase,new[]{"1페이즈","2페이즈","3페이즈"});
                if(s!=stage||p!=phase){stage=s;phase=p;Restart();}
                GUILayout.BeginHorizontal();
                if(GUILayout.Button("◀")){pattern=(pattern+13)%14;Restart();}
                GUILayout.Label(patterns[pattern],GUILayout.Width(180));
                if(GUILayout.Button("▶")){pattern=(pattern+1)%14;Restart();}
                GUILayout.EndHorizontal();
                GUILayout.Label("선택 패턴 반복 · 변경 시 전투 초기화");
                invulnerable=GUILayout.Toggle(invulnerable,"플레이어 무적");
                bool fire=GUILayout.Toggle(towers,"자동 포탑 공격");
                if(fire!=towers){towers=fire;if(PlayerHealth.Instance!=null)foreach(var t in PlayerHealth.Instance.GetComponentsInChildren<TowerController>())t.enabled=towers;}
                GUILayout.Label("재생 속도");
                int speedId=GUILayout.Toolbar(speed==.5f?0:speed==1?1:2,new[]{"0.5배","1배","2배"});
                speed=speedId==0?.5f:speedId==1?1:2;
                if(PlayerHealth.Instance!=null&&PlayerHealth.Instance.IsAlive)Time.timeScale=paused?0:speed;
                if(GUILayout.Button(paused?"재생 [Esc]":"일시 정지 [Esc]",GUILayout.Height(32)))paused=!paused;
                if(GUILayout.Button("선택 보스 다시 시작",GUILayout.Height(36)))Restart();
                GUILayout.Label(loading?"불러오는 중…":Boss!=null?"현재: "+Boss.PatternLabel:"보스 처치 완료 · 다시 시작 가능");
                GUILayout.Label("이동/마우스 공격: 인게임과 동일\n좌우 화살표로 패턴 선택\nTab: 패널 숨김 / Esc: 일시 정지");
                if(GUILayout.Button("테스트 종료 → 타이틀",GUILayout.Height(32)))
                {Time.timeScale=1;RunSession.Leave();Destroy(gameObject);SceneManager.LoadScene("Title");}
                GUI.enabled=true;
            }
            GUILayout.EndArea();GUI.skin.font=skinFont;GUI.matrix=old;
        }
        private void OnDestroy(){if(instance==this){instance=null;Time.timeScale=1;}}
    }
}
