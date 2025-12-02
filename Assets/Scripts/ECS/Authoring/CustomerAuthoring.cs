using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Authoring
{
    public class CustomerAuthoring : MonoBehaviour
    {
        [Header("손님 설정")]
        public float walkSpeed = 2.0f;
        public float maxPatience = 60.0f; // 60초 기다림

        // 손님 시각화 (머리 위 주문 말풍선 등은 나중에 VisualSystem에서 처리)

        private void Start()
        {
            // 베이킹 시스템을 안 쓰므로 수동 변환은 보통 Spawner가 Prefab을 Entity로 변환해서 씁니다.
            // 하지만 하이브리드 방식에서 프리팹 자체를 엔티티로 만들려면
            // Spawner가 이 Authoring이 붙은 프리팹을 Convert 해야 합니다.

            // *중요: 이 스크립트는 프리팹에 붙어있을 것이므로 Start()가 실행되지 않을 수 있습니다.
            // (프리팹은 씬에 없으니까요)
            // 따라서 여기서는 Baker를 쓰는 게 정석이지만, 
            // 주인님의 하이브리드 스타일(수동 생성)에 맞추려면 Spawner가 이 정보를 읽어가야 합니다.
        }

        // 하이브리드 방식에서는 프리팹에 붙은 MonoBehaviour 데이터를 
        // SpawnerSystem이 읽어서 엔티티 생성 시 넣어주는 게 편합니다.
        // 따라서 여기는 데이터 저장소 역할만 합니다.
    }
}