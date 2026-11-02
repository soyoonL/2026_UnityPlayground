using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 조준 중 바늘이 날아갈 궤적과 끝의 화살촉을 보여준다.
    /// 바늘과 같은 중력·감속으로 미리 계산하고, 땅이나 실 길이 끝에서 멈춘다.
    /// 차징할수록 색이 빨갛게 변하고, 최대가 되면 깜빡인다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ThrowTrajectory : MonoBehaviour
    {
        [Header("구성")]
        [Tooltip("궤적 끝에 표시할 화살촉 (오른쪽을 향한 스프라이트)")]
        [SerializeField] private SpriteRenderer arrowHead;

        [Header("계산")]
        [Tooltip("예측할 최대 물리 스텝 수 (90 = 약 1.8초)")]
        [SerializeField, Range(10, 200)] private int maxSteps = 90;

        [Header("화살촉")]
        [Tooltip("화살촉 스프라이트가 원래 향하는 방향 보정 (위를 향한 삼각형이면 -90)")]
        [SerializeField] private float arrowAngleOffset = -90f;
        [Tooltip("착지 지점에서 뒤로 물러나는 거리 (땅에 묻히지 않게). 보통 화살촉 길이의 절반")]
        [SerializeField, Min(0f)] private float arrowBackOffset = 0.15f;

        [Header("색상")]
        [SerializeField] private Color minChargeColor = Color.white;
        [SerializeField] private Color maxChargeColor = new Color(1f, 0.15f, 0.1f);
        [Tooltip("손 쪽 시작 부분의 투명도 (끝으로 갈수록 진해짐)")]
        [SerializeField, Range(0f, 1f)] private float startAlpha = 0.25f;
        [Tooltip("최대 차징일 때 깜빡이는 속도")]
        [SerializeField, Min(0f)] private float fullChargeBlinkSpeed = 8f;

        private LineRenderer line;
        private Vector3[] points;

        private void Awake() => CacheComponents();

        private void CacheComponents()
        {
            if (line != null) return;
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            points = new Vector3[maxSteps + 1];
        }

        public void Show(Vector2 origin, Vector2 velocity, Vector2 gravity, float damping,
                         float maxDistance, LayerMask groundMask, float charge01)
        {
            CacheComponents();

            // Rigidbody2D와 같은 방식으로 적분 (속도 갱신 → 감속 → 위치 이동)
            float dt = Time.fixedDeltaTime;
            float dampFactor = 1f / (1f + dt * damping);
            float maxDistSqr = maxDistance * maxDistance;

            Vector2 pos = origin;
            Vector2 vel = velocity;
            int count = 0;
            points[count++] = pos;

            for (int i = 0; i < maxSteps; i++)
            {
                vel = (vel + gravity * dt) * dampFactor;
                Vector2 next = pos + vel * dt;

                // 땅에 닿으면 거기서 끝
                RaycastHit2D hit = Physics2D.Linecast(pos, next, groundMask);
                if (hit.collider != null)
                {
                    pos = hit.point;
                    points[count++] = pos;
                    break;
                }

                // 실 길이를 넘으면 거기서 끝
                if ((next - origin).sqrMagnitude > maxDistSqr)
                {
                    pos = origin + (next - origin).normalized * maxDistance;
                    points[count++] = pos;
                    break;
                }

                pos = next;
                points[count++] = pos;
            }

            line.positionCount = count;
            for (int i = 0; i < count; i++) line.SetPosition(i, points[i]);

            // 차징 비율에 따라 흰색 → 빨간색, 최대면 깜빡임
            Color color = Color.Lerp(minChargeColor, maxChargeColor, charge01);
            if (charge01 >= 1f)
                color = Color.Lerp(color, Color.white, Mathf.PingPong(Time.time * fullChargeBlinkSpeed, 1f) * 0.6f);

            Color start = color;
            start.a *= startAlpha;
            line.startColor = start;
            line.endColor = color;
            line.enabled = true;

            if (arrowHead != null)
            {
                Vector2 dir = vel.sqrMagnitude > 0.0001f ? vel.normalized : Vector2.right;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + arrowAngleOffset;

                // 진행 반대 방향으로 조금 물려서 끝부분이 착지 지점에 닿게
                arrowHead.transform.SetPositionAndRotation(pos - dir * arrowBackOffset, Quaternion.Euler(0f, 0f, angle));
                arrowHead.color = color;
                arrowHead.enabled = true;
            }
        }

        public void Hide()
        {
            CacheComponents();
            line.enabled = false;
            if (arrowHead != null) arrowHead.enabled = false;
        }
    }
}
