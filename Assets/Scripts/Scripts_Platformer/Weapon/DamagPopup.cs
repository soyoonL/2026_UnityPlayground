using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace Platformer
{
    /// <summary>
    /// 튀어 올랐다가 사라지는 데미지 숫자. DamagePopupPool로 재사용한다.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class DamagePopup : MonoBehaviour
    {
        [Header("움직임")]
        [SerializeField] private Vector2 startVelocity = new Vector2(0f, 3.5f);
        [Tooltip("좌우로 퍼지는 정도 (여러 개가 겹치지 않게)")]
        [SerializeField, Min(0f)] private float randomX = 1.2f;
        [SerializeField] private float gravity = -8f;
        [SerializeField, Min(0.1f)] private float lifeTime = 0.7f;

        [Header("크기")]
        [Tooltip("처음에 커졌다가 원래 크기로 줄어듦")]
        [SerializeField, Min(0f)] private float popScale = 1.5f;
        [SerializeField, Min(0.01f)] private float popDuration = 0.12f;

        [Header("표시")]
        [Tooltip("{0} 자리에 데미지가 들어감")]
        [SerializeField] private string format = "-{0}";

        private TextMeshPro text;
        private IObjectPool<DamagePopup> pool;
        private Vector3 baseScale;
        private Color baseColor;
        private Vector2 velocity;
        private float spawnTime;
        private bool isActive;

        private void Awake()
        {
            text = GetComponent<TextMeshPro>();
            baseScale = transform.localScale;
            baseColor = text.color;
        }

        internal void SetPool(IObjectPool<DamagePopup> owner) => pool = owner;

        public void Show(Vector2 position, int amount)
        {
            transform.position = position;
            transform.localScale = baseScale * popScale;
            velocity = startVelocity + new Vector2(Random.Range(-randomX, randomX), 0f);

            text.SetText(format, amount); // 문자열을 새로 만들지 않는 방식 (GC 없음)
            text.color = baseColor;

            spawnTime = Time.time;
            isActive = true;
        }

        private void Update()
        {
            if (!isActive) return;

            float age = Time.time - spawnTime;
            if (age >= lifeTime)
            {
                Despawn();
                return;
            }

            velocity.y += gravity * Time.deltaTime;
            transform.position += (Vector3)(velocity * Time.deltaTime);
            transform.localScale = baseScale * Mathf.Lerp(popScale, 1f, age / popDuration);

            // 수명의 마지막 40% 동안 서서히 사라짐
            float fadeStart = lifeTime * 0.6f;
            if (age > fadeStart)
            {
                Color c = baseColor;
                c.a *= 1f - (age - fadeStart) / (lifeTime - fadeStart);
                text.color = c;
            }
        }

        private void Despawn()
        {
            if (!isActive) return;
            isActive = false;

            if (pool != null) pool.Release(this);
            else Destroy(gameObject);
        }
    }
}
