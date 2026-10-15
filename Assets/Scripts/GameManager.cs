using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TMP_Text timer;
    [SerializeField] private Fighters fighter1;
    [SerializeField] private Fighters fighter2;
    [SerializeField] private WheelController wheelController;

    [Header("UI")]
    [SerializeField] private RoundAnnouncement roundAnnouncement;
    [SerializeField] private RoundIndicator fighter1RoundIndicator;
    [SerializeField] private RoundIndicator fighter2RoundIndicator;
    [SerializeField] private VictoryScreen victoryScreen;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip normalFightMusic;
    [SerializeField] private AudioClip matchPointMusic;
    [SerializeField] private float musicVolume = 0.5f;
    [SerializeField] private float musicFadeOutTime = 1.5f;

    [Header("Round Winner")]
    [SerializeField] private float winnerBannerDelay = 0.6f;

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
    private bool waitingForAnnouncement = false;
    private bool matchOver = false;
    private bool victoryShown = false;
    private bool musicFadingOut = false;

    private void Start()
    {
        fighter1Health = fighter1.GetComponent<HealthAndDamage>();
        fighter2Health = fighter2.GetComponent<HealthAndDamage>();
        fighter1RoundIndicator.SetWins(0);
        fighter2RoundIndicator.SetWins(0);
        StartRound();
    }

    private void Update()
    {
        UpdateMusicFade();

        if (matchOver)
        {
            // The victory screen follows once the last round's winner banner has gone.
            if (!victoryShown && !roundAnnouncement.IsShowing)
            {
                victoryShown = true;
                ShowVictory();
            }
            return;
        }

        if (waitingForAnnouncement)
        {
            if (!roundAnnouncement.IsShowing)
            {
                waitingForAnnouncement = false;
                roundRunning = true;
                fighter1.SetCanFight(true);
                fighter2.SetCanFight(true);
            }
            return;
        }

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
                // Only during the regular rounds: in tiebreakers (currentRound > totalRounds) the rounds left would be negative.
                if (currentRound < totalRounds && (fighter1Wins + (totalRounds-currentRound) < fighter2Wins || fighter2Wins + (totalRounds-currentRound) < fighter1Wins))
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

        fighter1.SetCanFight(false);
        fighter2.SetCanFight(false);

        roundTimeLeft = roundDuration;
        timer.text = Mathf.CeilToInt(roundTimeLeft).ToString();
        if (IsTiebreaker())
        {
            roundAnnouncement.ShowMessage("Tiebreaker");
        }
        else
        {
            roundAnnouncement.Show(currentRound, currentRound == totalRounds);
        }
        PlayRoundMusic();
        waitingForAnnouncement = true;
    }

    private void PlayRoundMusic()
    {
        // Final round and every tiebreaker get the match point music.
        musicSource.clip = currentRound >= totalRounds ? matchPointMusic : normalFightMusic;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicFadingOut = false;
        musicSource.Play();
    }

    private void UpdateMusicFade()
    {
        if (!musicFadingOut) return;

        musicSource.volume = Mathf.MoveTowards(musicSource.volume, 0f, musicVolume / musicFadeOutTime * Time.unscaledDeltaTime);
        if (musicSource.volume <= 0f)
        {
            musicSource.Stop();
            musicFadingOut = false;
        }
    }

    // Rounds after the regular ones, played while the wins are level.
    private bool IsTiebreaker()
    {
        return currentRound > totalRounds;
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
        musicFadingOut = true;

        if (winner == fighter1)
        {
            fighter1Wins++;
        }
        else if (winner == fighter2)
        {
            fighter2Wins++;
        }
        else
        {
            ties++;
        }

        fighter1RoundIndicator.SetWins(fighter1Wins);
        fighter2RoundIndicator.SetWins(fighter2Wins);
        roundAnnouncement.ShowMessage(winner != null ? winner.name + " Wins!" : "Draw!", winnerBannerDelay);

        // After the last regular round (or a tiebreaker) the match only ends when someone is ahead;
        // otherwise another tiebreaker round is played.
        if (currentRound >= totalRounds && fighter1Wins != fighter2Wins)
        {
            EndMatch();
        }
        else
        {
            breakTimeLeft = timeBetweenRounds;
        }
    }

    private void ShowVictory()
    {
        if (fighter1Wins == fighter2Wins) return;

        Fighters winner = fighter1Wins > fighter2Wins ? fighter1 : fighter2;
        victoryScreen.Show(winner.name, roundAnnouncement.GetWinnerIcon(winner.name));
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
