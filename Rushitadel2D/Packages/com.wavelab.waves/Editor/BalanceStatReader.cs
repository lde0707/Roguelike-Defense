using UnityEditor;
using UnityEngine;
using WaveLab.Core;
namespace WaveLab.Waves.Editor
{
    public static class BalanceStatReader
    {
        public static bool Read(GameObject prefab, EnemyProfile profile, string id, out float value)
        {
            value=0;
            if(profile!=null)foreach(var binding in profile.statBindings)
            {
                if(binding==null||binding.statId!=id)continue;
                if(binding.source==null||string.IsNullOrEmpty(binding.propertyPath))return false;
                var so=new SerializedObject(binding.source);so.Update();var property=so.FindProperty(binding.propertyPath);
                if(property==null)return false;
                if(property.propertyType==SerializedPropertyType.Float)value=(float)property.doubleValue;
                else if(property.propertyType==SerializedPropertyType.Integer)value=property.longValue;
                else return false;
                return WaveValidation.Finite(value);
            }
            if(prefab!=null)foreach(var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                if(component is IStatProvider provider && provider.TryGetStat(id,out value))return WaveValidation.Finite(value);
            return false;
        }
        public static bool Interval(GameObject prefab, EnemyProfile profile, out float value)
        {
            if(Read(prefab,profile,"attack.interval",out value))return value>0;
            if(Read(prefab,profile,"attack.speed",out float rate)&&rate>0){value=1/rate;return true;}
            value=0;return false;
        }
    }
}
