using UnityEngine;
using _Project.Sporae.Core;

namespace _Project.Systems.FoodRoom
{
    [CreateAssetMenu(menuName = "Sporae/FoodRoomConfig")]
    public class FoodRoomConfig : ScriptableObject
    {
        [Header("Slots")]
        [Tooltip("Numero massimo di slot produzione (default: 1, max: 3)")]
        [SerializeField] [Range(1, 3)] private int _maxSlots = 1;

        [Header("Vegetable (FOOD-101)")]
        [SerializeField] private int _vegetableDays = 1;
        [SerializeField] private int _vegetableOutputQuantity = 3;
        [SerializeField] private int _vegetableCryPerDay = 1;
        [SerializeField] private int _vegetableActionBonus = 1;

        [Header("Fungus (FOOD-201)")]
        [SerializeField] private int _fungusDays = 2;
        [SerializeField] private int _fungusOutputQuantity = 2;
        [SerializeField] private int _fungusCryPerDay = 1;
        [SerializeField] private int _fungusActionBonus = 2;

        [Header("Meat (FOOD-301)")]
        [SerializeField] private int _meatDays = 3;
        [SerializeField] private int _meatOutputQuantity = 1;
        [SerializeField] private int _meatCryPerDay = 2;
        [SerializeField] private int _meatActionBonus = 3;

        public int MaxSlots => _maxSlots;

        public int GetDaysFor(FoodProductionType type)
        {
            switch (type)
            {
                case FoodProductionType.Vegetable: return _vegetableDays;
                case FoodProductionType.Fungus: return _fungusDays;
                case FoodProductionType.Meat: return _meatDays;
                default: return 0;
            }
        }

        public int GetOutputQuantityFor(FoodProductionType type)
        {
            switch (type)
            {
                case FoodProductionType.Vegetable: return _vegetableOutputQuantity;
                case FoodProductionType.Fungus: return _fungusOutputQuantity;
                case FoodProductionType.Meat: return _meatOutputQuantity;
                default: return 0;
            }
        }

        public int GetCryPerDayFor(FoodProductionType type)
        {
            switch (type)
            {
                case FoodProductionType.Vegetable: return _vegetableCryPerDay;
                case FoodProductionType.Fungus: return _fungusCryPerDay;
                case FoodProductionType.Meat: return _meatCryPerDay;
                default: return 0;
            }
        }

        public int GetActionBonusFor(FoodProductionType type)
        {
            switch (type)
            {
                case FoodProductionType.Vegetable: return _vegetableActionBonus;
                case FoodProductionType.Fungus: return _fungusActionBonus;
                case FoodProductionType.Meat: return _meatActionBonus;
                default: return 0;
            }
        }

        public string GetOutputTypeIdFor(FoodProductionType type)
        {
            switch (type)
            {
                case FoodProductionType.Vegetable: return Items.FoodVegetable;
                case FoodProductionType.Fungus: return Items.FoodFungus;
                case FoodProductionType.Meat: return Items.FoodMeat;
                default: return null;
            }
        }
    }
}
