using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Gun : MonoBehaviour
{
    public enum GunType { Semi, Burst, Auto }
    public GunType gunType;
    public float rpm;

    // components
    public Transform spawnProj;
    public Transform shellEjectionPoint;
    public AudioSource shootSFX;
    public Rigidbody shell;
    private LineRenderer tracer;

    // sys vars
    private float secsBetwixtShots;
    private float nextPossibeShootTime;

    void Start()
    {
        secsBetwixtShots = 60 / rpm;
        if (GetComponent<LineRenderer>())
        {
            tracer = GetComponent<LineRenderer>();
        }
    }

    public void Shoot()
    {
        if (CanShoot())
        {
            Ray ray = new(spawnProj.position, spawnProj.forward);
            float shotDist = 20;

            if (Physics.Raycast(ray, out RaycastHit hit, shotDist)) shotDist = hit.distance;
            
            Debug.DrawRay(ray.origin, ray.direction * shotDist, Color.red, 1f);

            nextPossibeShootTime = Time.time + secsBetwixtShots;

            if (shootSFX != null) shootSFX.Play();
            
            if (tracer) StartCoroutine("RenderTracer", ray.direction * shotDist);

            Rigidbody newShell = Instantiate(shell, shellEjectionPoint.position, shellEjectionPoint.rotation);
            newShell.AddForce(shellEjectionPoint.forward * Random.Range(150f,200f) + spawnProj.forward * Random.Range(-10f,10f));
        }
    }

    public void ShootContinuous()
    {
        switch (gunType)
        {
            case GunType.Auto: Shoot(); break;
            default: break;
        }
    }

    private bool CanShoot() => Time.time >= nextPossibeShootTime;

    IEnumerator RenderTracer(Vector3 hitPoint)
    {
        tracer.enabled = true;
        tracer.SetPosition(0, spawnProj.position);
        tracer.SetPosition(1, spawnProj.position + hitPoint);

        yield return null;
        tracer.enabled = false;
    }
}
