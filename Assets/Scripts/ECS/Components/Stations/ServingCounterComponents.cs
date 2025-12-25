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

        /// <summary>최대 대기 가능 인원(꽉차면 다른줄. 다른줄도 차면 안섬)</summary>
        public int MaxQueueCapacity;

        /// <summary>맨앞(주문중) 손님</summary>
        public Entity CurrentCustomer;
    }

    /// <summary>
    /// 손님 줄 서는 위치 정보
    /// </summary>
    public struct ServingQueuePoint : IComponentData
    {
        public float3 StartLocalPosition;
        public float QueueInterval; 
    }
}