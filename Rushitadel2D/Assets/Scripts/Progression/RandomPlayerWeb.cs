using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rushitadel.Progression
{
    [Serializable] public sealed class PlayerWebNode
    {
        public int upgrade, parent=-1, depth, cost;
        public float x,y;
        public bool owned;
    }
    [Serializable] public sealed class PlayerWebGraph
    {
        public int version=1, seed;
        public PlayerWebNode[] nodes;
    }
    public static class RandomPlayerWeb
    {
        public static readonly string[] Titles={
            "내구 보강","경량 추진","주포 강화","신속 장전","사거리 확장","탄속 가속","동력 축전","동력 순환",
            "완충 장갑","자가 수복","정밀 조준","치명 증폭","요격 탄두","피격 안정화","확산 압축","동력 효율",
            "집속 코어","장갑 패널","조준 코어","추진 날개","축전 코어","출력 코어","외장 링","잔상 추진",
            "관통 탄두","삼방 분산","폭발 탄두","독성 탄두","연쇄 요격","파편 회수","정화 맥동","재충전 보호막",
            "강화 골격","회복 증폭","대인 탄두","중장갑 파쇄","급속 발진","반동 제어","정비 보급","기력 회수"};
        public static readonly string[] Effects={
            "최대 HP +15","이동 속도 +6%","플레이어 기본탄 피해 +10%","플레이어 기본 사격 빈도 +8%","플레이어 기본탄 사거리 +8%","플레이어 기본탄 속도 +10%","최대 동력 +10","동력 회복 속도 +12%",
            "일반 피격 피해 -4% (후방 지형 피해 제외)","전투 중 초당 HP 0.3 회복","기본탄 치명타 확률 +5%p (기본 5%)","기본탄 치명타 배율 +0.15 (기본 1.5배)","기본탄의 적 투사체 피해 +12%","일반 피격 무적 시간 +0.04초","우클릭 범위 반지름 +8%","우클릭 동력 비용 -4%p (기본 70%)",
            "집속 장식 표시 · 기본탄 피해 +12%","장갑 패널 표시 · 최대 HP +20","조준 장식 표시 · 기본탄 치명타 확률 +5%","추진 날개 표시 · 이동 속도 +8%","축전 장식 표시 · 최대 동력 +15","출력 장식 표시 · 기본 사격 빈도 +10%","외장 링 표시 · 일반 피격 피해 -5%","회전 잔상 강화 · 이동 속도 +6%",
            "기본탄 추가 관통 +1","기본탄 3방향 · 각 탄 피해 55% (합계 상한 165%)","기본탄 폭발 반경 0.8 · 주변 최대 8대상에 피해 35%","기본탄 명중 적에게 3초 독 / 초당 8 피해","기본탄 명중 시 주변 적탄 최대 2개에 피해 25% 연쇄","기본탄으로 적탄 파괴 시 HP 0.5 회복 / 0.5초 쿨다운","8초마다 반경 1.6의 적탄 최대 12개에 30 피해","12초마다 일반 피격을 최대 20 흡수하는 보호막 재충전",
            "강화 후 최대 HP +8% (단계끼리 합산)","받는 HP 회복량 +15%","기본탄의 적 직접 명중 피해 +12% (적탄 제외)","기본탄으로 최대 HP 80 이상 적탄을 직접 맞히면 피해 +15%","우클릭 범위탄 비행 속도 +10%","발사 반동 거리 -15% (최대 -60%)","안전 정비의 HP 회복 선택 시 기본 회복량 +5","기본탄으로 적탄 파괴 시 동력 +1 / 0.5초 쿨다운"};
        public static int Category(int id)=>id<16||id>=32?0:id<24?1:2;
        public static int MaxRank(int id)=>Category(id)==0?4:1;
        public static string CategoryName(int id)=>new[]{"기본 능력","외형 변화","특수 효과"}[Category(id)];
        public static int Rank(CampaignState state,int id)=>state?.playerWeb?.nodes?.Count(n=>n.upgrade==id && n.owned)??0;
        public static int Rank(int id)=>Rank(CampaignDirector.Instance?.State,id);
        public static bool Has(int id)=>Rank(id)>0;
        public static float Stat(int id,float step)=>1+Rank(id)*step;
        public static float MaximumHealth(CampaignState state)=>(100+(state!=null && state.web!=null && state.web[5]?25:0)+15*Rank(state,0)+20*Rank(state,17))*(1+.08f*Rank(state,32));
        public static float MaximumPower(CampaignState state)=>100+10*Rank(state,6)+15*Rank(state,20);
        public static float MoveMultiplier=>1+.06f*Rank(1)+.08f*Rank(19)+.06f*Rank(23);
        public static float DamageMultiplier=>1+.1f*Rank(2)+.12f*Rank(16);
        public static float CadenceMultiplier=>1+.08f*Rank(3)+.1f*Rank(21);
        public static float CritChance=>.05f*(1+Rank(10)+Rank(18));
        public static void Ensure(CampaignState state,int seed)
        {
            if(state.playerWeb==null)state.playerWeb=Generate(seed);
        }
        public static PlayerWebGraph Generate(int seed)
        {
            var random=new System.Random(unchecked(seed^0x5f3759df));
            var nodes=new List<PlayerWebNode>();
            var layer=new List<int>{-1};
            var counts=new int[Titles.Length];
            for(int depth=1;depth<=5;depth++)
            {
                var next=new List<int>();
                for(int j=0;j<layer.Count;j++)
                {
                    int children=depth<=2?2:random.NextDouble()<(depth==3?.65:.35)?2:1;
                    children=Math.Min(children,12-next.Count-(layer.Count-j-1));
                    for(int k=0;k<children;k++)
                    {
                        double roll=random.NextDouble();
                        int category=depth==1?0:roll<.60?0:roll<.85?1:2;
                        var pool=Enumerable.Range(0,Titles.Length).Where(i=>Category(i)==category&&counts[i]<MaxRank(i)).ToArray();
                        if(category==0 && random.NextDouble()<.25)
                        {
                            var repeated=pool.Where(i=>counts[i]>0).ToArray();
                            if(repeated.Length>0)pool=repeated;
                        }
                        if(pool.Length==0)pool=Enumerable.Range(0,Titles.Length).Where(i=>counts[i]<MaxRank(i)).ToArray();
                        int id=pool[random.Next(pool.Length)];counts[id]++;
                        next.Add(nodes.Count);
                        nodes.Add(new PlayerWebNode{upgrade=id,parent=layer[j],depth=depth,cost=20+depth*8+Category(id)*25,y=920-depth*140+random.Next(-14,15)});
                    }
                }
                layer=next;
            }
            // Ordered leaves yield non-crossing binary branches; one-child paths remain visible.
            for(int i=0;i<layer.Count;i++)nodes[layer[i]].x=110+(i+.5f)*1240/layer.Count;
            for(int d=4;d>=1;d--)for(int i=0;i<nodes.Count;i++)if(nodes[i].depth==d)
                nodes[i].x=nodes.Where(n=>n.parent==i).Average(n=>n.x);
            // Guarantee access to both rarer categories without making every roll independent.
            for(int cat=1;cat<=2;cat++)if(!nodes.Any(n=>Category(n.upgrade)==cat))
            {var n=nodes[layer[cat-1]];n.upgrade=cat==1?16:24;n.cost=20+n.depth*8+cat*25;}
            return new PlayerWebGraph{seed=seed,nodes=nodes.ToArray()};
        }
        public static bool Valid(PlayerWebGraph graph)
        {
            if(graph==null)return true; // Legacy save: generated once on load, then persisted.
            if(graph.version!=1||graph.nodes==null||graph.nodes.Length<10||graph.nodes.Length>48)return false;
            var counts=new int[Titles.Length];
            for(int i=0;i<graph.nodes.Length;i++)
            {
                var n=graph.nodes[i];
                if(n==null||n.upgrade<0||n.upgrade>=Titles.Length||n.parent>=i||n.parent< -1||n.depth<1||n.depth>5||n.cost!=20+n.depth*8+Category(n.upgrade)*25||!float.IsFinite(n.x)||!float.IsFinite(n.y)||n.x<60||n.x>1390||n.y<150||n.y>830)return false;
                if(++counts[n.upgrade]>MaxRank(n.upgrade))return false;
                if(n.parent<0?n.depth!=1:n.depth!=graph.nodes[n.parent].depth+1)return false;
                if(n.owned&&n.parent>=0&&!graph.nodes[n.parent].owned)return false;
            }
            return graph.nodes.Count(n=>n.parent<0)==2 && Enumerable.Range(0,graph.nodes.Length).All(i=>graph.nodes.Count(n=>n.parent==i)<=2);
        }
    }
}
