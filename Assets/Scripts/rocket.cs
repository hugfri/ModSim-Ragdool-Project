using System.Runtime.CompilerServices;
using UnityEngine;

public class Rocket : MonoBehaviour
{
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private ParticleSystem smokeTrail;

    private BoxCollider boxCollider;
    private MeshRenderer meshRenderer;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
        
    }

    // Update is called once per frame
    void Update()
    {
       transform.Translate(Vector3.forward * Time.deltaTime * speed); 
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.gameObject.name);
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Debug.Log("Explosion instantiated at: " + transform.position);

            ParticleSystem ps = explosion.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(explosion, ps.main.duration);
            }

        }
        speed = 0;
        boxCollider.enabled = false;
        meshRenderer.enabled = false;
        Destroy(gameObject, 0.1f);
        smokeTrail.Stop();

    }
}
