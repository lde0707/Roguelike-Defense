using UnityEngine;
using Rushitadel.Combat;
namespace Rushitadel.Presentation
{
    public static class BattleSideSpace
    {
        public static float LeftPixels => Space(false);
        public static float RightPixels => Space(true);
        private static float Space(bool right)
        {
            var camera=Camera.main;var bounds=PlayfieldBoundary.Instance;
            if(camera==null||bounds==null)return Screen.width*.16f;
            float x=camera.WorldToScreenPoint(new Vector3(right?bounds.RightEdge:bounds.LeftEdge,camera.transform.position.y,0)).x;
            return Mathf.Clamp(right?camera.pixelWidth-x:x,0,camera.pixelWidth*.5f);
        }
    }
}
