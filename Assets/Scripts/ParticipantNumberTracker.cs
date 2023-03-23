using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ParticipantNumberTracker : MonoBehaviour
{
    [SerializeField] private Text participantNumberDescription;

    public static ParticipantNumberTracker instance;

    [HideInInspector] public string participantNumber;

    [HideInInspector] public bool bReturningToMenu;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if(instance != this)
        {
            Destroy(gameObject);
        }

    }

    public void ConfirmParticipantNumber(string pNumber)
    {
        participantNumber = pNumber;

        if (participantNumberDescription != null)
        {
            participantNumberDescription.text = "Participant " + participantNumber;
        }
    }

}
