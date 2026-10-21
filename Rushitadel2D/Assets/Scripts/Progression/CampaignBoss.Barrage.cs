using System.Collections;
using UnityEngine;

namespace Rushitadel.Progression
{
    public sealed partial class CampaignBoss
    {
        [SerializeField, Min(0f)] private float screenTopPadding = .25f;
        public float ScreenFitScale {get;private set;}=1;
        public static float CalculateScreenFit(Vector2 bodySize,float viewWidth,float viewHeight)
        {
            // At most 15% smaller; never enlarge art or continuously pump scale during attacks.
            return Mathf.Clamp(Mathf.Min(viewWidth*.42f/Mathf.Max(.01f,bodySize.x),
                viewHeight*.36f/Mathf.Max(.01f,bodySize.y)),.85f,1);
        }
        private void FitScreenFootprint()
        {
            var camera=Camera.main;if(camera==null)return;
            var art=GetComponentsInChildren<ThemeBossAnimation>();if(art.Length==0)return;
            var bounds=art[0].GetComponent<SpriteRenderer>().bounds;
            foreach(var part in art)bounds.Encapsulate(part.GetComponent<SpriteRenderer>().bounds);
            ScreenFitScale=CalculateScreenFit(bounds.size,camera.orthographicSize*2*camera.aspect,camera.orthographicSize*2);
            // Root scaling also scales body colliders and emitter offsets, not projectile dimensions or HP.
            transform.localScale=Vector3.one*ScreenFitScale;
            // Animation frames share a common crop, so this includes attack poses without
            // moving the boss each frame. Position before emitter refresh and satellite spawn.
            bounds=art[0].GetComponent<SpriteRenderer>().bounds;
            foreach(var part in art)bounds.Encapsulate(part.GetComponent<SpriteRenderer>().bounds);
            float top=camera.transform.position.y+camera.orthographicSize-screenTopPadding;
            transform.position+=Vector3.up*(top-bounds.max.y);
        }
        private string BarrageOrb=>Kind==BossKind.Twins?"Twins/01-pressure-orb":OrbAsset;
        private Transform BarrageSource(int side)
        {
            if(Kind==BossKind.Ice)return satellites[side]!=null?satellites[side]:sockets[0];
            return sockets[side];
        }
        private IEnumerator BarragePattern(int pattern)
        {
            if(pattern==7)yield return WovenStreams();
            else if(pattern==8)yield return MovingGate();
            else yield return PulseBattery();
        }

        // Two counter-rotating short streams form a readable weave, not a wall of random bullets.
        private IEnumerator WovenStreams()
        {
            PatternLabel=Kind switch{BossKind.Butterfly=>"나비 문양 · 꽃잎 직조",BossKind.Lava=>"용광로 · 가속 나선",
                BossKind.Ice=>"빙환 · 왕복 나선",_=>"쌍성 · 위상차 나선"};
            float charge=Kind==BossKind.Lava?1.4f:.8f;
            for(int side=0;side<2;side++)CuePart(BarrageSource(side),charge,2.8f,.5f);
            yield return new WaitForSeconds(charge);
            int waves=8+Phase*2;
            for(int wave=0;wave<waves;wave++)
            {
                for(int side=0;side<2;side++)
                {
                    var source=BarrageSource(side);if(source==null)continue;
                    float angle=(side==0?1:-1)*(Mathf.Sin(wave*.55f)*38+(Kind==BossKind.Twins?side*12:0));
                    int count=Kind==BossKind.Butterfly?3:2;
                    for(int arm=0;arm<count;arm++)
                    {
                        Vector2 direction=Quaternion.Euler(0,0,angle+(arm-(count-1)*.5f)*22)*Vector2.down;
                        bool returning=Kind==BossKind.Ice&&wave%4==3;
                        FireAt(source.position,direction,returning?"Ice/02-crescent-boomerang":BarrageOrb,
                            returning?35:12,returning?2:2.65f,.12f,0,0,returning?1.65f:0);
                    }
                }
                float interval=Kind==BossKind.Lava?Mathf.Lerp(.34f,.19f,wave/(float)(waves-1)):.27f;
                yield return new WaitForSeconds(interval);
            }
            yield return new WaitForSeconds(.7f);
        }

        // A broad arc with a three-slot opening. Each wave shifts the opening only one slot.
        // Use one central origin so two different gaps cannot accidentally seal one another.
        private IEnumerator MovingGate()
        {
            PatternLabel=Kind switch{BossKind.Butterfly=>"개화 장막 · 이동 틈새",BossKind.Lava=>"용암 탄벽 · 돌파구",
                BossKind.Ice=>"빙결 장막 · 반사 잔향",_=>"수압 장막 · 전격 박자"};
            int[] gaps={5,6,7,6};
            var source=sockets[Kind==BossKind.Butterfly?4:Kind==BossKind.Lava?2:0];
            for(int wave=0;wave<gaps.Length;wave++)
            {
                // Same clip, progressively shorter casting times, still at least .65 seconds of windup.
                float charge=1.1f-wave*.12f;
                CuePart(source,charge,.18f,.4f);yield return new WaitForSeconds(charge);
                Vector2 origin=Kind==BossKind.Twins?(Vector2)transform.position:(Vector2)source.position;
                for(int slot=0;slot<13;slot++)
                {
                    if(Mathf.Abs(slot-gaps[wave])<=1)continue;
                    float angle=(slot-6)*12;
                    // Only outer ice orbs ricochet once; reflected lanes never become an endless cage.
                    int bounce=Kind==BossKind.Ice&&(slot==0||slot==12)?1:0;
                    FireAt(origin,Quaternion.Euler(0,0,angle)*Vector2.down,BarrageOrb,
                        Kind==BossKind.Lava?22:14,2.2f,.13f,bounce);
                }
                yield return new WaitForSeconds(.55f);
            }
        }

        // Three separately telegraphed pulses: long preparation, short burst, shorter preparation.
        private IEnumerator PulseBattery()
        {
            PatternLabel=Kind switch{BossKind.Butterfly=>"호위 나비 · 삼박 레이저",BossKind.Lava=>"손바닥 · 맥동 소각",
                BossKind.Ice=>"빙환 · 교대 점사",_=>"수압·전격 · 엇박 공명"};
            for(int pulse=0;pulse<3;pulse++)
            {
                int side=pulse%2;
                var source=Kind==BossKind.Butterfly&&satellites[pulse]!=null?satellites[pulse]:BarrageSource(side);
                if(source==null)continue;
                string asset=Kind switch{BossKind.Butterfly=>"Butterfly/07-drone-laser",BossKind.Lava=>"Lava/07-palm-laser",
                    BossKind.Ice=>"Ice/06-ring-laser",_=>side==0?"Twins/06-hydro-laser":"Twins/07-lightning-tether"};
                float charge=pulse==0?1.5f:pulse==1?1:.75f;
                float hold=Kind==BossKind.Lava?.5f:.35f;
                // Fixed aim during warning and fire; no surprise sweep into the escape route.
                Laser(source,asset,Aim(source.position),.22f,hold,0,
                    Kind==BossKind.Twins?(side==0?Color.cyan:Color.yellow):Accent,charge);
                yield return new WaitForSeconds(charge+BossLaser.PilotSeconds+hold+BossLaser.FadeSeconds);
                // Bullets begin only once the pulse has faded, leaving movement between beats.
                var opposite=BarrageSource(1-side);
                if(opposite!=null)Fan(opposite.position,BarrageOrb,5,(pulse-1)*12,23,14,2.2f);
                yield return new WaitForSeconds(.45f);
            }
        }
    }
}
