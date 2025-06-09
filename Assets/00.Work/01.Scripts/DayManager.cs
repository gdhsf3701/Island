using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace _00.Work._01.Scripts
{
    public class DayManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private UnityEngine.UI.Text dayInfoText;
        [SerializeField] private UnityEngine.UI.Button skipToNightButton;
        
        // 데이 시스템
        private int currentDay = 1;
        private bool isNight = false;
        private float dayTimer = 0f;
        private const float DAY_DURATION = 300f; // 5분 (300초)
        private const float NIGHT_DURATION = 60f; // 1분 (60초)
        
        // 이벤트
        public static event Action OnNewDayStarted;
        public static event Action OnNightStarted;
        
        // 프로퍼티
        public int CurrentDay => currentDay;
        public bool IsNight => isNight;
        public float DayProgress => isNight ? dayTimer / NIGHT_DURATION : dayTimer / DAY_DURATION;
        
        void Start()
        {
            InitializeUI();
            UpdateDayDisplay();
        }
        
        void InitializeUI()
        {
            // 밤으로 넘어가기 버튼 설정
            if (skipToNightButton != null)
            {
                skipToNightButton.onClick.AddListener(SkipToNight);
            }
        }
        
        void Update()
        {
            HandleInput();
            UpdateDayNightCycle();
        }
        
        void UpdateDayNightCycle()
        {
            dayTimer += Time.deltaTime;
            
            if (isNight)
            {
                // 밤: 1분 후 자동으로 낮으로 변경
                if (dayTimer >= NIGHT_DURATION)
                {
                    StartNewDay();
                }
            }
            else
            {
                // 낮: 5분 후 자동으로 밤으로 변경
                if (dayTimer >= DAY_DURATION)
                {
                    StartNight();
                }
            }
            
            UpdateDayDisplay();
        }
        
        void HandleInput()
        {
            // N키로 밤으로 넘어가기 (단축키)
            if (Keyboard.current.nKey.wasPressedThisFrame && !isNight)
            {
                SkipToNight();
            }
        }
        
        public void SkipToNight()
        {
            if (!isNight)
            {
                StartNight();
            }
        }
        
        void StartNight()
        {
            isNight = true;
            dayTimer = 0f;
            Debug.Log($"🌙 {currentDay}일차 밤이 되었습니다.");
            OnNightStarted?.Invoke();
            UpdateDayDisplay();
        }
        
        void StartNewDay()
        {
            isNight = false;
            dayTimer = 0f;
            currentDay++;
            
            Debug.Log($"☀️ {currentDay}일차가 시작되었습니다!");
            OnNewDayStarted?.Invoke();
            UpdateDayDisplay();
        }
        
        void UpdateDayDisplay()
        {
            if (dayInfoText == null) return;
            
            string timeOfDay = isNight ? "밤" : "낮";
            float remainingTime = isNight ? NIGHT_DURATION - dayTimer : DAY_DURATION - dayTimer;
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            
            string displayText = $"🗓️ {currentDay}일차 {timeOfDay}\n";
            displayText += $"⏰ 남은 시간: {minutes:00}:{seconds:00}";
            
            if (!isNight)
            {
                displayText += "\n🌙 밤으로 넘어가려면 버튼 클릭 (N키)";
            }
            
            dayInfoText.text = displayText;
            
            // 버튼 활성화/비활성화
            if (skipToNightButton != null)
            {
                skipToNightButton.interactable = !isNight;
            }
        }
        
        // 테스트용 메서드들
        [ContextMenu("밤으로 넘어가기")]
        public void TestSkipToNight() => SkipToNight();
        
        [ContextMenu("다음 날로")]
        public void TestNextDay() => StartNewDay();
        
        // 시간 정보 가져오기
        public string GetTimeInfo()
        {
            string timeOfDay = isNight ? "밤" : "낮";
            float remainingTime = isNight ? NIGHT_DURATION - dayTimer : DAY_DURATION - dayTimer;
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            
            return $"{currentDay}일차 {timeOfDay} (남은시간: {minutes:00}:{seconds:00})";
        }
    }
}