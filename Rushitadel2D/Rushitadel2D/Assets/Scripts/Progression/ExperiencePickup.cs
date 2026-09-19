using UnityEngine;

namespace Rushitadel.Progression
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class ExperiencePickup : MonoBehaviour
    {
        [SerializeField, Min(1)] private int experienceValue = 20;
        [SerializeField, Min(0f)] private float collectSpeed = 14f;
        [SerializeField, Min(0f)] private float collectDistance = 0.5f;

        private PlayerExperience playerExperience;
        private bool isCollecting;

        private void Awake()
        {
            playerExperience = FindFirstObjectByType<PlayerExperience>();
            isCollecting = playerExperience != null;
        }

        private void Update()
        {
            if (!isCollecting || playerExperience == null)
            {
                return;
            }

            Vector3 destination = playerExperience.transform.position;
            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                collectSpeed * Time.deltaTime);

            if ((destination - transform.position).sqrMagnitude > collectDistance * collectDistance)
            {
                return;
            }

            playerExperience.AddExperience(experienceValue);
            Destroy(gameObject);
        }
    }
}
