using UnityEngine;
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;      // скорость движения
    public float rotateSpeed = 180f;  // скорость вращения (градусов в секунду)
    void Update()
    {
        // Получаем нажатия клавиш WASD или стрелок
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        // Движение в 2D (по осям X и Y)
        Vector2 movement = new Vector2(horizontal, vertical);
        transform.Translate(movement * moveSpeed * Time.deltaTime);
        // Вращение вокруг оси Z (в 2D это вращение "вперед-назад")
        // Поворачиваем при нажатии Q (влево) и E (вправо) для наглядности
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime);
        }
    }
}
