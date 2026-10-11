using UnityEngine;

namespace Rushitadel.Presentation
{
    // Editor-only snapshots built by the same code as the live frontend.
    // Runtime always uses GameFrontend; no duplicate input or event handlers.
    public sealed class TitleScenePreview:MonoBehaviour
    {
        public GameObject[] pages;
        [TextArea(5,12)] public string guide="Inspector에서 화면을 선택하세요. 실제 클릭/스크롤/저장/전환은 Play에서 검사합니다. 이 화면은 런타임 생성 코드의 스냅샷입니다. UI 변경 후 Rushitadel > Refresh Title Scene Preview로 갱신하세요.";
        private void Awake(){if(Application.isPlaying)gameObject.SetActive(false);}
    }
}
