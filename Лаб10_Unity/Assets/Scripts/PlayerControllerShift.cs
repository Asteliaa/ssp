using UnityEngine;
// Задание 1: ускорение в 2 раза при зажатой клавише Shift
public class PlayerControllerShift : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotateSpeed = 180f;
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed = moveSpeed * 2;
        }
        Vector2 movement = new Vector2(horizontal, vertical);
        transform.Translate(movement * speed * Time.deltaTime);
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
