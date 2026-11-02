using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 손과 바늘귀를 잇는 실을 그린다. 물리 없이 곡선만 계산해서 가볍다.
    /// 실이 느슨할수록 아래로 처지고, 당기거나 팽팽해지면 곧게 펴진다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class NeedleRope : MonoBehaviour
    {
        [Header("모양")]
        [Tooltip("곡선을 이루는 마디 수 (많을수록 부드러움)")]
        [SerializeField, Range(2, 64)] private int segments = 20;
        [Tooltip("처짐이 바뀌는 빠르기")]
        [SerializeField, Min(0f)] private float sagSharpness = 8f;

        private LineRenderer line;
        private Transform hand;
        private Needle needle;
        private NeedleData data;
        private float currentSag;

        private void Awake() => CacheComponents();

        private void CacheComponents()
        {
            if (line != null) return;
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = segments + 1;
            line.enabled = false;
        }

        public void Bind(Transform handPoint, Needle target, NeedleData needleData)
        {
            CacheComponents();
            hand = handPoint;
            needle = target;
            data = needleData;
        }

        // 물리 이동이 끝난(보간된) 위치에 맞춰 그리기
        private void LateUpdate()
        {
            if (needle == null || hand == null || !needle.IsActive)
            {
                if (line.enabled) line.enabled = false;
                currentSag = 0f;
                return;
            }

            Vector2 a = hand.position;
            Vector2 b = needle.RopeAnchorPosition;
            float distance = Vector2.Distance(a, b);

            // 남는 실이 많을수록 처짐. 당기는 중이면 팽팽하게
            float slack = 1f - Mathf.Clamp01(distance / data.MaxRopeLength);
            float targetSag = needle.IsPulling ? 0f : data.MaxRopeSag * slack * Mathf.Clamp01(distance * 0.5f);
            currentSag = Mathf.Lerp(currentSag, targetSag, 1f - Mathf.Exp(-sagSharpness * Time.deltaTime));

            // 2차 베지어 곡선: 조절점을 2배 내려야 가운데가 sag만큼 처짐
            Vector2 control = (a + b) * 0.5f + Vector2.down * (currentSag * 2f);
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float u = 1f - t;
                line.SetPosition(i, u * u * a + 2f * u * t * control + t * t * b);
            }

            if (!line.enabled) line.enabled = true;
        }
    }
}
