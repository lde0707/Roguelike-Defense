using Rushitadel.Data;
using UnityEngine;

namespace Rushitadel.Progression
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class ExperiencePickup : MonoBehaviour
    {
        [SerializeField] private PickupDefinition definition;
        public PickupDefinition Definition => definition;


        private int experienceValue => definition.ExperienceValue;

        private float collectSpeed => definition.CollectSpeed;

        private float collectDistance => definition.CollectDistance;

        private PlayerExperience playerExperience;
        private bool isCollecting;

        private void Awake()
        {
            if (!DefinitionGuard.Require(this, definition)) return;
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
