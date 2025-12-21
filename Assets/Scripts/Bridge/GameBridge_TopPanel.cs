using System;
using UnityEngine;

namespace Meow.Bridge
{
    public partial class GameBridge : MonoBehaviour
    {
        // ========================================================================
        // 재화
        // ========================================================================
        public int CurrentGold { get; private set; }

        public event Action<int> OnGoldChanged;

        public void UpdateGold(int newGold)
        {
            if (CurrentGold == newGold) return;

            CurrentGold = newGold;
            OnGoldChanged?.Invoke(CurrentGold);
        }

        // ========================================================================
        // 하트
        // ========================================================================
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }

        public event Action<int, int> OnLifeChanged;

        public void UpdateLife(int newHp, int newMaxHp)
        {
            if (CurrentHp == newHp && MaxHp == newMaxHp) return;

            CurrentHp = newHp;
            MaxHp = newMaxHp;
            OnLifeChanged?.Invoke(CurrentHp, MaxHp);
        }

        // ========================================================================
        // 날짜
        // ========================================================================
        public int CurrentDay { get; private set; }
        public event Action<int> OnDayChanged;

        public void UpdateDay(int newDay)
        {
            if (CurrentDay == newDay) return;

            CurrentDay = newDay;
            OnDayChanged?.Invoke(CurrentDay);
        }

        // ========================================================================
        // TopDynamic 패널용
        // ========================================================================
        public int CurrentCustomerCount { get; private set; }
        public int MaxCustomerCount { get; private set; }
        public int TodayTotalCustomer { get; private set; }

        // 현재 손님 수, 최대 손님 수, 오늘 온 전체 손님
        public event Action<int, int, int> OnCustomerDataChanged;

        public void UpdateCustomerData(int current, int max, int total)
        {
            if (CurrentCustomerCount == current &&
                MaxCustomerCount == max &&
                TodayTotalCustomer == total) return;

            CurrentCustomerCount = current;
            MaxCustomerCount = max;
            TodayTotalCustomer = total;

            OnCustomerDataChanged?.Invoke(CurrentCustomerCount, MaxCustomerCount, TodayTotalCustomer);
        }
    }
}