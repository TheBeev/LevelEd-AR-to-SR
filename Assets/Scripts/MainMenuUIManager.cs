using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUIManager : MonoBehaviour
{

    [SerializeField] private GameObject participantNumberSelectMenu;
    [SerializeField] private GameObject techniqueSelectMenu;
    [SerializeField] private Text participantNumberDescription;

    // Start is called before the first frame update
    void Start()
    {
        if (ParticipantNumberTracker.instance.bReturningToMenu)
        {
            participantNumberSelectMenu.SetActive(false);
            techniqueSelectMenu.SetActive(true);
            participantNumberDescription.text = "Participant " + ParticipantNumberTracker.instance.participantNumber;
        }
    }

    public void ChangeScene(string newScene)
    {
        SceneManager.LoadScene(newScene);
    }

}
