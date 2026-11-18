using System;
using System.Reflection;
using UnityEngine;

class P
{
    static int fail = 0;
    static void Check(string name, bool ok, string info)
    {
        Console.WriteLine((ok ? "PASS" : "FAIL") + " | " + name + " | " + info);
        if (!ok) fail++;
    }
    static void Call(object o, string m)
    {
        o.GetType().GetMethod(m, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(o, null);
    }
    static void Reset() { Input.Axes.Clear(); Input.Keys.Clear(); }
    static string F(float v) { return v.ToString("F2"); }

    static void Main()
    {
        // 1 секунда = 50 кадров по 0.02 с
        Reset(); Input.Axes["Horizontal"] = 1;
        var pc = new PlayerController();
        for (int i = 0; i < 50; i++) Call(pc, "Update");
        Check("PlayerController: движение вправо (D), 1 с",
            Math.Abs(pc.transform.position.x - 5f) < 0.01f && Math.Abs(pc.transform.position.y) < 0.01f,
            "x=" + F(pc.transform.position.x) + " y=" + F(pc.transform.position.y));

        Reset(); Input.Axes["Vertical"] = 1;
        pc = new PlayerController();
        for (int i = 0; i < 50; i++) Call(pc, "Update");
        Check("PlayerController: движение вверх (W), 1 с",
            Math.Abs(pc.transform.position.y - 5f) < 0.01f, "y=" + F(pc.transform.position.y));

        Reset(); Input.Keys.Add(KeyCode.Q);
        pc = new PlayerController();
        for (int i = 0; i < 50; i++) Call(pc, "Update");
        Check("PlayerController: вращение Q, 1 с",
            Math.Abs(pc.transform.angleZ - 180f) < 0.01f, "угол=" + F(pc.transform.angleZ));

        Reset(); Input.Keys.Add(KeyCode.E);
        pc = new PlayerController();
        for (int i = 0; i < 50; i++) Call(pc, "Update");
        Check("PlayerController: вращение E, 1 с",
            Math.Abs(pc.transform.angleZ + 180f) < 0.01f, "угол=" + F(pc.transform.angleZ));

        Reset(); Input.Axes["Horizontal"] = 1;
        var ps = new PlayerControllerShift();
        for (int i = 0; i < 50; i++) Call(ps, "Update");
        Check("Задание 1: без Shift, 1 с",
            Math.Abs(ps.transform.position.x - 5f) < 0.01f, "x=" + F(ps.transform.position.x));
        Input.Keys.Add(KeyCode.LeftShift);
        ps = new PlayerControllerShift();
        for (int i = 0; i < 50; i++) Call(ps, "Update");
        Check("Задание 1: с Shift, 1 с (скорость x2)",
            Math.Abs(ps.transform.position.x - 10f) < 0.01f, "x=" + F(ps.transform.position.x));

        Reset();
        var en = new EnemyFollow();
        var pl = new Transform();
        pl.position = new Vector3(6, 8, 0);
        en.player = pl; en.speed = 2f;
        for (int i = 0; i < 50; i++) Call(en, "Update");
        float d1 = Vector2.Distance(en.transform.position, pl.position);
        Check("Задание 2: за 1 с объект прошел 2 ед. к игроку",
            Math.Abs(d1 - 8f) < 0.01f, "расстояние=" + F(d1) + " (было 10.00)");
        for (int i = 0; i < 500; i++) Call(en, "Update");
        float d2 = Vector2.Distance(en.transform.position, pl.position);
        Check("Задание 2: объект достигает игрока без перелета", d2 < 0.001f, "расстояние=" + d2.ToString("F3"));

        Reset(); Input.Axes["Horizontal"] = 1; Input.Axes["Vertical"] = -1;
        var pp = new PlayerControllerPhysics();
        Call(pp, "Start"); Call(pp, "Update");
        var rb = pp.GetComponent<Rigidbody2D>();
        Check("Задание 3: rb.velocity = (5; -5)",
            rb.velocity.x == 5f && rb.velocity.y == -5f, "velocity=(" + rb.velocity.x + "; " + rb.velocity.y + ")");
        Reset(); Call(pp, "Update");
        Check("Задание 3: остановка при отпускании клавиш",
            rb.velocity.x == 0f && rb.velocity.y == 0f, "velocity=(" + rb.velocity.x + "; " + rb.velocity.y + ")");

        Console.WriteLine(fail == 0 ? "ВСЕ ПРОВЕРКИ ПРОЙДЕНЫ" : "ОШИБОК: " + fail);
    }
}
