using System;
using System.Collections;
using UnityEngine;

namespace Group3d.Notifications.Utilities
{
    internal static class ObjectMovingUtilities
    {
        /// <summary>
        /// Moves an object along y-axis in bezier curve within set duration.
        /// </summary>
        public static IEnumerator MoveYCoroutine(RectTransform transform, BezierCurve curve, float duration)
        {
            var pos = transform.anchoredPosition;
            yield return BezierCoroutine(t => transform.anchoredPosition = new Vector2(pos.x, t), curve, duration);
        }

        /// <summary>
        /// Animates an object along bezier curve within set duration.
        /// </summary>
        private static IEnumerator BezierCoroutine(Action<float> animatePropertyAction, BezierCurve curve, float duration)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (duration == 0f)
            {
                duration = 0.00001f;
                Debug.Log("Division by zero prevented");
            }

            var passedTime = 0f;

            while (passedTime < duration)
            {
                passedTime += Time.deltaTime;

                var t = Mathf.Clamp01(passedTime / duration);

                // Cubic bezier curve:
                // B(t) = (1-t)^3 * P0 + 3(1-t)^2t * P1 + 3(1-t)t^2 * P2 + t^3 * P3 , 0 < t < 1

                animatePropertyAction(Mathf.Pow(1 - t, 3) * curve.P0 + 3 * Mathf.Pow(1 - t, 2) * t * curve.P1 + 3 * (1 - t) * Mathf.Pow(t, 2) * curve.P2 + Mathf.Pow(t, 3) * curve.P3);

                yield return new WaitForEndOfFrame();
            }

            animatePropertyAction(curve.P3);
        }
    }
}
