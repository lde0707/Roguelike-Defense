using UnityEngine;
using Rushitadel.Player;

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
        private float age, delay;
        private bool consumed;
        public float AttractionDelay => delay;
        public void SetValue(int value) => experienceValue = Mathf.Max(1, value);

        private void Awake()
        {
            playerExperience = FindFirstObjectByType<PlayerExperience>();
            isCollecting = playerExperience != null;
            delay = Random.Range(1f, 2f);
            GetComponent<CircleCollider2D>().isTrigger = true;
            var art = GetComponent<SpriteRenderer>();
            if (art == null) art = gameObject.AddComponent<SpriteRenderer>();
            art.sprite = PixelRunArt.Crystal; art.color = Color.white; art.sortingOrder = 15;
            transform.localScale = Vector3.one * 0.38f;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f || PlayerHealth.Instance == null || !PlayerHealth.Instance.IsAlive || !isCollecting || playerExperience == null)
            {
                return;
            }

            age += Time.deltaTime;
            if (consumed || age < delay) return;

            Vector3 destination = playerExperience.transform.position;
            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                Mathf.Max(collectSpeed, Vector3.Distance(transform.position, destination) * 5f) * Time.deltaTime);

            if ((destination - transform.position).sqrMagnitude > collectDistance * collectDistance)
            {
                return;
            }

            consumed = true;
            playerExperience.AddExperience(experienceValue);
            Destroy(gameObject);
        }
    }
}
