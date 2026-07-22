using UnityEngine;

namespace Rushitadel.Progression
{
    [RequireComponent(typeof(Collider))]
    public sealed class ExperiencePickup : MonoBehaviour
    {
        [SerializeField, Min(1)] private int experienceValue = 20;
        [SerializeField, Min(0f)] private float collectSpeed = 700f;
        [SerializeField, Min(0f)] private float collectDistance = 25f;

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
