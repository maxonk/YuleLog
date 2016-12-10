using UnityEngine;
using System.Collections;

public class GoToURL : MonoBehaviour {

	public void goToURL(string url) {
        Application.OpenURL(url);
    }

    public void rateThisApp() {
#if UNITY_ANDROID
        Application.OpenURL("https://play.google.com/store/apps/details?id=com.IceWaterGames.Viridi");
#elif UNITY_IOS
        Application.OpenURL("https://itunes.apple.com/us/app/viridi/id1107708818");

#elif UNITY_STANDALONE
        Application.OpenURL("http://store.steampowered.com/app/375950/");
#endif
    }
}
