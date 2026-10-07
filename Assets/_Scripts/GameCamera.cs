using UnityEngine;

public class GameCamera : MonoBehaviour
{
    private Transform tgt;
    private Vector3 camTgt;
    
    void Start()
    {
        tgt = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        camTgt = new()
        {
            x = tgt.position.x,
            y = transform.position.y,
            z = tgt.position.z,
        };
        transform.position = Vector3.Lerp(transform.position, camTgt, Time.deltaTime * 8);
    }
}
