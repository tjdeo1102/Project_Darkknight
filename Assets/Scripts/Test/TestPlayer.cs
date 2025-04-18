using UnityEngine;

public class TestPlayer : MonoBehaviour
{
    public float Speed;
    CharacterController controller;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        var x = Input.GetAxisRaw("Horizontal");
        var z = Input.GetAxisRaw("Vertical");

        controller.Move(new Vector3 (x, 0, z));
    }
}
