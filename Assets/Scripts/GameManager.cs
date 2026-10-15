using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TMP_Text timer;
    [SerializeField] private Fighters fighter1;
    [SerializeField] private Fighters fighter2;
    [SerializeField] private WheelController wheelController;

    [Header("Match")]
    [SerializeField] private int totalRounds = 5;
    [SerializeField] private float roundDuration = 60f;
    [SerializeField] private float timeBetweenRounds = 3f;

    private HealthAndDamage fighter1Health;
    private HealthAndDamage fighter2Health;

    private int currentRound = 0;
    private int fighter1Wins = 0;
    private int fighter2Wins = 0;
    private int ties = 0;

    private float roundTimeLeft;
    private float breakTimeLeft;
    private bool roundRunning = false;
    private bool matchOver = false;

    private void Start()
    {
        fighter1Health = fighter1.GetComponent<HealthAndDamage>();
        fighter2Health = fighter2.GetComponent<HealthAndDamage>();
        StartRound();
    }

    private void Update()
    {
        if (matchOver) return;

        if (roundRunning)
        {
            roundTimeLeft -= Time.deltaTime;
            if (roundTimeLeft < 0f)
            {
                roundTimeLeft = 0f;
            }
            timer.text = Mathf.CeilToInt(roundTimeLeft).ToString();

            bool fighter1Dead = fighter1.State == Fighters.CombatState.Dead;
            bool fighter2Dead = fighter2.State == Fighters.CombatState.Dead;

            if (fighter1Dead || fighter2Dead)
            {
                EndRound(GetWinnerByDeath(fighter1Dead, fighter2Dead));
            }
            else if (roundTimeLeft <= 0f)
            {
                EndRound(GetWinnerByHealth());
            }
        }
        else
        {
            breakTimeLeft -= Time.deltaTime;
            if (breakTimeLeft <= 0f)
            {
                if (fighter1Wins + (totalRounds-currentRound) < fighter2Wins || fighter2Wins + (totalRounds-currentRound) < fighter1Wins)
                {
                    EndMatch();
                    return;
                }

                StartRound();
            }
        }
    }

    private void StartRound()
    {
        currentRound++;

        if (currentRound > 1)
        {
            fighter1.ResetForRound();
            fighter2.ResetForRound();
            fighter1Health.ResetHealth();
            fighter2Health.ResetHealth();
            wheelController.spin = 0f;
        }

        fighter1.SetCanFight(true);
        fighter2.SetCanFight(true);

        roundTimeLeft = roundDuration;
        roundRunning = true;
        Debug.Log("Round " + currentRound + " / " + totalRounds + " started!");
    }

    private Fighters GetWinnerByDeath(bool fighter1Dead, bool fighter2Dead)
    {
        if (fighter1Dead && fighter2Dead) return null;
        return fighter1Dead ? fighter2 : fighter1;
    }

    private Fighters GetWinnerByHealth()
    {
        if (fighter1Health.CurrentHealth > fighter2Health.CurrentHealth) return fighter1;
        if (fighter2Health.CurrentHealth > fighter1Health.CurrentHealth) return fighter2;
        return null;
    }

    private void EndRound(Fighters winner)
    {
        roundRunning = false;
        fighter1.SetCanFight(false);
        fighter2.SetCanFight(false);

        if (winner == fighter1)
        {
            fighter1Wins++;
            Debug.Log("Round " + currentRound + ": " + fighter1.name + " wins!");
        }
        else if (winner == fighter2)
        {
            fighter2Wins++;
            Debug.Log("Round " + currentRound + ": " + fighter2.name + " wins!");
        }
        else
        {
            ties++;
            Debug.Log("Round " + currentRound + ": Tie!");
        }

        Debug.Log("Score: " + fighter1.name + " " + fighter1Wins + " - " + fighter2Wins + " " + fighter2.name + " (ties: " + ties + ")");

        if (currentRound >= totalRounds)
        {
            EndMatch();
        }
        else
        {
            breakTimeLeft = timeBetweenRounds;
        }
    }

    private void EndMatch()
    {
        matchOver = true;

        if (fighter1Wins > fighter2Wins)
        {
            Debug.Log(fighter1.name + " wins the game!");
        }
        else if (fighter2Wins > fighter1Wins)
        {
            Debug.Log(fighter2.name + " wins the game!");
        }
        else
        {
            Debug.Log("The game is a tie!");
        }
    }
}
