using System.Collections;
using UnityEngine;

public class Shell : MonoBehaviour
{
    public Renderer renderer;
    public Rigidbody rigidBody;
    private float lifeTime = 5f;
    private Material mat;
    private Color ogCol;
    private float fadePercent;
    private float deathTime;
    private bool fading;

    void Start()
    {
        renderer = GetComponent<Renderer>();
        rigidBody = GetComponent<Rigidbody>();
        mat = renderer.material;
        ogCol = mat.color;
        deathTime = Time.time + lifeTime;
        StartCoroutine("Fade");
    }

    IEnumerator Fade()
    {
        while(true)
        {
            yield return new WaitForSeconds(.2f);

            if (fading)
            {
                fadePercent += Time.deltaTime;
                mat.color = Color.Lerp(ogCol, Color.clear, fadePercent);

                if (fadePercent >= 1f)
                {
                    Destroy(gameObject);
                }
            }
            else if (Time.time > deathTime)
            {
                fading = true;
            }
        }
    }

    void OnTriggerEnter(Collider c)
    {
        if (c.tag is "Ground")
        {
            rigidBody.Sleep();
        }
    }
}
