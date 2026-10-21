using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WaveLab.Waves;
using Rushitadel.Combat;
using Rushitadel.Spawning;

namespace Rushitadel.Progression
{
    public sealed class CampaignWaveSettings : ScriptableObject
    {
        public WavePlan plan;
        public static bool Variant(EnemyProfile profile, out int variant)
        {
            variant=-1;
            if(profile==null || profile.gameKey==null)return false;
            var parts=profile.gameKey.Split(':');
            if(parts.Length==4 && parts[0]=="rushitadel" && parts[1]=="theme")
                return int.TryParse(parts[2],out int theme) && theme>=0 && theme<5 &&
                    int.TryParse(parts[3],out variant) && variant>=0 && variant<7;
            return parts.Length==2 && parts[0]=="rushitadel" &&
                int.TryParse(parts[1],out variant) && variant>=0 && variant<7;
        }
        public static List<string> Errors(WavePlan plan)
        {
            var errors=WaveValidation.Errors(plan);
            if(errors.Count>0)return errors;
            if(plan.waves.Count!=9)errors.Add("본편은 3스테이지 × 3라운드, 총 9웨이브가 필요합니다.");
            if(plan.maximumAlive<1||plan.maximumAlive>8||plan.maximumElites<1||plan.maximumElites>2)
                errors.Add("본편 동시 적은 1~8, 동시 엘리트는 1~2로 지정하세요.");
            foreach(var wave in plan.waves)
            {
                if(wave.end!=WaveEnd.AllDefeated&&wave.end!=WaveEnd.DurationAndCleared)
                    errors.Add("본편은 잔여 적 정리 완료 조건을 사용합니다.");
                if(wave.restSeconds!=0)errors.Add("본편 휴식은 정비 화면이 담당합니다. 계획 휴식은 0초로 지정하세요.");
                foreach(var group in wave.groups)
                {
                    if(!Variant(group.enemy,out int variant))errors.Add("본편에는 ‘우리 게임 유닛’의 프로필만 연결하세요.");
                    else if((variant>=5)!=(group.role==EnemyRole.Elite))errors.Add("본편 엘리트 프로필의 역할은 엘리트로 유지하세요.");
                }
            }
            return errors;
        }
    }

    // CampaignDirector remains the only owner of round/boss/maintenance transitions.
    public sealed class CampaignWaveSchedule
    {
        private readonly WaveDefinition wave;
        private readonly int maximumAlive,maximumElites;
        private readonly int[] emitted,variants;
        private readonly double[] next;
        private double elapsed;
        private int spawned;
        public int Total {get;}
        public bool TargetTimeReached=>wave.end!=WaveEnd.DurationAndCleared||elapsed>=wave.durationSeconds;
        public CampaignWaveSchedule(WavePlan plan,int index)
        {
            wave=plan.waves[index];maximumAlive=plan.maximumAlive;maximumElites=plan.maximumElites;
            Total=wave.groups.Sum(g=>g.count);
            emitted=new int[wave.groups.Count];variants=new int[wave.groups.Count];next=new double[wave.groups.Count];
            for(int j=0;j<next.Length;j++)
            {
                next[j]=wave.groups[j].startSeconds+1.5;
                CampaignWaveSettings.Variant(wave.groups[j].enemy,out variants[j]);
            }
        }
        public int Tick(EnemySpawner spawner,int stage,float deltaTime)
        {
            if(deltaTime<=0)return spawned;
            elapsed+=deltaTime;
            for(int j=0;j<emitted.Length;j++)
            {
                var group=wave.groups[j];
                if(elapsed<next[j])continue;
                int batch=0;
                while(emitted[j]<group.count&&batch<group.batchSize&&EnemyRegistry.ActiveEnemies.Count()<maximumAlive)
                {
                    if(variants[j]>=5&&Object.FindObjectsByType<EnemyArchetype>(FindObjectsSortMode.None).Count(e=>e.Elite)>=maximumElites)break;
                    var enemy=spawner.SpawnCampaignEnemy(stage,false,variants[j],group.enemy.prefab);
                    if(enemy==null)break;
                    emitted[j]++;spawned++;batch++;
                }
                if(batch>0)next[j]=elapsed+group.intervalSeconds;
            }
            return spawned;
        }
    }
}
