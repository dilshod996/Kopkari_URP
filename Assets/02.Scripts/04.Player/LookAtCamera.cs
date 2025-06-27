using TMPro;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    void LateUpdate()
    {
        transform.forward = Camera.main.transform.forward;
    }
    private void OnEnable()
    {
        //GetNameAndLogo();
    }
    public void GetNameAndLogo()
    {
        if (nameText == null) return;
        string name = PlayerPrefs.GetString(Constants.Player.UsernameKey);
        if (string.IsNullOrEmpty(name))
        {
            nameText.text = "Player1";
        }
        else
        {
            nameText.text = name;
        }
    }
}
