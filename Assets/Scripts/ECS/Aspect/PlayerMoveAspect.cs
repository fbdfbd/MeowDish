using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Meow.ECS.Components;

namespace Meow.ECS.Aspects
{
    public readonly partial struct PlayerMoveAspect : IAspect
    {
        public readonly Entity Self;

        readonly RefRW<LocalTransform> _transform;
        readonly RefRW<PhysicsVelocity> _velocity;
        readonly RefRO<PlayerInputComponent> _input;
        readonly RefRO<PlayerStatsComponent> _stats; // 가변 스탯
        readonly RefRO<UnitConfig> _config;          // 불변 스탯(Blob)
        readonly RefRW<PlayerStateComponent> _state;

        public float FinalMoveSpeed
        {
            get
            {
                ref var baseStats = ref _config.ValueRO.BlobRef.Value;

                float baseSpeed = baseStats.BaseMoveSpeed;
                float bonus = _stats.ValueRO.MoveSpeedBonus;
                float mul = _stats.ValueRO.MoveSpeedMultiplier;
                float allMul = _stats.ValueRO.AllSpeedMultiplier;

                return (baseSpeed + bonus) * mul * allMul;
            }
        }

        // 회전 속도
        public float RotationSpeed => _config.ValueRO.BlobRef.Value.RotationSpeed;

        // 입력값 가져오기
        public float2 MoveInput => _input.ValueRO.MoveInput;

        // 회전 처리
        public void Rotate(float deltaTime)
        {
            if (math.lengthsq(MoveInput) > 0.001f)
            {
                float3 moveDir = new float3(MoveInput.x, 0, MoveInput.y);
                moveDir = math.normalize(moveDir);

                quaternion targetRot = quaternion.LookRotation(moveDir, math.up());
                _transform.ValueRW.Rotation = math.slerp(
                    _transform.ValueRO.Rotation,
                    targetRot,
                    RotationSpeed * deltaTime
                );

                // 마지막 이동 방향 저장
                _state.ValueRW.LastMoveDirection = moveDir;
            }
        }

        // 물리 속도 설정
        public void SetVelocity(float3 newVelocity)
        {
            _velocity.ValueRW.Linear = newVelocity;
        }

        public void Stop()
        {
            _velocity.ValueRW.Linear = float3.zero;
            _velocity.ValueRW.Angular = float3.zero;
        }

        // 위치 정보
        public float3 Position => _transform.ValueRO.Position;
        public quaternion Rotation => _transform.ValueRO.Rotation;
    }
}