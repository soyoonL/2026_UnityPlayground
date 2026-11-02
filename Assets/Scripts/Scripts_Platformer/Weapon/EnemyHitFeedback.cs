using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 적이 맞았을 때의 연출 (의지의 히어로 스타일).
    /// 흰색 번쩍 + 찌그러졌다 펴짐 + 파편 + 데미지 숫자.
    /// 넉백은 Enemy, 피격 표정은 Animator의 Hurt 상태가 담당한다.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class EnemyHitFeedback : MonoBehaviour
    {
        [Header("흰색 번쩍")]
        [Tooltip("Platformer/SpriteFlash 셰이더로 만든 머티리얼")]
        [SerializeField] private Material flashMaterial;
        [SerializeField, Min(0f)] private float flashDuration = 0.08f;

        [Header("찌그러짐")]
        [Tooltip("스프라이트가 붙은 자식 오브젝트. 비우면 자동으로 찾음 (루트에 있으면 사용 안 함)")]
        [SerializeField] private Transform visual;
        [SerializeField] private Vector2 squashScale = new Vector2(1.25f, 0.8f);
        [SerializeField, Min(0.01f)] private float squashDuration = 0.15f;

        [Header("파편")]
        [SerializeField] private ParticleSystem hitParticles;
        [SerializeField, Min(0)] private int particleCount = 12;

        [Header("데미지 숫자")]
        [SerializeField] private DamagePopup popupPrefab;
        [SerializeField] private Vector2 popupOffset = new Vector2(0f, 0.6f);

        private Enemy enemy;
        private SpriteRenderer spriteRenderer;
        private Material originalMaterial;
        private Vector3 visualBaseScale;
        private bool isFlashing;
        private float flashEndTime;
        private float squashStartTime = float.NegativeInfinity;

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) originalMaterial = spriteRenderer.sharedMaterial;

            // 루트를 찌그러뜨리면 콜라이더까지 변하므로 자식일 때만 사용
            if (visual == null && spriteRenderer != null && spriteRenderer.transform != transform)
                visual = spriteRenderer.transform;
            if (visual != null) visualBaseScale = visual.localScale;

            enemy.Damaged += OnDamaged;
            enabled = false; // 연출이 없을 때는 Update를 돌리지 않음
        }

        private void OnDestroy()
        {
            if (enemy != null) enemy.Damaged -= OnDamaged;
        }

        // 비활성화(사망 후 꺼짐 등)될 때 원래 모습으로 복구
        private void OnDisable() => Restore();

        private void OnDamaged(int damage, Vector2 hitDirection)
        {
            if (flashMaterial != null && spriteRenderer != null)
            {
                spriteRenderer.sharedMaterial = flashMaterial;
                isFlashing = true;
                flashEndTime = Time.time + flashDuration;
            }

            if (visual != null) squashStartTime = Time.time;
            if (hitParticles != null) hitParticles.Emit(particleCount);
            if (popupPrefab != null) DamagePopupPool.Spawn(popupPrefab, (Vector2)transform.position + popupOffset, damage);

            enabled = true;
        }

        private void Update()
        {
            bool busy = false;

            if (isFlashing)
            {
                if (Time.time >= flashEndTime)
                {
                    spriteRenderer.sharedMaterial = originalMaterial;
                    isFlashing = false;
                }
                else busy = true;
            }

            if (visual != null)
            {
                float t = (Time.time - squashStartTime) / squashDuration;
                if (t < 1f)
                {
                    // 맞는 순간 가장 찌그러지고, 빠르게 원래 크기로 돌아옴
                    float k = (1f - t) * (1f - t);
                    Vector2 s = Vector2.LerpUnclamped(Vector2.one, squashScale, k);
                    visual.localScale = new Vector3(visualBaseScale.x * s.x, visualBaseScale.y * s.y, visualBaseScale.z);
                    busy = true;
                }
                else visual.localScale = visualBaseScale;
            }

            if (!busy) enabled = false;
        }

        private void Restore()
        {
            if (isFlashing && spriteRenderer != null) spriteRenderer.sharedMaterial = originalMaterial;
            isFlashing = false;
            if (visual != null) visual.localScale = visualBaseScale;
            squashStartTime = float.NegativeInfinity;
        }
    }
}