using UnityEngine;
// Задание 3: движение через Rigidbody2D.velocity вместо transform.Translate
public class PlayerControllerPhysics : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotateSpeed = 180f;
    private Rigidbody2D rb;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        rb.velocity = new Vector2(horizontal, vertical) * moveSpeed;
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
