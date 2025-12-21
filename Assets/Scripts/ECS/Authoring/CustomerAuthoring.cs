using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Authoring
{
    public class CustomerAuthoring : MonoBehaviour
    {
        [Header("¼Õ´Ô ¼³Á¤")]
        public float walkSpeed = 2.0f;
        public float maxPatience = 60.0f;

    }
}