using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // (per avere un singleton accessibile da altri script)

    [Header("Stato Generale")]
    public int currentDay = 1;           // (per contare i giorni di gioco)
    public int maxActionsPerDay = 3;     // (numero slot azioni giornaliere)
    public int remainingActions;         // (slot ancora disponibili nel giorno attuale)

    [Header("Valuta / Energia")]
    public int cry = 50;                 // (energia/crediti iniziali)

    [Header("Inventario")]
    public List<string> seeds = new List<string>();  // (lista semplice per semi)
    public List<string> spores = new List<string>(); // (lista per spore)
    public List<string> fruits = new List<string>(); // (lista per frutti/piante raccolte)

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        StartNewDay(); // (inizializza il primo giorno)
    }

    public void StartNewDay()
    {
        remainingActions = maxActionsPerDay; // (resetta gli slot azione giornalieri)
        Debug.Log("Giorno " + currentDay + " iniziato. Azioni disponibili: " + remainingActions);
    }

    public bool SpendAction(int cost = 1)
    {
        if (remainingActions >= cost && cry >= cost)
        {
            remainingActions -= cost;
            cry -= cost; // (azioni e CRY sono legati: ogni azione consuma crediti)
            return true;
        }
        return false;
    }

    public void EndDay()
    {
        cry -= 20; // (costo corrente giornaliera come da GDD16)
        currentDay++;
        StartNewDay();
    }
}
