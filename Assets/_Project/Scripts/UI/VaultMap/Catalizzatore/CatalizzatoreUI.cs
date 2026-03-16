using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace _Project
{
    public class CatalizzatoreUI : MonoBehaviour
    {
        [SerializeField] private CatalizzatoreCircle _circlePrefab;
        [SerializeField] private CatalizzatoreConfig _config;

        [SerializeField] private GameObject _minigameGroup;
        [SerializeField] private GameObject _tutorialGroup;

        [SerializeField] private Button _continueButton;
        
        [SerializeField] private IncubatorUI _incubatorUI;
        
        private TextMeshProUGUI _scoreLabel;
        private TextMeshProUGUI _resultLabel;
        private Button _closeButton;

        private const string k_scoreId = "ScoreLabel";
        private const string k_resultId = "ResultLabel";
        private const string k_closeButtonId = "CloseButton";
        private const string k_miniGameGroup = "Minigame";

        private readonly List<CatalizzatoreCircle> _circles = new();

        private int _amountOfCircles;
        
        private int _scores;
        private int Scores
        {
            get => _scores;
            set
            {
                _scores = value;         
                _scoreLabel.text = $"Punteggio: {_scores}";
            }
        }
        
        private bool _empowered = false;

        private void Awake()
        {
            var minigame = transform.Find(k_miniGameGroup);
            _scoreLabel  = minigame.Find(k_scoreId).GetComponent<TextMeshProUGUI>();
            _resultLabel = minigame.Find(k_resultId).GetComponent<TextMeshProUGUI>();
            _closeButton = minigame.Find(k_closeButtonId).GetComponent<Button>();
        }
        
        private void Start()
        {
            _closeButton.onClick.AddListener(HandleClose);
            _continueButton.onClick.AddListener(ShowMinigame);
        }

        private void HandleClose()
        {
            if (_empowered)
                _incubatorUI.ShowEvening();
            
            gameObject.SetActive(false);
        }
        
        public void ShowTutorial()
        {
            gameObject.SetActive(true);
            
            _minigameGroup.SetActive(false);
            _tutorialGroup.SetActive(true);
            
            _resultLabel.gameObject.SetActive(false);
            _closeButton.gameObject.SetActive(false);
        }
        
        private void ShowMinigame() 
        {
            gameObject.SetActive(true);
            
            _tutorialGroup.SetActive(false);
            _minigameGroup.SetActive(true);
            
            _resultLabel.gameObject.SetActive(false);
            _closeButton.gameObject.SetActive(false);
            
            Scores = 0;
            _amountOfCircles = 0;
            
            _circles.Clear();
        
            StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            float startTime = Time.time;
            float endTime = startTime + _config.Session;

            while (Time.time < endTime)
            {
                var t = (Time.time - startTime) / _config.Session;
                var interval = Mathf.Lerp(_config.MaxInterval, _config.MinInterval, t);
                var duration = Mathf.Lerp(_config.MaxDuration, _config.MinDuration, t);
                
                SpawnCircle(duration);
                _amountOfCircles += 1;
                
                yield return new WaitForSeconds(interval);
            }

            CalcResult();
        }

        private void CalcResult()
        {
            _empowered = _scores > _amountOfCircles * 0.8f; 
            if (_empowered)
                _resultLabel.text = "Seme potenziato, livello coerente con l'origine.";
            else 
                _resultLabel.text = _scores > _amountOfCircles * 0.5f ?
                    "Seme instabile, livello degradato." : "Seme penalizzato, livello perso.";
            
            _resultLabel.gameObject.SetActive(true);
            _closeButton.gameObject.SetActive(true);
        }

        private void SpawnCircle(float duration)
        {
            var circle = Instantiate(_circlePrefab, transform);
            circle.transform.localPosition = new Vector3(
                Random.Range(-_config.FieldSize.x, _config.FieldSize.x),
                Random.Range(-_config.FieldSize.y, _config.FieldSize.y),
                0
                );
            
            circle.Init(duration);

            circle.OnSuccess += HandleSuccess;
            circle.OnFailed += HandleFail;
            
            _circles.Add(circle);
        }

        private void OnDestroy()
        {
            foreach (var circle in _circles.Where(circle => circle))
                Destroy(circle.gameObject);
            _circles.Clear();
        }

        private void HandleSuccess()
        {
            Scores += 1;
        }

        private void HandleFail()
        {
            
        }
    }
}