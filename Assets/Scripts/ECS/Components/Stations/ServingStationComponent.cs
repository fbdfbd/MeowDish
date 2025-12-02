using Unity.Entities;
using Unity.Mathematics;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 서빙 카운터 전용 데이터
    /// </summary>
    public struct ServingStationComponent : IComponentData
    {
        /// <summary>현재 이 카운터에 줄 서 있는 손님 수</summary>
        public int CurrentQueueCount;

        /// <summary>최대 대기 가능 인원 (예: 3명이면 꽉 차서 다른 줄로 감)</summary>
        public int MaxQueueCapacity;

        /// <summary>지금 주문 받고 있는 손님 (맨 앞)</summary>
        public Entity CurrentCustomer;
    }

    /// <summary>
    /// 손님들이 줄 서는 위치 정보
    /// </summary>
    public struct ServingQueuePoint : IComponentData
    {
        public float3 StartLocalPosition; // 첫 번째 손님 위치 (로컬)
        public float QueueInterval;       // 뒷사람 간격 (미터)
    }
}