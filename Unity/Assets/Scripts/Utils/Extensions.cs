using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SocialArcade.Unity.Utils
{
    public static class Extensions
    {
        public static void SetAlpha(this CanvasGroup canvasGroup, float alpha)
        {
            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }
        
        public static void SetInteractable(this CanvasGroup canvasGroup, bool interactable)
        {
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }
        
        public static void FadeIn(this CanvasGroup canvasGroup, float duration = 0.3f, Action onComplete = null)
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1, duration).OnComplete(() => onComplete?.Invoke());
        }
        
        public static void FadeOut(this CanvasGroup canvasGroup, float duration = 0.3f, Action onComplete = null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0, duration).OnComplete(() => onComplete?.Invoke());
        }
        
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
        
        public static Vector3 WithX(this Vector3 vector, float x)
        {
            return new Vector3(x, vector.y, vector.z);
        }
        
        public static Vector3 WithY(this Vector3 vector, float y)
        {
            return new Vector3(vector.x, y, vector.z);
        }
        
        public static Vector3 WithZ(this Vector3 vector, float z)
        {
            return new Vector3(vector.x, vector.y, z);
        }
        
        public static Vector2 WithX(this Vector2 vector, float x)
        {
            return new Vector2(x, vector.y);
        }
        
        public static Vector2 WithY(this Vector2 vector, float y)
        {
            return new Vector2(vector.x, y);
        }
        
        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
        
        public static float InverseLerp(float a, float b, float value)
        {
            if (Mathf.Approximately(a, b))
                return 0f;
            return Mathf.Clamp01((value - a) / (b - a));
        }
        
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Mathf.Clamp01(t);
        }
        
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 velocity, float smoothTime, float maxSpeed = Mathf.Infinity)
        {
            float smoothTime_ = Mathf.Max(0.0001f, smoothTime);
            float maxSpeed_ = Mathf.Max(0f, maxSpeed);
            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            
            float num = 2f / smoothTime_;
            float num2 = num * num;
            float num3 = num2 * deltaTime;
            float num4 = 1f / (1f + num3 + 0.48f * num3 * num3 + 0.235f * num3 * num3 * num3);
            
            Vector3 vector = current - target;
            Vector3 vector2 = vector + maxSpeed_ * Mathf.Min(deltaTime * maxSpeed_, smoothTime_) * Vector3.one;
            Vector3 vector3 = (current + target) * 0.5f;
            
            Vector3 a = velocity + num * vector3;
            Vector3 a2 = a * num;
            Vector3 a3 = a2 * deltaTime;
            Vector3 a4 = a3 * num;
            
            Vector3 vector4 = vector3 + a4;
            Vector3 vector5 = vector2 - vector4;
            Vector3 vector6 = a + a2 + a3 + a4;
            
            Vector3 result = vector5 + (vector4 + vector5) * num4;
            
            velocity = (velocity - num * vector6) * num4;
            
            return result;
        }
        
        public static string FormatTime(float seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            
            if (t.TotalHours >= 1)
                return $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
            else
                return $"{t.Minutes:D2}:{t.Seconds:D2}";
        }
        
        public static string FormatNumber(int number)
        {
            if (number >= 1000000000)
                return (number / 1000000000f).ToString("F1") + "B";
            if (number >= 1000000)
                return (number / 1000000f).ToString("F1") + "M";
            if (number >= 1000)
                return (number / 1000f).ToString("F1") + "K";
            
            return number.ToString();
        }
        
        public static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays}d {duration.Hours}h";
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            if (duration.TotalMinutes >= 1)
                return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
            
            return $"{duration.Seconds}s";
        }
        
        public static T[] Shuffle<T>(this T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                int k = UnityEngine.Random.Range(0, n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
            return array;
        }
        
        public static List<T> Shuffle<T>(this List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                int k = UnityEngine.Random.Range(0, n--);
                T temp = list[n];
                list[n] = list[k];
                list[k] = temp;
            }
            return list;
        }
        
        public static T GetRandomElement<T>(this T[] array)
        {
            if (array.Length == 0)
                return default;
            return array[UnityEngine.Random.Range(0, array.Length)];
        }
        
        public static T GetRandomElement<T>(this List<T> list)
        {
            if (list.Count == 0)
                return default;
            return list[UnityEngine.Random.Range(0, list.Count)];
        }
        
        public static bool Contains(this LayerMask mask, int layer)
        {
            return (mask & (1 << layer)) != 0;
        }
        
        public static void SetLayerRecursively(this GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }
        
        public static void DestroyChildren(this Transform transform)
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
        
        public static void DestroyChildrenImmediate(this Transform transform)
        {
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in transform)
            {
                children.Add(child.gameObject);
            }
            
            foreach (var child in children)
            {
                DestroyImmediate(child);
            }
        }
        
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                return true;
            return !enumerable.Any();
        }
        
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action?.Invoke(item);
            }
        }
        
        public static Vector3 GetDirection(Vector3 from, Vector3 to)
        {
            return (to - from).normalized;
        }
        
        public static float GetAngle(Vector3 from, Vector3 to)
        {
            return Vector3.Angle(from, to);
        }
        
        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            Vector3 direction = point - pivot;
            direction = Quaternion.Euler(angles) * direction;
            return direction + pivot;
        }
        
        public static bool Approximately(this float value, float other, float epsilon = 0.0001f)
        {
            return Mathf.Abs(value - other) < epsilon;
        }
        
        public static int Sign(this float value)
        {
            return value > 0 ? 1 : (value < 0 ? -1 : 0);
        }
    }
    
    public static class CoroutineExtensions
    {
        public static Coroutine StartCoroutine(this MonoBehaviour behaviour, IEnumerator routine)
        {
            return behaviour.StartCoroutine(routine);
        }
        
        public static void StopAllCoroutines(this MonoBehaviour behaviour)
        {
            behaviour.StopAllCoroutines();
        }
    }
}
