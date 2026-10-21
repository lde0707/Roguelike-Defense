using System;
using UnityEngine;
namespace WaveLab.Waves
{
    [CreateAssetMenu(menuName="WaveLab/Reusable Wave")]
    public sealed class WaveTemplate : ScriptableObject
    {
        public WaveDefinition wave = new WaveDefinition();
    }
    /// <summary>Copies composition while retaining references to enemy profile assets.</summary>
    public static class WavePlacement
    {
        public static WaveDefinition Copy(WaveDefinition source)
        {
            if(source==null)throw new ArgumentException("원본 웨이브를 선택하세요.");
            var copy=new WaveDefinition {title=source.title,end=source.end,
                durationSeconds=source.durationSeconds,restSeconds=source.restSeconds};
            foreach(var g in source.groups)
                copy.groups.Add(new SpawnGroup {enemy=g.enemy,role=g.role,spawnKey=g.spawnKey,
                    count=g.count,batchSize=g.batchSize,startSeconds=g.startSeconds,intervalSeconds=g.intervalSeconds});
            return copy;
        }
        public static void Place(WavePlan target,WaveDefinition source,int index,bool replace)
        {
            if(target==null)throw new ArgumentException("대상 계획을 선택하세요.");
            if(index<0||index>(replace?target.waves.Count-1:target.waves.Count))
                throw new ArgumentOutOfRangeException(nameof(index));
            // Copy before writing, including when source and destination belong to the same plan.
            var copy=Copy(source);
            if(replace)
            {
                // Keep destination identity/title: this is the user's named round slot.
                copy.id=target.waves[index].id;
                copy.title=target.waves[index].title;
                target.waves[index]=copy;
            }
            else target.waves.Insert(index,copy);
        }
    }
}
