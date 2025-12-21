using UnityEngine;

namespace Meow.Data
{
    [CreateAssetMenu(fileName = "NewPlayerStats", menuName = "Meow/PlayerStats")]
    public class PlayerStatsSO : ScriptableObject
    {
        [Header("기본 이동 스탯")]
        [Tooltip("기본 이동 속도")]
        public float BaseMoveSpeed = 1.0f;

        [Tooltip("기본 회전 속도")]
        [Range(5f, 20f)]
        public float RotationSpeed = 10f;

        [Header("기본 작업 스탯")]
        [Tooltip("기본 작업 속도 배율")]
        public float BaseActionSpeed = 1.0f;


    }
}