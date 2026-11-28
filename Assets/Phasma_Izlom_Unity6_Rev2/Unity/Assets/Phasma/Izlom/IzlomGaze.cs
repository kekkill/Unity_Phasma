using UnityEngine;
using UnityEngine.AI;

namespace Phasma.Izlom
{
    [DisallowMultipleComponent]
    public sealed class IzlomGaze : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private Transform target;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField, Min(0f)] private float turnDelay = 0.25f;
        [SerializeField, Range(0f, 45f)] private float maxYaw = 24f;
        [SerializeField, Min(0.01f)] private float response = 4.5f;

        private float stoppedFor;
        private float yaw;
        private bool applied;
        private Quaternion animatedRotation;
        private Quaternion appliedRotation;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void Awake()
        {
            if (agent == null)
                agent = GetComponentInParent<NavMeshAgent>();
        }

        private void LateUpdate()
        {
            if (head == null || head.parent == null)
                return;

            var moving = agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh
                && agent.velocity.sqrMagnitude > 0.0025f;
            stoppedFor = moving ? 0f : stoppedFor + Time.deltaTime;

            var desiredYaw = 0f;
            var up = transform.up;
            if (target != null && stoppedFor >= turnDelay)
            {
                var direction = Vector3.ProjectOnPlane(target.position - head.position, up);
                if (direction.sqrMagnitude > 0.0001f)
                {
                    var angle = Vector3.SignedAngle(transform.forward, direction, up);
                    desiredYaw = Mathf.Clamp(angle, -maxYaw, maxYaw);
                }
            }

            var local = head.localRotation;
            if (!applied || Quaternion.Angle(local, appliedRotation) > 0.01f)
                animatedRotation = local;

            yaw = Mathf.Lerp(yaw, desiredYaw, 1f - Mathf.Exp(-response * Time.deltaTime));
            var worldAnimation = head.parent.rotation * animatedRotation;
            head.rotation = Quaternion.AngleAxis(yaw, up) * worldAnimation;
            appliedRotation = head.localRotation;
            applied = true;
        }

        private void OnDisable()
        {
            if (head != null && applied && Quaternion.Angle(head.localRotation, appliedRotation) < 0.01f)
                head.localRotation = animatedRotation;

            applied = false;
            stoppedFor = 0f;
            yaw = 0f;
        }
    }
}
