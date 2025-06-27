using MalbersAnimations;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class LobbyAvatarController : MonoBehaviour
{
    private Animator AIAnimator;
    private NavMeshAgent agent;


    public GameObject RoomUI;
    [SerializeField] Transform TargetPosition1; // Birinchi manzil
    [SerializeField] Transform TargetPosition2; // Ikkinchi manzil
    [SerializeField] float moveSpeed = 0.1f; 
    private bool hasStoodUp = false;
    private bool reachedFirst = false;
    private bool reachedSecond = false;
    private bool setFinished = false;

    private string[] idleStates = { "Idle", "Idle2" };

    void Start()
    {
        AIAnimator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        if(!DataManager.Instance.LobbyAnimPlayed())
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = 0.2f;

            StartCoroutine(StartPlaying());
        }
        else
        {
            transform.position = new Vector3(268.97f, 0.39f, 317.56f);
            transform.rotation = Quaternion.Euler(0f, 92.55f, 0f);
            StartCoroutine(PlayRandomIdleLoop());
        }

    }
    IEnumerator PlayRandomIdleLoop()
    {
        while (true)
        {
            int randomIndex = Random.Range(0, idleStates.Length);
            string randomState = idleStates[randomIndex];

            AIAnimator.Play(randomState);
            yield return new WaitForSeconds(Random.Range(3f, 6f)); // 3-6 soniyadan keyin o¡®zgaradi
        }
    }
    IEnumerator StartPlaying()
    {
        yield return new WaitForSeconds(1f);
        // StandUp animatsiyasini ishga tushiramiz
        AIAnimator.SetTrigger("StandUp");
    }
    public void OnStandFinished()
    {
        hasStoodUp = true;
        Debug.Log("StandUp tugadi, yurishga o'tish");
        // Birinchi manzilga yo'l topishni boshlaymiz
        agent.SetDestination(TargetPosition1.position);
    }

    void Update()
    {
        if (hasStoodUp && !reachedSecond)
        {
            // Birinchi yoki ikkinchi manzilga harakatlanish davomida "Walk" animatsiyasi yoqilsin
            AIAnimator.SetBool("Walk", true);

            // Agar hali birinchi manzilga yetilmagan bo'lsa:
            if (!reachedFirst)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    reachedFirst = true;
                    AIAnimator.SetBool("Walk", false);
                    Debug.Log("Birinchi manzilga yetildi");
                    StartCoroutine(SetDestinationAfterDelay(1f));
                }
            }
            else if (reachedFirst && !reachedSecond)
            {
                if (!agent.pathPending && Vector3.Distance(transform.position, TargetPosition2.position) <= agent.stoppingDistance)
                {
                    reachedSecond = true;
                    agent.updateRotation = false;
                    AIAnimator.SetBool("Walk", false);
                    Debug.Log("Ikkinchi manzilga yetildi");
                    AIAnimator.SetTrigger("Reach");
                    agent.isStopped = true;
                    agent.ResetPath();

                    // **Avatarning pozitsiyasini o¡®zgartirish**
                    transform.position = new Vector3(268.97f, 0.39f, 317.56f);

                    // **Avatarning rotation ni sozlash (Euler graduslarda)**
                    transform.rotation = Quaternion.Euler(0f, 92.55f, 0f);
                    DataManager.Instance.animPlayed = true;
                }
            }
        }
        else if (setFinished&& reachedSecond&&reachedFirst)
        {
            AIAnimator.SetBool("SetFinished", true);
        }
    }

    IEnumerator SetDestinationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Ikkinchi manzilga yo'l topishni boshlaymiz
        agent.SetDestination(TargetPosition2.position);
        AIAnimator.SetBool("Walk", true);
    }
    public void SetFinished()
    {
        setFinished =  true;
    }
    public void EnableRoomUI()
    {
        RoomUI.SetActive(true);
    }
}
