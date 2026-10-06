using UnityEngine;
namespace Rushitadel.Combat
{
    public static class TurretForms
    {
        public static readonly string[] Names = {"기본 포탑","쌍열 연사포","부채 산탄포","부착 드릴포","관통 원반포","폭발 구체포","냉각 분사포","부식 분무포","복제 표식포","귀환 부메랑포","잔류 톱날포","충격 파동포","십자 분열포","감속 그물포","과충전 표식포","봉합 포말포","연쇄 도체포"};
        public static readonly string[] Effects = {"정면 사격","짧은 쌍열에서 교대 연사","60도 부채꼴 7발 · 적탄당 최대 2발","적탄에 부착 · 0.1초마다 25 피해 ×6","최대 4적탄 관통 · 각 22 피해","반경 0.65 폭발 · 최대 8적탄","45도 냉기 · 40% 둔화 1.2초","3방울 분무 · 받는 요격 피해 +30%","통과한 아군탄 50% 복제 · 지원 1슬롯","고정 발사 원점으로 귀환 · 왕복 타격","고정 톱날 · 0.8초 잔류","55도 파동 · 적탄 밀치기","명중 시 좌우 파편 각 1발","그물 설치 · 30% 둔화","다음 아군 발사 피해 +25% · 지원 1슬롯","HP 45 포말벽 · 접촉 적탄 요격","직격 후 가까운 적탄 최대 2개 연쇄"};
        public static readonly float[] Intervals = {.5f,.12f,.6f,.9f,.55f,.85f,.1f,.3f,.6f,.8f,.65f,.75f,.65f,.9f,.7f,.9f,.7f};
        public static bool Support(int id) => id == 8 || id == 14;
        public static bool Copyable(int id) => id >= 1 && id <= 5 || id == 7 || id == 9 || id == 12 || id == 16;
        public static readonly string[] ElementNames={"기본","불꽃","얼음","전기","독","물"};
        public static Color ElementColor(int id) => id switch {1=>new Color(1,.28f,.055f),2=>new Color(.25f,.85f,1),3=>new Color(1,.83f,.15f),4=>new Color(.4f,1,.2f),5=>new Color(.15f,.55f,1),_=>new Color(.65f,.25f,1)};
    }
}
