using UnityEngine;
// Задание 2: объект движется к игроку с постоянной скоростью
public class EnemyFollow : MonoBehaviour
{
    public Transform player;
    public float speed = 2f;
    void Update()
    {
        transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
    }
}
