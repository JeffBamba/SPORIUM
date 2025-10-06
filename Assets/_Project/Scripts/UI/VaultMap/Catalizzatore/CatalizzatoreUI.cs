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

        private TextMeshProUGUI _scoreLabel;
        private TextMeshProUGUI _resultLabel;
        private Button _closeButton;

        private const string k_scoreId = "ScoreLabel";
        private const string k_resultId = "ResultLabel";
        private const string k_closeButtonId = "CloseButton";

        private readonly List<CatalizzatoreCircle> _circles = new();

        private int _amountOfCircles;
        
        private int _scores;
        private int Scores
        {
            get => _scores;
            set
            {
                _scores = value;         
                _scoreLabel.text = $"Score: {_scores}";
            }
        }

        private void Awake()
        {
            _scoreLabel = transform.Find(k_scoreId).GetComponent<TextMeshProUGUI>();
            _resultLabel = transform.Find(k_resultId).GetComponent<TextMeshProUGUI>();
            _closeButton = transform.Find(k_closeButtonId).GetComponent<Button>();
        }
        
        private void Start()
        {
            _closeButton.onClick.AddListener(HandleClose);
        }

        private void HandleClose()
        {
            gameObject.SetActive(false);
        }

        public void Run() 
        {
            gameObject.SetActive(true);
            
            _resultLabel.gameObject.SetActive(false);
            _closeButton.gameObject.SetActive(false);
            
            Scores = 0;
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
            if (_scores > _amountOfCircles * 0.95f) 
                _resultLabel.text = "Empowered seed, Level consistent with origin.";
            
            _resultLabel.text = _scores > _amountOfCircles * 0.5f ?
                "Unstable seed, Level degraded." : "Penalized seed, Level lost.";
            
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