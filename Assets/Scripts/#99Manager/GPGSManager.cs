using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

public class GPGSManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI logText;


    private void Start()
    {
        
    }
    public  void GPGS_Login()
    {
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
    }
    internal void ProcessAuthentication(SignInStatus status)
    {
        if(status == SignInStatus.Success)
        {
            string displayName = PlayGamesPlatform.Instance.GetUserDisplayName();
            string userID = PlayGamesPlatform.Instance.GetUserId();

            Debug.Log($"�α��� ���� : {displayName} / {userID}");
        }
        else
        {
            Debug.Log($"�α��� ���� : {SignInStatus.Canceled}");
        }

    }


}
