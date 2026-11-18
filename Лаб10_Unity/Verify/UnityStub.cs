using System;
using System.Collections.Generic;
// Минимальная заглушка Unity API. Transform.Translate работает в локальных осях, как в Unity (Space.Self)
namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 operator *(Vector2 a, float k) { return new Vector2(a.x * k, a.y * k); }
        public static float Distance(Vector2 a, Vector2 b) { return (float)Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y)); }
        public static Vector2 MoveTowards(Vector2 c, Vector2 t, float d)
        {
            float dx = t.x - c.x, dy = t.y - c.y;
            float m = (float)Math.Sqrt(dx * dx + dy * dy);
            if (m <= d || m == 0f) return t;
            return new Vector2(c.x + dx / m * d, c.y + dy / m * d);
        }
        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
        public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x, v.y, 0); }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }
    public class Transform
    {
        public Vector3 position;
        public float angleZ;
        public void Translate(Vector3 v)
        {
            double a = angleZ * Math.PI / 180.0;
            position.x += (float)(v.x * Math.Cos(a) - v.y * Math.Sin(a));
            position.y += (float)(v.x * Math.Sin(a) + v.y * Math.Cos(a));
        }
        public void Rotate(float x, float y, float z) { angleZ += z; }
    }
    public class Rigidbody2D { public Vector2 velocity; }
    public enum KeyCode { Q, E, LeftShift }
    public static class Time { public static float deltaTime = 0.02f; }
    public static class Input
    {
        public static Dictionary<string, float> Axes = new Dictionary<string, float>();
        public static HashSet<KeyCode> Keys = new HashSet<KeyCode>();
        public static float GetAxis(string n) { float v; return Axes.TryGetValue(n, out v) ? v : 0f; }
        public static bool GetKey(KeyCode k) { return Keys.Contains(k); }
    }
    public class MonoBehaviour
    {
        public Transform transform = new Transform();
        Rigidbody2D rb = new Rigidbody2D();
        public T GetComponent<T>() where T : class { return rb as T; }
    }
}
